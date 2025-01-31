namespace StudioDrydock.AppStoreConnect.Model
{
    public class AppInfoList
    {
		public AppInfo[] apps { get; set; }

		public AppInfoList(AppInfo[] apps)
		{
			this.apps = apps;
		}
    }
}