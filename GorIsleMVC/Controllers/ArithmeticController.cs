using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;

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
                    using var img1 = await Image.LoadAsync<Rgba32>(stream1);
                    await img1.SaveAsPngAsync(image1Path);
                }

                var image2FileName = $"arithmetic2_{DateTime.Now:yyyyMMddHHmmss}.png";
                var image2Path = Path.Combine(uploadsFolder, image2FileName);
                using (var stream2 = new MemoryStream())
                {
                    await imageFile2.CopyToAsync(stream2);
                    stream2.Position = 0;
                    using var img2 = await Image.LoadAsync<Rgba32>(stream2);
                    await img2.SaveAsPngAsync(image2Path);
                }

                using var bitmap1 = await Image.LoadAsync<Rgba32>(image1Path);
                using var bitmap2 = await Image.LoadAsync<Rgba32>(image2Path);

                int maxWidth = Math.Max(bitmap1.Width, bitmap2.Width);
                int maxHeight = Math.Max(bitmap1.Height, bitmap2.Height);

                using var resized1 = bitmap1.Clone(ctx => ctx.Resize(maxWidth, maxHeight));
                using var resized2 = bitmap2.Clone(ctx => ctx.Resize(maxWidth, maxHeight));

                using var resultImage = ApplyArithmeticOperation(resized1, resized2, operation);
                var resultFileName = $"result_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                await resultImage.SaveAsPngAsync(resultPath);

                TempData["Image1"] = $"/uploads/{image1FileName}";
                TempData["Image2"] = $"/uploads/{image2FileName}";
                TempData["Result"] = $"/uploads/{resultFileName}";
                TempData["Operation"] = operation;
                TempData["ProcessType"] = "Görüntü Aritmetik İşlemi";
                TempData["OriginalSizes"] = $"Görüntü 1: {bitmap1.Width}x{bitmap1.Height}, Görüntü 2: {bitmap2.Width}x{bitmap2.Height}";
                TempData["FinalSize"] = $"İşlem Boyutu: {maxWidth}x{maxHeight}";
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyArithmeticOperation(Image<Rgba32> image1, Image<Rgba32> image2, string operation)
        {
            int width = image1.Width, height = image1.Height;
            var result = new Image<Rgba32>(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p1 = image1[x, y];
                    var p2 = image2[x, y];
                    int r, g, b;
                    switch (operation.ToLower())
                    {
                        case "add":
                            r = Math.Min(255, p1.R + p2.R);
                            g = Math.Min(255, p1.G + p2.G);
                            b = Math.Min(255, p1.B + p2.B);
                            break;
                        case "subtract":
                            r = Math.Max(0, p1.R - p2.R);
                            g = Math.Max(0, p1.G - p2.G);
                            b = Math.Max(0, p1.B - p2.B);
                            break;
                        case "multiply":
                            r = Math.Min(255, (p1.R * p2.R) / 255);
                            g = Math.Min(255, (p1.G * p2.G) / 255);
                            b = Math.Min(255, (p1.B * p2.B) / 255);
                            break;
                        case "average":
                            r = (p1.R + p2.R) / 2;
                            g = (p1.G + p2.G) / 2;
                            b = (p1.B + p2.B) / 2;
                            break;
                        default:
                            throw new ArgumentException("Geçersiz işlem türü");
                    }
                    result[x, y] = new Rgba32((byte)r, (byte)g, (byte)b, p1.A);
                }
            }
            return result;
        }
    }
}
