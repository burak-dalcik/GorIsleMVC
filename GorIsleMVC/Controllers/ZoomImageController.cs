using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;
using System.Globalization;

namespace GorIsleMVC.Controllers
{
    public class ZoomImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ZoomImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, string zoomFactor = "1.0")
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görsel yükleyin.";
                return RedirectToAction(nameof(Upload));
            }

            if (!imageFile.ContentType.StartsWith("image/"))
            {
                TempData["Error"] = "Lütfen geçerli bir görsel dosyası yükleyin.";
                return RedirectToAction(nameof(Upload));
            }

            if (!float.TryParse(zoomFactor.Replace(',', '.'), NumberStyles.Float, CultureInfo.InvariantCulture, out float zoomValue))
            {
                TempData["Error"] = "Geçersiz yakınlaştırma faktörü.";
                return RedirectToAction(nameof(Upload));
            }

            if (zoomValue <= 0.1f || zoomValue > 5.0f)
            {
                TempData["Error"] = "Yakınlaştırma faktörü 0.1 ile 5.0 arasında olmalıdır.";
                return RedirectToAction(nameof(Upload));
            }

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap resultBitmap = null;

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(originalPath, ImageFormat.Png);

                bitmap = new Bitmap(originalImage);
                resultBitmap = ApplyZoomWithManualArrays(bitmap, zoomValue);

                var zoomedFileName = $"zoomed_{DateTime.Now:yyyyMMddHHmmss}.png";
                var zoomedPath = Path.Combine(uploadsFolder, zoomedFileName);
                resultBitmap.Save(zoomedPath, ImageFormat.Png);

                TempData["ProcessedImage"] = zoomedFileName;
                TempData["OriginalImage"] = originalFileName;
                TempData["ZoomFactor"] = zoomValue;

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Upload));
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

        private Bitmap ApplyZoomWithManualArrays(Bitmap sourceBitmap, float zoomFactor)
        {
            int originalWidth = sourceBitmap.Width;
            int originalHeight = sourceBitmap.Height;

            // Crop boyutlarını hesapla
            int cropWidth = (int)(originalWidth / zoomFactor);
            int cropHeight = (int)(originalHeight / zoomFactor);

            // Merkezden kırpma koordinatları
            int cropX = (originalWidth - cropWidth) / 2;
            int cropY = (originalHeight - cropHeight) / 2;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] sourcePixels = new byte[originalWidth, originalHeight, 4, 1];  // ARGB formatında orijinal
            byte[,,,] croppedPixels = new byte[cropWidth, cropHeight, 4, 1];        // Kırpılmış görsel
            byte[,,,] resultPixels = new byte[originalWidth, originalHeight, 4, 1]; // Final sonuç (eski boyutlarda)

            // ADIM 1: Orijinal görselin piksellerini array'e aktar
            for (int x = 0; x < originalWidth; x++)
            {
                for (int y = 0; y < originalHeight; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1, 0] = pixel.R; // Red
                    sourcePixels[x, y, 2, 0] = pixel.G; // Green
                    sourcePixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            // ADIM 2: Merkezi kırpma işlemi - array'den array'e kopyalama
            for (int x = 0; x < cropWidth; x++)
            {
                for (int y = 0; y < cropHeight; y++)
                {
                    int sourceX = cropX + x;
                    int sourceY = cropY + y;

                    // Sınır kontrolü
                    if (sourceX >= 0 && sourceX < originalWidth && sourceY >= 0 && sourceY < originalHeight)
                    {
                        croppedPixels[x, y, 0, 0] = sourcePixels[sourceX, sourceY, 0, 0]; // Alpha
                        croppedPixels[x, y, 1, 0] = sourcePixels[sourceX, sourceY, 1, 0]; // Red
                        croppedPixels[x, y, 2, 0] = sourcePixels[sourceX, sourceY, 2, 0]; // Green
                        croppedPixels[x, y, 3, 0] = sourcePixels[sourceX, sourceY, 3, 0]; // Blue
                    }
                    else
                    {
                        // Sınır dışında kalırsa şeffaf piksel
                        croppedPixels[x, y, 0, 0] = 0;   // Alpha
                        croppedPixels[x, y, 1, 0] = 0;   // Red
                        croppedPixels[x, y, 2, 0] = 0;   // Green
                        croppedPixels[x, y, 3, 0] = 0;   // Blue
                    }
                }
            }

            // ADIM 3: Bilinear interpolation ile ölçeklendirme (array'den array'e)
            // Kırpılmış görseli orijinal boyutlara çıkar
            for (int x = 0; x < originalWidth; x++)
            {
                for (int y = 0; y < originalHeight; y++)
                {
                    // Orijinal koordinatları kırpılmış görsel koordinatlarına çevir
                    float srcX = (float)x * cropWidth / originalWidth;
                    float srcY = (float)y * cropHeight / originalHeight;

                    // Bilinear interpolation için 4 komşu piksel
                    int x1 = (int)Math.Floor(srcX);
                    int y1 = (int)Math.Floor(srcY);
                    int x2 = Math.Min(x1 + 1, cropWidth - 1);
                    int y2 = Math.Min(y1 + 1, cropHeight - 1);

                    // Interpolation ağırlıkları
                    float weightX = srcX - x1;
                    float weightY = srcY - y1;

                    // Sınır kontrolü
                    x1 = Math.Max(0, Math.Min(x1, cropWidth - 1));
                    y1 = Math.Max(0, Math.Min(y1, cropHeight - 1));

                    // Her kanal için bilinear interpolation
                    for (int channel = 0; channel < 4; channel++)
                    {
                        // 4 komşu pikselin değerleri
                        float topLeft = croppedPixels[x1, y1, channel, 0];
                        float topRight = croppedPixels[x2, y1, channel, 0];
                        float bottomLeft = croppedPixels[x1, y2, channel, 0];
                        float bottomRight = croppedPixels[x2, y2, channel, 0];

                        // Bilinear interpolation hesaplaması
                        float top = topLeft * (1 - weightX) + topRight * weightX;
                        float bottom = bottomLeft * (1 - weightX) + bottomRight * weightX;
                        float result = top * (1 - weightY) + bottom * weightY;

                        // Sonucu array'e kaydet
                        resultPixels[x, y, channel, 0] = (byte)Math.Max(0, Math.Min(255, Math.Round(result)));
                    }
                }
            }

            // ADIM 4: Array'den yeni Bitmap oluştur
            Bitmap resultBitmap = new Bitmap(originalWidth, originalHeight);
            for (int x = 0; x < originalWidth; x++)
            {
                for (int y = 0; y < originalHeight; y++)
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