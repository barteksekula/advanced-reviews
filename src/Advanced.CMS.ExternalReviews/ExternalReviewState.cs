using System.Collections.Concurrent;
using EPiServer.Personalization.VisitorGroups;
using Microsoft.AspNetCore.Http;

namespace Advanced.CMS.ExternalReviews;

internal class ExternalReviewState(IHttpContextAccessor httpContextAccessor)
{
    public static object locker = new();

    public string Token
    {
        get => httpContextAccessor.HttpContext?.Items["Token"] as string;
        set => httpContextAccessor.HttpContext.Items["Token"] = value;
    }

    public string PreferredLanguage
    {
        get => httpContextAccessor.HttpContext?.Items["PreferredLanguage"] as string;
        set => httpContextAccessor.HttpContext.Items["PreferredLanguage"] = value;
    }

    public bool IsEditLink
    {
        get => (string) httpContextAccessor.HttpContext?.Items["IsEditLink"] == bool.TrueString;
        set => httpContextAccessor.HttpContext.Items["IsEditLink"] = value.ToString();
    }

    /// <summary>
    /// VisitorGroupRole.ImpersonatedVisitorGroupByID is a special string from CMS Core which allows to impersonate as any VG
    /// </summary>
    public string[] ImpersonatedVisitorGroupsById
    {
        get => (string[]) httpContextAccessor.HttpContext?.Items[VisitorGroupRole.ImpersonatedVisitorGroupByID];
        set => httpContextAccessor.HttpContext.Items[VisitorGroupRole.ImpersonatedVisitorGroupByID] = value;
    }

    public IList<string> CustomLoaded
    {
        get
        {
            if (httpContextAccessor.HttpContext == null)
            {
                return new List<string>();
            }

            if (httpContextAccessor.HttpContext.Items["CustomLoaded"] as IList<string> == null)
            {
                lock (locker)
                {
                    if (httpContextAccessor.HttpContext.Items["CustomLoaded"] as IList<string> == null)
                    {
                        httpContextAccessor.HttpContext.Items["CustomLoaded"] = new List<string>();
                    }
                }
            }

            return httpContextAccessor.HttpContext.Items["CustomLoaded"] as IList<string>;
        }
    }

    public int? ProjectId
    {
        get => (int?) httpContextAccessor.HttpContext?.Items["ProjectId"];
        set => httpContextAccessor.HttpContext.Items["ProjectId"] = value;
    }

    public int LoadingContentCallCount
    {
        get => httpContextAccessor.HttpContext?.Items["LoadingContentCallCount"] as int? ?? 0;
        set => httpContextAccessor.HttpContext.Items["LoadingContentCallCount"] = value;
    }

    public bool IsInExternalReviewContext => !string.IsNullOrWhiteSpace(Token);
    public bool IsInProjectReviewContext => ProjectId.HasValue;

    public static object _linkLock = new object();

    private ConcurrentDictionary<string, IContent>  GetCachedLinksDictionary()
    {
        if (httpContextAccessor.HttpContext == null)
        {
            return null;
        }

        var key = "_cachedReviewLinks";
        if (httpContextAccessor.HttpContext.Items[key] as ConcurrentDictionary<string, IContent> == null)
        {
            lock (_linkLock)
            {
                if (httpContextAccessor.HttpContext.Items[key] as ConcurrentDictionary<string, IContent> == null)
                {
                    httpContextAccessor.HttpContext.Items[key] = new ConcurrentDictionary<string, IContent>();
                }
            }
        }

        return httpContextAccessor.HttpContext?.Items[key] as ConcurrentDictionary<string, IContent>;
    }

    public IContent GetCachedContent(ContentReference contentLink)
    {
        if (GetCachedLinksDictionary().TryGetValue(PreferredLanguage + "_" + contentLink.ToReferenceWithoutVersion(), out var result))
        {
            return result;
        }
        return null;
    }

    public void SetCachedLink(IContent contentLink)
    {
        GetCachedLinksDictionary()[PreferredLanguage + "_" + contentLink.ContentLink.ToReferenceWithoutVersion()] = contentLink;
    }
}
