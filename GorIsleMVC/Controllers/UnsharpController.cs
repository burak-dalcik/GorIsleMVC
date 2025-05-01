using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class UnsharpController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessImage(IFormFile imageFile, double amount, int radius, int threshold)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    imageFile.CopyTo(ms);
                    using (var originalImage = Image.FromStream(ms))
                    {
                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var processedImage = ApplyUnsharpMask(bitmap, amount, radius, threshold);

                            using (var resultStream = new MemoryStream())
                            {
                                processedImage.Save(resultStream, ImageFormat.Jpeg);
                                resultStream.Position = 0;
                                return File(resultStream.ToArray(), "image/jpeg", "unsharp_" + imageFile.FileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplyUnsharpMask(Bitmap original, double amount, int radius, int threshold)
        {
            int width = original.Width;
            int height = original.Height;
            
            // Bulanık görüntü oluştur
            var blurred = ApplyGaussianBlur(original, radius);
            
            // Keskinleştirilmiş görüntü
            Bitmap result = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color originalColor = original.GetPixel(x, y);
                    Color blurredColor = blurred.GetPixel(x, y);

                    // Her renk kanalı için keskinleştirme uygula
                    int r = ApplyUnsharpMaskToChannel(originalColor.R, blurredColor.R, amount, threshold);
                    int g = ApplyUnsharpMaskToChannel(originalColor.G, blurredColor.G, amount, threshold);
                    int b = ApplyUnsharpMaskToChannel(originalColor.B, blurredColor.B, amount, threshold);

                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }

        private int ApplyUnsharpMaskToChannel(int original, int blurred, double amount, int threshold)
        {
            int difference = original - blurred;

            // Eşik değeri kontrolü
            if (Math.Abs(difference) < threshold)
                return original;

            // Keskinleştirme formülü: Original + (Original - Blurred) * Amount
            int sharpened = (int)(original + difference * amount);
            return Math.Min(255, Math.Max(0, sharpened));
        }

        private Bitmap ApplyGaussianBlur(Bitmap original, int radius)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);

            // Gaussian kernel oluştur
            double[,] kernel = CreateGaussianKernel(radius);
            int kernelSize = radius * 2 + 1;
            int kernelRadius = kernelSize / 2;

            for (int x = kernelRadius; x < width - kernelRadius; x++)
            {
                for (int y = kernelRadius; y < height - kernelRadius; y++)
                {
                    double r = 0, g = 0, b = 0;
                    double weightSum = 0;

                    // Kernel'i uygula
                    for (int i = -kernelRadius; i <= kernelRadius; i++)
                    {
                        for (int j = -kernelRadius; j <= kernelRadius; j++)
                        {
                            Color pixel = original.GetPixel(x + i, y + j);
                            double weight = kernel[i + kernelRadius, j + kernelRadius];

                            r += pixel.R * weight;
                            g += pixel.G * weight;
                            b += pixel.B * weight;
                            weightSum += weight;
                        }
                    }

                    // Normalize et
                    r /= weightSum;
                    g /= weightSum;
                    b /= weightSum;

                    result.SetPixel(x, y, Color.FromArgb(
                        (int)Math.Round(r),
                        (int)Math.Round(g),
                        (int)Math.Round(b)));
                }
            }

            // Kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < kernelRadius; y++)
                {
                    result.SetPixel(x, y, original.GetPixel(x, y));
                    result.SetPixel(x, height - 1 - y, original.GetPixel(x, height - 1 - y));
                }
            }

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < kernelRadius; x++)
                {
                    result.SetPixel(x, y, original.GetPixel(x, y));
                    result.SetPixel(width - 1 - x, y, original.GetPixel(width - 1 - x, y));
                }
            }

            return result;
        }

        private double[,] CreateGaussianKernel(int radius)
        {
            int size = radius * 2 + 1;
            double[,] kernel = new double[size, size];
            double sigma = radius / 3.0;
            double sum = 0;

            for (int x = -radius; x <= radius; x++)
            {
                for (int y = -radius; y <= radius; y++)
                {
                    double exponent = -(x * x + y * y) / (2 * sigma * sigma);
                    kernel[x + radius, y + radius] = Math.Exp(exponent) / (2 * Math.PI * sigma * sigma);
                    sum += kernel[x + radius, y + radius];
                }
            }

            // Normalize kernel
            for (int x = 0; x < size; x++)
            {
                for (int y = 0; y < size; y++)
                {
                    kernel[x, y] /= sum;
                }
            }

            return kernel;
        }
    }
} 