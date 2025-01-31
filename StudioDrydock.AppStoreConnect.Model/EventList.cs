namespace StudioDrydock.AppStoreConnect.Model
{
    public class EventList
    {
		public Event[] events { get; set; }

		public EventList(Event[] events)
		{
			this.events = events;
		}
    }
}
