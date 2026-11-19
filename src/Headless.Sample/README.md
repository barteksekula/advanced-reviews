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
2. Set `Graph:SingleKey` for this app with `dotnet user-secrets set Graph:SingleKey <key>`. Set `Graph:Endpoint` too when Graph is not `https://cg.optimizely.com/content/v2`.
3. Run `run-headless.cmd` from the repository root, or run `Headless.AppHost` instead of `Alloy.Sample` from the IDE, with `dotnet run --project src/Headless.AppHost` or with `aspire run`. It needs the Aspire CLI, see https://get.aspire.dev. It starts `Alloy.Sample` on `http://localhost:8000` and this app on `http://localhost:8001`, writes the CMS URL to the console when Alloy responds and stops both when it stops. Content types are synchronized on startup and content by the Optimizely Graph synchronization jobs.
4. In CMS Settings, add a website application with
   * a Primary host `localhost:8001` using http
   * the preview URL format `{host}/preview?key={key}&ver={version}&loc={locale}&ctx={context}`
   * Use Preview Tokens selected
5. Create a view link or an editable link for a draft page under the website entry point and open it.
