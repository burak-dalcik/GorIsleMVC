using GorIsleMVC.Helpers;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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

        /// <summary>
        /// Tarayıcıda açınca API test formu döner. POST için bu URL'ye multipart/form-data ile imageFile gönderin.
        /// </summary>
        [HttpGet]
        public IActionResult GetTestPage()
        {
            var html = """
                <!DOCTYPE html>
                <html lang="tr">
                <head><meta charset="utf-8"><title>Arka Plan Çıkarma API</title></head>
                <body style="font-family:sans-serif;max-width:500px;margin:2rem auto;padding:1rem;">
                <h1>Arka Plan Çıkarma API</h1>
                <p>Bu endpoint <strong>POST</strong> ile kullanılır. Form alan adı: <code>imageFile</code></p>
                <form method="post" enctype="multipart/form-data" action="">
                <input type="file" name="imageFile" accept="image/*" required><br><br>
                <button type="submit">Gönder</button>
                </form>
                <hr>
                <p><small>Örnek: <code>curl -X POST -F "imageFile=@resim.png" https://gorisle.dalciksoft.com/api/BackgroundRemovalApi -o sonuc.png</code></small></p>
                </body>
                </html>
                """;
            return Content(html, "text/html; charset=utf-8");
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

                using var originalImage = await Image.LoadAsync<Rgba32>(stream);
                using var processedImage = BackgroundRemovalHelper.RemoveBackground(originalImage, threshold);

                using var outputStream = new MemoryStream();
                await processedImage.SaveAsPngAsync(outputStream);
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

