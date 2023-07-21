using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class AppInfo
    {
        public string id { get; set; }
        public string bundleId { get; set; }
        public string name { get; set; }
        public string sku { get; set; }
        public string primaryLocale { get; set; }

        public AppInfo()
        {
        }

        public AppInfo(AppStoreClient.AppsResponse.Data data)
        {
            this.id = data.id;
            this.bundleId = data.attributes.bundleId;
            this.name = data.attributes.name;
            this.sku = data.attributes.sku;
            this.primaryLocale = data.attributes.primaryLocale;
        }

        public AppInfo(AppStoreClient.AppResponse.Data data)
        {
            this.id = data.id;
            this.bundleId = data.attributes.bundleId;
            this.name = data.attributes.name;
            this.sku = data.attributes.sku;
            this.primaryLocale = data.attributes.primaryLocale;
        }
    }
}