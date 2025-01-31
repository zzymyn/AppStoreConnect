using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class Iap
    {
        public string id { get; set; }
        public string name { get; set; }
        public string productId { get; set; }
        public AppStoreClient.InAppPurchaseV2.Attributes.InAppPurchaseType inAppPurchaseType { get; set; }
        public AppStoreClient.InAppPurchaseV2.Attributes.State state { get; set; }
        public string reviewNote { get; set; }
        public bool? familySharable { get; set; }
        public bool? contentHosting { get; set; }

        public IapLocalization[] localizations { get; set; }

        public Iap()
        { }

        public Iap(AppStoreClient.InAppPurchaseV2 data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.productId = data.attributes.productId;
            this.inAppPurchaseType = data.attributes.inAppPurchaseType.Value;
            this.state = data.attributes.state.Value;
            this.reviewNote = data.attributes.reviewNote;
            this.familySharable = data.attributes.familySharable;
            this.contentHosting = data.attributes.contentHosting;
        }

        internal void UpdateWithResponse(AppStoreClient.InAppPurchaseV2 data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.productId = data.attributes.productId;
			this.inAppPurchaseType = data.attributes.inAppPurchaseType.Value;
			this.state = data.attributes.state.Value;
			this.reviewNote = data.attributes.reviewNote;
            this.familySharable = data.attributes.familySharable;
            this.contentHosting = data.attributes.contentHosting;
        }

        internal AppStoreClient.InAppPurchaseV2CreateRequest CreateCreateRequest(string appId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        name = this.name,
                        productId = this.productId,
                        inAppPurchaseType = EnumExtensions<AppStoreClient.InAppPurchaseV2CreateRequest.Data.Attributes.InAppPurchaseType>.Convert(this.inAppPurchaseType),
                        reviewNote = this.reviewNote,
                        familySharable = this.familySharable,
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

        internal AppStoreClient.InAppPurchaseV2UpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        name = this.name,
                        reviewNote = this.reviewNote,
                        familySharable = this.familySharable,
                    }
                }
            };
        }
    }
}
