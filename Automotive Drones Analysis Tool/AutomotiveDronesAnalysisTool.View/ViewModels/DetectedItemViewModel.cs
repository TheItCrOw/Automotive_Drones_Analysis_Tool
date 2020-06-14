using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Model.Models;
using AutomotiveDronesAnalysisTool.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;

namespace AutomotiveDronesAnalysisTool.View.ViewModels
{
    /// <summary>
    /// ViewModel that handles interaction of the <see cref="DetectedItemModel"/> with the UI
    /// </summary>
    public class DetectedItemViewModel : ViewModelBase
    {
        #region Properties
        private BitmapImage _image;
        private string _name;
        private int _x;
        private int _y;
        private int _width;
        private int _height;
        private DrawingShape _shape;
        private float _actualLength;

        /// <summary>
        /// The image that is being analysed.
        /// </summary>
        public BitmapImage Image
        {
            get => _image;
            set => SetProperty(ref _image, value);
        }

        public DetectedItemModel Model { get; }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Codename of this object that consists of the first and last letter of the name.
        /// </summary>
        public string CodeName
        {
            get => $"{Name.First()}{Name.Last()}";
        }
        public int X
        {
            get => _x;
            set => SetProperty(ref _x, value);
        }
        public int Y
        {
            get => _y;
            set => SetProperty(ref _y, value);
        }
        public int Width
        {
            get => _width;
            set => SetProperty(ref _width, value);
        }
        public int Height
        {
            get => _height;
            set => SetProperty(ref _height, value);
        }
        public float ActualLength
        {
            get => _actualLength;
            set => SetProperty(ref _actualLength, value);
        }
        public DrawingShape Shape
        {
            get => _shape;
            set => SetProperty(ref _shape, value);
        }
        #endregion
        public DetectedItemViewModel(DetectedItemModel model)
        {
            Id = model.Id;
            Model = model;
            Name = model.Name;
            X = model.X;
            Y = model.Y;
            Width = model.Width;
            Height = model.Height;
            Shape = model.Shape;
            ActualLength = model.ActualLength;
        }
    }
}
