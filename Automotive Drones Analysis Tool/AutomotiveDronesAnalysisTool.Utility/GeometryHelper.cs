using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;

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
        public static double Distance(Point p1, Point p2)
        {
            return Math.Sqrt(Math.Pow((p2.Y - p1.Y), 2) + Math.Pow((p2.X - p1.X), 2));
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
                double dist2 = Distance(p, toPoint);
                if (dist2 < minDist2)
                {
                    minDist2 = dist2;
                    nearestPoint = p;
                }
            }
            return new Tuple<Point, double>(nearestPoint, minDist2);
        }

        /// <summary>
        /// Returns the minor angle of two lines
        /// </summary>
        /// <param name="s1"></param>
        /// <param name="s2"></param>
        /// <returns></returns>
        public static double GetAngleBetweenTwoLines(Line s1, Line s2)
        {
            // Calulate angle of each segment
            double theta1 = Math.Atan2(s1.Y1 - s1.Y2, s1.X1 - s1.X2);
            double theta2 = Math.Atan2(s2.Y1 - s2.Y2, s2.X1 - s2.X2);

            // Taking the absolute value of the difference, you get the angle between the segments:
            double diff = Math.Abs(theta1 - theta2);

            // And finally, the minor angle
            double angle = Math.Min(diff, Math.Abs(180 - diff));

            // The actual angle in degrees
            double resultAngleInDegrees = angle * (180 / Math.PI);

            return resultAngleInDegrees;
        }

        public static double GetAngleBetweenTwoLines(Point p1, Point p2, Point p3, Point p4)
        {
            // Calulate angle of each segment
            double theta1 = Math.Atan2(p1.Y - p2.Y, p1.X - p2.X);
            double theta2 = Math.Atan2(p3.Y - p4.Y, p3.X - p4.X);

            // Taking the absolute value of the difference, you get the angle between the segments:
            double diff = Math.Abs(theta1 - theta2);

            // the minor arctan angle in radians
            double angle = Math.Min(diff, Math.Abs(180 - diff));

            // The actual angle in degrees
            double resultAngleInDegrees = angle * (180 / Math.PI);

            return resultAngleInDegrees;
        }

        /// <summary>
        /// Returns the slope of the given line
        /// </summary>
        /// <returns></returns>
        public static double GetSlopeOfLine(Point p1, Point p2) => (Math.Round((p2.Y - p1.Y), 4) / Math.Round((p2.X - p1.X), 4));

        /// <summary>
        /// Gets the center of the line as a coordinate.
        /// </summary>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <returns></returns>
        public static Point GetCenterOfLine(Point p1, Point p2)
        {
            var result = new Point();
            result.X = (p1.X + p2.X) / 2;
            result.Y = (p1.Y + p2.Y) / 2;
            return result;
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
