namespace StudioDrydock.AppStoreConnect.Api;

public interface INestedLog
    : ILog
{
    void SetState(NestedLogState state, params string[] path);
}
