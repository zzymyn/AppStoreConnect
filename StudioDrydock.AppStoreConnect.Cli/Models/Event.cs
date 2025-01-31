using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli.Models
{
    public class Event
    {
        public string id { get; set; }
        public string referenceName { get; set; }
        public AppStoreClient.AppEvent.Attributes.Badge badge { get; set; }
        public AppStoreClient.AppEvent.Attributes.EventState eventState { get; set; }
        public string deepLink { get; set; }
        public string purchaseRequirement { get; set; }
        public string primaryLocale { get; set; }
        public AppStoreClient.AppEvent.Attributes.Priority priority { get; set; }
        public AppStoreClient.AppEvent.Attributes.Purpose purpose { get; set; }
        public EventTerritorySchedule[] territorySchedules { get; set; }
        public EventLocalization[] localizations { get; set; }

        public Event()
        { }

        public Event(AppStoreClient.AppEvent data)
        {
            this.id = data.id;
            this.referenceName = data.attributes.referenceName;
            this.badge = data.attributes.badge.Value;
            this.eventState = data.attributes.eventState.Value;
            this.deepLink = data.attributes.deepLink;
            this.purchaseRequirement = data.attributes.purchaseRequirement;
            this.primaryLocale = data.attributes.primaryLocale;
            this.priority = data.attributes.priority.Value;
            this.purpose = data.attributes.purpose.Value;
            this.territorySchedules = data.attributes.territorySchedules.Select(x => new EventTerritorySchedule()
            {
                territories = x.territories.ToArray(),
                publishStart = x.publishStart,
                eventStart = x.eventStart,
                eventEnd = x.eventEnd
            }).ToArray();
        }

        internal void UpdateWithResponse(AppStoreClient.AppEvent data)
        {
            this.id = data.id;
            this.referenceName = data.attributes.referenceName;
            this.badge = data.attributes.badge.Value;
            this.eventState = data.attributes.eventState.Value;
            this.deepLink = data.attributes.deepLink;
            this.purchaseRequirement = data.attributes.purchaseRequirement;
            this.primaryLocale = data.attributes.primaryLocale;
            this.priority = data.attributes.priority.Value;
            this.purpose = data.attributes.purpose.Value;
            this.territorySchedules = data.attributes.territorySchedules.Select(x => new EventTerritorySchedule()
            {
                territories = x.territories.ToArray(),
                publishStart = x.publishStart,
                eventStart = x.eventStart,
                eventEnd = x.eventEnd
            }).ToArray();
        }

        internal AppStoreClient.AppEventCreateRequest CreateCreateRequest(string appId)
        {
            return new()
            {
                data = new()
                {
                    attributes = new()
                    {
                        referenceName = this.referenceName,
                        badge = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Badge>.Convert(this.badge),
                        deepLink = this.deepLink,
                        purchaseRequirement = this.purchaseRequirement,
                        primaryLocale = this.primaryLocale,
                        priority = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Priority>.Convert(this.priority),
                        purpose = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Purpose>.Convert(this.purpose),
                        territorySchedules = this.territorySchedules.Select(x => new AppStoreClient.AppEventCreateRequest.Data.Attributes.TerritorySchedules()
                        {
                            territories = x.territories,
                            publishStart = x.publishStart,
                            eventStart = x.eventStart,
                            eventEnd = x.eventEnd
                        }).ToArray()
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

        internal AppStoreClient.AppEventUpdateRequest CreateUpdateRequest()
        {
            return new()
            {
                data = new()
                {
                    id = this.id,
                    attributes = new()
                    {
                        referenceName = this.referenceName,
                        badge = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Badge>.Convert(this.badge),
                        deepLink = this.deepLink,
                        purchaseRequirement = this.purchaseRequirement,
                        primaryLocale = this.primaryLocale,
                        priority = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Priority>.Convert(this.priority),
                        purpose = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Purpose>.Convert(this.purpose),
                        territorySchedules = this.territorySchedules.Select(x => new AppStoreClient.AppEventUpdateRequest.Data.Attributes.TerritorySchedules()
                        {
                            territories = x.territories,
                            publishStart = x.publishStart,
                            eventStart = x.eventStart,
                            eventEnd = x.eventEnd
                        }).ToArray()
                    }
                }
            };
        }
    }
}
