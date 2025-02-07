using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class GameCenterAchievement
{
    public string? id { get; set; }
    public string? referenceName { get; set; }
    public string? vendorIdentifier { get; set; }
    public int? points { get; set; }
    public bool? showBeforeEarned { get; set; }
    public bool? repeatable { get; set; }
    public bool? archived { get; set; }
    public bool? live { get; set; }
    public GameCenterAchievementLocalization[]? localizations { get; set; }

    public GameCenterAchievement()
    {
    }

    public GameCenterAchievement(AppStoreClient.GameCenterAchievement data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        vendorIdentifier = data.attributes?.vendorIdentifier;
        points = data.attributes?.points;
        showBeforeEarned = data.attributes?.showBeforeEarned;
        repeatable = data.attributes?.repeatable;
        archived = data.attributes?.archived;
    }

    public void UpdateWithResponse(AppStoreClient.GameCenterAchievement data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        vendorIdentifier = data.attributes?.vendorIdentifier;
        points = data.attributes?.points;
        showBeforeEarned = data.attributes?.showBeforeEarned;
        repeatable = data.attributes?.repeatable;
        archived = data.attributes?.archived;
    }

    public AppStoreClient.GameCenterAchievementCreateRequest CreateCreateRequest(string detailId, string? groupId)
    {
        var req = new AppStoreClient.GameCenterAchievementCreateRequest()
        {
            data = new()
            {
                attributes = new()
                {
                    referenceName = referenceName!,
                    vendorIdentifier = vendorIdentifier!,
                    points = points!.Value,
                    showBeforeEarned = showBeforeEarned!.Value,
                    repeatable = repeatable!.Value,
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

    public AppStoreClient.GameCenterAchievementUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    referenceName = referenceName,
                    points = (live == true) ? null : points, // Fix for: the field 'points' can not be modified in the current state
                    showBeforeEarned = showBeforeEarned,
                    repeatable = repeatable,
                    archived = archived
                }
            }
        };
    }
}
