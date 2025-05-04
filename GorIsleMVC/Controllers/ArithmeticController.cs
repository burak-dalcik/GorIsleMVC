using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ArithmeticController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ArithmeticController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImages(IFormFile imageFile1, IFormFile imageFile2, string operation)
        {
            if (imageFile1 == null || imageFile2 == null || imageFile1.Length == 0 || imageFile2.Length == 0)
            {
                TempData["Error"] = "Lütfen iki görüntü dosyası da seçin.";
                return RedirectToAction("Index");
            }

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var image1FileName = $"arithmetic1_{DateTime.Now:yyyyMMddHHmmss}.png";
                var image1Path = Path.Combine(uploadsFolder, image1FileName);
                using (var stream1 = new MemoryStream())
                {
                    await imageFile1.CopyToAsync(stream1);
                    stream1.Position = 0;
                    using (var img1 = Image.FromStream(stream1))
                    {
                        img1.Save(image1Path, ImageFormat.Png);
                    }
                }

                var image2FileName = $"arithmetic2_{DateTime.Now:yyyyMMddHHmmss}.png";
                var image2Path = Path.Combine(uploadsFolder, image2FileName);
                using (var stream2 = new MemoryStream())
                {
                    await imageFile2.CopyToAsync(stream2);
                    stream2.Position = 0;
                    using (var img2 = Image.FromStream(stream2))
                    {
                        img2.Save(image2Path, ImageFormat.Png);
                    }
                }

                using (var bitmap1 = new Bitmap(image1Path))
                using (var bitmap2 = new Bitmap(image2Path))
                {
                    int maxWidth = Math.Max(bitmap1.Width, bitmap2.Width);
                    int maxHeight = Math.Max(bitmap1.Height, bitmap2.Height);

                    using (var resized1 = ResizeImage(bitmap1, maxWidth, maxHeight))
                    using (var resized2 = ResizeImage(bitmap2, maxWidth, maxHeight))
                    {
                        var resultFileName = $"result_{DateTime.Now:yyyyMMddHHmmss}.png";
                        var resultPath = Path.Combine(uploadsFolder, resultFileName);

                        // Aritmetik işlemin uygulanması
                        using (var resultImage = ApplyArithmeticOperation(resized1, resized2, operation))
                        {
                            resultImage.Save(resultPath, ImageFormat.Png);

                            TempData["Image1"] = $"/uploads/{image1FileName}";
                            TempData["Image2"] = $"/uploads/{image2FileName}";
                            TempData["Result"] = $"/uploads/{resultFileName}";
                            TempData["Operation"] = operation;
                            TempData["ProcessType"] = "Görüntü Aritmetik İşlemi";
                            TempData["OriginalSizes"] = $"Görüntü 1: {bitmap1.Width}x{bitmap1.Height}, Görüntü 2: {bitmap2.Width}x{bitmap2.Height}";
                            TempData["FinalSize"] = $"İşlem Boyutu: {maxWidth}x{maxHeight}";
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private Bitmap ResizeImage(Image image, int width, int height)
        {
            var result = new Bitmap(width, height);
            using (var graphics = Graphics.FromImage(result))
            {
                // yüksek kaliteli yeniden boyutlandırma ayarları
                graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                graphics.CompositingQuality = CompositingQuality.HighQuality;
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;

                graphics.DrawImage(image, 0, 0, width, height);
            }
            return result;
        }

        private Bitmap ApplyArithmeticOperation(Bitmap image1, Bitmap image2, string operation)
        {
            int width = image1.Width;
            int height = image1.Height;
            Bitmap result = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel1 = image1.GetPixel(x, y);
                    Color pixel2 = image2.GetPixel(x, y);

                    int r, g, b;
                    switch (operation.ToLower())
                    {
                        case "add":
                            r = Math.Min(255, pixel1.R + pixel2.R);
                            g = Math.Min(255, pixel1.G + pixel2.G);
                            b = Math.Min(255, pixel1.B + pixel2.B);
                            break;

                        case "subtract":
                            r = Math.Max(0, pixel1.R - pixel2.R);
                            g = Math.Max(0, pixel1.G - pixel2.G);
                            b = Math.Max(0, pixel1.B - pixel2.B);
                            break;

                        case "multiply":
                            r = Math.Min(255, (pixel1.R * pixel2.R) / 255);
                            g = Math.Min(255, (pixel1.G * pixel2.G) / 255);
                            b = Math.Min(255, (pixel1.B * pixel2.B) / 255);
                            break;

                        case "average":
                            r = (pixel1.R + pixel2.R) / 2;
                            g = (pixel1.G + pixel2.G) / 2;
                            b = (pixel1.B + pixel2.B) / 2;
                            break;

                        default:
                            throw new ArgumentException("Geçersiz işlem türü");
                    }

                    result.SetPixel(x, y, Color.FromArgb(r, g, b));
                }
            }

            return result;
        }
    }
} 