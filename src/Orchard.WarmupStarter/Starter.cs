using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Orchard.WarmupStarter
{
    public class Starter<T> where T : class
    {
        private readonly Func<T> _initialization;
        private readonly Action<HttpContext, T> _beginRequest;
        private readonly Action<HttpContext, T> _endRequest;
        private readonly object _synLock = new object();
        /// <summary>
        /// The result of the initialization queued work item.
        /// Set only when initialization has completed without errors.
        /// </summary>
        private volatile T _initializationResult;
        /// <summary>
        /// The (potential) error raised by the initialization thread. This is a "one-time"
        /// error signal, so that we can restart the initialization once another request
        /// comes in.
        /// </summary>
        private volatile Exception _error;
        /// <summary>
        /// The (potential) error from the previous initiazalition. We need to
        /// keep this error active until the next initialization is finished,
        /// so that we can keep reporting the error for all incoming requests.
        /// </summary>
        private volatile Exception _previousError;

        public Starter(Func<T> initialization, Action<HttpContext, T> beginRequest, Action<HttpContext, T> endRequest)
        {
            _initialization = initialization;
            _beginRequest = beginRequest;
            _endRequest = endRequest;
        }

        public void OnApplicationStart()
        {
            LaunchStartupThread();
        }

        /// <summary>
        /// The middleware notifying the begin and the end of the requests (formerly the Application_BeginRequest
        /// and Application_EndRequest events).
        /// </summary>
        public async Task InvokeAsync(HttpContext context, Func<Task> next)
        {
            OnBeginRequest(context);
            try
            {
                await next();
            }
            finally
            {
                OnEndRequest(context);
            }
        }

        public void OnBeginRequest(HttpContext context)
        {
            // Initialization resulted in an error
            if (_error != null)
            {
                // Save error for next requests and restart async initialization.
                // Note: The reason we have to retry the initialization is that the 
                //       application environment may change between requests,
                //       e.g. App_Data is made read-write for the AppPool.
                bool restartInitialization = false;

                lock (_synLock)
                {
                    if (_error != null)
                    {
                        _previousError = _error;
                        _error = null;
                        restartInitialization = true;
                    }
                }

                if (restartInitialization)
                {
                    LaunchStartupThread();
                }
            }

            // Previous initialization resulted in an error (and another initialization is running)
            if (_previousError != null)
            {
                throw new ApplicationException("Error during application initialization", _previousError);
            }

            // Only notify if the initialization has successfully completed
            if (_initializationResult != null)
            {
                _beginRequest(context, _initializationResult);
            }
        }

        public void OnEndRequest(HttpContext context)
        {
            // Only notify if the initialization has successfully completed
            if (_initializationResult != null)
            {
                _endRequest(context, _initializationResult);
            }
        }

        /// <summary>
        /// Run the initialization delegate asynchronously in a queued work item
        /// </summary>
        public void LaunchStartupThread()
        {
            // Make sure incoming requests are queued
            WarmupMiddleware.SignalWarmupStart();

            ThreadPool.QueueUserWorkItem(
                state =>
                {
                    try
                    {
                        var result = _initialization();
                        _initializationResult = result;
                    }
                    catch (Exception ex)
                    {
                        lock (_synLock)
                        {
                            _error = ex;
                            _previousError = null;
                        }
                    }
                    finally
                    {
                        // Execute pending requests as the initialization is over
                        WarmupMiddleware.SignalWarmupDone();
                    }
                });
        }
    }
}
