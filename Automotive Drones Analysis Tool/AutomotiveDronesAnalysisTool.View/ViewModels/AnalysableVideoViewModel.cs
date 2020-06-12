using Alturos.Yolo;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    public class AnalysableVideoViewModel : ViewModelBase
    {
        /// <summary>
        /// Indicates when the frame has been changed
        /// </summary>
        /// <param name="viewModel"></param>
        public delegate void IndexChangedEvent(int index);
        public event IndexChangedEvent IndexChanged;

        private string _projectName;
        private string _videoName;
        private BitmapSource _currentFrameBitmap;
        private string _videoPath;
        private int _totalFrames;
        private int _currentFrameIndex;
        private const int SCALE = 10;
        private const int WIDTH = 175;
        private const int HEIGHT = 102;
        private Dictionary<int, string> _indexToImageTempPath;
        private ConcurrentQueue<Tuple<Bitmap, string>> _imageQueue;
        bool _keepRunning;

        /// <summary>
        /// Name of the project
        /// </summary>
        public string ProjectName
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// Path of the video
        /// </summary>
        public string VideoPath
        {
            get => _videoPath;
            set => SetProperty(ref _videoPath, value);
        }

        /// <summary>
        /// Name of the selected video
        /// </summary>
        public string VideoName
        {
            get => _videoName;
            set => SetProperty(ref _videoName, value);
        }

        /// <summary>
        /// The current frame of the video shown in the UI
        /// </summary>
        public BitmapSource CurrentFrameBitmap
        {
            get => _currentFrameBitmap;
            set => SetProperty(ref _currentFrameBitmap, value);
        }

        /// <summary>
        /// The index of the currently active frame.
        /// </summary>
        public int CurrentFrameIndex
        {
            get => _currentFrameIndex;
            set
            {
                SetProperty(ref _currentFrameIndex, value);
                IndexChanged?.Invoke(value);
            }
        }

        /// <summary>
        /// The total amount of frames of the given video.
        /// </summary>
        public int TotalFrames
        {
            get => _totalFrames;
            set => SetProperty(ref _totalFrames, value);
        }

        public AnalysableVideoViewModel(AnalysableVideoModel analysableVideoModel)
        {
            _indexToImageTempPath = new Dictionary<int, string>();
            _imageQueue = new ConcurrentQueue<Tuple<Bitmap, string>>();

            ProjectName = analysableVideoModel.ProjectName;
            VideoName = analysableVideoModel.VideoName;
            VideoPath = analysableVideoModel.VideoPath;
            IndexChanged += AnalysableVideoViewModel_IndexChanged;
            SetupVideo();
        }

        public void Dispose()
        {
            // Dispose the iamges in the temp folder!
            foreach (var pair in _indexToImageTempPath)
                File.Delete(pair.Value);
        }

        /// <summary>
        /// Fires when the current active index changes. We need to adjust the frame bitmap then.
        /// </summary>
        /// <param name="index"></param>
        private void AnalysableVideoViewModel_IndexChanged(int index)
        {
            if (_indexToImageTempPath.TryGetValue(index, out var imagePath))
            {
                // read a single frame and convert the frame into a byte array
                var imageBytes = File.ReadAllBytes(imagePath);

                var imageOriginal = Mat.FromImageData(imageBytes);
                var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(WIDTH * SCALE, HEIGHT * SCALE));

                // display the detection result
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    CurrentFrameBitmap = BitmapHelper.BitmapToBitmapSource(BitmapConverter.ToBitmap(uiImage));
                });
            }
        }

        /// <summary>
        /// We extract every frame of the video and store it in a dictionary.
        /// Since this would blow up the RAM, the images are written into a temp folder on the disc.
        /// The speed is inhanced by using multiple threads and "powering" through the images in the concurrent queue.
        /// </summary>
        public void SetupVideo()
        {
            _indexToImageTempPath.Clear();
            _keepRunning = true;

            // We start around 15 queue worker, which should do the job in a good amount of time.
            for(int i = 0; i < 15; i++)
            {
                StartQueueWorker();
                i++;
            }
            // Read a video file and get each frame
            using (var videocapture = new VideoCapture(VideoPath))
            {
                for (int i = 0; i < videocapture.FrameCount; i++)
                {
                    using (Mat imageOriginal = new Mat())
                    {
                        // read a single frame and convert the frame into a bitmap
                        videocapture.Read(imageOriginal);
                        var bitmap = BitmapConverter.ToBitmap(imageOriginal);
                        var imagePath = Path.Combine(
                            ServiceContainer.GetService<GlobalEnviromentService>().Cv2TempVideoLocation,
                            $"temp_{i}.png");

                        // Enqueue the path and the bitmap into the queue so the workers can save it.
                        _indexToImageTempPath.Add(i, imagePath);
                        _imageQueue.Enqueue(Tuple.Create(bitmap, imagePath));
                    }
                }
            }
            _keepRunning = false;
        }

        /// <summary>
        /// Starts a queue worker that grabs a bitmap from the imagequeue and saves it into the temop folder.
        /// </summary>
        private void StartQueueWorker()
        {
            Task.Run(() =>
            {
                while(_keepRunning || _imageQueue.Count > 0)
                {
                    if(_imageQueue.TryDequeue(out var tuple))
                    {
                        tuple.Item1.Save(tuple.Item2);
                        tuple.Item1.Dispose();
                    }
                }
            });
        }

        /// <summary>
        /// analyses the active frame.
        /// </summary>
        public void AnalyseActiveFrame()
        {
            // YOLO setting
            int yoloWidth = WIDTH, yoloHeight = HEIGHT;
            var configurationDetector = new ConfigurationDetector();
            var config = configurationDetector.Detect();
            var yoloWrapper = new YoloWrapper(config);

            // OpenCV & WPF setting
            Mat image = new Mat();
            WriteableBitmap wb = new WriteableBitmap(yoloWidth, yoloHeight, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);

            byte[] imageInBytes = new byte[(int)(yoloWidth * yoloHeight * image.Channels())];

            if (_indexToImageTempPath.TryGetValue(CurrentFrameIndex, out var imagePath))
            {
                var imageBytes = File.ReadAllBytes(imagePath);
                var imageOriginal = Mat.FromImageData(imageBytes);

                image = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth, yoloHeight));
                imageInBytes = image.ToBytes();

                // conduct object detection and display the result
                var items = yoloWrapper.Detect(imageInBytes);
                // We use the image to detect the objects in a very small size - then we draw them onto the
                // uiImage and scale it up!
                var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth * SCALE, yoloHeight * SCALE));

                foreach (var item in items)
                {
                    var x = item.X * SCALE;
                    var y = item.Y * SCALE;
                    var width = item.Width * SCALE;
                    var height = item.Height * SCALE;
                    var type = item.Type;  // class name of the object

                    // draw a bounding box for the detected object
                    // you can set different colors for different classes
                    Cv2.Rectangle(uiImage, new OpenCvSharp.Rect(x, y, width, height), Scalar.Green, 3);

                    // Draw a connection line to each other object.
                    foreach (var item2 in items)
                        if (item2 != item)
                        {
                            Cv2.Line(uiImage,
                                new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y * SCALE),
                                new OpenCvSharp.Point(item2.Center().X * SCALE, item2.Center().Y * SCALE),
                                Scalar.Red);
                        }

                    Cv2.PutText(uiImage,
                        "Center",
                        new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y * SCALE),
                        HersheyFonts.HersheyComplex,
                        0.5f,
                        Scalar.Red);
                }

                // display the detection result
                Application.Current?.Dispatcher?.Invoke(() =>
                {
                    CurrentFrameBitmap = BitmapHelper.BitmapToBitmapSource(BitmapConverter.ToBitmap(uiImage));
                });
            }

            //// Read a video file and run object detection over it!
            //using (videocapture = new VideoCapture(VideoPath))
            //{
            //    for (int i = 0; i < videocapture.FrameCount; i++)
            //    {
            //        using (Mat imageOriginal = new Mat())
            //        {
            //            // read a single frame and convert the frame into a byte array
            //            videocapture.Read(imageOriginal);

            //            if (i == CurrentFrameIndex)
            //            {
            //                image = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth, yoloHeight));
            //                imageInBytes = image.ToBytes();

            //                // conduct object detection and display the result
            //                var items = yoloWrapper.Detect(imageInBytes);
            //                // We use the image to detect the objects in a very small size - then we draw them onto the
            //                // uiImage and scale it up!
            //                var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth * SCALE, yoloHeight * SCALE));

            //                foreach (var item in items)
            //                {
            //                    var x = item.X * SCALE;
            //                    var y = item.Y * SCALE;
            //                    var width = item.Width * SCALE;
            //                    var height = item.Height * SCALE;
            //                    var type = item.Type;  // class name of the object

            //                    // draw a bounding box for the detected object
            //                    // you can set different colors for different classes
            //                    Cv2.Rectangle(uiImage, new OpenCvSharp.Rect(x, y, width, height), Scalar.Green, 3);

            //                    // Draw a connection line to each other object.
            //                    foreach (var item2 in items)
            //                        if (item2 != item)
            //                        {
            //                            Cv2.Line(uiImage,
            //                                new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y * SCALE),
            //                                new OpenCvSharp.Point(item2.Center().X * SCALE, item2.Center().Y * SCALE),
            //                                Scalar.Red);
            //                        }

            //                    Cv2.PutText(uiImage,
            //                        "Center",
            //                        new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y * SCALE),
            //                        HersheyFonts.HersheyComplex,
            //                        0.5f,
            //                        Scalar.Red);
            //                }

            //                // display the detection result
            //                Application.Current?.Dispatcher?.Invoke(() =>
            //                {
            //                    CurrentFrameBitmap = BitmapHelper.BitmapToBitmapSource(BitmapConverter.ToBitmap(uiImage));
            //                });
            //            }
            //        }
            //        i++;
            //    }
            //}
        }

        /// <summary>
        /// Gets the first frame of the video
        /// </summary>
        /// <returns></returns>
        public void SetFirstFrame()
        {
            // Read a video file and get the first frame
            using (var videocapture = new VideoCapture(VideoPath))
            {
                using (Mat imageOriginal = new Mat())
                {
                    // read a single frame and convert the frame into a byte array
                    videocapture.Read(imageOriginal);

                    var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(WIDTH * SCALE, HEIGHT * SCALE));

                    // display the detection result
                    Application.Current?.Dispatcher?.Invoke(() =>
                    {
                        CurrentFrameBitmap = BitmapHelper.BitmapToBitmapSource(BitmapConverter.ToBitmap(uiImage));
                        CurrentFrameIndex = 1;
                    });

                    TotalFrames = videocapture.FrameCount;
                }
            }
        }



    }
}
