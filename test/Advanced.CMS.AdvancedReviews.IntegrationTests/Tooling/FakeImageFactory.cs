using System.Reflection;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;

public class FakeImageFactory
{
    private readonly IList<Media> _mediaCache = [];

    public IList<Media> GetMediaItems()
    {
        EnsurePopulated();
        return _mediaCache;
    }

    public Media GetMedia(int mediaId)
    {
        EnsurePopulated();
        return _mediaCache.First(x => x.Id == mediaId);
    }

    private void EnsurePopulated()
    {
        if (_mediaCache.Any())
        {
            return;
        }

        var assembly = Assembly.GetExecutingAssembly();
        var resourceNames = assembly.GetManifestResourceNames()
            .Where(x =>
                x.EndsWith(".gif", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                x.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
            .ToList();

        var mediaId = 1;

        foreach (var resourceName in resourceNames)
        {
            var ext = Path.GetExtension(resourceName)?.ToLowerInvariant();
            var contentType = ext switch
            {
                ".gif" => "image/gif",
                ".jpg" => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _ => "application/octet-stream"
            };

            using var stream = assembly.GetManifestResourceStream(resourceName);
            var bytes = Array.Empty<byte>();

            if (stream != null)
            {
                bytes = new byte[stream.Length];
                stream.ReadExactly(bytes);
            }

            var media = new Media
            {
                Id = mediaId,
                Name = GetFileNameFromResourceName(resourceName),
                ContentType = contentType,
                Bytes = bytes,
            };

            _mediaCache.Add(media);
            mediaId++;
        }
    }

    private static string GetFileNameFromResourceName(string resourceName)
    {
        if (string.IsNullOrEmpty(resourceName))
            return resourceName;

        const string marker = ".FakeImages.";
        var markerIdx = resourceName.IndexOf(marker, StringComparison.OrdinalIgnoreCase);
        var tail = markerIdx >= 0 ? resourceName[(markerIdx + marker.Length)..] : resourceName;

        var lastDot = tail.LastIndexOf('.');
        if (lastDot <= 0 || lastDot == tail.Length - 1)
            return tail;

        var prevDot = tail.LastIndexOf('.', lastDot - 1);
        return prevDot >= 0 ? tail[(prevDot + 1)..] : tail;
    }
}

public class Media
{
    public int Id { get; set; }
    public string Name { get; set; }
    public string ContentType { get; set; }
    public byte[] Bytes { get; set; }
}
