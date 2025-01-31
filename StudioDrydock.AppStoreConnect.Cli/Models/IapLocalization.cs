using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class IapLocalization
    {
        public string id { get; set; }
        public string name { get; set; }
        public string locale { get; set; }
        public string description { get; set; }
        public AppStoreClient.InAppPurchaseLocalization.Attributes.State state { get; set; }

        public IapLocalization()
        { }

        public IapLocalization(AppStoreClient.InAppPurchaseLocalization data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.locale = data.attributes.locale;
            this.description = data.attributes.description;
            this.state = data.attributes.state.Value;
        }

        internal void UpdateWithResponse(AppStoreClient.InAppPurchaseLocalization data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.locale = data.attributes.locale;
            this.description = data.attributes.description;
			this.state = data.attributes.state.Value;
		}

		public AppStoreClient.InAppPurchaseLocalizationCreateRequest CreateCreateRequest(string iapId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        name = this.name,
                        locale = this.locale,
                        description = this.description
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
                    id = this.id,
                    attributes = new()
                    {
                        name = this.name,
                        description = this.description
                    },
                }
            };
        }
    }

}