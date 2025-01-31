using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class EventLocalization
    {
        public string id { get; set; }
        public string name { get; set; }
        public string locale { get; set; }
        public string shortDescription { get; set; }
        public string longDescription { get; set; }

        public EventLocalization()
        { }

        public EventLocalization(AppStoreClient.AppEventLocalization data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.locale = data.attributes.locale;
            this.shortDescription = data.attributes.shortDescription;
            this.longDescription = data.attributes.longDescription;
        }

        internal void UpdateWithResponse(AppStoreClient.AppEventLocalization data)
        {
            this.id = data.id;
            this.name = data.attributes.name;
            this.locale = data.attributes.locale;
            this.shortDescription = data.attributes.shortDescription;
            this.longDescription = data.attributes.longDescription;
        }

        public AppStoreClient.AppEventLocalizationCreateRequest CreateCreateRequest(string eventId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        name = this.name,
                        locale = this.locale,
                        shortDescription = this.shortDescription,
                        longDescription = this.longDescription,
                    },
                    relationships = new()
                    {
                        appEvent = new()
                        {
                            data = new()
                            {
                                id = eventId,
                            }
                        }
                    },
                }
            };
        }

        public AppStoreClient.AppEventLocalizationUpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        name = this.name,
                        shortDescription = this.shortDescription,
                        longDescription = this.longDescription,
                    },
                }
            };
        }
    }

}