using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using KasaStreamer.Data;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class HealthChecker
    {
        #region Fields
        /// <summary>
        /// How often health checks should be executed (in seconds).
        /// </summary>
        private const int CHECKINTERVAL = 60;
        private readonly ILogger<HealthChecker> _logger;
        private readonly int _retryLimit;
        private readonly CameraConfig _cameraConfig;
        private readonly System.Timers.Timer _timer;
        private int _currentRetries;
        private bool _isHealthy;
        private Task _prevTask;
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the stream is identified as unhealthy.
        /// </summary>
        public event EventHandler UnhealthyResult;

        /// <summary>
        /// Event raised when the maximum number of retries have been reached for an unhealthy stream.
        /// </summary>
        public event EventHandler MaxRetriesReached;
        #endregion

        #region Initializers
        public HealthChecker(ILogger<HealthChecker> logger, int? retryLimit, CameraConfig cameraConfig)
        {
            _logger = logger;
            _retryLimit = retryLimit ?? 5;
            _cameraConfig = cameraConfig;

            // Setup timer
            _timer = new System.Timers.Timer(TimeSpan.FromSeconds(CHECKINTERVAL).TotalMilliseconds);
            _timer.Elapsed += CheckHealth;
        }

        #endregion

        #region Methods
        /// <summary>
        /// Starts the health checker.
        /// </summary>
        public void Start()
        {
            if (_isHealthy)
            {
                // Reset retry counter
                _currentRetries = 0;
            }
            // Start the timer
            _timer.Start();
        }

        /// <summary>
        /// Stops the health checker.
        /// </summary>
        public void Stop()
        {
            _timer.Stop();
        }

        /// <summary>
        /// Performs a health check.
        /// </summary>
        private void CheckHealth(object sender, ElapsedEventArgs elapsedEventArgs)
        {
            if (_retryLimit != -1 && _currentRetries == _retryLimit)
            {
                _logger.LogWarning($"Max retry attempts reached. Retried {_retryLimit} times");
                MaxRetriesReached?.Invoke(this, new EventArgs());
                _timer.Stop();
                return;
            }

            if (_prevTask?.IsCompleted ?? true)
            {
                _prevTask = Task.Run(() =>
                {
                    _logger.LogInformation($"[{_cameraConfig.CameraName}] Starting health check");
                    var ffProbe = new Process();
                    ffProbe.StartInfo.UseShellExecute = false;
                    ffProbe.StartInfo.CreateNoWindow = true;
                    ffProbe.StartInfo.FileName = "ffprobe";

                    ffProbe.StartInfo.Arguments = $"-loglevel quiet rtmp://localhost:1935/live/{_cameraConfig.CameraName}";
                    ffProbe.Start();

                    // Wait for the process to exit or timeout
                    var didExit = ffProbe.WaitForExit((int)TimeSpan.FromSeconds(30).TotalMilliseconds);

                    // If the timeout occurred then lets assume camera is not healthy.
                    // An exit code of 0 indicates success otherwise.
                    _isHealthy = didExit && ffProbe.ExitCode == 0;
                    _logger.LogInformation($"[{_cameraConfig.CameraName}] {(_isHealthy ? "IS" : "IS NOT")} healthy");

                    ffProbe.Kill();
                    if (_isHealthy)
                    {
                        // Reset retry count since things seem good
                        _currentRetries = 0;
                    }
                    else
                    {
                        UnhealthyResult?.Invoke(this, new EventArgs());
                        _currentRetries++;
                    }
                });
            }
        }
        #endregion
    }
}