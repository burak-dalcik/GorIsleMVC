using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class UnsharpController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public UnsharpController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile, float amount = 1.5f, float threshold = 0)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap resultBitmap = null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Png);

                bitmap = new Bitmap(originalImage);
                resultBitmap = ApplyUnsharpMask(bitmap, amount, threshold);

                var resultFileName = $"unsharp_{amount}_{threshold}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                resultBitmap.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = "/uploads/" + originalFileName;
                TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                TempData["ProcessType"] = "Unsharp Masking";
                TempData["Parameters"] = $"Amount: {amount}, Threshold: {threshold}";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup işlemleri
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                resultBitmap?.Dispose();
            }
        }

        private Bitmap ApplyUnsharpMask(Bitmap sourceBitmap, float amount, float threshold)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] sourcePixels = new byte[width, height, 4, 1];   // ARGB formatında orijinal
            float[,,,] blurredPixels = new float[width, height, 4, 1]; // Gaussian blur sonucu (float precision)
            byte[,,,] resultPixels = new byte[width, height, 4, 1];   // Unsharp mask sonucu

            // Gaussian Blur için 3x3 kernel
            float[,] kernel = {
                { 1/16f, 2/16f, 1/16f },
                { 2/16f, 4/16f, 2/16f },
                { 1/16f, 2/16f, 1/16f }
            };

            // ADIM 1: Orijinal görselin piksellerini array'e aktar
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1, 0] = pixel.R; // Red
                    sourcePixels[x, y, 2, 0] = pixel.G; // Green
                    sourcePixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            // ADIM 2: Array üzerinde Gaussian Blur uygula
            // İlk olarak kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    blurredPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha
                    blurredPixels[x, y, 1, 0] = sourcePixels[x, y, 1, 0]; // Red
                    blurredPixels[x, y, 2, 0] = sourcePixels[x, y, 2, 0]; // Green
                    blurredPixels[x, y, 3, 0] = sourcePixels[x, y, 3, 0]; // Blue
                }
            }

            // Gaussian blur'u iç piksellere uygula (padding=1)
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    float sumR = 0, sumG = 0, sumB = 0;

                    // 3x3 kernel ile convolution
                    for (int ky = -1; ky <= 1; ky++)
                    {
                        for (int kx = -1; kx <= 1; kx++)
                        {
                            int sourceX = x + kx;
                            int sourceY = y + ky;
                            float weight = kernel[ky + 1, kx + 1];

                            // Array'den RGB değerlerini al
                            float red = sourcePixels[sourceX, sourceY, 1, 0];
                            float green = sourcePixels[sourceX, sourceY, 2, 0];
                            float blue = sourcePixels[sourceX, sourceY, 3, 0];

                            sumR += red * weight;
                            sumG += green * weight;
                            sumB += blue * weight;
                        }
                    }

                    // Blur sonucunu array'e kaydet
                    blurredPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha aynı kalır
                    blurredPixels[x, y, 1, 0] = Math.Min(255, Math.Max(0, sumR)); // Red
                    blurredPixels[x, y, 2, 0] = Math.Min(255, Math.Max(0, sumG)); // Green
                    blurredPixels[x, y, 3, 0] = Math.Min(255, Math.Max(0, sumB)); // Blue
                }
            }

            // ADIM 3: Array üzerinde Unsharp Mask uygula
            // İlk olarak kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha
                    resultPixels[x, y, 1, 0] = sourcePixels[x, y, 1, 0]; // Red
                    resultPixels[x, y, 2, 0] = sourcePixels[x, y, 2, 0]; // Green
                    resultPixels[x, y, 3, 0] = sourcePixels[x, y, 3, 0]; // Blue
                }
            }

            // Unsharp mask'ı iç piksellere uygula
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    // Array'den orijinal ve bulanık piksel değerlerini al
                    byte originalR = sourcePixels[x, y, 1, 0];
                    byte originalG = sourcePixels[x, y, 2, 0];
                    byte originalB = sourcePixels[x, y, 3, 0];

                    float blurredR = blurredPixels[x, y, 1, 0];
                    float blurredG = blurredPixels[x, y, 2, 0];
                    float blurredB = blurredPixels[x, y, 3, 0];

                    // Fark hesapla
                    float diffR = originalR - blurredR;
                    float diffG = originalG - blurredG;
                    float diffB = originalB - blurredB;

                    // Threshold kontrolü
                    if (Math.Abs(diffR) > threshold || Math.Abs(diffG) > threshold || Math.Abs(diffB) > threshold)
                    {
                        // Keskinleştirme uygula
                        int newR = (int)Math.Min(255, Math.Max(0, originalR + (diffR * amount)));
                        int newG = (int)Math.Min(255, Math.Max(0, originalG + (diffG * amount)));
                        int newB = (int)Math.Min(255, Math.Max(0, originalB + (diffB * amount)));

                        resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha aynı kalır
                        resultPixels[x, y, 1, 0] = (byte)newR; // Keskinleştirilmiş Red
                        resultPixels[x, y, 2, 0] = (byte)newG; // Keskinleştirilmiş Green
                        resultPixels[x, y, 3, 0] = (byte)newB; // Keskinleştirilmiş Blue
                    }
                    else
                    {
                        // Threshold altındaki pikselleri olduğu gibi kopyala
                        resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha
                        resultPixels[x, y, 1, 0] = sourcePixels[x, y, 1, 0]; // Red
                        resultPixels[x, y, 2, 0] = sourcePixels[x, y, 2, 0]; // Green
                        resultPixels[x, y, 3, 0] = sourcePixels[x, y, 3, 0]; // Blue
                    }
                }
            }

            // ADIM 4: Unsharp mask uygulanmış array'den yeni Bitmap oluştur
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = resultPixels[x, y, 0, 0];
                    byte red = resultPixels[x, y, 1, 0];
                    byte green = resultPixels[x, y, 2, 0];
                    byte blue = resultPixels[x, y, 3, 0];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }
    }
}