namespace StudioDrydock.AppStoreConnect.Api;

public static class LogEx
{
    public static void StdLog(INestedLog? log, string subLog, Action<INestedLog?> body)
    {
        if (log != null)
            log = new NestedLogSubLog(log, subLog);
        try
        {
            log?.SetState(NestedLogState.Processing);
            body(log);
            log?.SetState(NestedLogState.Success);
        }
        catch (Exception ex)
        {
            log?.Log(LogLevel.Error, ex.Message);
            log?.SetState(NestedLogState.Failure);
            throw;
        }
    }

    public static T StdLog<T>(INestedLog? log, string subLog, Func<INestedLog?, T> body)
    {
        if (log != null)
            log = new NestedLogSubLog(log, subLog);
        try
        {
            log?.SetState(NestedLogState.Processing);
            var result = body(log);
            log?.SetState(NestedLogState.Success);
            return result;
        }
        catch (Exception ex)
        {
            log?.Log(LogLevel.Error, ex.Message);
            log?.SetState(NestedLogState.Failure);
            throw;
        }
    }

    public static async Task StdLog(INestedLog? log, string subLog, Func<INestedLog?, Task> body)
    {
        if (log != null)
            log = new NestedLogSubLog(log, subLog);
        try
        {
            log?.SetState(NestedLogState.Processing);
            await body(log);
            log?.SetState(NestedLogState.Success);
        }
        catch (Exception ex)
        {
            log?.Log(LogLevel.Error, ex.Message);
            log?.SetState(NestedLogState.Failure);
            throw;
        }
    }

    public static async Task<T> StdLog<T>(INestedLog? log, string subLog, Func<INestedLog?, Task<T>> body)
    {
        if (log != null)
            log = new NestedLogSubLog(log, subLog);
        try
        {
            log?.SetState(NestedLogState.Processing);
            var result = await body(log);
            log?.SetState(NestedLogState.Success);
            return result;
        }
        catch (Exception ex)
        {
            log?.Log(LogLevel.Error, ex.Message);
            log?.SetState(NestedLogState.Failure);
            throw;
        }
    }

    public static Task ForEachAsync<T>(IEnumerable<T> source, Func<T, Task> body)
    {
        return Task.WhenAll(source.Select(body));
    }

    public static Task ForEachAsyncLog<T>(IEnumerable<T> source, INestedLog? log, Func<T, string> subLogName, Func<T, INestedLog?, Task> body)
    {
        return ForEachAsync(source, item =>
        {
            return StdLog(log, subLogName(item), log =>
            {
                return body(item, log);
            });
        });
    }
}
