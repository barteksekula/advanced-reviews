using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.ServiceLocation;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Basic;

[Collection(IntegrationTestCollection.Name)]
public class When_Updating_Review_Link(When_Updating_Review_Link.TestFixture fixture)
    : IClassFixture<CommonFixture>, IClassFixture<When_Updating_Review_Link.TestFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        public IContentRepository ContentRepository { get; } = siteFixture.Services.GetInstance<IContentRepository>();
        public IExternalReviewLinksRepository LinksRepository { get; } = siteFixture.Services.GetInstance<IExternalReviewLinksRepository>();

        public StandardPage Page { get; private set; }

        public Task InitializeAsync()
        {
            Page = ContentRepository.CreatePage();
            return Task.CompletedTask;
        }

        public async Task DisposeAsync() => await ContentRepository.CleanupAsync(Page);
    }

    [Fact]
    public void Link_Without_Visitor_Groups_Does_Not_Impersonate_Any_Visitor_Group()
    {
        var link = fixture.LinksRepository.AddLink(fixture.Page.ContentLink, false, TimeSpan.FromDays(1), null);

        var updatedLink = fixture.LinksRepository.UpdateLink(link.Token, null, null, "Reviewers", null);

        Assert.Empty(updatedLink.VisitorGroups);
    }
}
