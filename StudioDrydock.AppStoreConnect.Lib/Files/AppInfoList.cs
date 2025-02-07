using StudioDrydock.AppStoreConnect.Lib.Model;

namespace StudioDrydock.AppStoreConnect.Lib.Files;

public class AppInfoList(List<AppInfo> apps)
{
    public List<AppInfo> apps { get; set; } = apps;
}