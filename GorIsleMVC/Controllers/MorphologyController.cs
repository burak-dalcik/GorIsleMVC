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
        public async Task<IActionResult> ApplyMorphology(IFormFile imageFile, string operation, int kernelSize = 3, bool isColorImage = true)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction("Index");
            }

            if (kernelSize % 2 == 0 || kernelSize < 3 || kernelSize > 9)
            {
                TempData["Error"] = "Kernel boyutu 3, 5, 7 veya 9 olmalıdır.";
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

                // Morfolojik işlemi uygula
                resultBitmap = operation.ToLower() switch
                {
                    "dilation" => isColorImage ? ApplyColorDilation(bitmap, kernelSize) : ApplyDilation(bitmap, kernelSize),
                    "erosion" => isColorImage ? ApplyColorErosion(bitmap, kernelSize) : ApplyErosion(bitmap, kernelSize),
                    "opening" => isColorImage ? ApplyColorOpening(bitmap, kernelSize) : ApplyOpening(bitmap, kernelSize),
                    "closing" => isColorImage ? ApplyColorClosing(bitmap, kernelSize) : ApplyClosing(bitmap, kernelSize),
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
                TempData["IsColorImage"] = isColorImage;

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

        private Bitmap ApplyColorDilation(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int offset = kernelSize / 2;

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Dilation sonucu

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

            // ADIM 2: Array üzerinde color dilation işlemi yap
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

            // Dilation işlemini padding dahilindeki piksellere uygula
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    byte maxR = 0, maxG = 0, maxB = 0;

                    // Kernel içindeki pikselleri array'den kontrol et
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // Array'den RGB değerlerini al
                            byte red = sourcePixels[currentX, currentY, 1, 0];
                            byte green = sourcePixels[currentX, currentY, 2, 0];
                            byte blue = sourcePixels[currentX, currentY, 3, 0];

                            maxR = Math.Max(maxR, red);
                            maxG = Math.Max(maxG, green);
                            maxB = Math.Max(maxB, blue);
                        }
                    }

                    // Sonucu array'e kaydet (Alpha değeri aynı kalır)
                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha aynı kalır
                    resultPixels[x, y, 1, 0] = maxR; // Max Red
                    resultPixels[x, y, 2, 0] = maxG; // Max Green
                    resultPixels[x, y, 3, 0] = maxB; // Max Blue
                }
            }

            // ADIM 3: Dilation uygulanmış array'den yeni Bitmap oluştur
            return CreateBitmapFromArray(resultPixels, width, height);
        }

        private Bitmap ApplyColorErosion(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int offset = kernelSize / 2;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Erosion sonucu

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

            // ADIM 2: Array üzerinde color erosion işlemi yap
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

            // Erosion işlemini padding dahilindeki piksellere uygula
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    byte minR = 255, minG = 255, minB = 255;

                    // Kernel içindeki pikselleri array'den kontrol et
                    for (int i = -offset; i <= offset; i++)
                    {
                        for (int j = -offset; j <= offset; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // Array'den RGB değerlerini al
                            byte red = sourcePixels[currentX, currentY, 1, 0];
                            byte green = sourcePixels[currentX, currentY, 2, 0];
                            byte blue = sourcePixels[currentX, currentY, 3, 0];

                            minR = Math.Min(minR, red);
                            minG = Math.Min(minG, green);
                            minB = Math.Min(minB, blue);
                        }
                    }

                    // Sonucu array'e kaydet (Alpha değeri aynı kalır)
                    resultPixels[x, y, 0, 0] = sourcePixels[x, y, 0, 0]; // Alpha aynı kalır
                    resultPixels[x, y, 1, 0] = minR; // Min Red
                    resultPixels[x, y, 2, 0] = minG; // Min Green
                    resultPixels[x, y, 3, 0] = minB; // Min Blue
                }
            }

            // ADIM 3: Erosion uygulanmış array'den yeni Bitmap oluştur
            return CreateBitmapFromArray(resultPixels, width, height);
        }

        private Bitmap ApplyColorOpening(Bitmap sourceBitmap, int kernelSize)
        {
            using (var erodedImage = ApplyColorErosion(sourceBitmap, kernelSize))
            {
                return ApplyColorDilation(erodedImage, kernelSize);
            }
        }

        private Bitmap ApplyColorClosing(Bitmap sourceBitmap, int kernelSize)
        {
            using (var dilatedImage = ApplyColorDilation(sourceBitmap, kernelSize))
            {
                return ApplyColorErosion(dilatedImage, kernelSize);
            }
        }

        private Bitmap ApplyDilation(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int offset = kernelSize / 2;

            // Önce binary'e çevir
            var binaryArray = ConvertToBinaryArray(sourceBitmap);

            // KENDİ ARRAY'LERİNİ OLUŞTUR - Binary için sadece 1 kanal yeterli
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Dilation sonucu

            // ADIM 1: Array üzerinde binary dilation işlemi yap
            // İlk olarak kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte binaryValue = binaryArray[x, y, 0, 0];
                    resultPixels[x, y, 0, 0] = 255; // Alpha
                    resultPixels[x, y, 1, 0] = binaryValue; // Red
                    resultPixels[x, y, 2, 0] = binaryValue; // Green
                    resultPixels[x, y, 3, 0] = binaryValue; // Blue
                }
            }

            // Binary dilation işlemini padding dahilindeki piksellere uygula
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool hasWhitePixel = false;

                    // Kernel içindeki pikselleri array'den kontrol et
                    for (int i = -offset; i <= offset && !hasWhitePixel; i++)
                    {
                        for (int j = -offset; j <= offset && !hasWhitePixel; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // Array'den binary değeri al
                            byte binaryValue = binaryArray[currentX, currentY, 0, 0];
                            if (binaryValue == 255) // Beyaz piksel var mı?
                            {
                                hasWhitePixel = true;
                            }
                        }
                    }

                    // Sonucu array'e kaydet
                    byte resultValue = (byte)(hasWhitePixel ? 255 : 0);
                    resultPixels[x, y, 0, 0] = 255; // Alpha
                    resultPixels[x, y, 1, 0] = resultValue; // Red
                    resultPixels[x, y, 2, 0] = resultValue; // Green
                    resultPixels[x, y, 3, 0] = resultValue; // Blue
                }
            }

            // ADIM 2: Binary dilation uygulanmış array'den yeni Bitmap oluştur
            return CreateBitmapFromArray(resultPixels, width, height);
        }

        private Bitmap ApplyErosion(Bitmap sourceBitmap, int kernelSize)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int offset = kernelSize / 2;

            // Önce binary'e çevir
            var binaryArray = ConvertToBinaryArray(sourceBitmap);

            // KENDİ ARRAY'LERİNİ OLUŞTUR - Binary için sadece 1 kanal yeterli
            byte[,,,] resultPixels = new byte[width, height, 4, 1];  // Erosion sonucu

            // ADIM 1: Array üzerinde binary erosion işlemi yap
            // İlk olarak kenar pikselleri kopyala
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte binaryValue = binaryArray[x, y, 0, 0];
                    resultPixels[x, y, 0, 0] = 255; // Alpha
                    resultPixels[x, y, 1, 0] = binaryValue; // Red
                    resultPixels[x, y, 2, 0] = binaryValue; // Green
                    resultPixels[x, y, 3, 0] = binaryValue; // Blue
                }
            }

            // Binary erosion işlemini padding dahilindeki piksellere uygula
            for (int x = offset; x < width - offset; x++)
            {
                for (int y = offset; y < height - offset; y++)
                {
                    bool allWhitePixels = true;

                    // Kernel içindeki pikselleri array'den kontrol et
                    for (int i = -offset; i <= offset && allWhitePixels; i++)
                    {
                        for (int j = -offset; j <= offset && allWhitePixels; j++)
                        {
                            int currentX = x + i;
                            int currentY = y + j;

                            // Array'den binary değeri al
                            byte binaryValue = binaryArray[currentX, currentY, 0, 0];
                            if (binaryValue == 0) // Siyah piksel var mı?
                            {
                                allWhitePixels = false;
                            }
                        }
                    }

                    // Sonucu array'e kaydet
                    byte resultValue = (byte)(allWhitePixels ? 255 : 0);
                    resultPixels[x, y, 0, 0] = 255; // Alpha
                    resultPixels[x, y, 1, 0] = resultValue; // Red
                    resultPixels[x, y, 2, 0] = resultValue; // Green
                    resultPixels[x, y, 3, 0] = resultValue; // Blue
                }
            }

            // ADIM 2: Binary erosion uygulanmış array'den yeni Bitmap oluştur
            return CreateBitmapFromArray(resultPixels, width, height);
        }

        private Bitmap ApplyOpening(Bitmap sourceBitmap, int kernelSize)
        {
            using (var erodedImage = ApplyErosion(sourceBitmap, kernelSize))
            {
                return ApplyDilation(erodedImage, kernelSize);
            }
        }

        private Bitmap ApplyClosing(Bitmap sourceBitmap, int kernelSize)
        {
            using (var dilatedImage = ApplyDilation(sourceBitmap, kernelSize))
            {
                return ApplyErosion(dilatedImage, kernelSize);
            }
        }

        private byte[,,,] ConvertToBinaryArray(Bitmap sourceBitmap, int threshold = 128)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            byte[,,,] binaryArray = new byte[width, height, 4, 1];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    var pixel = sourceBitmap.GetPixel(x, y);
                    int grayValue = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    byte binaryValue = (byte)(grayValue >= threshold ? 255 : 0);

                    binaryArray[x, y, 0, 0] = binaryValue; // Binary değeri tek kanala kaydet
                    binaryArray[x, y, 1, 0] = binaryValue;
                    binaryArray[x, y, 2, 0] = binaryValue;
                    binaryArray[x, y, 3, 0] = binaryValue;
                }
            }

            return binaryArray;
        }

        private Bitmap CreateBitmapFromArray(byte[,,,] pixelArray, int width, int height)
        {
            Bitmap resultBitmap = new Bitmap(width, height);

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = pixelArray[x, y, 0, 0];
                    byte red = pixelArray[x, y, 1, 0];
                    byte green = pixelArray[x, y, 2, 0];
                    byte blue = pixelArray[x, y, 3, 0];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }
    }
}