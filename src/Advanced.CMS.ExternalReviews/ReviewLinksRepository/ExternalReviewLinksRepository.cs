using Advanced.CMS.ExternalReviews.PinCodeSecurity;
using EPiServer.Data.Dynamic;
using EPiServer.Framework.Cache;

namespace Advanced.CMS.ExternalReviews.ReviewLinksRepository;

internal class ExternalReviewLinksRepository(
    ExternalReviewLinkBuilder externalReviewLinkBuilder,
    DynamicDataStoreFactory dataStoreFactory,
    ISynchronizedObjectInstanceCache cache)
    : IExternalReviewLinksRepository
{
    public IEnumerable<ExternalReviewLink> GetLinksForContent(ContentReference contentLink, int? projectId)
    {
        if (!projectId.HasValue)
        {
            // when project is null, then set it to -1
            // to make sure that it won't return all links with no projects set
            projectId = -1;
        }
        return GetStore().Items<ExternalReviewLinkDds>().Where(x => x.ContentLink == contentLink || x.ProjectId == projectId).ToList().Select(
            externalReviewLinkBuilder.FromExternalReview);
    }

    public int RemoveExpiredLinks()
    {
        var store = GetStore();
        var expiredItems = store.Items<ExternalReviewLinkDds>().Where(x => x.ValidTo < DateTime.Now).ToList();
        foreach (var item in expiredItems)
        {
            store.Delete(item.Id);
        }

        return expiredItems.Count;
    }

    private ExternalReviewLinkDds GetReviewLinkDds(string token)
    {
        if (string.IsNullOrWhiteSpace(token))
        {
            return null;
        }

        token = token.ToLower();
        if (cache.Get(token) is ExternalReviewLinkDds item)
        {
            return item;
        }

        var externalReviewLinkDds = GetStore().Items<ExternalReviewLinkDds>().FirstOrDefault(x => x.Token == token);
        cache.Insert(token, externalReviewLinkDds, CacheEvictionPolicy.Empty);

        return externalReviewLinkDds;
    }

    public bool HasPinCode(string token)
    {
        var externalReviewLinkDds = GetReviewLinkDds(token);
        if (externalReviewLinkDds == null)
        {
            return false;
        }

        return !string.IsNullOrWhiteSpace(externalReviewLinkDds.PinCode);
    }

    public ExternalReviewLink GetContentByToken(string token)
    {
        var externalReviewLinkDds = GetReviewLinkDds(token);
        return externalReviewLinkDds == null
            ? null
            : externalReviewLinkBuilder.FromExternalReview(externalReviewLinkDds);
    }

    public ExternalReviewLink AddLink(ContentReference contentLink, bool isEditable, TimeSpan validTo,
        int? projectId)
    {
        if (contentLink.WorkID == 0)
        {
            throw new InvalidOperationException("Cannot create a link for version agnostic content reference");
        }

        var externalReviewLink = new ExternalReviewLinkDds
        {
            ContentLink = contentLink,
            IsEditable = isEditable,
            ProjectId = projectId,
            Token = Guid.NewGuid().ToString(),
            ValidTo = DateTime.Now.Add(validTo)
        };
        GetStore().Save(externalReviewLink);
        return externalReviewLinkBuilder.FromExternalReview(externalReviewLink);
    }

    public ExternalReviewLink UpdateLink(string token, DateTime? validTo, string pinCode, string displayName, string[] visitorGroups)
    {
        var store = GetStore();
        var item = store.Items<ExternalReviewLinkDds>()
            .FirstOrDefault(x => x.Token == token);
        if (item == null)
        {
            return null;
        }

        if (validTo.HasValue)
        {
            item.ValidTo = validTo.Value;
        }

        if (pinCode != null)
        {
            item.PinCode = pinCode == string.Empty ? null : PinCodeHashGenerator.Hash(pinCode, token);
        }

        item.DisplayName = displayName;
        item.VisitorGroups = visitorGroups ?? [];

        store.Save(item);
        cache.Remove(token);
        return externalReviewLinkBuilder.FromExternalReview(item);
    }

    public void DeleteLink(string token)
    {
        var item = GetStore().Items<ExternalReviewLinkDds>()
            .FirstOrDefault(x => x.Token == token);
        if (item == null)
        {
            return;
        }
        GetStore().Delete(item.Id);
        cache.Remove(token);
    }

    private DynamicDataStore GetStore()
    {
        return dataStoreFactory.GetStore(typeof(ExternalReviewLinkDds)) ?? dataStoreFactory.CreateStore(typeof(ExternalReviewLinkDds));
    }

}
