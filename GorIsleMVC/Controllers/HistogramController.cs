using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class HistogramController : Controller
    {
        private readonly IWebHostEnvironment _hostEnvironment;

        public HistogramController(IWebHostEnvironment hostEnvironment)
        {
            _hostEnvironment = hostEnvironment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult Upload(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            var uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads");
            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var uniqueFileName = Guid.NewGuid().ToString() + "_" + imageFile.FileName;
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);

            MemoryStream stream = null;
            Image originalImage = null;
            Bitmap bitmap = null;

            try
            {
                stream = new MemoryStream();
                imageFile.CopyTo(stream);
                stream.Position = 0;

                originalImage = Image.FromStream(stream);
                originalImage.Save(filePath, ImageFormat.Jpeg);

                bitmap = new Bitmap(originalImage);

                var histogramData = CalculateHistogramManual(bitmap);

                TempData["RedHistogram"] = string.Join(",", histogramData.RedHistogram);
                TempData["GreenHistogram"] = string.Join(",", histogramData.GreenHistogram);
                TempData["BlueHistogram"] = string.Join(",", histogramData.BlueHistogram);
                TempData["GrayHistogram"] = string.Join(",", histogramData.GrayHistogram);
                TempData["ImagePath"] = "/uploads/" + uniqueFileName;

                return RedirectToAction("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
            }
        }

        public IActionResult Result()
        {
            if (TempData["ImagePath"] == null)
            {
                return RedirectToAction("Index");
            }
            return View();
        }

        [HttpPost]
        public IActionResult Equalize(string imagePath)
        {
            if (string.IsNullOrEmpty(imagePath))
            {
                TempData["Error"] = "Görüntü bulunamadı.";
                return RedirectToAction("Index");
            }

            var fullPath = Path.Combine(_hostEnvironment.WebRootPath, imagePath.TrimStart('/'));
            var outputFileName = "equalized_" + Path.GetFileName(imagePath);
            var outputPath = Path.Combine(_hostEnvironment.WebRootPath, "uploads", outputFileName);

            Image originalImage = null;
            Bitmap bitmap = null;
            Bitmap equalizedBitmap = null;

            try
            {
                originalImage = Image.FromFile(fullPath);
                bitmap = new Bitmap(originalImage);
                equalizedBitmap = PerformHistogramEqualizationManual(bitmap);

                equalizedBitmap.Save(outputPath, ImageFormat.Jpeg);

                TempData["OriginalImage"] = imagePath;
                TempData["EqualizedImage"] = "/uploads/" + outputFileName;

                return RedirectToAction("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Histogram eşitleme sırasında hata oluştu: {ex.Message}";
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup işlemleri
                originalImage?.Dispose();
                bitmap?.Dispose();
                equalizedBitmap?.Dispose();
            }
        }

        private HistogramData CalculateHistogramManual(Bitmap sourceBitmap)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal

            int[] redHistogram = new int[256];
            int[] greenHistogram = new int[256];
            int[] blueHistogram = new int[256];
            int[] grayHistogram = new int[256];

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

            // ADIM 2: Array üzerinde histogram hesaplama
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte red = sourcePixels[x, y, 1, 0];
                    byte green = sourcePixels[x, y, 2, 0];
                    byte blue = sourcePixels[x, y, 3, 0];

                    redHistogram[red]++;
                    greenHistogram[green]++;
                    blueHistogram[blue]++;

                    int gray = (red + green + blue) / 3;
                    grayHistogram[gray]++;
                }
            }

            return new HistogramData
            {
                RedHistogram = redHistogram,
                GreenHistogram = greenHistogram,
                BlueHistogram = blueHistogram,
                GrayHistogram = grayHistogram
            };
        }

        private Bitmap PerformHistogramEqualizationManual(Bitmap sourceBitmap)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;
            int totalPixels = width * height;

            byte[,,,] sourcePixels = new byte[width, height, 4, 1];  // ARGB formatında orijinal
            byte[,,,] equalizedPixels = new byte[width, height, 4, 1]; // Eşitlenmiş sonuç

            int[] histogram = new int[256];
            int[] cdf = new int[256];

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);
                    sourcePixels[x, y, 0, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1, 0] = pixel.R; // Red
                    sourcePixels[x, y, 2, 0] = pixel.G; // Green
                    sourcePixels[x, y, 3, 0] = pixel.B; // Blue

                    int gray = (pixel.R + pixel.G + pixel.B) / 3;
                    histogram[gray]++;
                }
            }

            // Cumulative Distribution Function (CDF) hesapla
            cdf[0] = histogram[0];
            for (int i = 1; i < 256; i++)
            {
                cdf[i] = cdf[i - 1] + histogram[i];
            }

            //  Array üzerinde histogram eşitleme işlemi 
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte red = sourcePixels[x, y, 1, 0];
                    byte green = sourcePixels[x, y, 2, 0];
                    byte blue = sourcePixels[x, y, 3, 0];
                    byte alpha = sourcePixels[x, y, 0, 0];

                    int gray = (red + green + blue) / 3;

                    int newValue = (int)((cdf[gray] * 255.0) / totalPixels);

                    newValue = Math.Max(0, Math.Min(255, newValue));
                    byte equalizedGray = (byte)newValue;

                    equalizedPixels[x, y, 0, 0] = alpha;         // Alpha aynı kalır
                    equalizedPixels[x, y, 1, 0] = equalizedGray; // Red = Equalized Gray
                    equalizedPixels[x, y, 2, 0] = equalizedGray; // Green = Equalized Gray
                    equalizedPixels[x, y, 3, 0] = equalizedGray; // Blue = Equalized Gray
                }
            }

            // Eşitlenmiş array'den yeni  oluştur
            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = equalizedPixels[x, y, 0, 0];
                    byte grayValue = equalizedPixels[x, y, 1, 0]; // R, G, B hepsi aynı değer

                    Color resultColor = Color.FromArgb(alpha, grayValue, grayValue, grayValue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }

        private class HistogramData
        {
            public int[] RedHistogram { get; set; }
            public int[] GreenHistogram { get; set; }
            public int[] BlueHistogram { get; set; }
            public int[] GrayHistogram { get; set; }
        }
    }
}