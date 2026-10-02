using Xunit;

namespace Advanced.CMS.AdvancedReviews.E2ETests;

[CollectionDefinition(Name, DisableParallelization = true)]
public class E2ETestCollection : ICollectionFixture<WebServerFixture>
{
    public const string Name = "Advanced Reviews E2E tests";
}
