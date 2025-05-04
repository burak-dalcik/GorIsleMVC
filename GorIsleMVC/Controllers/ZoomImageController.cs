using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Drawing2D;
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

            try
            {
                var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder))
                {
                    Directory.CreateDirectory(uploadsFolder);
                }

                var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.png";
                var originalPath = Path.Combine(uploadsFolder, originalFileName);

                using (var stream = new MemoryStream())
                {
                    await imageFile.CopyToAsync(stream);
                    stream.Position = 0;

                    using (var image = Image.FromStream(stream))
                    {
                        image.Save(originalPath, ImageFormat.Png);

                        // Crop boyutlarını hesapla
                        int cropWidth = (int)(image.Width / zoomValue);
                        int cropHeight = (int)(image.Height / zoomValue);

                        // Merkezden kırp
                        int cropX = (image.Width - cropWidth) / 2;
                        int cropY = (image.Height - cropHeight) / 2;
                        Rectangle cropRect = new Rectangle(cropX, cropY, cropWidth, cropHeight);

                        using (var cropped = new Bitmap(cropWidth, cropHeight))
                        {
                            using (var g = Graphics.FromImage(cropped))
                            {
                                g.DrawImage(image, new Rectangle(0, 0, cropWidth, cropHeight), cropRect, GraphicsUnit.Pixel);
                            }

                            // Şimdi eski boyutlara yeniden ölçekle
                            using (var zoomedImage = new Bitmap(image.Width, image.Height))
                            {
                                using (var graphics = Graphics.FromImage(zoomedImage))
                                {
                                    graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
                                    graphics.SmoothingMode = SmoothingMode.HighQuality;
                                    graphics.PixelOffsetMode = PixelOffsetMode.HighQuality;
                                    graphics.CompositingQuality = CompositingQuality.HighQuality;

                                    graphics.Clear(Color.Transparent);
                                    graphics.DrawImage(cropped, 0, 0, image.Width, image.Height);
                                }

                                var zoomedFileName = $"zoomed_{DateTime.Now:yyyyMMddHHmmss}.png";
                                var zoomedPath = Path.Combine(uploadsFolder, zoomedFileName);
                                zoomedImage.Save(zoomedPath, ImageFormat.Png);

                                TempData["ProcessedImage"] = zoomedFileName;
                                TempData["OriginalImage"] = originalFileName;
                                TempData["ZoomFactor"] = zoomValue;
                            }
                        }
                    }
                }

                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
        }
    }
}