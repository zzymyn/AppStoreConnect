using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class ScreenshotSet
{
    public string? id { get; set; }
    public AppStoreClient.AppScreenshotSet.Attributes.ScreenshotDisplayType? screenshotDisplayType { get; set; }
    public Screenshot[]? screenshots { get; set; }

    public ScreenshotSet()
    {
    }

    public ScreenshotSet(AppStoreClient.AppScreenshotSet data)
    {
        id = data.id;
        screenshotDisplayType = data.attributes?.screenshotDisplayType;
    }

    public void UpdateWithResponse(AppStoreClient.AppScreenshotSet data)
    {
        id = data.id;
        screenshotDisplayType = data.attributes?.screenshotDisplayType;
    }

    public AppStoreClient.AppScreenshotSetCreateRequest CreateCreateRequest(string id)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    screenshotDisplayType = EnumExtensions<AppStoreClient.AppScreenshotSetCreateRequest.Data.Attributes.ScreenshotDisplayType>.Convert(screenshotDisplayType)!.Value,
                },
                relationships = new()
                {
                    appStoreVersionLocalization = new()
                    {
                        data = new()
                        {
                            id = id,
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.AppScreenshotSetAppScreenshotsLinkagesRequest CreateUpdateRequest()
    {
        return new()
        {
            data = screenshots!.Select(a => new AppStoreClient.AppScreenshotSetAppScreenshotsLinkagesRequest.Data
            {
                id = a.id!
            }).ToArray(),
        };
    }
}
