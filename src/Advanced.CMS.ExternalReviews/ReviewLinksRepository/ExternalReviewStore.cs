using System.Net;
using Advanced.CMS.ApprovalReviews;
using EPiServer.Cms.Shell.UI.Rest.Projects;
using EPiServer.Notification;
using EPiServer.Shell.Services.Rest;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace Advanced.CMS.ExternalReviews.ReviewLinksRepository;

/// <summary>
/// Manage external review links
/// </summary>
[RestStore("externalreviewstore")]
internal class ExternalReviewStore(
    IContentLoader contentLoader,
    NotificationOptions notificationOptions,
    IExternalReviewLinksRepository externalReviewLinksRepository,
    INotificationProvider emailNotificationProvider,
    IOptions<ExternalReviewOptions> externalReviewOptions,
    CurrentProject currentProject,
    ISiteUriResolver siteUriResolver)
    : RestControllerBase
{
    private void HidePinCode(ExternalReviewLink externalReviewLink)
    {
        if (!string.IsNullOrWhiteSpace(externalReviewLink.PinCode))
        {
            externalReviewLink.PinCode = "****";
        }
    }

    [HttpGet]
    public ActionResult Get(ContentReference id, int? projectId)
    {
        var externalReviewLinks = externalReviewLinksRepository.GetLinksForContent(id, projectId).ToList();
        foreach (var externalReviewLink in externalReviewLinks)
        {
            HidePinCode(externalReviewLink);
        }
        return Rest(externalReviewLinks);
    }

    [HttpPost]
    public ActionResult Post([FromBody] PostExternalReviewLinkModel externalLink)
    {
        if (externalLink?.ContentLink == null || !ContentReference.TryParse(externalLink.ContentLink, out var contentLink))
        {
            return new RestStatusCodeResult(HttpStatusCode.BadRequest);
        }

        // should not be possible to add editable link when option is not available
        if (externalLink.IsEditable && !externalReviewOptions.Value.EditableLinksEnabled)
        {
            return new RestStatusCodeResult(HttpStatusCode.BadRequest);
        }

        var validTo = externalLink.IsEditable ? externalReviewOptions.Value.EditLinkValidTo: externalReviewOptions.Value.ViewLinkValidTo;
        var result = externalReviewLinksRepository.AddLink(contentLink, externalLink.IsEditable, validTo, currentProject.ProjectId);
        HidePinCode(result);
        return Rest(result);
    }

    [HttpPost]
    public ActionResult Edit(string id, [FromBody] PinDto pinDto)
    {
        if (id == null)
        {
            return new BadRequestResult();
        }

        var result = externalReviewLinksRepository.UpdateLink(id, pinDto.ValidTo, pinDto.PinCode, pinDto.DisplayName, pinDto.VisitorGroups);
        HidePinCode(result);

        return Rest(result);
    }

    [HttpPost]
    public async Task<ActionResult> ShareReviewLink(string id, [FromBody] ShareEmailDto dto)
    {
        if (id == null)
        {
            return new BadRequestResult();
        }

        if (string.IsNullOrWhiteSpace(notificationOptions.NotificationEmailAddress))
        {
            return new BadRequestObjectResult("Sender email address is not configured. Contact with system administrator");
        }

        var externalReviewLink = externalReviewLinksRepository.GetContentByToken(id);
        if (externalReviewLink == null)
        {
            return new NotFoundObjectResult("Token not found");
        }

        if (!contentLoader.TryGet(externalReviewLink.ContentLink, out IContent _))
        {
            return new NotFoundObjectResult("Content not found");
        }

        await SendMail(externalReviewLink, dto.Email, dto.Subject, dto.Message);

        return Rest(true);
    }

    private async Task<bool> SendMail(ExternalReviewLink externalReviewLink, string email, string subject,
        string message)
    {
        var linkUrl = new Uri(siteUriResolver.GetUri(externalReviewLink.ContentLink), externalReviewLink.LinkUrl);

        message = message.Replace("[#link#]", linkUrl.ToString());

        var providerNotificationMessages = new List<ProviderNotificationMessage>
        {
            new ProviderNotificationMessage
            {
                Content = message,
                RecipientAddresses = new[] {email},
                SenderAddress = notificationOptions.NotificationEmailAddress,
                Subject = subject,
                SenderDisplayName = notificationOptions.NotificationEmailDisplayName
            }
        };
        var result = true;
#pragma warning disable 618
        await emailNotificationProvider.SendAsync(providerNotificationMessages.Select(Task.FromResult), msg => { result = true; },
#pragma warning restore 618
            (_, _) => { result = false; }).ConfigureAwait(true);
        return result;
    }

    [HttpDelete]
    public ActionResult Delete(string id)
    {
        externalReviewLinksRepository.DeleteLink(id);
        return Rest(true);
    }
}

internal class PostExternalReviewLinkModel
{
    public string ContentLink { get; set; }
    public bool IsEditable { get; set; }
}

internal class ShareEmailDto
{
    public string Email { get; set; }
    public string Subject { get; set; }
    public string Message { get; set; }
}

internal class PinDto
{
    public DateTime? ValidTo { get; set; }
    public string PinCode { get; set; }
    public string DisplayName { get; set; }
    public string[] VisitorGroups { get; set; }
}
