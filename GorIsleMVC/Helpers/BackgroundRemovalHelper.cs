using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GorIsleMVC.Helpers
{
    public static class BackgroundRemovalHelper
    {
        /// <summary>
        /// ImageSharp kullanır; Windows ve Linux/Docker'da libgdiplus olmadan çalışır.
        /// </summary>
        public static Image<Rgba32> RemoveBackground(Image<Rgba32> original, byte threshold)
        {
            int width = original.Width;
            int height = original.Height;

            long sumR = 0, sumG = 0, sumB = 0;
            long count = 0;

            int step = Math.Max(1, Math.Min(width, height) / 100);
            if (step < 2) step = 2;

            for (int x = 0; x < width; x += step)
            {
                var top = original[x, 0];
                var bottom = original[x, height - 1];
                sumR += top.R + bottom.R;
                sumG += top.G + bottom.G;
                sumB += top.B + bottom.B;
                count += 2;
            }

            for (int y = 0; y < height; y += step)
            {
                var left = original[0, y];
                var right = original[width - 1, y];
                sumR += left.R + right.R;
                sumG += left.G + right.G;
                sumB += left.B + right.B;
                count += 2;
            }

            if (count == 0)
                return original.Clone();

            byte bgR = (byte)(sumR / count);
            byte bgG = (byte)(sumG / count);
            byte bgB = (byte)(sumB / count);

            int toleranceSquared = threshold * threshold * 3;

            var result = new Image<Rgba32>(width, height);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var pixel = original[x, y];
                    int dR = pixel.R - bgR;
                    int dG = pixel.G - bgG;
                    int dB = pixel.B - bgB;
                    int distanceSquared = dR * dR + dG * dG + dB * dB;

                    byte a = (byte)(distanceSquared <= toleranceSquared ? 0 : 255);
                    result[x, y] = new Rgba32(pixel.R, pixel.G, pixel.B, a);
                }
            }

            return result;
        }
    }
}
