using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class IapLocalization
    {
        public string id { get; set; }
        public string name { get; set; }
        public string locale { get; set; }
        public string description { get; set; }
        public InAppPurchaseLocaliztionState state { get; set; }

        public IapLocalization()
        { }

        public IapLocalization(AppStoreClient.InAppPurchaseLocalizationsResponse.Data data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.locale = data.attributes.locale;
            this.description = data.attributes.description;
            this.state = EnumExtensions<InAppPurchaseLocaliztionState>.Convert(data.attributes.state.Value);
        }

        //internal AppStoreClient.AppStoreVersionLocalizationUpdateRequest CreateUpdateRequest()
        //{
        //    return new AppStoreClient.AppStoreVersionLocalizationUpdateRequest()
        //    {
        //        data = new AppStoreClient.AppStoreVersionLocalizationUpdateRequest.Data()
        //        {
        //            id = this.id,
        //            attributes = new AppStoreClient.AppStoreVersionLocalizationUpdateRequest.Data.Attributes()
        //            {
        //                description = this.description,
        //                keywords = this.keywords,
        //                marketingUrl = this.marketingUrl,
        //                promotionalText = this.promotionalText,
        //                supportUrl = this.supportUrl,
        //                whatsNew = this.whatsNew
        //            }
        //        }
        //    };
        //}
    }

}