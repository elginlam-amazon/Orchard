using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Orchard.WarmupStarter
{
    /// <summary>
    /// Holds the incoming requests while the application is warming up (formerly the WarmupHttpModule IIS module).
    /// The requests for which a warmup copy or a static file exists are served immediately.
    /// </summary>
    public class WarmupMiddleware
    {
        private static readonly object _synLock = new object();
        private static IList<TaskCompletionSource<bool>> _awaiting = new List<TaskCompletionSource<bool>>();

        private readonly RequestDelegate _next;

        public WarmupMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        private static bool InWarmup()
        {
            lock (_synLock)
            {
                return _awaiting != null;
            }
        }

        /// <summary>
        /// Warmup code is about to start: Any new incoming request is queued 
        /// until "SignalWarmupDone" is called.
        /// </summary>
        public static void SignalWarmupStart()
        {
            lock (_synLock)
            {
                if (_awaiting == null)
                {
                    _awaiting = new List<TaskCompletionSource<bool>>();
                }
            }
        }

        /// <summary>
        /// Warmup code just completed: All pending requests in the "_await" queue are processed, 
        /// and any new incoming request is now processed immediately.
        /// </summary>
        public static void SignalWarmupDone()
        {
            IList<TaskCompletionSource<bool>> temp;

            lock (_synLock)
            {
                temp = _awaiting;
                _awaiting = null;
            }

            if (temp != null)
            {
                foreach (var awaiter in temp)
                {
                    awaiter.TrySetResult(true);
                }
            }
        }

        /// <summary>
        /// Returns a task completed when the warmup is over.
        /// </summary>
        private static Task Await()
        {
            lock (_synLock)
            {
                if (_awaiting == null)
                {
                    return Task.CompletedTask;
                }

                var awaiter = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                _awaiting.Add(awaiter);
                return awaiter.Task;
            }
        }

        public async Task InvokeAsync(HttpContext context)
        {
            if (InWarmup())
            {
                // host is not available: serve the warmup copy or the static file, otherwise hold the request
                if (await WarmupUtility.DoBeginRequestAsync(context))
                {
                    // the warmup copy has been served
                    if (context.Response.HasStarted)
                    {
                        return;
                    }
                }
                else
                {
                    // this is the "on hold" execution path
                    await Await();
                }
            }

            await _next(context);
        }
    }
}
