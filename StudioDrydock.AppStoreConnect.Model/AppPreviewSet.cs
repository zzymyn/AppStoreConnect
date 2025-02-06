using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class AppPreviewSet
{
    public string? id { get; set; }
    public AppStoreClient.AppPreviewSet.Attributes.PreviewType? previewType { get; set; }
    public AppPreview[]? appPreviews { get; set; }

    public AppPreviewSet()
    {
    }

    public AppPreviewSet(AppStoreClient.AppPreviewSet data)
    {
        id = data.id;
        previewType = data.attributes?.previewType;
    }

    public void UpdateWithResponse(AppStoreClient.AppPreviewSet data)
    {
        id = data.id;
        previewType = data.attributes?.previewType;
    }

    public AppStoreClient.AppPreviewSetCreateRequest CreateCreateRequest(string id)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    previewType = EnumExtensions<AppStoreClient.AppPreviewSetCreateRequest.Data.Attributes.PreviewType>.Convert(previewType)!.Value,
                },
                relationships = new()
                {
                    appStoreVersionLocalization = new()
                    {
                        data = new()
                        {
                            id = id,
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.AppPreviewSetAppPreviewsLinkagesRequest CreateUpdateRequest()
    {
        return new()
        {
            data = appPreviews!.Select(a => new AppStoreClient.AppPreviewSetAppPreviewsLinkagesRequest.Data
            {
                id = a.id!,
            }).ToArray(),
        };
    }
}
