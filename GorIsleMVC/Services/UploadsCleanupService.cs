using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GorIsleMVC.Services;

/// <summary>
/// uploads klasöründeki belirli süreden eski dosyaları periyodik siler; sunucuda yer kaplamalarını önler.
/// </summary>
public class UploadsCleanupService : BackgroundService
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<UploadsCleanupService> _logger;
    private readonly TimeSpan _maxFileAge;
    private readonly TimeSpan _interval;

    public UploadsCleanupService(
        IWebHostEnvironment env,
        ILogger<UploadsCleanupService> logger,
        IOptions<UploadsCleanupOptions>? options = null)
    {
        _env = env;
        _logger = logger;
        var opts = options?.Value ?? new UploadsCleanupOptions();
        _maxFileAge = TimeSpan.FromMinutes(opts.MaxFileAgeMinutes);
        _interval = TimeSpan.FromMinutes(Math.Max(1, opts.CleanupIntervalMinutes));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Uploads temizleme servisi başladı. Aralık: {Interval} dk, silinecek dosya yaşı: {MaxAge} dk.",
            _interval.TotalMinutes, _maxFileAge.TotalMinutes);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(_interval, stoppingToken);
                await CleanupAsync(stoppingToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uploads temizlenirken hata oluştu.");
            }
        }
    }

    private async Task CleanupAsync(CancellationToken ct)
    {
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
        if (!Directory.Exists(uploadsPath))
        {
            await Task.CompletedTask;
            return;
        }

        var cutoff = DateTime.UtcNow - _maxFileAge;
        var files = Directory.GetFiles(uploadsPath);
        var deleted = 0;

        foreach (var file in files)
        {
            if (ct.IsCancellationRequested) break;
            try
            {
                var info = new FileInfo(file);
                if (info.LastWriteTimeUtc < cutoff)
                {
                    File.Delete(file);
                    deleted++;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Dosya silinemedi: {File}", file);
            }
        }

        if (deleted > 0)
            _logger.LogInformation("Uploads temizlendi: {Count} dosya silindi.", deleted);

        await Task.CompletedTask;
    }
}

public class UploadsCleanupOptions
{
    /// <summary>Bu süreden (dakika) eski dosyalar silinir.</summary>
    public int MaxFileAgeMinutes { get; set; } = 60;

    /// <summary>Temizleme aralığı (dakika).</summary>
    public int CleanupIntervalMinutes { get; set; } = 30;
}
