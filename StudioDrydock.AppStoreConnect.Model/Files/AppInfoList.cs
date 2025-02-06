namespace StudioDrydock.AppStoreConnect.Model.Files;

public class AppInfoList(List<AppInfo> apps)
{
    public List<AppInfo> apps { get; set; } = apps;
}