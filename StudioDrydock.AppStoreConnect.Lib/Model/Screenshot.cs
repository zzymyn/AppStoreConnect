using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class Screenshot
{
    public string? id { get; set; }
    public int? fileSize { get; set; }
    public string? fileName { get; set; }
    public string? sourceFileChecksum { get; set; }

    public Screenshot()
    {
    }

    public Screenshot(AppStoreClient.AppScreenshot data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
        sourceFileChecksum = data.attributes?.sourceFileChecksum;
    }

    public void UpdateWithResponse(AppStoreClient.AppScreenshot data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
        sourceFileChecksum = data.attributes?.sourceFileChecksum;
    }

    public AppStoreClient.AppScreenshotUpdateRequest CreateUploadCompleteRequest(string fileHash)
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    uploaded = true,
                    sourceFileChecksum = fileHash,
                }
            }
        };
    }

    public AppStoreClient.AppScreenshotCreateRequest CreateCreateRequest(string setId, int fileSize, string fileName)
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
                    appScreenshotSet = new()
                    {
                        data = new()
                        {
                            id = setId,
                        }
                    }
                }
            }
        };
    }
}
