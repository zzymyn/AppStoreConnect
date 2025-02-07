using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class GameCenterGroup
{
    public string? id { get; set; }
    public string? referenceName { get; set; }

    public GameCenterGroup()
    {
    }

    public GameCenterGroup(AppStoreClient.GameCenterGroup data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
    }
}
