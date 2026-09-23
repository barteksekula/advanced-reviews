using Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;
using Advanced.CMS.ExternalReviews;
using Advanced.CMS.ExternalReviews.ManageLinks;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.ManageLinks;

[Collection(IntegrationTestCollection.Name)]
public class When_Component_Settings_Are_Read_Concurrently(SiteFixture siteFixture)
{
    private const int ParallelReaders = 16;
    private const int FreshComponents = 300;

    [Fact]
    public async Task Settings_Are_Built_Without_Corrupting_Shared_State()
    {
        var options = siteFixture.Services.GetRequiredService<IOptions<ExternalReviewOptions>>();
        var visitorGroupRepository = siteFixture.Services.GetRequiredService<IVisitorGroupRepository>();

        for (var round = 0; round < FreshComponents; round++)
        {
            var component = new ExternalReviewLinksManageComponent(options, visitorGroupRepository);
            using var barrier = new Barrier(ParallelReaders);

            var readers = Enumerable.Range(0, ParallelReaders).Select(_ => Task.Run(() =>
            {
                barrier.SignalAndWait();
                var settings = component.Settings;
                Assert.Equal(options.Value.IsEnabled, settings["isEnabled"]);
                Assert.Equal(options.Value.ProlongDays, settings["prolongDays"]);
                Assert.Equal(options.Value.EmailSubject, settings["initialMailSubject"]);
            }));

            await Task.WhenAll(readers);
        }
    }

    [Fact]
    public void Settings_Reads_Do_Not_Mutate_The_Definition_Settings()
    {
        var options = siteFixture.Services.GetRequiredService<IOptions<ExternalReviewOptions>>();
        var visitorGroupRepository = siteFixture.Services.GetRequiredService<IVisitorGroupRepository>();
        var component = new ExternalReviewLinksManageComponent(options, visitorGroupRepository);

        var first = component.Settings;
        var second = component.Settings;

        Assert.NotSame(first, second);
    }
}
