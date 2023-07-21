using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class AppVersionLocalization
    {
        public string id { get; set; }
        public string locale { get; set; }
        public string description { get; set; }
        public string keywords { get; set; }
        public string promotionalText { get; set; }
        public string marketingUrl { get; set; }
        public string supportUrl { get; set; }
        public string whatsNew { get; set; }

        public AppVersionLocalization()
        { }

        public AppVersionLocalization(AppStoreClient.AppStoreVersionLocalizationsResponse.Data data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.description = data.attributes.description;
            this.keywords = data.attributes.keywords;
            this.promotionalText = data.attributes.promotionalText;
            this.marketingUrl = data.attributes.marketingUrl;
            this.supportUrl = data.attributes.supportUrl;
            this.whatsNew = data.attributes.whatsNew;
        }

        internal void UpdateWithResponse(AppStoreClient.AppStoreVersionLocalizationResponse.Data data)
        {
            this.id = data.id;
            this.locale = data.attributes.locale;
            this.description = data.attributes.description;
            this.keywords = data.attributes.keywords;
            this.promotionalText = data.attributes.promotionalText;
            this.marketingUrl = data.attributes.marketingUrl;
            this.supportUrl = data.attributes.supportUrl;
            this.whatsNew = data.attributes.whatsNew;
        }

        internal AppStoreClient.AppStoreVersionLocalizationCreateRequest CreateCreateRequest(string versionId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        description = this.description,
                        locale = this.locale,
                        keywords = this.keywords,
                        marketingUrl = this.marketingUrl,
                        promotionalText = this.promotionalText,
                        supportUrl = this.supportUrl,
                        whatsNew = this.whatsNew
                    },
                    relationships = new()
                    {
                        appStoreVersion = new()
                        {
                            data = new()
                            {
                                id = versionId,
                            }
                        }
                    }
                }
            };
        }

        internal AppStoreClient.AppStoreVersionLocalizationUpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        description = this.description,
                        keywords = this.keywords,
                        marketingUrl = this.marketingUrl,
                        promotionalText = this.promotionalText,
                        supportUrl = this.supportUrl,
                        whatsNew = this.whatsNew
                    }
                }
            };
        }
    }

}