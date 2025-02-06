using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class AppVersion
{
    public string? id { get; set; }
    public AppStoreClient.AppStoreVersion.Attributes.Platform? platform { get; set; }
    public string? versionString { get; set; }

    public AppStoreClient.AppStoreVersion.Attributes.AppStoreState? appStoreState { get; set; }
    public string? copyright { get; set; }

    public AppStoreClient.AppStoreVersion.Attributes.ReleaseType? releaseType { get; set; }
    public string? earliestReleaseDate { get; set; }
    public bool? downloadable { get; set; }
    public string? createdDate { get; set; }

    public AppVersionLocalization[]? localizations { get; set; }

    public AppVersion()
    {
    }

    public AppVersion(AppStoreClient.AppStoreVersion data)
    {
        id = data.id;
        platform = data.attributes?.platform;
        versionString = data.attributes?.versionString;
        appStoreState = data.attributes?.appStoreState;
        copyright = data.attributes?.copyright;
        releaseType = data.attributes?.releaseType;
        earliestReleaseDate = data.attributes?.earliestReleaseDate;
        downloadable = data.attributes?.downloadable;
        createdDate = data.attributes?.createdDate;
    }

    public void UpdateWithResponse(AppStoreClient.AppStoreVersion data)
    {
        id = data.id;
        platform = data.attributes?.platform;
        versionString = data.attributes?.versionString;
        appStoreState = data.attributes?.appStoreState;
        copyright = data.attributes?.copyright;
        releaseType = data.attributes?.releaseType;
        earliestReleaseDate = data.attributes?.earliestReleaseDate;
        downloadable = data.attributes?.downloadable;
        createdDate = data.attributes?.createdDate;
    }

    public AppStoreClient.AppStoreVersionCreateRequest CreateCreateRequest(string appId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    platform = EnumExtensions<AppStoreClient.AppStoreVersionCreateRequest.Data.Attributes.Platform>.Convert(platform)!.Value,
                    versionString = versionString!,
                    copyright = copyright,
                    releaseType = EnumExtensions<AppStoreClient.AppStoreVersionCreateRequest.Data.Attributes.ReleaseType>.Convert(releaseType),
                    earliestReleaseDate = earliestReleaseDate,
                },
                relationships = new()
                {
                    app = new()
                    {
                        data = new()
                        {
                            id = appId,
                        }
                    }
                },
            }
        };
    }

    public AppStoreClient.AppStoreVersionUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    versionString = versionString,
                    copyright = copyright,
                    releaseType = EnumExtensions<AppStoreClient.AppStoreVersionUpdateRequest.Data.Attributes.ReleaseType>.Convert(releaseType),
                    earliestReleaseDate = earliestReleaseDate,
                    downloadable = downloadable,
                },
            }
        };
    }
}