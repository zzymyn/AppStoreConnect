using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class GameCenterLeaderboardLocalization
{
    public string? id { get; set; }
    public string? locale { get; set; }
    public string? name { get; set; }
    public AppStoreClient.GameCenterLeaderboardLocalization.Attributes.FormatterOverride? formatterOverride { get; set; }
    public string? formatterSuffix { get; set; }
    public string? formatterSuffixSingular { get; set; }
    public GameCenterLeaderboardImage? image { get; set; }

    public GameCenterLeaderboardLocalization()
    {
    }

    public GameCenterLeaderboardLocalization(AppStoreClient.GameCenterLeaderboardLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        name = data.attributes?.name;
        formatterOverride = data.attributes?.formatterOverride;
        formatterSuffix = data.attributes?.formatterSuffix;
        formatterSuffixSingular = data.attributes?.formatterSuffixSingular;
    }

    public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        name = data.attributes?.name;
        formatterOverride = data.attributes?.formatterOverride;
        formatterSuffix = data.attributes?.formatterSuffix;
        formatterSuffixSingular = data.attributes?.formatterSuffixSingular;
    }

    public AppStoreClient.GameCenterLeaderboardLocalizationCreateRequest CreateCreateRequest(string lbId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    locale = locale!,
                    name = name!,
                    formatterOverride = EnumExtensions<AppStoreClient.GameCenterLeaderboardLocalizationCreateRequest.Data.Attributes.FormatterOverride>.Convert(formatterOverride),
                    formatterSuffix = formatterSuffix,
                    formatterSuffixSingular = formatterSuffixSingular
                },
                relationships = new()
                {
                    gameCenterLeaderboard = new()
                    {
                        data = new()
                        {
                            id = lbId
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.GameCenterLeaderboardLocalizationUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    name = name,
                    formatterOverride = EnumExtensions<AppStoreClient.GameCenterLeaderboardLocalizationUpdateRequest.Data.Attributes.FormatterOverride>.Convert(formatterOverride),
                    formatterSuffix = formatterSuffix,
                    formatterSuffixSingular = formatterSuffixSingular
                }
            }
        };
    }
}
