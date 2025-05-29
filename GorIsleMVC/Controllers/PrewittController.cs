using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class PrewittController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public PrewittController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> ProcessImage(IFormFile imageFile)
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
                processedImage = ApplyPrewittOperator(bitmap);

                var resultFileName = $"prewitt_{DateTime.Now:yyyyMMddHHmmss}.png";
                var resultPath = Path.Combine(uploadsFolder, resultFileName);
                processedImage.Save(resultPath, ImageFormat.Png);

                TempData["OriginalImage"] = $"/uploads/{originalFileName}";
                TempData["ProcessedImage"] = $"/uploads/{resultFileName}";
                TempData["ProcessType"] = "Prewitt Kenar Bulma";

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
            finally
            {
                // Manuel cleanup
                stream?.Dispose();
                originalImage?.Dispose();
                bitmap?.Dispose();
                processedImage?.Dispose();
            }
        }

        private Bitmap ApplyPrewittOperator(Bitmap sourceBitmap)
        {
            int width = sourceBitmap.Width;
            int height = sourceBitmap.Height;

            // PREWITT KERNELS - Sabit matrisler
            int[,] prewittX = new int[,] {
                { -1, 0, 1 },
                { -1, 0, 1 },
                { -1, 0, 1 }
            };

            int[,] prewittY = new int[,] {
                { -1, -1, -1 },
                {  0,  0,  0 },
                {  1,  1,  1 }
            };

            // ARRAY TABANLI YAKLAŞIM
            byte[,,] sourcePixels = new byte[width, height, 4];    // ARGB - Orijinal
            byte[,,] grayPixels = new byte[width, height, 1];      // Grayscale array
            byte[,,] resultPixels = new byte[width, height, 4];    // ARGB - Sonuç

            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    Color pixel = sourceBitmap.GetPixel(x, y);

              
                    sourcePixels[x, y, 0] = pixel.A; // Alpha
                    sourcePixels[x, y, 1] = pixel.R; // Red
                    sourcePixels[x, y, 2] = pixel.G; // Green
                    sourcePixels[x, y, 3] = pixel.B; // Blue

                    int gray = (int)((pixel.R + pixel.G + pixel.B) / 3.0);
                    grayPixels[x, y, 0] = (byte)gray;

                    resultPixels[x, y, 0] = pixel.A;
                    resultPixels[x, y, 1] = (byte)gray;
                    resultPixels[x, y, 2] = (byte)gray;
                    resultPixels[x, y, 3] = (byte)gray;
                }
            }

            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int gx = 0; // X yönü gradyanı (dikey kenarlar)
                    int gy = 0; // Y yönü gradyanı (yatay kenarlar)

                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            int currentX = x + j;
                            int currentY = y + i;

                            byte grayValue = grayPixels[currentX, currentY, 0];

                            gx += grayValue * prewittX[i + 1, j + 1];
                            gy += grayValue * prewittY[i + 1, j + 1];
                        }
                    }

                    // Gradyan büyüklüğü hesaplama: √(Gx² + Gy²)
                    double magnitude = Math.Sqrt(gx * gx + gy * gy);

                    // 0-255 aralığında sınırla
                    int edgeValue = (int)Math.Min(255, Math.Max(0, magnitude));

                    // Sonuç array'ine kaydet (grayscale olarak)
                    resultPixels[x, y, 0] = sourcePixels[x, y, 0]; // Alpha aynı kalır
                    resultPixels[x, y, 1] = (byte)edgeValue;       // Red = edge value
                    resultPixels[x, y, 2] = (byte)edgeValue;       // Green = edge value
                    resultPixels[x, y, 3] = (byte)edgeValue;       // Blue = edge value
                }
            }

            ProcessEdgePixels(sourcePixels, resultPixels, width, height);

            Bitmap resultBitmap = new Bitmap(width, height);
            for (int x = 0; x < width; x++)
            {
                for (int y = 0; y < height; y++)
                {
                    byte alpha = resultPixels[x, y, 0];
                    byte red = resultPixels[x, y, 1];
                    byte green = resultPixels[x, y, 2];
                    byte blue = resultPixels[x, y, 3];

                    Color resultColor = Color.FromArgb(alpha, red, green, blue);
                    resultBitmap.SetPixel(x, y, resultColor);
                }
            }

            return resultBitmap;
        }

        private void ProcessEdgePixels(byte[,,] sourcePixels, byte[,,] resultPixels, int width, int height)
        {
            for (int x = 0; x < width; x++)
            {
                int grayTop = (int)((sourcePixels[x, 0, 1] + sourcePixels[x, 0, 2] + sourcePixels[x, 0, 3]) / 3.0);
                resultPixels[x, 0, 0] = sourcePixels[x, 0, 0]; // Alpha
                resultPixels[x, 0, 1] = (byte)grayTop;         // Red
                resultPixels[x, 0, 2] = (byte)grayTop;         // Green
                resultPixels[x, 0, 3] = (byte)grayTop;         // Blue

                int grayBottom = (int)((sourcePixels[x, height - 1, 1] + sourcePixels[x, height - 1, 2] + sourcePixels[x, height - 1, 3]) / 3.0);
                resultPixels[x, height - 1, 0] = sourcePixels[x, height - 1, 0]; // Alpha
                resultPixels[x, height - 1, 1] = (byte)grayBottom;               // Red
                resultPixels[x, height - 1, 2] = (byte)grayBottom;               // Green
                resultPixels[x, height - 1, 3] = (byte)grayBottom;               // Blue
            }

            for (int y = 0; y < height; y++)
            {
                int grayLeft = (int)((sourcePixels[0, y, 1] + sourcePixels[0, y, 2] + sourcePixels[0, y, 3]) / 3.0);
                resultPixels[0, y, 0] = sourcePixels[0, y, 0]; // Alpha
                resultPixels[0, y, 1] = (byte)grayLeft;        // Red
                resultPixels[0, y, 2] = (byte)grayLeft;        // Green
                resultPixels[0, y, 3] = (byte)grayLeft;        // Blue

                int grayRight = (int)((sourcePixels[width - 1, y, 1] + sourcePixels[width - 1, y, 2] + sourcePixels[width - 1, y, 3]) / 3.0);
                resultPixels[width - 1, y, 0] = sourcePixels[width - 1, y, 0]; // Alpha
                resultPixels[width - 1, y, 1] = (byte)grayRight;               // Red
                resultPixels[width - 1, y, 2] = (byte)grayRight;               // Green
                resultPixels[width - 1, y, 3] = (byte)grayRight;               // Blue
            }
        }
    }
}