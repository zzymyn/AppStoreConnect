namespace StudioDrydock.AppStoreConnect.Core;

public interface INestedLog
{
    void SetState(NestedLogState state, params string[] path);
    void Log(NestedLogLevel state, string caption, params string[] path);

    INestedLog SubPath(params string[] path) => new NestedLogSubLog(this, path);
}
