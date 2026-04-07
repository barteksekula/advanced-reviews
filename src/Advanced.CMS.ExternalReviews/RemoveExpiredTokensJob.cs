using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.PlugIn;
using EPiServer.Scheduler;
using EPiServer.ServiceLocation;

namespace Advanced.CMS.ExternalReviews;

[ScheduledJob(DisplayName = "Remove Expired Tokens Job", GUID = "ee619008-3e76-4886-b3c7-aa025a0c2603")]
internal class RemoveExpiredTokensJob : ScheduledJobBase
{
    public override string Execute()
    {
        OnStatusChanged($"Starting execution of {GetType()}");

        var repository = ServiceLocator.Current.GetInstance<IExternalReviewLinksRepository>();
        var removedLinksCount = repository.RemoveExpiredLinks();

        return $"Removed {removedLinksCount} links";
    }
}
