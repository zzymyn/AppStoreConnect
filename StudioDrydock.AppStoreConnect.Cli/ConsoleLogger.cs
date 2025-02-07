using StudioDrydock.AppStoreConnect.Api;

namespace StudioDrydock.AppStoreConnect.Cli;

internal sealed class ConsoleLogger : INestedLog
{
    private readonly TextWriter m_TextWriter = Console.Error;
    private readonly int m_Verbosity;

    public ConsoleLogger(int verbosity)
    {
        m_Verbosity = verbosity;
    }

    public void Log(LogLevel state, string caption, params string[] path)
    {
        lock (m_TextWriter)
        {
            switch (state)
            {
                case LogLevel.VerboseNote:
                case LogLevel.Note:
                    if (m_Verbosity < 3)
                        return;
                    break;
                case LogLevel.Warning:
                    if (m_Verbosity < 2)
                        return;
                    break;
                case LogLevel.Error:
                    break;
            }

            // use emojis for state:
            switch (state)
            {
                case LogLevel.Note:
                    m_TextWriter.Write("📝 ");
                    break;
                case LogLevel.Warning:
                    m_TextWriter.Write("⚠️ ");
                    break;
                case LogLevel.Error:
                    m_TextWriter.Write("❌ ");
                    break;
                case LogLevel.VerboseNote:
                    m_TextWriter.Write("🔍 ");
                    break;

            }

            m_TextWriter.Write(string.Join(" > ", path));

            if (!string.IsNullOrEmpty(caption))
            {
                m_TextWriter.Write(" > ");
                m_TextWriter.Write(caption);
            }
            m_TextWriter.WriteLine();
            m_TextWriter.Flush();
        }
    }

    public void SetState(NestedLogState state, params string[] path)
    {
        lock (m_TextWriter)
        {
            switch (state)
            {
                case NestedLogState.Pending:
                    if (m_Verbosity < 3)
                        return;
                    break;
                case NestedLogState.Warning:
                    if (m_Verbosity < 2)
                        return;
                    break;
                case NestedLogState.Success:
                    if (m_Verbosity < 1)
                        return;
                    break;
                case NestedLogState.Processing:
                case NestedLogState.Failure:
                    break;
            }

            // use emojis for state:
            switch (state)
            {
                case NestedLogState.Pending:
                    m_TextWriter.Write("⏳ ");
                    break;
                case NestedLogState.Warning:
                    m_TextWriter.Write("⚠️ ");
                    break;
                case NestedLogState.Success:
                    m_TextWriter.Write("✅ ");
                    break;
                case NestedLogState.Processing:
                    m_TextWriter.Write("🔄 ");
                    break;
                case NestedLogState.Failure:
                    m_TextWriter.Write("❌ ");
                    break;
            }

            m_TextWriter.Write(string.Join(" > ", path));
            m_TextWriter.WriteLine();
            m_TextWriter.Flush();
        }
    }
}