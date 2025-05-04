using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    public class RotateImageController : Controller
    {
        private readonly IWebHostEnvironment _environment;

        public RotateImageController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpGet]
        public IActionResult Upload()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile imageFile, float angle = 90)
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

            var uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads");
            var uniqueFileName = $"rotated_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var filePath = Path.Combine(uploadsFolder, uniqueFileName);
            var originalFileName = $"original_{DateTime.Now:yyyyMMddHHmmss}.jpg";
            var originalPath = Path.Combine(uploadsFolder, originalFileName);

            var tempPath = Path.GetTempFileName();
            try
            {
                using (var stream = new FileStream(tempPath, FileMode.Create))
                {
                    await imageFile.CopyToAsync(stream);
                }

                using (var originalImage = Image.FromFile(tempPath))
                {
   
                    originalImage.Save(originalPath, ImageFormat.Jpeg);

  
                    using (var rotatedImage = new Bitmap(originalImage.Width, originalImage.Height))
                    {
                        using (Graphics g = Graphics.FromImage(rotatedImage))
                        {
                      
                            g.TranslateTransform(rotatedImage.Width / 2, rotatedImage.Height / 2);
   
                            g.RotateTransform(angle);
               
                            g.TranslateTransform(-rotatedImage.Width / 2, -rotatedImage.Height / 2);
                            
                            g.DrawImage(originalImage, Point.Empty);
                        }

                        rotatedImage.Save(filePath, ImageFormat.Jpeg);
                    }
                }

                TempData["ProcessedImage"] = uniqueFileName;
                TempData["OriginalImage"] = originalFileName;
                TempData["Angle"] = angle;
                return View("Result");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"Görsel işlenirken bir hata oluştu: {ex.Message}";
                return RedirectToAction(nameof(Upload));
            }
            finally
            {
                if (System.IO.File.Exists(tempPath))
                {
                    System.IO.File.Delete(tempPath);
                }
            }
        }
    }
} 