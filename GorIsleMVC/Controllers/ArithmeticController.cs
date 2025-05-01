using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ArithmeticController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public ArithmeticController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile firstImage, IFormFile secondImage, string operation)
        {
            if (firstImage == null || secondImage == null)
            {
                TempData["Error"] = "Lütfen iki görüntü dosyası da seçin.";
                return RedirectToAction("Index");
            }

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            // İlk görüntüyü kaydet
            var firstFileName = Guid.NewGuid().ToString() + "_" + firstImage.FileName;
            var firstFilePath = Path.Combine(uploadsFolder, firstFileName);
            using (var fileStream = new FileStream(firstFilePath, FileMode.Create))
            {
                firstImage.CopyTo(fileStream);
            }

            // İkinci görüntüyü kaydet
            var secondFileName = Guid.NewGuid().ToString() + "_" + secondImage.FileName;
            var secondFilePath = Path.Combine(uploadsFolder, secondFileName);
            using (var fileStream = new FileStream(secondFilePath, FileMode.Create))
            {
                secondImage.CopyTo(fileStream);
            }

            // Sonuç görüntüsünü oluştur
            var resultFileName = $"result_{operation}_{Path.GetFileName(firstFileName)}";
            var resultPath = Path.Combine(uploadsFolder, resultFileName);

            using (var firstBitmap = new Bitmap(firstFilePath))
            using (var secondBitmap = new Bitmap(secondFilePath))
            {
                if (firstBitmap.Width != secondBitmap.Width || firstBitmap.Height != secondBitmap.Height)
                {
                    TempData["Error"] = "Görüntüler aynı boyutta olmalıdır.";
                    return RedirectToAction("Index");
                }

                var resultBitmap = new Bitmap(firstBitmap.Width, firstBitmap.Height);

                for (int x = 0; x < firstBitmap.Width; x++)
                {
                    for (int y = 0; y < firstBitmap.Height; y++)
                    {
                        var firstPixel = firstBitmap.GetPixel(x, y);
                        var secondPixel = secondBitmap.GetPixel(x, y);
                        Color resultPixel;

                        switch (operation.ToLower())
                        {
                            case "add":
                                resultPixel = Color.FromArgb(
                                    Math.Min(255, firstPixel.R + secondPixel.R),
                                    Math.Min(255, firstPixel.G + secondPixel.G),
                                    Math.Min(255, firstPixel.B + secondPixel.B));
                                break;

                            case "subtract":
                                resultPixel = Color.FromArgb(
                                    Math.Max(0, firstPixel.R - secondPixel.R),
                                    Math.Max(0, firstPixel.G - secondPixel.G),
                                    Math.Max(0, firstPixel.B - secondPixel.B));
                                break;

                            case "multiply":
                                resultPixel = Color.FromArgb(
                                    Math.Min(255, (firstPixel.R * secondPixel.R) / 255),
                                    Math.Min(255, (firstPixel.G * secondPixel.G) / 255),
                                    Math.Min(255, (firstPixel.B * secondPixel.B) / 255));
                                break;

                            case "divide":
                                resultPixel = Color.FromArgb(
                                    secondPixel.R == 0 ? 255 : Math.Min(255, (firstPixel.R * 255) / secondPixel.R),
                                    secondPixel.G == 0 ? 255 : Math.Min(255, (firstPixel.G * 255) / secondPixel.G),
                                    secondPixel.B == 0 ? 255 : Math.Min(255, (firstPixel.B * 255) / secondPixel.B));
                                break;

                            default:
                                TempData["Error"] = "Geçersiz işlem.";
                                return RedirectToAction("Index");
                        }

                        resultBitmap.SetPixel(x, y, resultPixel);
                    }
                }

                resultBitmap.Save(resultPath, ImageFormat.Jpeg);
            }

            TempData["FirstImage"] = "/uploads/" + firstFileName;
            TempData["SecondImage"] = "/uploads/" + secondFileName;
            TempData["ResultImage"] = "/uploads/" + resultFileName;
            TempData["Operation"] = operation;

            return RedirectToAction("Result");
        }

        public IActionResult Result()
        {
            if (TempData["ResultImage"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }
    }
} 