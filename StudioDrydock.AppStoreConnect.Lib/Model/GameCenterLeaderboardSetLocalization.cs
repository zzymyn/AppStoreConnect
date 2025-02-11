using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class GameCenterLeaderboardSetLocalization
{
    public string? id { get; set; }
    public string? locale { get; set; }
    public string? name { get; set; }
    public GameCenterLeaderboardSetImage? image { get; set; }

    public GameCenterLeaderboardSetLocalization()
    {
    }

    public GameCenterLeaderboardSetLocalization(AppStoreClient.GameCenterLeaderboardSetLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        name = data.attributes?.name;
    }

    public bool Matches(GameCenterLeaderboardSetLocalization other)
    {
        return string.IsNullOrEmpty(name) && string.IsNullOrEmpty(other.name) || name == other.name;
    }

    public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardSetLocalization data)
    {
        id = data.id;
        locale = data.attributes?.locale;
        name = data.attributes?.name;
    }

    public AppStoreClient.GameCenterLeaderboardSetLocalizationCreateRequest CreateCreateRequest(string lbsetId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    locale = locale!,
                    name = name!,
                },
                relationships = new()
                {
                    gameCenterLeaderboardSet = new()
                    {
                        data = new()
                        {
                            id = lbsetId
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.GameCenterLeaderboardSetLocalizationUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    name = name
                }
            }
        };
    }
}
