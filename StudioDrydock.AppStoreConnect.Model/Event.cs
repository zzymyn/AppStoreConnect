using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Model;

public class Event
{
    public string? id { get; set; }
    public string? referenceName { get; set; }
    public AppStoreClient.AppEvent.Attributes.Badge? badge { get; set; }
    public AppStoreClient.AppEvent.Attributes.EventState? eventState { get; set; }
    public string? deepLink { get; set; }
    public string? purchaseRequirement { get; set; }
    public string? primaryLocale { get; set; }
    public AppStoreClient.AppEvent.Attributes.Priority? priority { get; set; }
    public AppStoreClient.AppEvent.Attributes.Purpose? purpose { get; set; }
    public EventTerritorySchedule[]? territorySchedules { get; set; }
    public EventLocalization[]? localizations { get; set; }

    public Event()
    {
    }

    public Event(AppStoreClient.AppEvent data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        badge = data.attributes?.badge;
        eventState = data.attributes?.eventState;
        deepLink = data.attributes?.deepLink;
        purchaseRequirement = data.attributes?.purchaseRequirement;
        primaryLocale = data.attributes?.primaryLocale;
        priority = data.attributes?.priority;
        purpose = data.attributes?.purpose;
        territorySchedules = data.attributes?.territorySchedules?.Select(x => new EventTerritorySchedule()
        {
            territories = x.territories?.ToArray(),
            publishStart = x.publishStart,
            eventStart = x.eventStart,
            eventEnd = x.eventEnd
        }).ToArray();
    }

    public void UpdateWithResponse(AppStoreClient.AppEvent data)
    {
        id = data.id;
        referenceName = data.attributes?.referenceName;
        badge = data.attributes?.badge;
        eventState = data.attributes?.eventState;
        deepLink = data.attributes?.deepLink;
        purchaseRequirement = data.attributes?.purchaseRequirement;
        primaryLocale = data.attributes?.primaryLocale;
        priority = data.attributes?.priority;
        purpose = data.attributes?.purpose;
        territorySchedules = data.attributes?.territorySchedules?.Select(x => new EventTerritorySchedule()
        {
            territories = x.territories?.ToArray(),
            publishStart = x.publishStart,
            eventStart = x.eventStart,
            eventEnd = x.eventEnd
        }).ToArray();
    }

    public AppStoreClient.AppEventCreateRequest CreateCreateRequest(string appId)
    {
        return new()
        {
            data = new()
            {
                attributes = new()
                {
                    referenceName = referenceName!,
                    badge = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Badge>.Convert(badge),
                    deepLink = deepLink,
                    purchaseRequirement = purchaseRequirement,
                    primaryLocale = primaryLocale,
                    priority = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Priority>.Convert(priority),
                    purpose = EnumExtensions<AppStoreClient.AppEventCreateRequest.Data.Attributes.Purpose>.Convert(purpose),
                    territorySchedules = territorySchedules?.Select(x => new AppStoreClient.AppEventCreateRequest.Data.Attributes.TerritorySchedules()
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

    public AppStoreClient.AppEventUpdateRequest CreateUpdateRequest()
    {
        return new()
        {
            data = new()
            {
                id = id!,
                attributes = new()
                {
                    referenceName = referenceName,
                    badge = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Badge>.Convert(badge),
                    deepLink = deepLink,
                    purchaseRequirement = purchaseRequirement,
                    primaryLocale = primaryLocale,
                    priority = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Priority>.Convert(priority),
                    purpose = EnumExtensions<AppStoreClient.AppEventUpdateRequest.Data.Attributes.Purpose>.Convert(purpose),
                    territorySchedules = territorySchedules?.Select(x => new AppStoreClient.AppEventUpdateRequest.Data.Attributes.TerritorySchedules()
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
