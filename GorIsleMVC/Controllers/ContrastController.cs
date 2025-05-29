using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class ContrastController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public ContrastController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile, double contrast = 1.0)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap processedImage = null;

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
                processedImage = AdjustContrast(bitmap, contrast);

                var resultFileName = $"contrast_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                processedImage.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Kontrast Ayarlama";
                TempData["Parameters"] = $"Kontrast Değeri: {contrast:F2}";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                processedImage?.Dispose();
            }
        }

        private Bitmap AdjustContrast(Bitmap original, double contrast)
        {
            int width = original.Width;
            int height = original.Height;

            byte[,,,] originalPixels = new byte[width, height, 4, 1]; // ARGB formatında orijinal
            byte[,,,] contrastPixels = new byte[width, height, 4, 1];  // Kontrast ayarlı sonuç

            double factor = (259 * (contrast + 255)) / (255 * (259 - contrast));
            int[] lookupTable = new int[256];
            for (int i = 0; i < 256; i++)
            {
                double temp = factor * (i - 128) + 128;
                lookupTable[i] = (int)Math.Min(255, Math.Max(0, temp));
            }

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = original.GetPixel(x, y);
                    originalPixels[x, y, 0, 0] = pixel.A; // Alpha
                    originalPixels[x, y, 1, 0] = pixel.R; // Red
                    originalPixels[x, y, 2, 0] = pixel.G; // Green
                    originalPixels[x, y, 3, 0] = pixel.B; // Blue
                }
            }

            // ADIM 2: Array üzerinde kontrast ayarlama işlemi yap
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    // RGB değerlerini array'den al
                    byte alpha = originalPixels[x, y, 0, 0];
                    byte red = originalPixels[x, y, 1, 0];
                    byte green = originalPixels[x, y, 2, 0];
                    byte blue = originalPixels[x, y, 3, 0];

                    // Lookup table kullanarak kontrast ayarlama
                    byte newRed = (byte)lookupTable[red];
                    byte newGreen = (byte)lookupTable[green];
                    byte newBlue = (byte)lookupTable[blue];

                    // Kontrast ayarlı sonucu array'e kaydet
                    contrastPixels[x, y, 0, 0] = alpha;    // Alpha değeri aynı kalır
                    contrastPixels[x, y, 1, 0] = newRed;   // Yeni Red değeri
                    contrastPixels[x, y, 2, 0] = newGreen; // Yeni Green değeri
                    contrastPixels[x, y, 3, 0] = newBlue;  // Yeni Blue değeri
                }
            }

            // ADIM 3: Kontrast ayarlı array'den yeni Bitmap oluştur
            Bitmap result = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = contrastPixels[x, y, 0, 0];
                    byte red = contrastPixels[x, y, 1, 0];
                    byte green = contrastPixels[x, y, 2, 0];
                    byte blue = contrastPixels[x, y, 3, 0];

                    Color contrastColor = Color.FromArgb(alpha, red, green, blue);
                    result.SetPixel(x, y, contrastColor);
                }
            }

            return result;
        }
    }
}