using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Lib.Model;

public class IapLocalization
{
    public string? id { get; set; }
    public string? name { get; set; }
    public string? locale { get; set; }
    public string? description { get; set; }
    public AppStoreClient.InAppPurchaseLocalization.Attributes.State? state { get; set; }

    public IapLocalization()
    {
    }

    public IapLocalization(AppStoreClient.InAppPurchaseLocalization data)
    {
        id = data.id;
        name = data.attributes?.name;
        locale = data.attributes?.locale;
        description = data.attributes?.description;
        state = data.attributes?.state;
    }

    public void UpdateWithResponse(AppStoreClient.InAppPurchaseLocalization data)
    {
        id = data.id;
        name = data.attributes?.name;
        locale = data.attributes?.locale;
        description = data.attributes?.description;
        state = data.attributes?.state;
    }

    public AppStoreClient.InAppPurchaseLocalizationCreateRequest CreateCreateRequest(string iapId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    name = name!,
                    locale = locale!,
                    description = description
                },
                relationships = new()
                {
                    inAppPurchaseV2 = new()
                    {
                        data = new()
                        {
                            id = iapId,
                        }
                    }
                },
            }
        };
    }

    public AppStoreClient.InAppPurchaseLocalizationUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    name = name,
                    description = description
                },
            }
        };
    }
}