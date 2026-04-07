using System.Net;
using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ApprovalReviews;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TestSite.Models;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.PinSecurity;

[Collection(IntegrationTestCollection.Name)]
public class When_Pin_Security_Enabled(When_Pin_Security_Enabled.TestFixture fixture)
    : IClassFixture<CommonFixture>,
      IClassFixture<When_Pin_Security_Enabled.TestFixture>,
      IClassFixture<ExternalReviewOptionsPinEnabledFixture>
{
    public class TestFixture(SiteFixture siteFixture) : IAsyncLifetime
    {
        private readonly IContentRepository contentRepository = siteFixture.Services.GetService<IContentRepository>();
        public readonly ISiteUriResolver siteResolver = siteFixture.Services.GetService<ISiteUriResolver>();
        public readonly IOptions<ExternalReviewOptions> ExternalReviewOptions = siteFixture.Services.GetService<IOptions<ExternalReviewOptions>>();
        public HttpClient Client { get; } = siteFixture.Client;

        public StandardPage Page { get; private set; }
        public ExternalReviewLink GeneratedReviewLink { get; set; }
        public string ReviewedPageContent = "test page";
        public string PinCode = "123";

        public async Task InitializeAsync()
        {
            Page = contentRepository.CreatePage(ReviewedPageContent).WithoutEveryoneAccess();

            var reviewLinksRepository =
                siteFixture.Services.GetRequiredService<IExternalReviewLinksRepository>();
            reviewLinksRepository.GetLinksForContent(Page.ContentLink, null);
            var link = reviewLinksRepository.AddLink(Page.ContentLink, false, TimeSpan.FromDays(1), null);
            GeneratedReviewLink = reviewLinksRepository.UpdateLink(link.Token, DateTime.Now.AddDays(1), PinCode, null, null);

            await Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await contentRepository.CleanupAsync(Page);
        }
    }

    [Fact]
    public async Task Login_Screen_Is_Shown()
    {
        var message = new HttpRequestMessage(HttpMethod.Get, fixture.GeneratedReviewLink.LinkUrl);
        var response = await fixture.Client.SendAsync(message);
        var text = await response.Content.ReadAsStringAsync();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("Go", text);
    }

    [Fact]
    public async Task User_Sees_404_After_Submitting_Wrong_PIN()
    {
        var siteDefinition = fixture.siteResolver.GetWebsite();

        var externalLoginUrl = new Uri(siteDefinition.Hosts.FirstOrDefault().Url,
            fixture.ExternalReviewOptions.Value.PinCodeSecurity.ExternalReviewLoginUrl);
        var failedLoginMessage = new HttpRequestMessage(HttpMethod.Post,
            externalLoginUrl);
        failedLoginMessage.Content = new FormUrlEncodedContent(new LoginModel
        {
            Code = "111111111111111", Token = fixture.GeneratedReviewLink.Token
        }.AsDictionary<string>());
        var failedLoginResponse = await fixture.Client.SendAsync(failedLoginMessage);
        Assert.Equal(HttpStatusCode.NotFound, failedLoginResponse.StatusCode);
    }

    [Fact]
    public async Task User_Sees_Content_Review_After_Submitting_Correct_PIN()
    {
        var siteDefinition = fixture.siteResolver.GetWebsite();

        var externalLoginUrl = new Uri(siteDefinition.Hosts.FirstOrDefault().Url,
            fixture.ExternalReviewOptions.Value.PinCodeSecurity.ExternalReviewLoginUrl);

        var loginMessage = new HttpRequestMessage(HttpMethod.Post,
            externalLoginUrl);
        loginMessage.Content = new FormUrlEncodedContent(new LoginModel
        {
            Code = fixture.PinCode, Token = fixture.GeneratedReviewLink.Token
        }.AsDictionary<string>());
        var loginResponse = await fixture.Client.SendAsync(loginMessage);
        var loginText = await loginResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, loginResponse.StatusCode);
        Assert.Contains(fixture.ReviewedPageContent, loginText);
    }
}
