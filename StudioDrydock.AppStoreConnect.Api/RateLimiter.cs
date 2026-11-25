namespace StudioDrydock.AppStoreConnect.Api;

internal sealed class RateLimiter
{
    private const int MaxRequestsPerWindow = 250;
    private static readonly TimeSpan WindowDuration = TimeSpan.FromMinutes(1);

    private readonly Queue<DateTime> m_RequestTimestamps = new();
    private DateTime m_PauseUntil = DateTime.MinValue;
    private DateTime m_ThrottleUntil = DateTime.MinValue;
    private int m_RequestsRemainingThisHour = -1;
    private int m_OutstandingRequests = 0;

    public async Task<Token> Begin()
    {
        while (true)
        {
            Token? token = null;
            int? preReturnDelay = null;

            var now = DateTime.UtcNow;
            var windowStart = now - WindowDuration;

            lock (this)
            {
                while (m_RequestTimestamps.Count > 0 && m_RequestTimestamps.Peek() <= windowStart)
                {
                    m_RequestTimestamps.Dequeue();
                }

                if (now < m_PauseUntil)
                {
                    // we're currently paused due to a rate limit exceeded response from the API
                }
                else if (m_RequestTimestamps.Count >= MaxRequestsPerWindow)
                {
                    // we've exceeded the max requests per window, so we wait
                }
                else if (now < m_ThrottleUntil)
                {
                    // only allow one request per second while throttled:
                    if (m_OutstandingRequests == 0)
                    {
                        preReturnDelay = 1000;
                        token = CreateToken(now);
                    }
                }
                else if (m_RequestsRemainingThisHour < 0)
                {
                    // if m_RequestsRemainingThisHour is -1, we haven't yet made a request to the API to get the rate limit
                    // so only allow one request to be made at a time:

                    if (m_OutstandingRequests == 0)
                    {
                        token = CreateToken(now);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 1500)
                {
                    // more than 3000 requests remaining, allow 8 requests at a time:
                    if (m_OutstandingRequests < 8)
                    {
                        token = CreateToken(now);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 600)
                {
                    // less than 1500 requests remaining, but more than 600, allow one request every second:
                    if (m_OutstandingRequests == 0)
                    {
                        preReturnDelay = 1000;
                        token = CreateToken(now);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 0)
                {
                    // less than 600 requests remaining, allow one request every 10 seconds:
                    if (m_OutstandingRequests == 0)
                    {
                        preReturnDelay = 10000;
                        token = CreateToken(now);
                    }
                }
            }

            if (token != null)
            {
                if (preReturnDelay.HasValue)
                {
                    await Task.Delay(preReturnDelay.Value);
                }
                return token;
            }

            // TODO: don't just spin here, use a semaphore or something
            await Task.Delay(200);
        }
    }

    public void SetRequestsRemainingThisHour(int requestsRemainingThisHour)
    {
        lock (this)
        {
            m_RequestsRemainingThisHour = requestsRemainingThisHour;
        }
    }

    public void NotifyRateLimitExceeded(TimeSpan retryAfter, TimeSpan throttleFor)
    {
        lock (this)
        {
            var pauseUntil = DateTime.UtcNow + retryAfter;
            if (pauseUntil > m_PauseUntil)
            {
                m_PauseUntil = pauseUntil;
            }

            var throttleUntil = DateTime.UtcNow + retryAfter + throttleFor;
            if (throttleUntil > m_ThrottleUntil)
            {
                m_ThrottleUntil = throttleUntil;
            }
        }
    }

    private Token CreateToken(DateTime now)
    {
        m_RequestTimestamps.Enqueue(now);
        return new Token(this);
    }

    private void Take()
    {
        lock (this)
        {
            m_OutstandingRequests++;
        }
    }

    private void Release()
    {
        lock (this)
        {
            m_OutstandingRequests--;
        }
    }

    internal class Token : IDisposable
    {
        private readonly RateLimiter m_Limiter;

        internal Token(RateLimiter limiter)
        {
            m_Limiter = limiter;
            m_Limiter.Take();
        }

        public void Dispose()
        {
            m_Limiter.Release();
        }
    }
}
