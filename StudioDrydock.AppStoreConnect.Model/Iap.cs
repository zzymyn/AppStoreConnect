using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class Iap
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? productId { get; set; }
    public AppStoreClient.InAppPurchaseV2.Attributes.InAppPurchaseType? inAppPurchaseType { get; set; }
    public AppStoreClient.InAppPurchaseV2.Attributes.State? state { get; set; }
    public string? reviewNote { get; set; }
    public bool? familySharable { get; set; }
    public bool? contentHosting { get; set; }

    public IapLocalization[]? localizations { get; set; }

    public Iap()
    {
    }

    public Iap(AppStoreClient.InAppPurchaseV2 data)
    {
        id = data.id;
        name = data.attributes?.name;
        productId = data.attributes?.productId;
        inAppPurchaseType = data.attributes?.inAppPurchaseType;
        state = data.attributes?.state;
        reviewNote = data.attributes?.reviewNote;
        familySharable = data.attributes?.familySharable;
        contentHosting = data.attributes?.contentHosting;
    }

    public void UpdateWithResponse(AppStoreClient.InAppPurchaseV2 data)
    {
        id = data.id;
        name = data.attributes?.name;
        productId = data.attributes?.productId;
        inAppPurchaseType = data.attributes?.inAppPurchaseType;
        state = data.attributes?.state;
        reviewNote = data.attributes?.reviewNote;
        familySharable = data.attributes?.familySharable;
        contentHosting = data.attributes?.contentHosting;
    }

    public AppStoreClient.InAppPurchaseV2CreateRequest CreateCreateRequest(string appId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    name = name!,
                    productId = productId!,
                    inAppPurchaseType = EnumExtensions<AppStoreClient.InAppPurchaseV2CreateRequest.Data.Attributes.InAppPurchaseType>.Convert(inAppPurchaseType)!.Value,
                    reviewNote = reviewNote,
                    familySharable = familySharable,
                },
                relationships = new()
                {
                    app = new()
                    {
                        data = new()
                        {
                            id = appId,
                        }
                    }
                }
            }
        };
    }

    public AppStoreClient.InAppPurchaseV2UpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    name = name,
                    reviewNote = reviewNote,
                    familySharable = familySharable,
                }
            }
        };
    }
}
