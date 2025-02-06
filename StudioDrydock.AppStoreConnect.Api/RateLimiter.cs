using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StudioDrydock.AppStoreConnect.Api;

internal sealed class RateLimiter
{
    private int m_RequestsRemainingThisHour = -1;
    private int m_OutstandingRequests = 0;

    public async Task<Token> Begin()
    {
        while (true)
        {
            Token? token = null;
            int? preReturnDelay = null;

            lock (this)
            {
                // if m_RequestsRemainingThisHour is -1, we haven't yet made a request to the API to get the rate limit
                // so only allow one request to be made at a time:
                if (m_RequestsRemainingThisHour < 0)
                {
                    if (m_OutstandingRequests == 0)
                    {
                        token = new Token(this);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 1500)
                {
                    // more than 3000 requests remaining, allow 8 requests at a time:
                    if (m_OutstandingRequests < 8)
                    {
                        token = new Token(this);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 600)
                {
                    // less than 1500 requests remaining, but more than 600, allow one request every second:
                    if (m_OutstandingRequests == 0)
                    {
                        preReturnDelay = 1000;
                        token = new Token(this);
                    }
                }
                else if (m_RequestsRemainingThisHour - m_OutstandingRequests > 0)
                {
                    // less than 600 requests remaining, allow one request every 10 seconds:
                    if (m_OutstandingRequests == 0)
                    {
                        preReturnDelay = 10000;
                        token = new Token(this);
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
