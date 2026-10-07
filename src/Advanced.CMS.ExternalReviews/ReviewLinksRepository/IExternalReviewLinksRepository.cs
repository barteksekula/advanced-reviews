namespace Advanced.CMS.ExternalReviews.ReviewLinksRepository;

public interface IExternalReviewLinksRepository
{
    IEnumerable<ExternalReviewLink> GetLinksForContent(ContentReference contentLink, int? projectId);
    ExternalReviewLink GetContentByToken(string token);
    int RemoveExpiredLinks();
    bool HasPinCode(string token);
    ExternalReviewLink AddLink(ContentReference contentLink, bool isEditable, TimeSpan validTo, int? projectId);

    /// <summary>
    /// Update link
    /// </summary>
    /// <param name="token"></param>
    /// <param name="validTo"></param>
    /// <param name="pinCode">New PIN code. If null then PIN is not updated, if empty then PIN is removed</param>
    /// <param name="displayName">Link display name, when empty then fallback to token</param>
    /// <param name="visitorGroups">Impersonate with the following visitor groups ids</param>
    ExternalReviewLink UpdateLink(string token, DateTime? validTo, string pinCode, string displayName, string[] visitorGroups);

    void DeleteLink(string token);
}
