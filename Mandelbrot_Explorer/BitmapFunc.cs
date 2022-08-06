using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace Mandelbrot_Explorer
{
    static class BitmapFunc
    {
        /// <summary>
        /// Writes the bitmap write over source, at the given (x, y) position.
        /// </summary>
        /// <param name="source">The original bitmap to be overwritten.</param>
        /// <param name="write">The bitmap to be drawn.</param>
        /// <param name="x">The x-coordinate of the point to start overwriting.</param>
        /// <param name="y">The y-coordinate of the point to start overwriting.</param>
        /// <returns></returns>
        public static Bitmap PartialOverwrite(Bitmap source, Bitmap write, int x, int y)
        {
            if (write == null)
            {
                return source;
            }
            if (source.Width < write.Width + x || source.Height < write.Height + y)
                return source;

            Graphics g = Graphics.FromImage(source);
            g.DrawImage(write, x, y, write.Width, write.Height);

            return source;
        }

        /// <summary>
        /// Resize the image to the specified width and height.
        /// </summary>
        /// <param name="image">The image to resize.</param>
        /// <param name="width">The width to resize to.</param>
        /// <param name="height">The height to resize to.</param>
        /// <returns>The resized image.</returns>
        public static Bitmap ResizeImage(Image image, int width, int height)
        {
            var destRect = new Rectangle(0, 0, width, height);
            var destImage = new Bitmap(width, height);

            destImage.SetResolution(image.HorizontalResolution, image.VerticalResolution);

            using (var graphics = Graphics.FromImage(destImage))
            {
                graphics.CompositingMode = CompositingMode.SourceCopy;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                using (var wrapMode = new ImageAttributes())
                {
                    wrapMode.SetWrapMode(WrapMode.TileFlipXY);
                    graphics.DrawImage(image, destRect, 0, 0, image.Width, image.Height, GraphicsUnit.Pixel, wrapMode);
                }
            }

            return destImage;
        }
    }
}