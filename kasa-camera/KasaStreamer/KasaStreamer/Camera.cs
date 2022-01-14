using System;
using System.Net.Http;
using System.Threading.Tasks;
using KasaStreamer.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class Camera
    {
        #region Fields
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<Camera> _logger;
        private readonly Data.CameraConfig _config;
        private readonly KasaStreamReader _kasaStreamReader;
        private readonly Ffmpeg _ffmpeg;
        private readonly HealthChecker _healthChecker;
        private readonly float _retrySleep;
        #endregion

        #region Events
        /// <summary>
        /// Event raised when the maximum number of retries have been reached for an unhealthy stream.
        /// </summary>
        public event EventHandler MaxRetriesReached
        {
            add
            {
                _healthChecker.MaxRetriesReached += value;
            }
            remove
            {
                _healthChecker.MaxRetriesReached -= value;
            }
        }
        #endregion

        #region Initializers
        public Camera(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<Camera> logger, Configuration config, HealthChecker healthChecker, CameraConfig cameraConfig)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
            _healthChecker = healthChecker;
            _config = cameraConfig;

            _retrySleep = config.RetrySleep ?? 30;
            _kasaStreamReader = new KasaStreamReader(serviceProvider.GetService<ILogger<KasaStreamReader>>(), _config, _httpClientFactory.CreateClient("KasaHttpClient"));
            _ffmpeg = new Ffmpeg(serviceProvider.GetService<ILogger<Ffmpeg>>(), cameraConfig);

            // Subscribe to health checker events
            _healthChecker.UnhealthyResult += RestartCamera;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Restart the camera stream.
        /// </summary>
        private void RestartCamera(object sender, EventArgs e)
        {
            Stop();
            Task.Delay(TimeSpan.FromSeconds(_retrySleep)).Wait();
            Start();
        }

        /// <summary>
        /// Starts streaming the camera.
        /// </summary>
        public void Start()
        {
            try
            {
                _logger.LogInformation($"[{_config.CameraName}] Starting camera");

                // Start stream processor
                var (audioPort, videoPort) = _kasaStreamReader.Start();

                // Start FFmpeg wrapper
                _ffmpeg.Start(audioPort, videoPort);

                // Start health checker
                _healthChecker.Start();
            }
            catch (Exception ex)
            {
                _logger.LogError(exception: ex, null);
            }
        }

        /// <summary>
        /// Stop streaming the camera.
        /// </summary>
        public void Stop()
        {
            _logger.LogInformation($"[{_config.CameraName}] Stopping camera");
            _ffmpeg.Stop();
            _kasaStreamReader.Stop();
            _healthChecker.Stop();
        }
        #endregion
    }
}
