using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

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

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap convertedImage = null;

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

                convertedImage = conversionType switch
                {
                    "RGB->YUV" => RGBtoYUV(bitmap),
                    "YUV->RGB" => YUVtoRGB(bitmap),
                    _ => throw new ArgumentException("Geçersiz dönüşüm tipi")
                };

                var resultFileName = $"converted_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                convertedImage.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ConversionType"] = conversionType;

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görüntü işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup işlemleri
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                convertedImage?.Dispose();
            }
        }

        private Bitmap RGBtoYUV(Bitmap rgb)
        {
            int width = rgb.Width;
            int height = rgb.Height;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] rgbPixels = new byte[width, height, 4, 1];    // ARGB formatında orijinal
            byte[,,,] yuvPixels = new byte[width, height, 4, 1];    // YUV dönüşüm sonucu

            // ADIM 1: RGB görselinin piksellerini array'e aktar
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = rgb.GetPixel(x, y);
                    rgbPixels[x, y, 0, 0] = pixel.A; // Alpha
                    rgbPixels[x, y, 1, 0] = pixel.R; // Red
                    rgbPixels[x, y, 2, 0] = pixel.G; // Green
                    rgbPixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            // ADIM 2: Array üzerinde RGB -> YUV dönüşümü yap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // RGB değerlerini array'den al
                    byte alpha = rgbPixels[x, y, 0, 0];
                    byte red = rgbPixels[x, y, 1, 0];
                    byte green = rgbPixels[x, y, 2, 0];
                    byte blue = rgbPixels[x, y, 3, 0];

                    // RGB -> YUV dönüşüm formülleri
                    byte Y = (byte)(0.299 * red + 0.587 * green + 0.114 * blue);
                    byte U = (byte)(128 - 0.168736 * red - 0.331264 * green + 0.5 * blue);
                    byte V = (byte)(128 + 0.5 * red - 0.418688 * green - 0.081312 * blue);

                    // YUV sonucu array'e kaydet
                    yuvPixels[x, y, 0, 0] = alpha; // Alpha değeri aynı kalır
                    yuvPixels[x, y, 1, 0] = Y;     // Y bileşeni
                    yuvPixels[x, y, 2, 0] = U;     // U bileşeni
                    yuvPixels[x, y, 3, 0] = V;     // V bileşeni
                }
            }

            // ADIM 3: YUV array'den yeni Bitmap oluştur
            Bitmap result = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = yuvPixels[x, y, 0, 0];
                    byte Y = yuvPixels[x, y, 1, 0];
                    byte U = yuvPixels[x, y, 2, 0];
                    byte V = yuvPixels[x, y, 3, 0];

                    Color yuvColor = Color.FromArgb(alpha, Y, U, V);
                    result.SetPixel(x, y, yuvColor);
                }
            }

            return result;
        }

        private Bitmap YUVtoRGB(Bitmap yuv)
        {
            int width = yuv.Width;
            int height = yuv.Height;

            // KENDİ ARRAY'LERİNİ OLUŞTUR - 4 boyutlu array [x, y, kanal, 1]
            byte[,,,] yuvPixels = new byte[width, height, 4, 1];    // YUV formatında orijinal
            byte[,,,] rgbPixels = new byte[width, height, 4, 1];    // RGB dönüşüm sonucu

            // ADIM 1: YUV görselinin piksellerini array'e aktar
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = yuv.GetPixel(x, y);
                    yuvPixels[x, y, 0, 0] = pixel.A; // Alpha
                    yuvPixels[x, y, 1, 0] = pixel.R; // Y bileşeni
                    yuvPixels[x, y, 2, 0] = pixel.G; // U bileşeni
                    yuvPixels[x, y, 3, 0] = pixel.B; // V bileşeni
                }
            }

            // ADIM 2: Array üzerinde YUV -> RGB dönüşümü yap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // YUV değerlerini array'den al
                    byte alpha = yuvPixels[x, y, 0, 0];
                    double Y = yuvPixels[x, y, 1, 0]; // Y bileşeni
                    double U = yuvPixels[x, y, 2, 0] - 128; // U bileşeni
                    double V = yuvPixels[x, y, 3, 0] - 128; // V bileşeni

                    // YUV -> RGB dönüşüm formülleri
                    byte R = (byte)Math.Max(0, Math.Min(255, Y + 1.402 * V));
                    byte G = (byte)Math.Max(0, Math.Min(255, Y - 0.344136 * U - 0.714136 * V));
                    byte B = (byte)Math.Max(0, Math.Min(255, Y + 1.772 * U));

                    // RGB sonucu array'e kaydet
                    rgbPixels[x, y, 0, 0] = alpha; // Alpha değeri aynı kalır
                    rgbPixels[x, y, 1, 0] = R;     // Red bileşeni
                    rgbPixels[x, y, 2, 0] = G;     // Green bileşeni
                    rgbPixels[x, y, 3, 0] = B;     // Blue bileşeni
                }
            }

            // ADIM 3: RGB array'den yeni Bitmap oluştur
            Bitmap result = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = rgbPixels[x, y, 0, 0];
                    byte red = rgbPixels[x, y, 1, 0];
                    byte green = rgbPixels[x, y, 2, 0];
                    byte blue = rgbPixels[x, y, 3, 0];

                    Color rgbColor = Color.FromArgb(alpha, red, green, blue);
                    result.SetPixel(x, y, rgbColor);
                }
            }

            return result;
        }
    }
}