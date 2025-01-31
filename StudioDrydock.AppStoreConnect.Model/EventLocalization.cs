using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model
{
    public class EventLocalization
    {
        public string? id { get; set; }
        public string? name { get; set; }
        public string? locale { get; set; }
        public string? shortDescription { get; set; }
        public string? longDescription { get; set; }

        public EventLocalization()
        {
        }

        public EventLocalization(AppStoreClient.AppEventLocalization data)
        {
            id = data.id;
            name = data.attributes?.name;
            locale = data.attributes?.locale;
            shortDescription = data.attributes?.shortDescription;
            longDescription = data.attributes?.longDescription;
        }

		public void UpdateWithResponse(AppStoreClient.AppEventLocalization data)
        {
            id = data.id;
            name = data.attributes?.name;
            locale = data.attributes?.locale;
            shortDescription = data.attributes?.shortDescription;
            longDescription = data.attributes?.longDescription;
        }

        public AppStoreClient.AppEventLocalizationCreateRequest CreateCreateRequest(string eventId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        name = name,
                        locale = locale!,
                        shortDescription = shortDescription,
                        longDescription = longDescription,
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
                    id = id!,
                    attributes = new()
                    {
                        name = name,
                        shortDescription = shortDescription,
                        longDescription = longDescription,
                    },
                }
            };
        }
    }

}