using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

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

    public void UpdateWithResponse(AppStoreClient.GameCenterAchievementImage data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
    }

    public AppStoreClient.GameCenterAchievementImageUpdateRequest CreateUploadCompleteRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    uploaded = true,
                }
            }
        };
    }

    public static AppStoreClient.GameCenterAchievementImageCreateRequest CreateCreateRequest(string achLocId, int fileSize, string fileName)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    fileName = fileName,
                    fileSize = fileSize,
                },
                relationships = new()
                {
                    gameCenterAchievementLocalization = new()
                    {
                        data = new()
                        {
                            id = achLocId
                        }
                    }
                }
            }
        };
    }
}
