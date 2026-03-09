using GorIsleMVC.Helpers;
using Microsoft.AspNetCore.Mvc;
using System.Drawing;
using System.Drawing.Imaging;

namespace GorIsleMVC.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class BackgroundRemovalApiController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;

        public BackgroundRemovalApiController(IWebHostEnvironment environment)
        {
            _environment = environment;
        }

        [HttpPost]
        [RequestSizeLimit(10_000_000)]
        public async Task<IActionResult> RemoveBackgroundApi(IFormFile imageFile)
        {
            if (imageFile == null || imageFile.Length == 0)
            {
                return BadRequest("Lütfen bir görüntü dosyası gönderin.");
            }

            if (!imageFile.ContentType.StartsWith("image/"))
            {
                return BadRequest("Geçerli bir görüntü dosyası gönderin.");
            }

            try
            {
                byte threshold = 40;

                using var stream = new MemoryStream();
                await imageFile.CopyToAsync(stream);
                stream.Position = 0;

                using var originalImage = Image.FromStream(stream);
                using var bitmap = new Bitmap(originalImage);
                using var processedImage = BackgroundRemovalHelper.RemoveBackground(bitmap, threshold);

                using var outputStream = new MemoryStream();
                processedImage.Save(outputStream, ImageFormat.Png);
                outputStream.Position = 0;

                return File(outputStream.ToArray(), "image/png", "background-removed.png");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Görüntü işlenirken bir hata oluştu: {ex.Message}");
            }
        }
    }
}

