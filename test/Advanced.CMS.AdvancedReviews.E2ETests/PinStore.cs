using Advanced.CMS.ApprovalReviews;
using EPiServer.ServiceLocation;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

public static class PinStore
{
    public static async Task<ReviewLocation> WaitForPinAsync(WebServerFixture fixture, ContentReference contentLink, string comment)
    {
        var repository = fixture.Services.GetInstance<IApprovalReviewsRepository>();
        var timeout = DateTime.UtcNow.AddSeconds(15);
        while (true)
        {
            var pin = repository.Load(contentLink).FirstOrDefault(x => x.Data.Contains(comment));
            if (pin != null)
            {
                return pin;
            }

            if (DateTime.UtcNow > timeout)
            {
                throw new TimeoutException($"No pin with the comment '{comment}' was saved");
            }

            await Task.Delay(200);
        }
    }
}
