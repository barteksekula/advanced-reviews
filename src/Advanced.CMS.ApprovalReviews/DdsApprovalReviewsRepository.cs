using EPiServer.Data.Dynamic;
using EPiServer.Framework.Serialization;
using EPiServer.Logging;

namespace Advanced.CMS.ApprovalReviews;

internal class DdsApprovalReviewsRepository(
    DynamicDataStoreFactory dataStoreFactory,
    IObjectSerializerFactory serializerFactory)
    : IApprovalReviewsRepository
{
    private readonly object _lock = new();
    private static readonly ILogger _log = LogManager.GetLogger(typeof(DdsApprovalReviewsRepository));

    public event EventHandler<BeforeUpdateEventArgs> OnBeforeUpdate;

    public void Save(ContentReference contentLink, IEnumerable<ReviewLocation> reviewLocations)
    {
        var objectSerializer = serializerFactory.GetSerializer(KnownContentTypes.Json);
        lock (_lock)
        {
            var approvalReview = LoadApprovalReview(contentLink) ?? new ApprovalReview
            {
                ContentLink = contentLink
            };
            approvalReview.SerializedReview = objectSerializer.Serialize(reviewLocations);
            GetStore().Save(approvalReview);
        }
    }

    public void RemoveReviewLocation(string id, ContentReference contentLink)
    {
        lock (_lock)
        {
            var reviewLocations = Load(contentLink).ToList();

            var reviewLocation = reviewLocations.FirstOrDefault(x => x.Id == id);
            if (reviewLocation == null)
            {
                throw new ReviewLocationNotFoundException();
            }

            reviewLocations.Remove(reviewLocation);

            Save(contentLink, reviewLocations);
        }
    }

    public IEnumerable<ReviewLocation> Load(ContentReference contentLink)
    {
        var approvalReview = LoadApprovalReview(contentLink);
        var data = approvalReview?.SerializedReview;
        if (string.IsNullOrWhiteSpace(data))
        {
            return Enumerable.Empty<ReviewLocation>();
        }

        try
        {
            var reviewLocations = serializerFactory.GetSerializer(KnownContentTypes.Json).Deserialize<IEnumerable<ReviewLocation>>(data);
            return reviewLocations;
        }
        catch (Exception ex)
        {
            _log.Debug("Access denied while deleting", ex);
            return Enumerable.Empty<ReviewLocation>();
        }
    }

    public IEnumerable<ApprovalReview> LoadAll()
    {
        return GetStore().Items<ApprovalReview>();
    }

    public void Delete(ContentReference contentLink)
    {
        //TODO: check if the current user is the owner of the pin
        var review = GetStore().Items<ApprovalReview>().FirstOrDefault(x => x.ContentLink == contentLink);
        if (review == null)
        {
            throw new ArgumentOutOfRangeException($"Can't find review location for: {contentLink}");
        }
        GetStore().Delete(review.Id);
    }

    public ReviewLocation Update(ContentReference contentLink, ReviewLocation reviewLocation)
    {
        var data = reviewLocation.Data;

        lock (_lock)
        {
            var reviewLocations = Load(contentLink).ToList();

            var isNew = string.IsNullOrWhiteSpace(reviewLocation.Id);

            var beforeUpdateEventArgs = new BeforeUpdateEventArgs
            {
                ReviewLocations = reviewLocations,
                IsNew = isNew,
                Cancel = false
            };
            OnBeforeUpdate?.Invoke(this, beforeUpdateEventArgs);
            if (beforeUpdateEventArgs.Cancel)
            {
                return null;
            }

            if (isNew)
            {
                reviewLocation = new ReviewLocation
                {
                    Id = Guid.NewGuid().ToString(),
                    Token = reviewLocation.Token
                };
                reviewLocations.Add(reviewLocation);
            }
            else
            {
                reviewLocation = reviewLocations.FirstOrDefault(x => x.Id == reviewLocation.Id);
                if (reviewLocation == null)
                {
                    throw new ReviewLocationNotFoundException();
                }
            }

            reviewLocation.Data = data;
            Save(contentLink, reviewLocations);
        }

        return reviewLocation;
    }

    private ApprovalReview LoadApprovalReview(ContentReference contentLink)
    {
        return GetStore().Items<ApprovalReview>().FirstOrDefault(x => x.ContentLink == contentLink);
    }

    private DynamicDataStore GetStore()
    {
        return dataStoreFactory.GetStore(typeof(ApprovalReview)) ?? dataStoreFactory.CreateStore(typeof(ApprovalReview));
    }
}

internal class ReviewLocationNotFoundException : Exception
{
}

public class BeforeUpdateEventArgs : EventArgs
{
    public IEnumerable<ReviewLocation> ReviewLocations { get; set; }
    public bool IsNew { get; set; }
    public bool Cancel { get; set; }
}
