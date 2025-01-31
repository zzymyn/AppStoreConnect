using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class AppInfo
    {
        public string? id { get; set; }
        public string? bundleId { get; set; }
        public string? name { get; set; }
        public string? sku { get; set; }
        public string? primaryLocale { get; set; }

        public AppInfo()
        {
        }

        public AppInfo(AppStoreClient.App data)
        {
            id = data.id;
            bundleId = data.attributes?.bundleId;
            name = data.attributes?.name;
            sku = data.attributes?.sku;
            primaryLocale = data.attributes?.primaryLocale;
        }
    }
}