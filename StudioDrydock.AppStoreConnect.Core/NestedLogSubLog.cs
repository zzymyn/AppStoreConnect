namespace StudioDrydock.AppStoreConnect.Core;

internal sealed class NestedLogSubLog(INestedLog parent, params string[] path)
    : INestedLog
{
    private readonly INestedLog m_Parent = parent;
    private readonly List<string> m_Path = new(path);

    public void Log(NestedLogLevel state, string caption, params string[] path)
    {
        m_Parent.Log(state, caption, [.. m_Path, .. path]);
    }

    public void SetState(NestedLogState state, params string[] path)
    {
        m_Parent.SetState(state, [.. m_Path, .. path]);
    }
}
