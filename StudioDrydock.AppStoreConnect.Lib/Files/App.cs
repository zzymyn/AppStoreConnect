using StudioDrydock.AppStoreConnect.Lib.Model;

namespace StudioDrydock.AppStoreConnect.Lib.Files;

public class App(string appId, AppInfo appInfo, List<AppVersion> appVersions)
{
    public string appId { get; set; } = appId;
    public AppInfo appInfo { get; set; } = appInfo;
    public List<AppVersion> appVersions { get; set; } = appVersions;
}