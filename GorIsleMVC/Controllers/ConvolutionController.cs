using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ConvolutionController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ConvolutionController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ApplyMeanFilter(IFormFile imageFile, int kernelSize = 3)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction("Index");
            }

            if (kernelSize % 2 == 0 || kernelSize < 3 || kernelSize > 9)
            {
                TempData["Error"] = "Filtre boyutu 3, 5, 7 veya 9 olmalıdır.";
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
                resultBitmap = ApplyMean(bitmap, kernelSize);

                var resultFileName = $"mean_filter_{kernelSize}x{kernelSize}_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                resultBitmap.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = "/uploads/" + originalFileName;
                TempData["ProcessedImage"] = "/uploads/" + resultFileName;
                TempData["KernelSize"] = kernelSize;

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

        private Bitmap ApplyMean(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int padding = kernelSize / 2;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Mean filter sonucu

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

            // ADIM 2: Array üzerinde mean filter işlemi yap
            // İlk olarak kenar pikselleri kopyala (padding alanları)
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // Varsayılan olarak orijinal değerleri kopyala
                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha
                    resultPixels[x, y, 1, 0] = sourcePixels[x, y, 1, 0]; // Red
                    resultPixels[x, y, 2, 0] = sourcePixels[x, y, 2, 0]; // Green
                    resultPixels[x, y, 3, 0] = sourcePixels[x, y, 3, 0]; // Blue
                }
            }

            // Mean filter'ı padding dahilindeki piksellere uygula
            for (int x = padding; x < width - padding; x++)
            {
                for (int y = padding; y < height - padding; y++)
                {
                    double sumR = 0, sumG = 0, sumB = 0;
                    int count = 0;

                    // Kernel içindeki pikselleri array'den topla
                    for (int i = -padding; i <= padding; i++)
                    {
                        for (int j = -padding; j <= padding; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // Array'den RGB değerlerini al
                            byte red = sourcePixels[currentX, currentY, 1, 0];
                            byte green = sourcePixels[currentX, currentY, 2, 0];
                            byte blue = sourcePixels[currentX, currentY, 3, 0];

                            sumR += red;
                            sumG += green;
                            sumB += blue;
                            count++;
                        }
                    }

                    // Ortalama hesapla
                    byte avgR = (byte)(sumR / count);
                    byte avgG = (byte)(sumG / count);
                    byte avgB = (byte)(sumB / count);

                    // Sonucu array'e kaydet (Alpha değeri aynı kalır)
                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha aynı kalır
                    resultPixels[x, y, 1, 0] = avgR; // Ortalama Red
                    resultPixels[x, y, 2, 0] = avgG; // Ortalama Green
                    resultPixels[x, y, 3, 0] = avgB; // Ortalama Blue
                }
            }

            // ADIM 3: Mean filter uygulanmış array'den yeni Bitmap oluştur
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