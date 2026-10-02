using System.Globalization;
using Advanced.CMS.ExternalReviews.ReviewLinksRepository;
using EPiServer.Applications;
using EPiServer.Authorization;
using EPiServer.Cms.Shell.UI.Rest;
using EPiServer.Core.Internal;
using EPiServer.DataAccess;
using EPiServer.Framework.Blobs;
using EPiServer.Security;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using TestSite.Models;

namespace Advanced.CMS.AdvancedReviews.IntegrationTests.Tooling;

public static class ContentRepositoryTestExtensions
{
    private static IContentRepository ContentRepository => ServiceLocator.Current.GetInstance<IContentRepository>();
    private static IApplicationRepository ApplicationRepository => ServiceLocator.Current.GetInstance<IApplicationRepository>();

    public static StandardPage CreatePage(this IContentRepository repo, string name = null)
    {
        var applications = ApplicationRepository.List();
        var website = applications.First() as InProcessWebsite;
        var page = repo.GetDefault<StandardPage>(website.EntryPoint);
        page.PageName = name ?? Guid.NewGuid().ToString();
        repo.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage CreatePageInDifferentLanguage(this IContentRepository repo, string name = null)
    {
        var applications = ApplicationRepository.List();
        var website = applications.First() as InProcessWebsite;
        var page = repo.GetDefault<StandardPage>(website.EntryPoint);
        page.PageName = (name ?? Guid.NewGuid().ToString()) + "sv";
        page.Language = new CultureInfo("sv");
        repo.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage ShowBreadcrumbs(this StandardPage page)
    {
        page.ShowBreadcrumbs = true;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage UpdatePage(this StandardPage page)
    {
        page.PageName += StaticTexts.UpdatedString;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static ImageFile CreateDraftImage(this IContentRepository repo, Media fakeImage)
    {
        var image = ContentRepository.GetDefault<ImageFile>(ContentReference.GlobalBlockFolder);
        image.Name = fakeImage.Name;
        image.Copyright = StaticTexts.FakeImageCopyright;
        var blobFactory = ServiceLocator.Current.GetInstance<IBlobFactory>();
        var blob = blobFactory.CreateBlob(image.BinaryDataContainer, ".jpg");
        blob.Write(new MemoryStream(fakeImage.Bytes));
        image.BinaryData = blob;

        var urlSegmentCreator = ServiceLocator.Current.GetInstance<IUrlSegmentCreator>();
        image.RouteSegment = urlSegmentCreator.Create(image, null);
        ContentRepository.Save(image, AccessLevel.NoAccess);
        return image;
    }

    public static VectorImageFile CreateDraftVectorImage(this IContentRepository repo, Media fakeImage)
    {
        var image = ContentRepository.GetDefault<VectorImageFile>(ContentReference.GlobalBlockFolder);
        image.Name = fakeImage.Name;
        var blobFactory = ServiceLocator.Current.GetInstance<IBlobFactory>();
        var blob = blobFactory.CreateBlob(image.BinaryDataContainer, ".svg");
        blob.Write(new MemoryStream(fakeImage.Bytes));
        image.BinaryData = blob;

        var urlSegmentCreator = ServiceLocator.Current.GetInstance<IUrlSegmentCreator>();
        image.RouteSegment = urlSegmentCreator.Create(image, null);
        ContentRepository.Save(image, AccessLevel.NoAccess);
        return image;
    }

    public static ImageFile PublishImage(this ImageFile image)
    {
        ContentRepository.Publish(image, AccessLevel.NoAccess);
        return image;
    }

    public static ImageFile ReplaceImageInProject(this ImageFile image, Media replacement, Project project)
    {
        var newVersion = (ImageFile)ContentRepository.Get<ImageFile>(image.ContentLink.ToReferenceWithoutVersion()).CreateWritableClone();
        var blob = ServiceLocator.Current.GetInstance<IBlobFactory>().CreateBlob(newVersion.BinaryDataContainer, ".jpg");
        blob.Write(new MemoryStream(replacement.Bytes));
        newVersion.BinaryData = blob;
        ContentRepository.Save(newVersion, SaveAction.Save | SaveAction.ForceNewVersion, AccessLevel.NoAccess);

        ServiceLocator.Current.GetInstance<ProjectRepository>().SaveItems([
            new ProjectItem
            {
                ProjectID = project.ID,
                ContentLink = newVersion.ContentLink,
                Language = CultureInfo.InvariantCulture,
                Category = "default"
            }
        ]);

        return newVersion;
    }

    public static StandardPage ReferenceImageInXhtml(this StandardPage page, IContent image)
    {
        page.Html = new XhtmlString($"<img id=\"{StaticTexts.XhtmlImageId}\" src=\"/link/{image.ContentGuid:N}.aspx\" />");
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage ReferenceUnpublishedImageInContentReference(this StandardPage page, ContentReference draftImageContentLink)
    {
        page.Image = draftImageContentLink;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage ReferenceUnpublishedImageInContentArea(this StandardPage page, ContentReference draftImageContentLink)
    {
        var contentArea = new ContentArea();
        contentArea.Items.Add(new ContentAreaItem
        {
            ContentLink = draftImageContentLink
        });

        page.ContentArea = contentArea;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage ReferenceUnpublishedImageInXhtml(this StandardPage page, ContentReference draftImageContentLink)
    {
        var contentFragmentFactory = ServiceLocator.Current.GetInstance<ContentFragmentFactory>();

        var html = new XhtmlString();
        var fragment = contentFragmentFactory.CreateContentFragment(draftImageContentLink, Guid.Empty, null);
        html.Fragments.Add(fragment);
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage AddBlock(this StandardPage page)
    {
        var block = ContentRepository.GetDefault<EditorialBlock>(ContentReference.GlobalBlockFolder);
        var blockContent = block as IContent;
        blockContent.Name = Guid.NewGuid().ToString();
        block.MainBody = StaticTexts.OriginalBlockContent;
        ContentRepository.Save(blockContent, AccessLevel.NoAccess);
        var contentAreaItem = new ContentAreaItem
        {
            ContentLink = blockContent.ContentLink,
        };
        var contentArea = new ContentArea();
        contentArea.Items.Add(contentAreaItem);
        page.ContentArea = contentArea;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage AddLinkInXhtml(this StandardPage page, IContent target)
    {
        page.Html = CreateLinkXhtml(target);
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage AddBlockWithLink(this StandardPage page, IContent target)
    {
        var block = ContentRepository.GetDefault<EditorialBlock>(ContentReference.GlobalBlockFolder);
        var blockContent = block as IContent;
        blockContent.Name = Guid.NewGuid().ToString();
        block.Html = CreateLinkXhtml(target);
        ContentRepository.Publish(blockContent, AccessLevel.NoAccess);
        var contentArea = new ContentArea();
        contentArea.Items.Add(new ContentAreaItem
        {
            ContentLink = blockContent.ContentLink,
        });
        page.ContentArea = contentArea;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    private static XhtmlString CreateLinkXhtml(IContent target) =>
        new($"<a id=\"{StaticTexts.LinkToOtherPageId}\" href=\"/link/{target.ContentGuid:N}.aspx\">{target.Name}</a>");

    public static StandardPage AddNestedBlock(this StandardPage page, bool useForThisPage = false)
    {
        //TODO: utilize `useForThisPage` to save also outside of the global asset folder

        var nestedBlock = ContentRepository.GetDefault<EditorialBlock>(ContentReference.GlobalBlockFolder);
        var nestedBlockContent = nestedBlock as IContent;
        nestedBlockContent.Name = Guid.NewGuid().ToString();
        nestedBlock.MainBody = StaticTexts.OriginalNestedBlockContent;
        ContentRepository.Save(nestedBlockContent, AccessLevel.NoAccess);

        var blockContentArea = new ContentArea();
        blockContentArea.Items.Add(new ContentAreaItem
        {
            ContentLink = nestedBlockContent.ContentLink,
        });
        page.ContentArea = blockContentArea;

        var block = ContentRepository.GetDefault<EditorialBlock>(ContentReference.GlobalBlockFolder);
        block.NestedContentArea = blockContentArea;
        var blockContent = block as IContent;
        blockContent.Name = Guid.NewGuid().ToString();
        ContentRepository.Save(blockContent, AccessLevel.NoAccess);

        var pageContentArea = new ContentArea();
        pageContentArea.Items.Add(new ContentAreaItem
        {
            ContentLink = blockContent.ContentLink,
        });
        page.ContentArea = pageContentArea;
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage UpdateBlock(this StandardPage page, Project project = null)
    {
        var projectRepository = ServiceLocator.Current.GetInstance<ProjectRepository>();
        var _blockReference = page.ContentArea.Items.FirstOrDefault().ContentLink;

        var block = ContentRepository.Get<EditorialBlock>(_blockReference).CreateWritableClone() as EditorialBlock;
        var blockContent = block as IContent;
        if (project != null)
        {
            block.MainBody = block.MainBody.Replace(StaticTexts.OriginalBlockContent, StaticTexts.ProjectUpdatedString)
                .Replace(StaticTexts.UpdatedString, StaticTexts.ProjectUpdatedString);
            ContentRepository.Save(blockContent, SaveAction.Save, AccessLevel.NoAccess);
            var projectItem = new ProjectItem
            {
                ProjectID = project.ID,
                ContentLink = blockContent.ContentLink,
                Language = blockContent is ILocalizable localizable
                    ? localizable.Language
                    : CultureInfo.InvariantCulture,
                Category = "default"
            };

            projectRepository.SaveItems([projectItem]);
        }
        else
        {
            block.MainBody = block.MainBody.Replace(StaticTexts.OriginalBlockContent, StaticTexts.UpdatedString)
                .Replace(StaticTexts.ProjectUpdatedString, StaticTexts.UpdatedString);
            var draft = ServiceLocator.Current.GetInstance<IContentVersionRepository>().LoadCommonDraft(_blockReference,
                LanguageSelector.AutoDetect().LanguageBranch);
            if (draft.IsCommonDraft)
            {
                ContentRepository.Save(blockContent, SaveAction.Save, AccessLevel.NoAccess);
            }
            else
            {
                ContentRepository.Save(blockContent, SaveAction.Save | SaveAction.ForceNewVersion,
                    AccessLevel.NoAccess);
            }
        }

        return page;
    }

    public static StandardPage WithoutEveryoneAccess(this StandardPage page)
    {
        var accessControlList = new AccessControlList
        {
            new AccessControlEntry(Roles.CmsAdmins, AccessLevel.Administer)
        };
        accessControlList.IsInherited = false;
        page.SaveSecurityInfo(ServiceLocator.Current.GetInstance<IContentSecurityRepository>(), accessControlList, SecuritySaveType.Replace);
        return page;
    }

    public static ExternalReviewLink GenerateExternalReviewLink(this StandardPage page, Project project = null)
    {
        return ServiceLocator.Current.GetInstance<IExternalReviewLinksRepository>().AddLink(page.ContentLink, false, TimeSpan.FromDays(1), project?.ID);
    }

    public static ExternalReviewLink ExpireReviewLink(this ExternalReviewLink externalReviewLink)
    {
        return ServiceLocator.Current.GetInstance<IExternalReviewLinksRepository>().UpdateLink(externalReviewLink.Token,
            DateTime.Now.Subtract(TimeSpan.FromSeconds(1)), null, null, null);
    }

    public static StandardPage PublishPage(this StandardPage page)
    {
        ContentRepository.Publish(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage Translate(this StandardPage page, string language = "sv", Project project = null)
    {
        var translatedPage = ContentRepository.CreateLanguageBranch<StandardPage>(page.ContentLink.ToReferenceWithoutVersion(), new CultureInfo(language));
        translatedPage.PageName = page.PageName + "_" + language;
        ContentRepository.Save(translatedPage, AccessLevel.NoAccess);
        if (project != null)
        {
            AddToProject(translatedPage, project);
        }
        return translatedPage;
    }

    public static StandardPage SetHtml(this StandardPage page, string text)
    {
        page.Html = new XhtmlString($"<p>{text}</p>");
        ContentRepository.Save(page, AccessLevel.NoAccess);
        return page;
    }

    public static StandardPage UpdateHtmlInNewVersion(this StandardPage page, string text, Project project = null)
    {
        var newVersion = (StandardPage)ContentRepository.Get<StandardPage>(page.ContentLink.ToReferenceWithoutVersion(), page.Language).CreateWritableClone();
        newVersion.Html = new XhtmlString($"<p>{text}</p>");
        ContentRepository.Save(newVersion, SaveAction.Save | SaveAction.ForceNewVersion, AccessLevel.NoAccess);
        if (project != null)
        {
            AddToProject(newVersion, project);
        }
        return newVersion;
    }

    private static void AddToProject(StandardPage page, Project project)
    {
        ServiceLocator.Current.GetInstance<ProjectRepository>().SaveItems([
            new ProjectItem
            {
                ProjectID = project.ID,
                ContentLink = page.ContentLink,
                Language = page.Language,
                Category = "default"
            }
        ]);
    }

    public static ContentReference CreateTargetFolder(this IContentRepository repo)
    {
        var folder = repo.GetDefault<ContentFolder>(ContentReference.GlobalBlockFolder);
        folder.Name = Guid.NewGuid().ToString();
        repo.Save(folder, AccessLevel.NoAccess);
        return folder.ContentLink;
    }

    public static Task CleanupAsync(this IContentRepository repo, IContent page)
    {
        var projectRepository = ServiceLocator.Current.GetInstance<ProjectRepository>();
        repo.Delete(page.ContentLink, true, AccessLevel.NoAccess);
        foreach (var contentType in ServiceLocator.Current.GetInstance<IContentTypeRepository>().List())
        {
            ResetContentType(contentType);
        }

        var projects = projectRepository.List().ToList();

        foreach (var project in projects)
        {
            projectRepository.Delete(project.ID);
        }

        return Task.CompletedTask;
    }

    private static void ResetContentType(ContentType contentType)
    {
        var propertyDefinitionRepository = ServiceLocator.Current.GetInstance<IPropertyDefinitionRepository>();
        var writableContentType = contentType.CreateWritableClone() as ContentType;
        foreach (var property in writableContentType.PropertyDefinitions)
        {
            if (property.ExistsOnModel)
            {
                continue;
            }

            //TODO: CMS13
            // propertyDefinitionRepository.Delete(property);
        }

        writableContentType.ResetContentType();
        ServiceLocator.Current.GetInstance<IContentTypeRepository>().Save(writableContentType);
    }
}
