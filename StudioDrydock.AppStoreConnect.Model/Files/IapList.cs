namespace StudioDrydock.AppStoreConnect.Model.Files;

public class IapList(string appId, List<Iap> iaps)
{
    public string appId { get; set; } = appId;
    public List<Iap> iaps { get; set; } = iaps;
}
