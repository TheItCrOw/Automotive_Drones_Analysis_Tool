using Alturos.Yolo;
using AutomotiveDronesAnalysisTool.Utility;
using OpenCvSharp;
using OpenCvSharp.Extensions;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.Views.Test
{
    /// <summary>
    /// Interaktionslogik für TestView.xaml
    /// </summary>
    public partial class TestView : System.Windows.Window
    {
        public TestView()
        {
            InitializeComponent();
            TestVideoDetection();
        }

        private void TestVideoDetection()
        {
            Task.Run(() =>
            {
                // YOLO setting
                //int yoloWidth = 1920, yoloHeight = 1129;
                int yoloWidth = 175, yoloHeight = 102;
                //int yoloWidth = 1000, yoloHeight = 588;
                var configurationDetector = new ConfigurationDetector();
                var config = configurationDetector.Detect();
                var yoloWrapper = new YoloWrapper(config);

                // OpenCV & WPF setting
                VideoCapture videocapture;
                Mat image = new Mat();
                WriteableBitmap wb = new WriteableBitmap(yoloWidth, yoloHeight, 96, 96, System.Windows.Media.PixelFormats.Bgr24, null);

                byte[] imageInBytes = new byte[(int)(yoloWidth * yoloHeight * image.Channels())];

                // Read a video file and run object detection over it!
                using (videocapture = new VideoCapture("E:\\WPF Projects\\Automotive_Drones_Analysis_Tool\\Daten_automatisches_Fahren\\DJI_0137.MP4"))
                {
                    for (int i = 0; i < videocapture.FrameCount; i++)
                    {
                        using (Mat imageOriginal = new Mat())
                        {
                            // read a single frame and convert the frame into a byte array
                            videocapture.Read(imageOriginal);
                            image = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth, yoloHeight));
                            imageInBytes = image.ToBytes();

                            // conduct object detection and display the result
                            var items = yoloWrapper.Detect(imageInBytes);
                            // We use the image to detect the objects in a very small size - then we draw them onto the
                            // uiImage and scale it up!
                            var uiImage = imageOriginal.Resize(new OpenCvSharp.Size(yoloWidth * 10, yoloHeight * 10));

                            foreach (var item in items)
                            {
                                var x = item.X * 10;
                                var y = item.Y * 10;
                                var width = item.Width * 10;
                                var height = item.Height * 10;
                                var type = item.Type;  // class name of the object

                                // draw a bounding box for the detected object
                                // you can set different colors for different classes
                                Cv2.Rectangle(uiImage, new OpenCvSharp.Rect(x, y, width, height), Scalar.Green, 3);
                            }

                            // display the detection result
                            Application.Current?.Dispatcher?.Invoke(() =>
                            {
                                videoViewer.Source = BitmapHelper.BitmapToBitmapSource(BitmapConverter.ToBitmap(uiImage));
                            });
                        }
                        i++;
                    }
                }
            });
        }
    }
}
