namespace StudioDrydock.AppStoreConnect.Model
{
    public class App
    {
        public AppInfo appInfo { get; set; }
        public AppVersion[] appVersions { get; set; }

		public App(AppInfo appInfo, AppVersion[] appVersions)
		{
			this.appInfo = appInfo;
			this.appVersions = appVersions;
		}
	}
}