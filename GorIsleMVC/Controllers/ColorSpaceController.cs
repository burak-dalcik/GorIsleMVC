using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ColorSpaceController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ColorSpaceController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Convert(IFormFile imageFile, string conversionType)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction("Index");
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var originalImage = Image.FromStream(stream))
                    {
                        originalImage.Save(originalPath, ImageFormat.Png);

                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var convertedImage = conversionType switch
                            {
                                "RGB->YUV" => RGBtoYUV(bitmap),
                                "YUV->RGB" => YUVtoRGB(bitmap),
                                _ => throw new ArgumentException("Geçersiz dönüşüm tipi")
                            };

                            var resultFileName = $"converted_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            convertedImage.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                            TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                            TempData["ConversionType"] = conversionType;
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görüntü işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private Bitmap RGBtoYUV(Bitmap rgb)
        {
            int width = rgb.Width;
            int height = rgb.Height;
            Bitmap result = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = rgb.GetPixel(x, y);
                    
                    // RGB -> YUV dönüşüm formülleri
                    byte Y = (byte)(0.299 * pixel.R + 0.587 * pixel.G + 0.114 * pixel.B);
                    byte U = (byte)(128 - 0.168736 * pixel.R - 0.331264 * pixel.G + 0.5 * pixel.B);
                    byte V = (byte)(128 + 0.5 * pixel.R - 0.418688 * pixel.G - 0.081312 * pixel.B);

                    result.SetPixel(x, y, Color.FromArgb(Y, U, V));
                }
            }

            return result;
        }

        private Bitmap YUVtoRGB(Bitmap yuv)
        {
            int width = yuv.Width;
            int height = yuv.Height;
            Bitmap result = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = yuv.GetPixel(x, y);
                    
                    double Y = pixel.R; // Y bileşeni
                    double U = pixel.G - 128; // U bileşeni
                    double V = pixel.B - 128; // V bileşeni

                    // YUV -> RGB dönüşüm formülleri
                    byte R = (byte)Math.Max(0, Math.Min(255, Y + 1.402 * V));
                    byte G = (byte)Math.Max(0, Math.Min(255, Y - 0.344136 * U - 0.714136 * V));
                    byte B = (byte)Math.Max(0, Math.Min(255, Y + 1.772 * U));

                    result.SetPixel(x, y, Color.FromArgb(R, G, B));
                }
            }

            return result;
        }
    }
} 