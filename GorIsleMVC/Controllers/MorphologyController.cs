using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

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
        public async Task<IActionResult> ApplyMorphology(IFormFile imageFile, string operation, int kernelSize = 3)
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
                            // Morfolojik işlemi uygula
                            var resultBitmap = operation.ToLower() switch
                            {
                                "dilation" => ApplyDilation(bitmap, kernelSize),
                                "erosion" => ApplyErosion(bitmap, kernelSize),
                                "opening" => ApplyOpening(bitmap, kernelSize),
                                "closing" => ApplyClosing(bitmap, kernelSize),
                                _ => throw new ArgumentException("Geçersiz morfolojik işlem.")
                            };

                            // Sonucu kaydet
                            var resultFileName = $"morphology_{operation}_{kernelSize}_{DateTime.Now:yyyyMMddHHmmss}.png";
                            var resultPath = Path.Combine(uploadsFolder, resultFileName);
                            resultBitmap.Save(resultPath, ImageFormat.Png);

                            TempData["OriginalImage"] = "/uploads/" + originalFileName;
                            TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                            TempData["Operation"] = operation;
                            TempData["KernelSize"] = kernelSize;
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplyDilation(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            var resultBitmap = new Bitmap(width, height);
            int offset = kernelSize / 2;

            // Önce görüntüyü ikili (binary) formata çevir
            var binaryBitmap = ConvertToBinary(sourceBitmap);

            // Genişleme işlemi
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool hasWhitePixel = false;

                    // Kernel içindeki pikselleri kontrol et
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            var pixel = binaryBitmap.GetPixel(x + i, y + j);
                            if (pixel.R == 255) // Beyaz piksel bulundu
                            {
                                hasWhitePixel = true;
                                break;
                            }
                        }
                        if (hasWhitePixel) break;
                    }

                    resultBitmap.SetPixel(x, y, hasWhitePixel ? Color.White : Color.Black);
                }
            }

            return resultBitmap;
        }

        private Bitmap ApplyErosion(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            var resultBitmap = new Bitmap(width, height);
            int offset = kernelSize / 2;

            // Önce görüntüyü ikili (binary) formata çevir
            var binaryBitmap = ConvertToBinary(sourceBitmap);

            // Aşınma işlemi
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool allWhitePixels = true;

                    // Kernel içindeki pikselleri kontrol et
                    for (int i = -offset; i <= offset && allWhitePixels; i++)
                    {
                        for (int j = -offset; j <= offset && allWhitePixels; j++)
                        {
                            var pixel = binaryBitmap.GetPixel(x + i, y + j);
                            if (pixel.R == 0) // Siyah piksel bulundu
                            {
                                allWhitePixels = false;
                                break;
                            }
                        }
                    }

                    resultBitmap.SetPixel(x, y, allWhitePixels ? Color.White : Color.Black);
                }
            }

            return resultBitmap;
        }

        private Bitmap ApplyOpening(Bitmap sourceBitmap, int kernelSize)
        {
            // Açma = Aşınma + Genişleme
            var erodedImage = ApplyErosion(sourceBitmap, kernelSize);
            return ApplyDilation(erodedImage, kernelSize);
        }

        private Bitmap ApplyClosing(Bitmap sourceBitmap, int kernelSize)
        {
            // Kapama = Genişleme + Aşınma
            var dilatedImage = ApplyDilation(sourceBitmap, kernelSize);
            return ApplyErosion(dilatedImage, kernelSize);
        }

        private Bitmap ConvertToBinary(Bitmap sourceBitmap, int threshold = 128)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            var resultBitmap = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = sourceBitmap.GetPixel(x, y);
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    resultBitmap.SetPixel(x, y, grayValue >= threshold ? Color.White : Color.Black);
                }
            }

            return resultBitmap;
        }
    }
} 