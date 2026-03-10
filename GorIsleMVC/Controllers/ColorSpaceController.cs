using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

        public IActionResult Upload()
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

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                await originalImage.SaveAsPngAsync(originalPath);

                Image<Rgba32> convertedImage = conversionType switch
                {
                    "RGB->YUV" => RgbToYuv(originalImage),
                    "YUV->RGB" => YuvToRgb(originalImage),
                    _ => throw new ArgumentException("Geçersiz dönüşüm tipi")
                };

                using (convertedImage)
                {
                    var resultFileName = $"converted_{DateTime.Now:yyyyMMddHHmmss}.png";
                    var resultPath = Path.Combine(uploadsFolder, resultFileName);
                    await convertedImage.SaveAsPngAsync(resultPath);

                    TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                    TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                    TempData["ConversionType"] = conversionType;
                }
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görüntü işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> RgbToYuv(Image<Rgba32> rgb)
        {
            int width = rgb.Width, height = rgb.Height;
            var result = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = rgb[x, y];
                    byte Y = (byte)(0.299 * p.R + 0.587 * p.G + 0.114 * p.B);
                    byte U = (byte)(128 - 0.168736 * p.R - 0.331264 * p.G + 0.5 * p.B);
                    byte V = (byte)(128 + 0.5 * p.R - 0.418688 * p.G - 0.081312 * p.B);
                    result[x, y] = new Rgba32(Y, U, V, p.A);
                }
            }
            return result;
        }

        private static Image<Rgba32> YuvToRgb(Image<Rgba32> yuv)
        {
            int width = yuv.Width, height = yuv.Height;
            var result = new Image<Rgba32>(width, height);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    var p = yuv[x, y];
                    double Y = p.R, U = p.G - 128, V = p.B - 128;
                    byte R = (byte)Math.Clamp(Y + 1.402 * V, 0, 255);
                    byte G = (byte)Math.Clamp(Y - 0.344136 * U - 0.714136 * V, 0, 255);
                    byte B = (byte)Math.Clamp(Y + 1.772 * U, 0, 255);
                    result[x, y] = new Rgba32(R, G, B, p.A);
                }
            }
            return result;
        }
    }
}
