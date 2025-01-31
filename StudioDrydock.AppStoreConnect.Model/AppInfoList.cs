namespace StudioDrydock.AppStoreConnect.Model
{
    public class AppInfoList(AppInfo[] apps)
	{
		public AppInfo[] apps { get; set; } = apps;
	}
}