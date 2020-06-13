using Alturos.Yolo;
using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Services;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using Prism.Commands;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

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
        private int SCALE = ServiceContainer.GetService<Cv2Service>().SCALE;
        private int WIDTH = ServiceContainer.GetService<Cv2Service>().WIDTH;
        private int HEIGHT = ServiceContainer.GetService<Cv2Service>().HEIGHT;
        private Dictionary<int, string> _indexToImageTempPath;
        private ConcurrentQueue<Tuple<Bitmap, string>> _imageQueue;
        bool _keepRunning;
        private bool _isPlaying;
        private Task _playTask;
        private CancellationTokenSource _tokenSource;
        private CancellationToken _token;
        object _locked = new object();
        private DetectedItemArguments _referenceLineArgs;
        private bool _isSetup;

        public DelegateCommand PlayCommand => new DelegateCommand(Play);
        public DelegateCommand PauseCommand => new DelegateCommand(Pause);
        public DelegateCommand FastForwardCommand => new DelegateCommand(FastForward);
        public DelegateCommand RewindCommand => new DelegateCommand(Rewind);
        public DelegateCommand<DetectedItemArguments> AddVideoReferenceLineCommand => new DelegateCommand<DetectedItemArguments>(AddVideoReferenceLine);
        public DelegateCommand DeleteVideoReferenceLineCommand => new DelegateCommand(DeleteVideoReferenceLine);
        public DelegateCommand SetupVideoCommand => new DelegateCommand(SetupVideo);

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
        /// True if this video ahs been setup and analysed.
        /// </summary>
        public bool IsSetup
        {
            get => _isSetup;
            set => SetProperty(ref _isSetup, value);
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
        /// True if the user us currently letting the video run by pressing the play button
        /// </summary>
        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
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
        }

        public void Dispose()
        {
            // TODO: Enable dispose!
            // Dispose the iamges in the temp folder!
            //foreach (var pair in _indexToImageTempPath)
            //    File.Delete(pair.Value);
        }

        /// <summary>
        /// We extract every frame of the video and store it in a dictionary.
        /// Since this would blow up the RAM, the images are written into a temp folder on the disc.
        /// The speed is inhanced by using multiple threads and "powering" through the images in the concurrent queue.
        /// </summary>
        public void SetupVideo()
        {
            if(_referenceLineArgs == null)
            {
                ServiceContainer.GetService<DialogService>()
                    .InformUser("Reference line missing", "Please add a reference line to the image first! " +
                    "You can do so by simply drawing onto the image with your mouse.");
                return;
            }

            _indexToImageTempPath.Clear();
            _keepRunning = true;

            // Start the queue worker which take bitmaps of the queue and save them
            for (int i = 0; i < 10; i++)
            {
                StartQueueWorker();
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
                        var analysedImageOriginal = AnalyseFrame(imageOriginal);
                        // TODO: Put analyedImageOriginal back and decomment this above!
                        var bitmap = BitmapConverter.ToBitmap(analysedImageOriginal);
                        var imagePath = System.IO.Path.Combine(
                            ServiceContainer.GetService<GlobalEnviromentService>().Cv2TempVideoLocation,
                            $"temp_{i}.png");

                        // Enqueue the path and the bitmap into the queue so the workers can save it.
                        _indexToImageTempPath.Add(i, imagePath);
                        _imageQueue.Enqueue(Tuple.Create(bitmap, imagePath));
                    }
                }
            }

            IsSetup = true;
            _keepRunning = false;
        }

        /// <summary>
        /// analyses the given frame.
        /// </summary>
        public Mat AnalyseFrame(Mat imageOriginal)
        {
            // YOLO setting
            int yoloWidth = WIDTH, yoloHeight = HEIGHT;
            var configurationDetector = new ConfigurationDetector();
            var config = configurationDetector.Detect();
            using (var yoloWrapper = new YoloWrapper(config))
            {
                // OpenCV & WPF setting
                Mat image = new Mat();
                WriteableBitmap wb = new WriteableBitmap(yoloWidth, yoloHeight, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);

                byte[] imageInBytes = new byte[(int)(yoloWidth * yoloHeight * image.Channels())];

                image = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth, yoloHeight));
                imageInBytes = image.ToBytes();

                // conduct object detection and display the result
                var items = yoloWrapper.Detect(imageInBytes);
                // We use the image to detect the objects in a very small size - then we draw them onto the
                // uiImage and scale it up!
                var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth * SCALE, yoloHeight * SCALE));
                var cv2Service = ServiceContainer.GetService<Cv2Service>();

                // Draw the image ref line onto a every frame and get the length of a coordiante step!
                cv2Service.DrawReferenceLine(uiImage, _referenceLineArgs, out double coordinateLength);

                // Now drwa the yolo items.
                foreach (var item in items)
                {
                    var x = item.X * SCALE;
                    var y = item.Y * SCALE;
                    var width = item.Width * SCALE;
                    var height = item.Height * SCALE;
                    var type = item.Type;  // class name of the object

                    // draw a bounding box for the detected object
                    // you can set different colors for different classes
                    Cv2.Rectangle(uiImage, new OpenCvSharp.Rect(x, y, width, height), Scalar.White, 3);
                    // Draw the disatnce and angle of this object to the reference line.
                    cv2Service.DrawDistanceAndAngleToReferenceLine(uiImage, 
                        new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y *SCALE));

                    // Draw a connection line to each other object.
                    foreach (var item2 in items)
                        if (item2 != item)
                        {
                            // Draws the line and lenght of the two items.
                            cv2Service.DrawLineFromItemToItem(uiImage, item, item2, out Line line);
                        }
                }
                return uiImage;
            }
        }

        /// <summary>
        /// Sets the first frame of the video
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

        /// <summary>
        /// Deletes the current reference line.
        /// </summary>
        private void DeleteVideoReferenceLine() => _referenceLineArgs = null;

        /// <summary>
        /// Adds the single reference line to the logic.
        /// </summary>
        /// <param name="refLine"></param>
        private void AddVideoReferenceLine(DetectedItemArguments refLine) => _referenceLineArgs = refLine;

        /// <summary>
        /// Fires when the current active index changes. We need to adjust the frame bitmap then.
        /// </summary>
        /// <param name="index"></param>
        private void AnalysableVideoViewModel_IndexChanged(int index)
        {
            // If the video is not setup, return
            if (!IsSetup) return;

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
        /// Starts a queue worker that grabs a bitmap from the imagequeue and saves it into the temop folder.
        /// </summary>
        private void StartQueueWorker()
        {
            Task.Run(() =>
            {
                lock (_locked)
                {
                    while (_keepRunning || _imageQueue.Count > 0)
                    {
                        if (_imageQueue.TryDequeue(out var tuple))
                        {
                            // Deque the bitmap and write it to disc.
                            // Lower the quality to save ressources.
                            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageEncoders();
                            ImageCodecInfo ici = null;

                            foreach (ImageCodecInfo codec in codecs)
                            {
                                if (codec.MimeType == "image/jpeg")
                                    ici = codec;
                            }

                            var encoderParameters = new EncoderParameters();
                            encoderParameters.Param[0] = new EncoderParameter(System.Drawing.Imaging.Encoder.Quality, (long)70);

                            tuple.Item1.Save(tuple.Item2, ici, encoderParameters);
                            tuple.Item1.Dispose();
                        }
                    }
                }
            });
        }

        /// <summary>
        /// Goes one frame back
        /// </summary>
        private void Rewind()
        {
            // If the video is not setup, return
            if (!IsSetup) return;

            if (CurrentFrameIndex > 1)
                CurrentFrameIndex--;
        }

        /// <summary>
        /// Goes one frame forward
        /// </summary>
        private void FastForward()
        {
            // If the video is not setup, return
            if (!IsSetup) return;

            if (CurrentFrameIndex < TotalFrames)
                CurrentFrameIndex++;
        }

        /// <summary>
        /// Lets the video play on its own and analyse each frame.
        /// </summary>
        private void Play()
        {
            // If the video is not setup, return
            if (!IsSetup) return;

            IsPlaying = true;

            // Create a cancellation token to stop the task if needed.
            _tokenSource = new CancellationTokenSource();
            _token = _tokenSource.Token;

            _playTask = Task.Run(() =>
            {
                for (int i = CurrentFrameIndex; i < TotalFrames; i++)
                {
                    if (_token.IsCancellationRequested)
                        break;

                    CurrentFrameIndex++;
                    Thread.Sleep(25);
                }
                IsPlaying = false;
            }, _token);
        }

        /// <summary>
        /// Pauses the video by canceling the <see cref="_playTask"/>        
        /// </summary>
        private void Pause()
        {
            // If the video is not setup, return
            if (!IsSetup) return;

            if (_playTask != null)
            {
                _tokenSource.Cancel();
                if(_token.IsCancellationRequested)
                    IsPlaying = false;
            }
        }
    }
}
