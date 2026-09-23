using EPiServer.Framework.Blobs;
using EPiServer.Framework.DataAnnotations;

namespace TestSite.Models;

[ContentType]
[MediaDescriptor(ExtensionString = "svg")]
public class VectorImageFile : ImageData
{
    public override Blob Thumbnail => BinaryData;
}
