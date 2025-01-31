namespace StudioDrydock.AppStoreConnect.Model
{
    public class IapList
    {
		public Iap[] iaps { get; set; }

		public IapList(Iap[] iaps)
		{
			this.iaps = iaps;
		}
    }
}
