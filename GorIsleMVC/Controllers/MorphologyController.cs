using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace GorIsleMVC.Controllers
{
    public class MorphologyController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public MorphologyController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyMorphology(IFormFile imageFile, string operation, int kernelSize = 3, bool isColorImage = true)
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

                Image<Rgba32> resultBitmap = operation.ToLower() switch
                {
                    "dilation" => isColorImage ? ApplyColorDilation(originalImage, kernelSize) : ApplyDilation(originalImage, kernelSize),
                    "erosion" => isColorImage ? ApplyColorErosion(originalImage, kernelSize) : ApplyErosion(originalImage, kernelSize),
                    "opening" => isColorImage ? ApplyColorOpening(originalImage, kernelSize) : ApplyOpening(originalImage, kernelSize),
                    "closing" => isColorImage ? ApplyColorClosing(originalImage, kernelSize) : ApplyClosing(originalImage, kernelSize),
                    _ => throw new ArgumentException("Geçersiz morfolojik işlem.")
                };

                using (resultBitmap)
                {
                    var resultFileName = $"morphology_{operation}_{kernelSize}_{DateTime.Now:yyyyMMddHHmmss}.png";
                    var resultPath = Path.Combine(uploadsFolder, resultFileName);
                    await resultBitmap.SaveAsPngAsync(resultPath);

                    TempData["OriginalImage"] = "/uploads/" + originalFileName;
                    TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                    TempData["Operation"] = operation;
                    TempData["KernelSize"] = kernelSize;
                    TempData["IsColorImage"] = isColorImage;
                }
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görüntü işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private static Image<Rgba32> ApplyColorDilation(Image<Rgba32> source, int kernelSize)
        {
            int width = source.Width, height = source.Height;
            var result = source.Clone();
            int offset = kernelSize / 2;
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    int maxR = 0, maxG = 0, maxB = 0;
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            var p = source[x + i, y + j];
                            maxR = Math.Max(maxR, p.R); maxG = Math.Max(maxG, p.G); maxB = Math.Max(maxB, p.B);
                        }
                    }
                    result[x, y] = new Rgba32((byte)maxR, (byte)maxG, (byte)maxB, source[x, y].A);
                }
            }
            return result;
        }

        private static Image<Rgba32> ApplyColorErosion(Image<Rgba32> source, int kernelSize)
        {
            int width = source.Width, height = source.Height;
            var result = source.Clone();
            int offset = kernelSize / 2;
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    int minR = 255, minG = 255, minB = 255;
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            var p = source[x + i, y + j];
                            minR = Math.Min(minR, p.R); minG = Math.Min(minG, p.G); minB = Math.Min(minB, p.B);
                        }
                    }
                    result[x, y] = new Rgba32((byte)minR, (byte)minG, (byte)minB, source[x, y].A);
                }
            }
            return result;
        }

        private static Image<Rgba32> ApplyColorOpening(Image<Rgba32> source, int kernelSize)
        {
            using var eroded = ApplyColorErosion(source, kernelSize);
            return ApplyColorDilation(eroded, kernelSize);
        }

        private static Image<Rgba32> ApplyColorClosing(Image<Rgba32> source, int kernelSize)
        {
            using var dilated = ApplyColorDilation(source, kernelSize);
            return ApplyColorErosion(dilated, kernelSize);
        }

        private static Image<Rgba32> ConvertToBinary(Image<Rgba32> source, int threshold = 128)
        {
            int width = source.Width, height = source.Height;
            var result = new Image<Rgba32>(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var p = source[x, y];
                    int gray = (p.R + p.G + p.B) / 3;
                    byte v = (byte)(gray >= threshold ? 255 : 0);
                    result[x, y] = new Rgba32(v, v, v, p.A);
                }
            }
            return result;
        }

        private static Image<Rgba32> ApplyDilation(Image<Rgba32> source, int kernelSize)
        {
            int width = source.Width, height = source.Height;
            using var binary = ConvertToBinary(source);
            var result = binary.Clone();
            int offset = kernelSize / 2;
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool hasWhite = false;
                    for (int i = -offset; i <= offset && !hasWhite; i++)
                        for (int j = -offset; j <= offset && !hasWhite; j++)
                            if (binary[x + i, y + j].R == 255) hasWhite = true;
                    byte v = (byte)(hasWhite ? 255 : 0);
                    result[x, y] = new Rgba32(v, v, v, source[x, y].A);
                }
            }
            return result;
        }

        private static Image<Rgba32> ApplyErosion(Image<Rgba32> source, int kernelSize)
        {
            int width = source.Width, height = source.Height;
            using var binary = ConvertToBinary(source);
            var result = binary.Clone();
            int offset = kernelSize / 2;
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool allWhite = true;
                    for (int i = -offset; i <= offset && allWhite; i++)
                        for (int j = -offset; j <= offset && allWhite; j++)
                            if (binary[x + i, y + j].R == 0) allWhite = false;
                    byte v = (byte)(allWhite ? 255 : 0);
                    result[x, y] = new Rgba32(v, v, v, source[x, y].A);
                }
            }
            return result;
        }

        private static Image<Rgba32> ApplyOpening(Image<Rgba32> source, int kernelSize)
        {
            using var eroded = ApplyErosion(source, kernelSize);
            return ApplyDilation(eroded, kernelSize);
        }

        private static Image<Rgba32> ApplyClosing(Image<Rgba32> source, int kernelSize)
        {
            using var dilated = ApplyDilation(source, kernelSize);
            return ApplyErosion(dilated, kernelSize);
        }
    }
}
