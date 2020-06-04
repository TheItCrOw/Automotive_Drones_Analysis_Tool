using AForge;
using AForge.Imaging;
using AForge.Imaging.Filters;
using AForge.Math.Geometry;
using Alturos.Yolo;
using Alturos.Yolo.Model;
using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using AutomotiveDronesAnalysisTool.View.Extensions;
using AutomotiveDronesAnalysisTool.View.Services;
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
        private BitmapImage _cleaImageCopy;
        private readonly AnalysableImageModel Model;
        private bool _alreadyAnalysed;
        private string _imageName;

        public DelegateCommand AddInformationCommand => new DelegateCommand(AddInformation);
        public DelegateCommand<string> EditInformationCommand => new DelegateCommand<string>(EditInformation);
        public DelegateCommand<string> DeleteInformationCommand => new DelegateCommand<string>(DeleteInformation);
        public DelegateCommand AnalyseImageCommand => new DelegateCommand(AnalyseImage);
        public DelegateCommand<DetectedItemArguments> AddDetectedItemCommand => new DelegateCommand<DetectedItemArguments>(AddDetectedItem);
        public DelegateCommand<string> AddCommentCommand => new DelegateCommand<string>(AddComment);
        public DelegateCommand<string> DeleteCommentCommand => new DelegateCommand<string>(DeleteComment);

        public string Projectname
        {
            get => _projectName;
            set => SetProperty(ref _projectName, value);
        }

        public string ImageName
        {
            get => _imageName;
            set => SetProperty(ref _imageName, value);
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
        /// The copy of the image
        /// </summary>
        public BitmapImage CleanImageCopy
        {
            get => _cleaImageCopy;
            set => SetProperty(ref _cleaImageCopy, value);
        }

        /// <summary>
        /// Metadata of the image
        /// </summary>
        public Dictionary<string, string> Metadata { get; set; } = new Dictionary<string, string>();

        /// <summary>
        /// Additional information added by user
        /// </summary>
        public ObservableCollection<Tuple<string, string>> AdditionalInformation { get; set; }

        /// <summary>
        /// List of found objects by YOLO
        /// </summary>
        public ObservableCollection<DetectedItemViewModel> DetectedObjects { get; set; }

        /// <summary>
        /// List of comments added by the user in the report view.
        /// </summary>
        public ObservableCollection<string> Comments { get; set; }

        public AnalysableImageViewModel(AnalysableImageModel model)
        {
            AdditionalInformation = new ObservableCollection<Tuple<string, string>>();
            DetectedObjects = new ObservableCollection<DetectedItemViewModel>();

            Model = model;
            Id = new Guid();
            Projectname = model.ProjectName;
            ImageName = model.ImageName;
            Metadata = model.MetaData;
            Image = BitmapHelper.ConvertBitmapToBitmapImage(model.Image);
            CleanImageCopy = BitmapHelper.ConvertBitmapToBitmapImage(model.Image);

            if (model.AdditionalInformation != null)
                foreach (var pair in model.AdditionalInformation)
                    AdditionalInformation.Add(Tuple.Create(pair.Item1, pair.Item2));
        }

        /// <summary>
        /// Returns the instance of the model.
        /// </summary>
        public AnalysableImageModel GetModelInstance() => Model;

        /// <summary>
        /// Deletes the given item from the detectedItemslist and from the image
        /// </summary>
        /// <param name="key"></param>
        public void DeleteDetectedItem(string key)
        {
            DetectedObjects.Remove(DetectedObjects.FirstOrDefault(i => i.Name == key));
            var drawnImage = DrawObjectsOntoImage(DetectedObjects, (Bitmap)Model.Image.Clone());
            Image = BitmapHelper.ConvertBitmapToBitmapImage(drawnImage);
        }

        /// <summary>
        /// Detects object and analyses the image.
        /// </summary>
        public void AnalyseImage()
        {
            if (_alreadyAnalysed)
                return;

            // Clear the list.
            Application.Current?.Dispatcher?.Invoke(() => DetectedObjects.Clear());

            // First detect all the opjects in the image.
            foreach (var item in ServiceContainer.GetService<YOLOCommunicationService>().DetectItemsInBitmap(Model.Image))
            {
                var detectedItemArgs = new DetectedItemArguments()
                {
                    X = item.X,
                    Y = item.Y,
                    Width = item.Width,
                    Height = item.Height,
                    CanvasSize = new System.Drawing.Point(Model.Image.Width, Model.Image.Height)
                };

                AddDetectedItem(detectedItemArgs);
            }

            var drawnImage = DrawObjectsOntoImage(DetectedObjects, (Bitmap)Model.Image.Clone());
            Image = BitmapHelper.ConvertBitmapToBitmapImage(drawnImage);
            _alreadyAnalysed = true;
        }

        /// <summary>
        /// Dispose the viewmodel
        /// </summary>
        public void Dispose()
        {
            Image?.StreamSource.Dispose(); // Clear the stream
            Image = null;
            CleanImageCopy?.StreamSource.Dispose(); // Clear copy stream
            CleanImageCopy = null;
            Model.Image.Dispose();
            Model.Image = null;
            Metadata.Clear();

            foreach (var item in DetectedObjects) // Clear detected items.
            {
                item?.Image?.StreamSource?.Dispose();
                item.Image = null;
            }
            DetectedObjects?.Clear();
        }

        /// <summary>
        /// Sets up the YOLO config correctly.
        /// </summary>
        public void SetupYOLOConfig()
        {
            // Calculate the current widthHeight and set it into the YOLo config.
            var yoloWidthHeight = CalculateYoloWidthHeightFromImageModel(Model);

            if (yoloWidthHeight == 0)
            {
                if (ServiceContainer.GetService<DialogService>()
                    .AskForInteger(
                    "Missing altitude",
                    "We couldn't find the absolute altitude in the metadata of the image from where the picture was taken. " +
                    "If you do know the altitude of the given picture, please add the value of it now and press the confirm button. This helps analysing the image. " +
                    "Please only enter a value if you can ensure the correctness. Typically it varies from 450 to 470. " +
                    "If the altitude is not known or simply doesn't exist, please press the cancel button. A default value will then be chosen to analyse the image.",
                    out var result))
                {
                    // Add the result as "AdditionalInformation"
                    AdditionalInformation.Add(Tuple.Create("AbsoluteAltitude", result.ToString()));
                    yoloWidthHeight = CalculateYoloWidthHeightFromImageModel(Model, result);
                }
                else
                {
                    // if the user does not input the altitude, we assume the deafult altitude
                    yoloWidthHeight = CalculateYoloWidthHeightFromImageModel(Model, 453); // 453 is a default value.
                }
            }

            // Update the YOLO config with the given widthHeight
            ServiceContainer.GetService<YOLOCommunicationService>().SetWidthHeight(yoloWidthHeight);
        }

        /// <summary>
        /// Adds a comment to the viewmodel
        /// </summary>
        /// <param name="comment"></param>
        private void AddComment(string comment) => Comments.Add(comment);

        /// <summary>
        /// Delete the given comment from the viewmodel
        /// </summary>
        /// <param name="comment"></param>
        private void DeleteComment(string comment) => Comments.Remove(comment);

        /// <summary>
        /// Adds a new yoloItem and draws it onto the picture.
        /// </summary>
        /// <param name="yoloItem"></param>
        private void AddDetectedItem(DetectedItemArguments detectedItemArgs)
        {
            // If the new item is the reference line - handle it exclusivly.
            if(detectedItemArgs.Shape == DrawingShape.ReferenceLine)
            {
                AddReferenceLine(detectedItemArgs);
                return;
            }

            // First we need to transform the coordiantes to match with the different image size
            var canvasWidth = detectedItemArgs.CanvasSize.X;
            var canvasHeight = detectedItemArgs.CanvasSize.Y;

            var imageWidth = Model.Image.Width;
            var imageHeight = Model.Image.Height;

            double widthRatio = (double)imageWidth / (double)canvasWidth;
            double heightRatio = (double)imageHeight / (double)canvasHeight;

            // We dont want identical names
            var itemName = "object #0";
            int i = 0;
            while (DetectedObjects.Any(o => o.Name == itemName))
            {
                i++;
                itemName = $"object #{i}";
            }

            var detectedItem = new DetectedItemModel()
            {
                Id = Guid.NewGuid(),
                AnalysableImageModelId = Id,
                Name = itemName,
                X = (int)(detectedItemArgs.X * widthRatio),
                Y = (int)(detectedItemArgs.Y * heightRatio),
                Width = (int)(detectedItemArgs.Width * widthRatio),
                Height = (int)(detectedItemArgs.Height * heightRatio),
                Shape = detectedItemArgs.Shape
            };
            var detectedItemViewModel = new DetectedItemViewModel(detectedItem);

            // We only want to analyse the items that have been cropped out via rectangle
            if (detectedItem.Shape == DrawingShape.Rectangle)
                AnalyseItem(detectedItemViewModel);

            Application.Current?.Dispatcher?.Invoke(() => DetectedObjects.Add(detectedItemViewModel));

            var newImage = DrawObjectsOntoImage(DetectedObjects, (Bitmap)Model.Image.Clone());
            Image = BitmapHelper.ConvertBitmapToBitmapImage(newImage);
        }

        /// <summary>
        /// Handles the specific case of the new item being the reference line. There may only exist one reference line.
        /// </summary>
        /// <param name="detectedItemArgs"></param>
        private void AddReferenceLine(DetectedItemArguments detectedItemArgs)
        {
            // We only want one reference line
            if(DetectedObjects.Any(o => o.Shape == DrawingShape.ReferenceLine))
            {
                ServiceContainer.GetService<DialogService>()
                    .InformUser("Reference line already added",
                                "A reference line has already been added. You can only draw one. If you wish to change the current " +
                                "reference line, delete it and draw it again.");
                return;
            }

            if(ServiceContainer.GetService<DialogService>()
                .AskForFloat("Actual length", "Please enter the actual length of the reference line in meters.", out var actualLength))
            {
                // Calculate the correct pos and length according to the images pixels
                var canvasWidth = detectedItemArgs.CanvasSize.X;
                var canvasHeight = detectedItemArgs.CanvasSize.Y;

                var imageWidth = Model.Image.Width;
                var imageHeight = Model.Image.Height;

                double widthRatio = (double)imageWidth / (double)canvasWidth;
                double heightRatio = (double)imageHeight / (double)canvasHeight;

                var detectedItem = new DetectedItemModel()
                {
                    Id = Guid.NewGuid(),
                    AnalysableImageModelId = Id,
                    Name = "Reference-line",
                    X = (int)(detectedItemArgs.X * widthRatio),
                    Y = (int)(detectedItemArgs.Y * heightRatio),
                    Width = (int)(detectedItemArgs.Width * widthRatio),
                    Height = (int)(detectedItemArgs.Height * heightRatio),
                    Shape = detectedItemArgs.Shape,
                    ActualLength = actualLength
                };
                var detectedItemViewModel = new DetectedItemViewModel(detectedItem);

                Application.Current?.Dispatcher?.Invoke(() => DetectedObjects.Add(detectedItemViewModel));

                var newImage = DrawObjectsOntoImage(DetectedObjects, (Bitmap)Model.Image.Clone());
                Image = BitmapHelper.ConvertBitmapToBitmapImage(newImage);
            }
        }

        /// <summary>
        /// Draws the given items onto the Image.
        /// </summary>
        /// <param name="items"></param>
        private Bitmap DrawObjectsOntoImage(IEnumerable<DetectedItemViewModel> items, Bitmap image)
        {
            foreach (var item in items)
            {
                using (var g = Graphics.FromImage(image))
                {
                    // TODO: Place that into a user config? 
                    var penWidth = image.Width * 0.0025f;
                    var fontSize = image.Width * 0.02f;
                    var pen = new Pen(Color.White, penWidth);

                    // Draw the shape of the object
                    switch (item.Shape)
                    {
                        case DrawingShape.Rectangle:
                            g.DrawRectangle(pen, item.X, item.Y, item.Width, item.Height);
                            break;
                        case DrawingShape.Line:
                            penWidth = image.Width * 0.0025f;
                            pen = new Pen(Color.Blue, penWidth);
                            g.DrawLine(pen, item.X, item.Y, item.Width, item.Height);
                            break;
                        case DrawingShape.ReferenceLine:
                            penWidth = image.Width * 0.0025f;
                            pen = new Pen(Color.Lime, penWidth);
                            pen.DashStyle = System.Drawing.Drawing2D.DashStyle.Dash;
                            g.DrawLine(pen, item.X, item.Y, item.Width, item.Height);
                            break;
                        default:
                            break;
                    }

                    var font = new Font(FontFamily.GenericSansSerif, fontSize, System.Drawing.FontStyle.Bold, GraphicsUnit.Pixel);
                    g.DrawString(item.Name, font, Brushes.Red, new PointF(item.X, item.Y));
                }
            }
            return image;
        }

        /// <summary>
        /// Splits the image into its objects and analyses them individually
        /// </summary>
        /// <param name="items"></param>
        /// <param name="image"></param>
        private void AnalyseItem(DetectedItemViewModel item)
        {
            var currentImage = new Bitmap(item.Width, item.Height);
            using (var g = Graphics.FromImage(currentImage))
            {
                g.DrawImage(Model.Image,
                    new Rectangle(0, 0, currentImage.Width, currentImage.Height), // target bitmap
                    new Rectangle(item.X, item.Y, item.Width, item.Height), // source bitmap. define the part to crop out.
                    GraphicsUnit.Pixel);
            }
            //var shapeDetectedImage = TestShapeDetection(currentImage);
            item.Image = BitmapHelper.ConvertBitmapToBitmapImage(currentImage);
        }

        /// <summary>
        /// Tries to read the altitude of the image to set the width and height correctly of the YOLO wrapper
        /// Returns 0 if it was not possible due to incorrect altitude.
        /// </summary>
        /// <param name="model"></param>
        private int CalculateYoloWidthHeightFromImageModel(AnalysableImageModel model, int knownAltitude = 0)
        {
            // First look if the model has any altitude to it.
            if (model.MetaData.Keys.Any(k => k.ToLower().Contains("absolutealtitude") || knownAltitude > 0))
            {
                // Then we get the altitude
                // If the user has entered the altitude, we will not find anything in the foreach loop.
                var altitude = knownAltitude;

                foreach (var pair in model.MetaData)
                    if (pair.Key.ToLower().Contains("absolutealtitude"))
                    {
                        var trimmedString = pair.Value;
                        if (trimmedString.StartsWith("-") || trimmedString.StartsWith("+"))
                            trimmedString = pair.Value.Substring(1, pair.Value.Length - 1); // Get rid of the "+" at the start of string

                        if (trimmedString.Contains(".")) // If the string is a double or float
                        {
                            var index = trimmedString.IndexOf(".");
                            trimmedString = trimmedString.Substring(0, index);
                        }

                        altitude = (int)float.Parse(trimmedString); // Get the altitude.
                    }

                if (altitude == 0) return 0;

                // Now calculate the width and height for the YOLO config
                var maxHeighWidthToAltitude = AltitudeToWidthHeightHelper.MaxWidthHeightToAltitude;

                // If the altitude is greater than the limit, return the maximum widthHeight
                if (altitude > maxHeighWidthToAltitude.Value)
                    return maxHeighWidthToAltitude.Key;

                // If the altitude is lower than the minimum, return the minimum widthHeight
                if (altitude < AltitudeToWidthHeightHelper.MinWidthHeightToAltitude.Value)
                    return AltitudeToWidthHeightHelper.MinWidthHeightToAltitude.Key;

                var currentWidthHeight = maxHeighWidthToAltitude.Key;
                var currAltitude = altitude;

                // Now we calculate the widht and height to the altitude. 
                while (currAltitude < maxHeighWidthToAltitude.Value)
                {
                    currentWidthHeight -= 75; // 70 = 1 altitude less. This is the step size
                    currAltitude++;
                }

                var remainder = currentWidthHeight % 32;
                if (remainder != 0)
                {
                    // We need the currentWidhtHeight to be cleanly dividable by 32.
                    // So if the remainder is >= 16, we add the remaining 32 - remainder.
                    // If the remainder is < 16, we just substract the remainder.
                    currentWidthHeight = remainder >= 16 ? currentWidthHeight + 32 - remainder : currentWidthHeight - remainder;
                }
                return currentWidthHeight;
            }
            else // No altitude
            {
                return 0;
            }
        }

        /// <summary>
        /// This is/was for testing pupose. Maybe using it later.
        /// </summary>
        /// <param name="image"></param>
        /// <returns></returns>
        [Obsolete]
        private Bitmap TestShapeDetection(Bitmap image)
        {
            // step 1 - turn background to black
            // create filter
            HSLFiltering filter = new HSLFiltering();
            // set color ranges to keep
            filter.Hue = new IntRange(0, 359);
            filter.Saturation = new AForge.Range(0, 1f);
            filter.Luminance = new AForge.Range(0.45f, 1);
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
            blobCounter.MinHeight = 50;
            blobCounter.MinWidth = 50;

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
