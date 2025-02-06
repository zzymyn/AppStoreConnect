using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class GameCenterAchievementImage
{
    public string? id { get; set; }
    public int? fileSize { get; set; }
    public string? fileName { get; set; }

    public GameCenterAchievementImage()
    {
    }

    public GameCenterAchievementImage(AppStoreClient.GameCenterAchievementImage data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
    }
}
