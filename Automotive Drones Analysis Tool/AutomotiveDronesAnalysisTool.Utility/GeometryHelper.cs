using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AutomotiveDronesAnalysisTool.Utility
{
    public class GeometryHelper
    {
        private static double Pow2(double x)
        {
            return x * x;
        }

        /// <summary>
        /// Gets the distance of 2 points
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static double Distance2(Point p1, Point p2)
        {
            return Pow2(p2.X - p1.X) + Pow2(p2.Y - p1.Y);
        }

        /// <summary>
        /// Gets the nearest point to the target "toPoint" out of a given list.
        /// </summary>
        /// <param name="toPoint"></param>
        /// <param name="points"></param>
        /// <returns></returns>
        public static Tuple<Point, double> GetNearestPoint(Point toPoint, LinkedList<Point> points)
        {
            Point nearestPoint = new Point();
            double minDist2 = double.MaxValue;
            foreach (Point p in points)
            {
                double dist2 = Distance2(p, toPoint);
                if (dist2 < minDist2)
                {
                    minDist2 = dist2;
                    nearestPoint = p;
                }
            }
            return new Tuple<Point, double>(nearestPoint, minDist2);
        }

        /// <summary>
        /// Calcualtes height and width of a given textblock
        /// </summary>
        /// <param name="candidate"></param>
        /// <param name="textBlock"></param>
        /// <returns></returns>
        public static Size MeasureTextblock(TextBlock textBlock)
        {
            var formattedText = new FormattedText(
                textBlock.Text,
                CultureInfo.CurrentCulture,
                FlowDirection.LeftToRight,
                new Typeface(textBlock.FontFamily, textBlock.FontStyle, textBlock.FontWeight, textBlock.FontStretch),
                textBlock.FontSize,
                Brushes.Black,
                new NumberSubstitution(),
                1);

            return new Size(formattedText.Width, formattedText.Height);
        }
    }
}
