using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class GameCenterLeaderboardSet
{
    public string? id { get; set; }
    public string? referenceName { get; set; }
    public string? vendorIdentifier { get; set; }
    public bool? live { get; set; }
    public GameCenterLeaderboardSetLocalization[]? localizations { get; set; }

    public GameCenterLeaderboardSet()
    {
    }

    public GameCenterLeaderboardSet(AppStoreClient.GameCenterLeaderboardSet data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        vendorIdentifier = data.attributes?.vendorIdentifier;
    }

    public void UpdateWithResponse(AppStoreClient.GameCenterLeaderboardSet data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        vendorIdentifier = data.attributes?.vendorIdentifier;
    }

    public AppStoreClient.GameCenterLeaderboardSetCreateRequest CreateCreateRequest(string detailId, string? groupId)
    {
        var req = new AppStoreClient.GameCenterLeaderboardSetCreateRequest()
        {
            data = new()
            {
                attributes = new()
                {
                    referenceName = referenceName!,
                    vendorIdentifier = vendorIdentifier!,
                },
                relationships = new()
                {
                },
            },
        };
        if (groupId != null)
        {
            req.data.relationships.gameCenterGroup = new()
            {
                data = new()
                {
                    id = groupId,
                }
            };
        }
        else
        {
            req.data.relationships.gameCenterDetail = new()
            {
                data = new()
                {
                    id = detailId,
                }
            };
        }
        return req;
    }

    public AppStoreClient.GameCenterLeaderboardSetUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    referenceName = referenceName,
                }
            }
        };
    }
}
