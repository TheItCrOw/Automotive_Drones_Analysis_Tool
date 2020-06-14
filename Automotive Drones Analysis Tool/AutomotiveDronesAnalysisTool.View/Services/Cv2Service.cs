using Alturos.Yolo.Model;
using AutomotiveDronesAnalysisTool.Model.Arguments;
using AutomotiveDronesAnalysisTool.Utility;
using OpenCvSharp;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using System.Windows.Shapes;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    /// <summary>
    /// Service that handles Cv2 operations.
    /// </summary>
    public class Cv2Service : ServiceBase
    {
        public readonly int SCALE = 10;
        public readonly int WIDTH = 200;
        public readonly int HEIGHT = 118;
        private double coordinateLength;
        private Line _referenceLine;

        /// <summary>
        /// Draws the given reference line onto the mat
        /// </summary>
        public void DrawReferenceLine(Mat image, DetectedItemArguments refLineArgs, out double lengthOfCoordinate)
        {
            // Calculate distance and length of coordinate first!
            var distance = GeometryHelper.Distance(
                new System.Windows.Point(refLineArgs.X, refLineArgs.Y),
                new System.Windows.Point(refLineArgs.Width, refLineArgs.Height));

            lengthOfCoordinate = refLineArgs.ActualLength / distance;
            coordinateLength = lengthOfCoordinate;

            // Draw the image ref line onto a every frame!
            var widthRatio = (double)image.Width / (double)refLineArgs.CanvasSize.X;
            var heightRatio = (double)image.Height / (double)refLineArgs.CanvasSize.Y;
            var refPoint1 = new OpenCvSharp.Point(refLineArgs.X * widthRatio, refLineArgs.Y * heightRatio);
            var refPoint2 = new OpenCvSharp.Point(refLineArgs.Width * widthRatio, refLineArgs.Height * heightRatio);
            Cv2.Line(image, refPoint1, refPoint2, Scalar.Lime, 2);

            _referenceLine = new Line()
            {
                X1 = refPoint1.X,
                Y1 = refPoint1.Y,
                X2 = refPoint2.X,
                Y2 = refPoint2.Y
            };

            // Get the center of the line and draw the actual length into the center.
            var center = GeometryHelper.GetCenterOfLine(
                new System.Windows.Point(refPoint1.X, refPoint1.Y),
                new System.Windows.Point(refPoint2.X, refPoint2.Y));

            DrawTextblockWithBackground(
                image,
                center,
                $"{string.Format("{0:0.00}", refLineArgs.ActualLength)}m",
                Scalar.Lime
                );
        }

        /// <summary>
        /// Draws the connection line between two items and also the lenght
        /// </summary>
        /// <param name="uiImage"></param>
        /// <param name="item"></param>
        /// <param name="item2"></param>
        public void DrawLineFromItemToItem(Mat uiImage, YoloItem item, YoloItem item2, out Line line)
        {
            var startPoint = new OpenCvSharp.Point(item.Center().X * SCALE, item.Center().Y * SCALE);
            var endPoint = new OpenCvSharp.Point(item2.Center().X * SCALE, item2.Center().Y * SCALE);

            line = new Line()
            {
                X1 = item.Center().X * SCALE,
                Y1 = item.Center().Y * SCALE,
                X2 = item2.Center().X * SCALE,
                Y2 = item2.Center().Y * SCALE
            };

            // Draw the line
            Cv2.Line(uiImage,
                startPoint,
                endPoint,
                Scalar.Red, 2);

            // Draw distance and length
            // Casting from points to points is really annoying... TODO: Refactor that.
            var sysStartPoint = new System.Windows.Point(startPoint.X, startPoint.Y);
            var sysEndPoint = new System.Windows.Point(endPoint.X, endPoint.Y);
            var distance = GeometryHelper.Distance(sysStartPoint, sysEndPoint);
            var length = distance * coordinateLength;
            var centerOfLine = GeometryHelper.GetCenterOfLine(sysStartPoint, sysEndPoint);

            // Put text in it.
            DrawTextblockWithBackground(
                uiImage,
                centerOfLine,
                $"{string.Format("{0:0.00}", length)}m",
                Scalar.Red
                );
        }

        /// <summary>
        /// Draws the distance of the given point to the reference line and adds teh angle
        /// </summary>
        /// <param name="uiImage"></param>
        /// <param name="center"></param>
        public void DrawDistanceAndAngleToReferenceLine(Mat uiImage, Point center)
        {
            if (_referenceLine == null) return;

            var sysCenter = new System.Windows.Point(center.X, center.Y);

            // Distance 1 of objects ref line to image refernce line
            var dist = GeometryHelper.GetDistanceFromPointToLine(
                sysCenter,
                new Line()
                {
                    X1 = _referenceLine.X1,
                    Y1 = _referenceLine.Y1,
                    X2 = _referenceLine.X2,
                    Y2 = _referenceLine.Y2
                },
                out var closestPoint);

            // Draw the line from given lien to closes point of ref line.
            var connectionLine = new Line()
            {
                X1 = closestPoint.X,
                Y1 = closestPoint.Y,
                X2 = center.X,
                Y2 = center.Y
            };
            // actuall draw it
            Cv2.Line(uiImage,
                new OpenCvSharp.Point(closestPoint.X, closestPoint.Y),
                center,
                Scalar.Green, 2);

            // Get the distance and length and draw it
            var distance = GeometryHelper.Distance(closestPoint, sysCenter);
            var actualLength = distance * coordinateLength;
            var centerOfLine = GeometryHelper.GetCenterOfLine(closestPoint, sysCenter);

            // Put text in it.
            DrawTextblockWithBackground(
                uiImage,
                centerOfLine,
                $"{string.Format("{0:0.00}", actualLength)}m",
                Scalar.Green
                );

            // Not get angle and drwa it too
            // Now also get angle and draw it
            var minorAngle = GeometryHelper.GetAngleBetweenTwoLines(_referenceLine, connectionLine);
            double majorAngle = 360 - minorAngle;
            var smallestAngle = minorAngle < majorAngle ? minorAngle : majorAngle;

            // Draw angle
            DrawTextblockWithBackground(
                uiImage,
                closestPoint,
                $"{(int)smallestAngle}",
                Scalar.Green
                );
        }

        /// <summary>
        /// Draws the angle and distance from the given line to the images reference line.
        /// </summary>
        /// <param name="line"></param>
        public void DrawDistanceAndAngleToReferenceLine(Mat uiImage, Line line)
        {
            if (_referenceLine == null) return;

            var point1 = new System.Windows.Point(line.X1, line.Y1);
            var point2 = new System.Windows.Point(line.X2, line.Y2);

            // Distance 1 of objects ref line to image refernce line
            var dist1 = GeometryHelper.GetDistanceFromPointToLine(
                point1,
                new Line()
                {
                    X1 = _referenceLine.X1,
                    Y1 = _referenceLine.Y1,
                    X2 = _referenceLine.X2,
                    Y2 = _referenceLine.Y2
                },
                out var closestPoint1);

            // Distance 1 of objects ref line to image refernce line
            var dist2 = GeometryHelper.GetDistanceFromPointToLine(
                point2,
                new Line()
                {
                    X1 = _referenceLine.X1,
                    Y1 = _referenceLine.Y1,
                    X2 = _referenceLine.X2,
                    Y2 = _referenceLine.Y2
                },
                out var closestPoint2);

            // Get the closest of the 2 possible closes points
            var closestPointOnRefLine = dist1 < dist2 ? closestPoint1 : closestPoint2;
            var closesPointOfLine = dist1 < dist2 ? point1 : point2;

            // Draw the line from given lien to closes point of ref line.
            Cv2.Line(uiImage,
                new OpenCvSharp.Point(closesPointOfLine.X, closesPointOfLine.Y),
                new OpenCvSharp.Point(closestPointOnRefLine.X, closestPointOnRefLine.Y),
                Scalar.Green, 2);

            var distance = GeometryHelper.Distance(closesPointOfLine, closestPointOnRefLine);
            var actualLength = distance * coordinateLength;
            var centerOfLine = GeometryHelper.GetCenterOfLine(closestPointOnRefLine, closesPointOfLine);

            // Put text in it.
            DrawTextblockWithBackground(
                uiImage,
                centerOfLine,
                $"{string.Format("{0:0.00}", actualLength)}m",
                Scalar.Green
                );

            // Now also get angle and draw it
            var minorAngle = GeometryHelper.GetAngleBetweenTwoLines(line, _referenceLine);
            double majorAngle = 360 - minorAngle;
            
            // Draw angle
            DrawTextblockWithBackground(
                uiImage,
                closestPointOnRefLine,
                $"{Math.Round(minorAngle, 4)}° | {Math.Round(majorAngle, 4)}",
                Scalar.Green
                );
        }

        /// <summary>
        /// Wraps the Cv2.PutText method but draws a background
        /// </summary>
        public void DrawTextblockWithBackground(Mat uiImage, System.Windows.Point point, string text, Scalar color)
        {
            // Draw a rectangle behind the textblock
            // This should have the same fontsize as the textblock we draw. We need to measure the string.
            var font = new System.Drawing.Font("Arial", 19.0F);
            var textSize = TextRenderer.MeasureText(text, font);

            Cv2.Rectangle(uiImage, 
                new OpenCvSharp.Rect((int)point.X, (int)point.Y - (int)(textSize.Height / 1.5), textSize.Width, textSize.Height), 
                Scalar.White, 
                -1);

            Cv2.PutText(uiImage,
                text,
                new OpenCvSharp.Point(point.X, point.Y),
                HersheyFonts.HersheyDuplex,
                0.75f,
                color,
                2);
        }

    }
}
