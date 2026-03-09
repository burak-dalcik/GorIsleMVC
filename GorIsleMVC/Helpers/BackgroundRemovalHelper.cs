using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Helpers
{
    public static class BackgroundRemovalHelper
    {
        public static Bitmap RemoveBackground(Bitmap original, byte threshold)
        {
            int width = original.Width;
            int height = original.Height;

            var result = new Bitmap(width, height, PixelFormat.Format32bppArgb);

            long sumR = 0;
            long sumG = 0;
            long sumB = 0;
            long count = 0;

            int step = Math.Max(1, Math.Min(width, height) / 100);
            if (step < 2)
            {
                step = 2;
            }

            for (int x = 0; x < width; x += step)
            {
                Color topPixel = original.GetPixel(x, 0);
                Color bottomPixel = original.GetPixel(x, height - 1);

                sumR += topPixel.R + bottomPixel.R;
                sumG += topPixel.G + bottomPixel.G;
                sumB += topPixel.B + bottomPixel.B;
                count += 2;
            }

            for (int y = 0; y < height; y += step)
            {
                Color leftPixel = original.GetPixel(0, y);
                Color rightPixel = original.GetPixel(width - 1, y);

                sumR += leftPixel.R + rightPixel.R;
                sumG += leftPixel.G + rightPixel.G;
                sumB += leftPixel.B + rightPixel.B;
                count += 2;
            }

            if (count == 0)
            {
                return original;
            }

            byte bgR = (byte)(sumR / count);
            byte bgG = (byte)(sumG / count);
            byte bgB = (byte)(sumB / count);

            int tolerance = threshold;
            int toleranceSquared = tolerance * tolerance * 3;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    Color pixel = original.GetPixel(x, y);

                    int dR = pixel.R - bgR;
                    int dG = pixel.G - bgG;
                    int dB = pixel.B - bgB;

                    int distanceSquared = dR * dR + dG * dG + dB * dB;

                    if (distanceSquared <= toleranceSquared)
                    {
                        result.SetPixel(x, y, Color.FromArgb(0, pixel.R, pixel.G, pixel.B));
                    }
                    else
                    {
                        result.SetPixel(x, y, Color.FromArgb(255, pixel.R, pixel.G, pixel.B));
                    }
                }
            }

            return result;
        }
    }
}

