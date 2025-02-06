using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class AppPreview
{
    public string? id { get; set; }
    public int? fileSize { get; set; }
    public string? fileName { get; set; }
    public string? sourceFileChecksum { get; set; }
    public string? previewFrameTimeCode { get; set; }

    public AppPreview()
    {
    }

    public AppPreview(AppStoreClient.AppPreview data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
        sourceFileChecksum = data.attributes?.sourceFileChecksum;
        previewFrameTimeCode = data.attributes?.previewFrameTimeCode;
    }

    public void UpdateWithResponse(AppStoreClient.AppPreview data)
    {
        id = data.id;
        fileSize = data.attributes?.fileSize;
        fileName = data.attributes?.fileName;
        sourceFileChecksum = data.attributes?.sourceFileChecksum;
        previewFrameTimeCode = data.attributes?.previewFrameTimeCode;
    }

    public AppStoreClient.AppPreviewUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    previewFrameTimeCode = previewFrameTimeCode
                }
            }
        };
    }

    public AppStoreClient.AppPreviewUpdateRequest CreateUploadCompleteRequest(string fileHash)
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

    public AppStoreClient.AppPreviewCreateRequest CreateCreateRequest(string setId, int fileSize, string fileName)
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
                    appPreviewSet = new()
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
