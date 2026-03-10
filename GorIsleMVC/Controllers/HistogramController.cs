using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
        public async Task<IActionResult> Upload(IFormFile imageFile)
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

            await using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await imageFile.CopyToAsync(fileStream);
            }

            using (var image = await Image.LoadAsync<Rgba32>(filePath))
            {
                int[] redHistogram = new int[256], greenHistogram = new int[256];
                int[] blueHistogram = new int[256], grayHistogram = new int[256];

                for (int i = 0; i < image.Width; i++)
                {
                    for (int j = 0; j < image.Height; j++)
                    {
                        var p = image[i, j];
                        redHistogram[p.R]++;
                        greenHistogram[p.G]++;
                        blueHistogram[p.B]++;
                        int gray = (p.R + p.G + p.B) / 3;
                        grayHistogram[gray]++;
                    }
                }

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
                return RedirectToAction("Index");
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Equalize(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                TempData["Error"] = "Görüntü bulunamadı.";
                return RedirectToAction("Index");
            }

            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, imagePath.TrimStart('/'));
            if (!System.IO.File.Exists(fullPath))
            {
                TempData["Error"] = "Görüntü dosyası bulunamadı.";
                return RedirectToAction("Index");
            }

            var outputFileName = "equalized_" + Path.GetFileName(imagePath);
            var outputPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", outputFileName);

            using (var bitmap = await Image.LoadAsync<Rgba32>(fullPath))
            {
                int[] histogram = new int[256], cdf = new int[256];
                int totalPixels = bitmap.Width * bitmap.Height;

                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        var p = bitmap[i, j];
                        int gray = (p.R + p.G + p.B) / 3;
                        histogram[gray]++;
                    }
                }

                cdf[0] = histogram[0];
                for (int i = 1; i < 256; i++)
                    cdf[i] = cdf[i - 1] + histogram[i];

                var equalizedImage = new Image<Rgba32>(bitmap.Width, bitmap.Height);
                for (int i = 0; i < bitmap.Width; i++)
                {
                    for (int j = 0; j < bitmap.Height; j++)
                    {
                        var p = bitmap[i, j];
                        int gray = (p.R + p.G + p.B) / 3;
                        int newValue = (int)((cdf[gray] * 255.0) / totalPixels);
                        byte n = (byte)Math.Clamp(newValue, 0, 255);
                        equalizedImage[i, j] = new Rgba32(n, n, n, p.A);
                    }
                }
                await equalizedImage.SaveAsJpegAsync(outputPath);
                equalizedImage.Dispose();
            }

            TempData["OriginalImage"] = imagePath;
            TempData["EqualizedImage"] = "/uploads/" + outputFileName;
            return RedirectToAction("Result");
        }
    }
}
