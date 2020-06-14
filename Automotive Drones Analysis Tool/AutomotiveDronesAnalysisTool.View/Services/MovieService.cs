using AForge.Video.FFMPEG;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace AutomotiveDronesAnalysisTool.View.Services
{
    /// <summary>
    /// This was done a haze and is not wokring right now.
    /// </summary>
    [Obsolete]
    public class MovieService : ServiceBase
    {
        /// <summary>
        /// COnverts a byteareray to a bitmap
        /// </summary>
        /// <param name="byteArrayIn"></param>
        /// <returns></returns>
        public Bitmap ToBitmap(byte[] byteArrayIn)
        {
            using(var ms = new System.IO.MemoryStream(byteArrayIn))
            {
                var returnImage = System.Drawing.Image.FromStream(ms);
                var bitmap = new System.Drawing.Bitmap(returnImage);
                return bitmap;
            }
        }

        /// <summary>
        /// Reduces the bitmaps quality
        /// </summary>
        /// <param name="original"></param>
        /// <param name="reducedWidth"></param>
        /// <param name="reducedHeight"></param>
        /// <returns></returns>
        public Bitmap ReduceBitmap(Bitmap original, int reducedWidth, int reducedHeight)
        {
            var reduced = new Bitmap(reducedWidth, reducedHeight);
            using (var dc = Graphics.FromImage(reduced))
            {
                // you might want to change properties like
                dc.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                dc.DrawImage(original, new Rectangle(0, 0, reducedWidth, reducedHeight), new Rectangle(0, 0, original.Width, original.Height), GraphicsUnit.Pixel);
            }

            return reduced;
        }

        /// <summary>
        /// Creates a move out of images lying in the given path
        /// </summary>
        /// <param name="path"></param>
        public void CreateMovie(List<Bitmap> bitmaps)
        {
            int width = 320;
            int height = 240;
            var framRate = 200;

            // create instance of video writer
            using (var vFWriter = new VideoFileWriter())
            {
                // create new video file
                vFWriter.Open("E:\\test.avi", width, height, framRate, VideoCodec.Raw);

                //loop throught all images in the collection
                foreach (var bitmap in bitmaps)
                {
                    var bmpReduced = ReduceBitmap(bitmap, width, height);
                    vFWriter.WriteVideoFrame(bmpReduced);
                }
                vFWriter.Close();
            }
        }
    }
}
