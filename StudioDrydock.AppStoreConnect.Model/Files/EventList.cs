namespace StudioDrydock.AppStoreConnect.Model.Files;

public class EventList(string appId, List<Event> events)
{
    public string appId { get; set; } = appId;
    public List<Event> events { get; set; } = events;
}
