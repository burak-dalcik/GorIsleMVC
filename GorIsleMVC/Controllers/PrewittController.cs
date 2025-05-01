using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class PrewittController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }

        [HttpPost]
        public IActionResult ProcessImage(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                TempData["Error"] = "Lütfen bir görüntü dosyası seçin.";
                return RedirectToAction("Index");
            }

            try
            {
                using (var ms = new MemoryStream())
                {
                    imageFile.CopyTo(ms);
                    using (var originalImage = Image.FromStream(ms))
                    {
                        using (var bitmap = new Bitmap(originalImage))
                        {
                            var processedImage = ApplyPrewittOperator(bitmap);

                            using (var resultStream = new MemoryStream())
                            {
                                processedImage.Save(resultStream, ImageFormat.Jpeg);
                                resultStream.Position = 0;
                                return File(resultStream.ToArray(), "image/jpeg", "prewitt_" + imageFile.FileName);
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = "Görüntü işleme sırasında bir hata oluştu: " + ex.Message;
                return RedirectToAction("Index");
            }
        }

        private Bitmap ApplyPrewittOperator(Bitmap original)
        {
            int width = original.Width;
            int height = original.Height;
            Bitmap result = new Bitmap(width, height);

            // Prewitt operatör matrisleri
            int[,] prewittX = new int[,] { { -1, 0, 1 }, { -1, 0, 1 }, { -1, 0, 1 } };
            int[,] prewittY = new int[,] { { -1, -1, -1 }, { 0, 0, 0 }, { 1, 1, 1 } };

            for (int y = 1; y < height - 1; y++)
            {
                for (int x = 1; x < width - 1; x++)
                {
                    int gx = 0;
                    int gy = 0;

                    // 3x3 komşuluk için gradyan hesaplama
                    for (int i = -1; i <= 1; i++)
                    {
                        for (int j = -1; j <= 1; j++)
                        {
                            Color pixel = original.GetPixel(x + j, y + i);
                            int gray = (int)((pixel.R + pixel.G + pixel.B) / 3.0);

                            gx += gray * prewittX[i + 1, j + 1];
                            gy += gray * prewittY[i + 1, j + 1];
                        }
                    }

                    // Gradyan büyüklüğü hesaplama
                    int magnitude = (int)Math.Sqrt(gx * gx + gy * gy);
                    magnitude = Math.Min(255, Math.Max(0, magnitude));

                    result.SetPixel(x, y, Color.FromArgb(magnitude, magnitude, magnitude));
                }
            }

            return result;
        }
    }
} 