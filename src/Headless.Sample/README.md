# Headless.Sample

A minimal front-end that reads content from Optimizely Graph. It is used to test external review view links for headless websites.

## Routes

| Route | Description |
| ---- | ---- |
| `/` | Published pages, queried with the Graph single key |
| `/content/{key}?loc={locale}` | One published content item |
| `/preview?key={key}&ver={version}&loc={locale}&ctx={context}&preview_token={token}` | The previewed version, queried with the preview token as a bearer token |

Content pages list the properties of the content type with their values, read from the `_json` field. Metadata, the preview token claims and the raw Graph response are shown in collapsed sections.

When `ctx` is `edit`, the preview page loads `communicationinjector.js` from `Cms:Url` and marks each property with `data-epi-edit`. When an editor saves a property, the CMS raises `optimizely:cms:contentSaved` and the page loads the `previewUrl` from the event, which has the saved version and a new preview token. View links use `ctx=preview` and do not refresh, the reviewer opens the link again to see later changes. Editable links use `ctx=edit`, and the reviewer adds pins on the marked properties.

## Setup

1. Set the Graph keys for `Alloy.Sample` with user secrets, under `Optimizely:ContentGraph` with `GatewayAddress`, `AppKey`, `Secret` and `SingleKey`.
2. Run `Alloy.Sample`. Content types are synchronized on startup and content by the Optimizely Graph synchronization jobs.
3. Set `Graph:SingleKey` for this app with `dotnet user-secrets set Graph:SingleKey <key>`. Set `Graph:Endpoint` too when Graph is not `https://cg.optimizely.com/content/v2`.
4. Run the app, it listens on `http://localhost:8001`.
5. In CMS Settings, add a website application with
   * a Primary host `localhost:8001` using http
   * the preview URL format `{host}/preview?key={key}&ver={version}&loc={locale}&ctx={context}`
   * Use Preview Tokens selected
6. Create a view link or an editable link for a draft page under the website entry point and open it.
