using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class HistogramController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public HistogramController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                imageFile.CopyTo(fileStream);
            }

            using (var bitmap = new Bitmap(filePath))
            {
                // Histogram verilerini hesapla
                int[] redHistogram = new int[256];
                int[] greenHistogram = new int[256];
                int[] blueHistogram = new int[256];
                int[] grayHistogram = new int[256];

                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        Color pixel = bitmap.GetPixel(i, j);
                        redHistogram[pixel.R]++;
                        greenHistogram[pixel.G]++;
                        blueHistogram[pixel.B]++;
                        int gray = (pixel.R + pixel.G + pixel.B) / 3;
                        grayHistogram[gray]++;
                    }
                }

                // Histogram verilerini TempData'ya kaydet
                TempData["RedHistogram"] = string.Join(",", redHistogram);
                TempData["GreenHistogram"] = string.Join(",", greenHistogram);
                TempData["BlueHistogram"] = string.Join(",", blueHistogram);
                TempData["GrayHistogram"] = string.Join(",", grayHistogram);
                TempData["ImagePath"] = "/uploads/" + uniqueFileName;
            }

            return RedirectToAction("Result");
        }

        public IActionResult Result()
        {
            if (TempData["ImagePath"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Equalize(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                TempData["Error"] = "Görüntü bulunamadı.";
                return RedirectToAction("Index");
            }

            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, imagePath.TrimStart('/'));
            var outputFileName = "equalized_" + Path.GetFileName(imagePath);
            var outputPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", outputFileName);

            using (var bitmap = new Bitmap(fullPath))
            {
                // Histogram eşitleme işlemi
                int[] histogram = new int[256];
                int[] cdf = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;

                // Gri tonlamalı histogram hesapla
                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        Color pixel = bitmap.GetPixel(i, j);
                        int gray = (pixel.R + pixel.G + pixel.B) / 3;
                        histogram[gray]++;
                    }
                }

                // CDF hesapla
                cdf[0] = histogram[0];
                for (int i = 1; i < 256; i++)
                {
                    cdf[i] = cdf[i - 1] + histogram[i];
                }

                // Yeni görüntüyü oluştur
                Bitmap equalizedImage = new Bitmap(bitmap.Width, bitmap.Height);
                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        Color pixel = bitmap.GetPixel(i, j);
                        int gray = (pixel.R + pixel.G + pixel.B) / 3;
                        int newValue = (int)((cdf[gray] * 255.0) / totalPixels);
                        equalizedImage.SetPixel(i, j, Color.FromArgb(newValue, newValue, newValue));
                    }
                }

                equalizedImage.Save(outputPath, ImageFormat.Jpeg);
            }

            TempData["OriginalImage"] = imagePath;
            TempData["EqualizedImage"] = "/uploads/" + outputFileName;
            return RedirectToAction("Result");
        }
    }
} 