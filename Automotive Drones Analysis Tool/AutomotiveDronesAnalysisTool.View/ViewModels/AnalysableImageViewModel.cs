using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
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
            //Pen blackPen = new Pen(Color.Black, 3);

            //using(var graphics = Graphics.FromImage(model.Image))
            //{
            //    graphics.DrawLine(blackPen, 0, 0, 5472, 3648);

            //}

            //GOOD HERE
            // First prepare the image and make the background black
            // create filter
            //HSLFiltering filter = new HSLFiltering();
            //// set color ranges to keep
            //filter.Hue = new IntRange(160, 200);
            //filter.Saturation = new AForge.Range(0, 1);
            //filter.Luminance = new AForge.Range(0, 1);
            //// apply the filter
            //filter.ApplyInPlace(model.Image);





            // lock image
            var bitmapData = model.Image.LockBits(
                new Rectangle(0, 0, model.Image.Width, model.Image.Height),
                ImageLockMode.ReadWrite, model.Image.PixelFormat);

            // step 1 - turn background to black
            ColorFiltering colorFilter = new ColorFiltering();

            colorFilter.Red = new IntRange(0, 170);
            colorFilter.Green = new IntRange(0, 170);
            colorFilter.Blue = new IntRange(0, 170);
            colorFilter.FillOutsideRange = false;

            colorFilter.ApplyInPlace(bitmapData);

            // step 2 - locating objects
            BlobCounter blobCounter = new BlobCounter();

            blobCounter.FilterBlobs = true;
            blobCounter.MinHeight = 50;
            blobCounter.MinWidth = 50;

            blobCounter.ProcessImage(bitmapData);
            Blob[] blobs = blobCounter.GetObjectsInformation();
            model.Image.UnlockBits(bitmapData);

            // step 3 - check objects' type and highlight
            SimpleShapeChecker shapeChecker = new SimpleShapeChecker();

            Graphics g = Graphics.FromImage(model.Image);
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






            // locate objects using blob counter
            //BlobCounter blobCounter = new BlobCounter();
            //blobCounter.ProcessImage(model.Image);
            //Blob[] blobs = blobCounter.GetObjectsInformation();
            //// create Graphics object to draw on the image and a pen
            //Graphics g = Graphics.FromImage(model.Image);
            //Pen bluePen = new Pen(Color.Blue, 2);
            //// check each object and draw circle around objects, which
            //// are recognized as circles
            //for (int i = 0, n = blobs.Length -2; i < n; i++)
            //{
            //    List<IntPoint> edgePoints = blobCounter.GetBlobsEdgePoints(blobs[i]);
            //    if (edgePoints.Count < 2)
            //        continue;
            //    List<IntPoint> corners = PointsCloud.FindQuadrilateralCorners(edgePoints);

            //    g.DrawPolygon(bluePen, ToPointsArray(corners));
            //}

            //bluePen.Dispose();
            //g.Dispose();



            // <=


            Image = BitmapHelper.ConvertBitmapToBitmapImage(model.Image);
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
