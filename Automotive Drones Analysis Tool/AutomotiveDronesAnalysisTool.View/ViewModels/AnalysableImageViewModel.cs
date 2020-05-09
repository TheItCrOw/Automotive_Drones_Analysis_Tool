using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using Alturos.Yolo;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Extensions;
using AutomotiveDronesAnalysisTool.View.Views.Modal;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    public class AnalysableImageViewModel : ViewModelBase
    {
        private string _projectName;
        private BitmapImage _image;

        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);
        public DelegateCommand<string> EditInformationCommand => new DelegateCommand<string>(EditInformation);
        public DelegateCommand<string> DeleteInformationCommand => new DelegateCommand<string>(DeleteInformation);

        public string Projectname
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        /// <summary>
        /// The image that is being analysed.
        /// </summary>
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        /// <summary>
        /// Metadata of the image
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Additional information added by user
        /// </summary>
        public ObservableCollection<Tuple<string, string>> AdditionalInformation { get; set; }

        public AnalysableImageViewModel(AnalysableImageModel model)
        {
            AdditionalInformation = new ObservableCollection<Tuple<string, string>>();
            Id = new Guid();
            Projectname = model.Projectname;
            Metadata = model.MetaData;

            if (model.AdditionalInformation != null)
                foreach (var pair in model.AdditionalInformation)
                    AdditionalInformation.Add(Tuple.Create(pair.Item1, pair.Item2));


            // Testing =>



            // <=

            //model.Image = TestShapeDetection(model.Image);
            YoloTest(model.Image);
            Image = BitmapHelper.ConvertBitmapToBitmapImage(model.Image);
        }

        private void YoloTest(Bitmap image)
        {
            var configurationDetector = new ConfigurationDetector();
            var config = configurationDetector.Detect();

            var cfgPath = "E:\\WPF Projects\\Automotive_Drones_Analysis_Tool\\LearnedDatasets\\yolov3.cfg";
            var weightsPath = "E:\\WPF Projects\\Automotive_Drones_Analysis_Tool\\LearnedDatasets\\yolov3.weights";
            var namesPath = "E:\\WPF Projects\\Automotive_Drones_Analysis_Tool\\LearnedDatasets\\coco.names";

            using(var yoloWrapper = new YoloWrapper(cfgPath, weightsPath, namesPath))
            {
                using(var memStream = new MemoryStream())
                {
                    image.Save(memStream, ImageFormat.Png);
                    var items = yoloWrapper.Detect(memStream.ToArray());

                    foreach(var foundItem in items)
                    {
                        using(var g = Graphics.FromImage(image))
                        {
                            var pen = new Pen(Color.White, 10);
                            g.DrawRectangle(pen, foundItem.X, foundItem.Y, foundItem.Width, foundItem.Height);
                            var font = new Font(FontFamily.GenericMonospace, 150.0F, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                            g.DrawString(foundItem.Type.ToString(), font, Brushes.Red, new PointF(foundItem.X, foundItem.Y));
                        }
                    }
                }
            }
        }

        private void TensorTest(Bitmap image)
        {
            var weightsPath = "E:\\WPF Projects\\Automotive_Drones_Analysis_Tool\\LearnedDatasets\\trained_weights_final.h5";

        }

        private Bitmap TestShapeDetection(Bitmap image)
        {
            // step 1 - turn background to black
            // create filter
            HSLFiltering filter = new HSLFiltering();
            // set color ranges to keep
            filter.Hue = new IntRange(30, 225);
            filter.Saturation = new AForge.Range(0, 1f);
            filter.Luminance = new AForge.Range(0.50f, 1);
            // apply the filter
            filter.ApplyInPlace(image);

            // create filter
            //YCbCrFiltering filter = new YCbCrFiltering();
            //// set color ranges to keep
            //filter.Y = new AForge.Range(0.5f, 1);
            //filter.Cb = new AForge.Range(-0.2f, 0.5f);
            //filter.Cr = new AForge.Range(-0.2f, 0.5f);
            //// apply the filter
            //filter.ApplyInPlace(model.Image);

            // lock image
            var bitmapCopy = image;
            var bitmapData = image.LockBits(
                new Rectangle(0, 0, image.Width, image.Height),
                ImageLockMode.ReadWrite, image.PixelFormat);

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 30;
            blobCounter.MinWidth = 30;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            image.UnlockBits(bitmapData);

            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Graphics g = Graphics.FromImage(bitmapCopy);
            Pen redPen = new Pen(Color.Red, 10);
            Pen yellowPen = new Pen(Color.Yellow, 2);
            Pen greenPen = new Pen(Color.Green, 2);
            Pen bluePen = new Pen(Color.Blue, 2);

            for (int i = 0, n = blobs.Length; i < n; i++)
            {
                List<IntPoint> edgePoints =
                    blobCounter.GetBlobsEdgePoints(blobs[i]);

                AForge.Point center;
                float radius;

                if (shapeChecker.IsCircle(edgePoints, out center, out radius))
                {
                    g.DrawEllipse(yellowPen,
                        (float)(center.X - radius), (float)(center.Y - radius),
                        (float)(radius * 2), (float)(radius * 2));
                }
                else
                {
                    List<IntPoint> corners;

                    if (shapeChecker.IsQuadrilateral(edgePoints, out corners))
                    {
                        if (shapeChecker.CheckPolygonSubType(corners) ==
                            PolygonSubType.Rectangle)
                        {
                            g.DrawPolygon(greenPen, ToPointsArray(corners));
                        }
                        else
                        {
                            g.DrawPolygon(bluePen, ToPointsArray(corners));
                        }
                    }
                    else
                    {
                        corners = PointsCloud.FindQuadrilateralCorners(edgePoints);
                        g.DrawPolygon(redPen, ToPointsArray(corners));
                    }
                }
            }

            redPen.Dispose();
            greenPen.Dispose();
            bluePen.Dispose();
            yellowPen.Dispose();
            g.Dispose();
            return image;
        }

        private System.Drawing.Point[] ToPointsArray(List<IntPoint> points)
        {
            return points.Select(p => new System.Drawing.Point(p.X, p.Y)).ToArray();
        }

        /// <summary>
        /// Adds a new information to the meta data
        /// </summary>
        private void AddInformation()
        {
            // The Hashset contains all properties we already have. We do not want to add those again.
            var blacklist = new HashSet<string>(Metadata.Keys.ToList());
            foreach (var item in AdditionalInformation)
                blacklist.Add(item.Item1);

            var addInfoView = new AddInformationView(blacklist);
            addInfoView.ShowDialog();

            if (addInfoView.DialogResult == true)
            {
                AdditionalInformation.Add(addInfoView.NewInformation);
            }
        }

        /// <summary>
        /// Edits an information
        /// </summary>
        /// <param name="key"></param>
        private void EditInformation(string key)
        {
            // The Hashset contains all properties we already have. We do not want to add those again.
            var blacklist = new HashSet<string>(Metadata.Keys.ToList());
            foreach (var item in AdditionalInformation)
                if (item.Item1 != key) // We do not blacklist the current editable key.
                    blacklist.Add(item.Item1);

            var addInfoView = new AddInformationView(
                blacklist,
                AdditionalInformation.FirstOrDefault(p => p.Item1 == key)); // Pass the current selected key

            addInfoView.ShowDialog();

            if (addInfoView.DialogResult == true)
            {
                var newInfo = addInfoView.NewInformation;
                var oldInfo = AdditionalInformation.FirstOrDefault(t => t.Item1 == key);
                // We want to insert the new edited item at the same position the old one stayed.
                var index = AdditionalInformation.IndexOf(oldInfo);
                AdditionalInformation.Remove(oldInfo);
                AdditionalInformation.Insert(index--, newInfo);
            }
        }

        /// <summary>
        /// Deletes an information
        /// </summary>
        /// <param name="key"></param>
        private void DeleteInformation(string key) => AdditionalInformation?.Remove(AdditionalInformation.FirstOrDefault(t => t.Item1 == key));
    }
}
