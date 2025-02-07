using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class GameCenterLeaderboardSetImage
{
    public string? id { get; set; }
    public int? fileSize { get; set; }
    public string? fileName { get; set; }

    public GameCenterLeaderboardSetImage()
    {
    }

    public GameCenterLeaderboardSetImage(AppStoreClient.GameCenterLeaderboardSetImage data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
    }
}
