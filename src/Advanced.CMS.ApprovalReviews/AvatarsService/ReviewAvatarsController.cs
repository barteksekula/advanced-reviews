using System.Web;
using Microsoft.AspNetCore.Mvc;

namespace Advanced.CMS.ApprovalReviews.AvatarsService;

/// <summary>
/// Handler used to get user avatar based on username
/// </summary>
internal class ReviewAvatarsController(ICustomAvatarResolver customAvatarResolver) : Controller
{
    private readonly IdenticonGenerator _identiconGenerator = new();

    //TODO: try to use EPiServer.Web.MediaHandlerBase to turn on caching
    [HttpGet]
    public IActionResult Index(string id)
    {
        var userName = id;
        if (string.IsNullOrWhiteSpace(userName))
        {
            return new NotFoundResult();
        }

        userName = HttpUtility.UrlDecode(userName);

        var customAvatar = customAvatarResolver.GetImage(userName);
        if (customAvatar != null)
        {
            return File(customAvatar, "image/png");
        }

        var identicon = _identiconGenerator.CreateIdenticon(userName, 100);
        return File(identicon, "image/png");
    }

    public bool IsReusable => false;
}
