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
    /// <summary>
    /// Helper class for calculating small operations
    /// </summary>
    public class GeometryHelper
    {
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
        /// Gets the center of a rectangle
        /// </summary>
        /// <param name="rect"></param>
        /// <returns></returns>
        public static Point GetCenterOfRectangle(System.Drawing.Rectangle rect)
        {
            return new Point(rect.X + rect.Width / 2,
                             rect.Y + rect.Height / 2);
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

        // overload
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
        /// Gets the shortest disatnce between a line and a point
        /// </summary>
        /// <param name="pt"></param>
        /// <param name="p1"></param>
        /// <param name="p2"></param>
        /// <param name="closest"></param>
        /// <returns></returns>
        public static double GetDistanceFromPointToLine(Point pt, Line line, out Point closest)
        {
            var p1 = new Point(line.X1, line.Y1);
            var p2 = new Point(line.X2, line.Y2);

            double dx = p2.X - p1.X;
            double dy = p2.Y - p1.Y;
            if ((dx == 0) && (dy == 0))
            {
                // It's a point not a line segment.
                closest = p1;
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
                return Math.Sqrt(dx * dx + dy * dy);
            }

            // Calculate the t that minimizes the distance.
            double t = ((pt.X - p1.X) * dx + (pt.Y - p1.Y) * dy) /
                (dx * dx + dy * dy);

            // See if this represents one of the segment's
            // end points or a point in the middle.
            if (t < 0)
            {
                closest = new Point(p1.X, p1.Y);
                dx = pt.X - p1.X;
                dy = pt.Y - p1.Y;
            }
            else if (t > 1)
            {
                closest = new Point(p2.X, p2.Y);
                dx = pt.X - p2.X;
                dy = pt.Y - p2.Y;
            }
            else
            {
                closest = new Point(p1.X + t * dx, p1.Y + t * dy);
                dx = pt.X - closest.X;
                dy = pt.Y - closest.Y;
            }

            return Math.Sqrt(dx * dx + dy * dy);
        }

        /// <summary>
        ///  Returns Point of intersection if do intersect - otherwise default Point (null)
        /// </summary>
        /// <param name="lineA"></param>
        /// <param name="lineB"></param>
        /// <param name="tolerance"></param>
        /// <returns></returns>
        public static Point GetIntersection(Line lineA, Line lineB, double tolerance = 0.001)
        {
            double x1 = lineA.X1, y1 = lineA.Y1;
            double x2 = lineA.X2, y2 = lineA.Y2;

            double x3 = lineB.X1, y3 = lineB.Y1;
            double x4 = lineB.X2, y4 = lineB.Y2;

            // equations of the form x = c (two vertical lines)
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance && Math.Abs(x1 - x3) < tolerance)
            {
                throw new Exception("Both lines overlap vertically, ambiguous intersection points.");
            }

            //equations of the form y=c (two horizontal lines)
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance && Math.Abs(y1 - y3) < tolerance)
            {
                throw new Exception("Both lines overlap horizontally, ambiguous intersection points.");
            }

            //equations of the form x=c (two vertical lines)
            if (Math.Abs(x1 - x2) < tolerance && Math.Abs(x3 - x4) < tolerance)
            {
                return default(Point);
            }

            //equations of the form y=c (two horizontal lines)
            if (Math.Abs(y1 - y2) < tolerance && Math.Abs(y3 - y4) < tolerance)
            {
                return default(Point);
            }

            //general equation of line is y = mx + c where m is the slope
            //assume equation of line 1 as y1 = m1x1 + c1 
            //=> -m1x1 + y1 = c1 ----(1)
            //assume equation of line 2 as y2 = m2x2 + c2
            //=> -m2x2 + y2 = c2 -----(2)
            //if line 1 and 2 intersect then x1=x2=x & y1=y2=y where (x,y) is the intersection point
            //so we will get below two equations 
            //-m1x + y = c1 --------(3)
            //-m2x + y = c2 --------(4)

            double x, y;

            //lineA is vertical x1 = x2
            //slope will be infinity
            //so lets derive another solution
            if (Math.Abs(x1 - x2) < tolerance)
            {
                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x1=c1=x
                //subsitute x=x1 in (4) => -m2x1 + y = c2
                // => y = c2 + m2x1 
                x = x1;
                y = c2 + m2 * x1;
            }
            //lineB is vertical x3 = x4
            //slope will be infinity
            //so lets derive another solution
            else if (Math.Abs(x3 - x4) < tolerance)
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //equation of vertical line is x = c
                //if line 1 and 2 intersect then x3=c3=x
                //subsitute x=x3 in (3) => -m1x3 + y = c1
                // => y = c1 + m1x3 
                x = x3;
                y = c1 + m1 * x3;
            }
            //lineA & lineB are not vertical 
            //(could be horizontal we can handle it with slope = 0)
            else
            {
                //compute slope of line 1 (m1) and c2
                double m1 = (y2 - y1) / (x2 - x1);
                double c1 = -m1 * x1 + y1;

                //compute slope of line 2 (m2) and c2
                double m2 = (y4 - y3) / (x4 - x3);
                double c2 = -m2 * x3 + y3;

                //solving equations (3) & (4) => x = (c1-c2)/(m2-m1)
                //plugging x value in equation (4) => y = c2 + m2 * x
                x = (c1 - c2) / (m2 - m1);
                y = c2 + m2 * x;

                //verify by plugging intersection point (x, y)
                //in orginal equations (1) & (2) to see if they intersect
                //otherwise x,y values will not be finite and will fail this check
                if (!(Math.Abs(-m1 * x + y - c1) < tolerance
                    && Math.Abs(-m2 * x + y - c2) < tolerance))
                {
                    return default(Point);
                }
            }

            //x,y can intersect outside the line segment since line is infinitely long
            //so finally check if x, y is within both the line segments
            if (IsInsideLine(lineA, x, y) &&
                IsInsideLine(lineB, x, y))
            {
                return new Point(x, y);
            }

            //return default null (no intersection)
            return default(Point);
        }

        /// <summary>
        ///Returns true if given point(x, y) is inside the given line segment
        /// </summary>
        /// <param name="line"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public static bool IsInsideLine(Line line, double x, double y)
        {
            return (x >= line.X1 && x <= line.X2
                        || x >= line.X2 && x <= line.X1)
                   && (y >= line.Y1 && y <= line.Y2
                        || y >= line.Y2 && y <= line.Y1);
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
