using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class AppVersionLocalization
{
    public string? id { get; set; }
    public string? locale { get; set; }
    public string? description { get; set; }
    public string? keywords { get; set; }
    public string? promotionalText { get; set; }
    public string? marketingUrl { get; set; }
    public string? supportUrl { get; set; }
    public string? whatsNew { get; set; }
    public ScreenshotSet[]? screenshotSets { get; set; }
    public AppPreviewSet[]? appPreviewSets { get; set; }

    public AppVersionLocalization()
    {
    }

    public AppVersionLocalization(AppStoreClient.AppStoreVersionLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        description = data.attributes?.description;
        keywords = data.attributes?.keywords;
        promotionalText = data.attributes?.promotionalText;
        marketingUrl = data.attributes?.marketingUrl;
        supportUrl = data.attributes?.supportUrl;
        whatsNew = data.attributes?.whatsNew;
    }

    public void UpdateWithResponse(AppStoreClient.AppStoreVersionLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        description = data.attributes?.description;
        keywords = data.attributes?.keywords;
        promotionalText = data.attributes?.promotionalText;
        marketingUrl = data.attributes?.marketingUrl;
        supportUrl = data.attributes?.supportUrl;
        whatsNew = data.attributes?.whatsNew;
    }

    public AppStoreClient.AppStoreVersionLocalizationCreateRequest CreateCreateRequest(string versionId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    description = description,
                    locale = locale!,
                    keywords = keywords,
                    marketingUrl = marketingUrl,
                    promotionalText = promotionalText,
                    supportUrl = supportUrl,
                    whatsNew = whatsNew
                },
                relationships = new()
                {
                    appStoreVersion = new()
                    {
                        data = new()
                        {
                            id = versionId,
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.AppStoreVersionLocalizationUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    description = description,
                    keywords = keywords,
                    marketingUrl = marketingUrl,
                    promotionalText = promotionalText,
                    supportUrl = supportUrl,
                    whatsNew = whatsNew
                }
            }
        };
    }
}