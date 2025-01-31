namespace StudioDrydock.AppStoreConnect.Model
{
    public class App(AppInfo appInfo, AppVersion[] appVersions)
	{
		public AppInfo appInfo { get; set; } = appInfo;
		public AppVersion[] appVersions { get; set; } = appVersions;
	}
}