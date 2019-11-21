﻿using System;
using System.Web.Routing;
using AdvancedExternalReviews.ReviewLinksRepository;
using EPiServer;
using EPiServer.Core;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Segments;

namespace AdvancedExternalReviews
{
    /// <summary>
    /// Partial router used to display readonly version of the page
    /// </summary>
    public class PagePreviewPartialRouter : IPartialRouter<PageData, PageData>
    {
        private readonly IContentLoader _contentLoader;
        private readonly ProjectContentResolver _projectContentResolver;
        private readonly IExternalReviewLinksRepository _externalReviewLinksRepository;
        private readonly ExternalReviewOptions _externalReviewOptions;

        public PagePreviewPartialRouter(IContentLoader contentLoader,
            IExternalReviewLinksRepository externalReviewLinksRepository, ExternalReviewOptions externalReviewOptions,
            ProjectContentResolver projectContentResolver)
        {
            _contentLoader = contentLoader;
            _externalReviewLinksRepository = externalReviewLinksRepository;
            _externalReviewOptions = externalReviewOptions;
            _projectContentResolver = projectContentResolver;
        }

        public PartialRouteData GetPartialVirtualPath(PageData content, string language, RouteValueDictionary routeValues, RequestContext requestContext)
        {
            return null;
        }

        public object RoutePartial(PageData content, SegmentContext segmentContext)
        {
            var nextSegment = segmentContext.GetNextValue(segmentContext.RemainingPath);
            if (string.IsNullOrWhiteSpace(nextSegment.Next))
            {
                return null;
            }

            if (!string.Equals(nextSegment.Next, _externalReviewOptions.ContentPreviewUrl, StringComparison.CurrentCultureIgnoreCase))
            {
                return null;
            }

            nextSegment = segmentContext.GetNextValue(nextSegment.Remaining);
            var token = nextSegment.Next;

            var externalReviewLink = _externalReviewLinksRepository.GetContentByToken(token);
            if (!externalReviewLink.IsPreviewableLink())
            {
                return null;
            }

            var contentReference = externalReviewLink.ContentLink;

            if (externalReviewLink.ProjectId.HasValue)
            {
                var contentContentLink = PreviewUrlResolver.IsGenerated(segmentContext.QueryString) ? content.ContentLink : contentReference;
                contentReference = _projectContentResolver.GetProjectReference(contentContentLink, externalReviewLink.ProjectId.Value);
                ExternalReview.ProjectId = externalReviewLink.ProjectId;
            }

            try
            {
                var page = _contentLoader.Get<IContent>(contentReference);
                segmentContext.RemainingPath = nextSegment.Remaining;
                ExternalReview.Token = token;

                return page;
            }
            catch (ContentNotFoundException)
            {
                return null;
            }
        }
    }
}
