using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class GameCenterDetail
{
    public string? id { get; set; }
    public bool? arcadeEnabled { get; set; }
    public bool? challengeEnabled { get; set; }

    public GameCenterDetail()
    {
    }

    public GameCenterDetail(AppStoreClient.GameCenterDetail data)
    {
        id = data.id;
        arcadeEnabled = data.attributes?.arcadeEnabled;
        challengeEnabled = data.attributes?.challengeEnabled;
    }
}
