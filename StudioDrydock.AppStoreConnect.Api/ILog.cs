namespace StudioDrydock.AppStoreConnect.Api;

public interface ILog
{
    void Log(LogLevel state, string caption, params string[] path);
}
