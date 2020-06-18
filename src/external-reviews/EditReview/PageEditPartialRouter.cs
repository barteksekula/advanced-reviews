﻿using System.Web;
using System.Web.Routing;
using AdvancedExternalReviews.ReviewLinksRepository;
using EPiServer;
using EPiServer.Core;
using EPiServer.ServiceLocation;
using EPiServer.Web;
using EPiServer.Web.Routing;
using EPiServer.Web.Routing.Segments;

namespace AdvancedExternalReviews.EditReview
{
    public class PageEditPartialRouter : IPartialRouter<PageData, PageData>
    {
        private readonly IContentLoader _contentLoader;
        private readonly IExternalReviewLinksRepository _externalReviewLinksRepository;
        private readonly ExternalReviewOptions _externalReviewOptions;

        public PageEditPartialRouter(IContentLoader contentLoader, IExternalReviewLinksRepository externalReviewLinksRepository, ExternalReviewOptions externalReviewOptions)
        {
            _contentLoader = contentLoader;
            _externalReviewLinksRepository = externalReviewLinksRepository;
            _externalReviewOptions = externalReviewOptions;
        }

        public PartialRouteData GetPartialVirtualPath(PageData content, string language, RouteValueDictionary routeValues, RequestContext requestContext)
        {
            return new PartialRouteData();
        }

        public object RoutePartial(PageData content, SegmentContext segmentContext)
        {
            if (!_externalReviewOptions.IsEnabled)
            {
                return null;
            }

            var nextSegment = segmentContext.GetNextValue(segmentContext.RemainingPath);
            if (nextSegment.Next != _externalReviewOptions.ContentIframeEditUrlSegment)
            {
                return null;
            }

            nextSegment = segmentContext.GetNextValue(nextSegment.Remaining);
            var token = nextSegment.Next;

            var externalReviewLink = _externalReviewLinksRepository.GetContentByToken(token);
            if (!externalReviewLink.IsEditableLink())
            {
                return null;
            }

            try
            {
                var page = _contentLoader.Get<IContent>(externalReviewLink.ContentLink);
                segmentContext.RemainingPath = nextSegment.Remaining;

                segmentContext.ContextMode = ContextMode.Edit;
                // set ContentLink in DataTokens to make IPageRouteHelper working
                segmentContext.RouteData.DataTokens[RoutingConstants.NodeKey] = page.ContentLink;

                var requestContext = ServiceLocator.Current.GetInstance<System.Web.Routing.RequestContext>();
                if (ContentReference.IsNullOrEmpty(requestContext.GetContentLink()))
                {
                    requestContext.SetContentLink(page.ContentLink);
                }

/*
                var routeCollection = ServiceLocator.Current.GetInstance<RouteCollection>();
                routeCollection[0].GetRouteData(requestContext.HttpContext).DataTokens[RoutingConstants.NodeKey] = page.ContentLink;
*/


                //var pageRouteHelper = ServiceLocator.Current.GetInstance<IPageRouteHelper>();


                ExternalReview.Token = token;

                return page;
            }
            catch
            {
                return null;
            }
        }
    }
}
