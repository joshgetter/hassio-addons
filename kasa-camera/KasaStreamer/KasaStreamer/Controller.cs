using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using KasaStreamer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class Controller : IHostedService
    {
        #region Fields
        private readonly ILogger<Controller> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Configuration _configuration;
        private readonly HealthCheckerFactory _healthCheckerFactory;
        private readonly HAListener _haListener;
        private readonly List<Camera> _cameras;
        #endregion

        #region Initializers
        public Controller(ILogger<Controller> logger, IServiceProvider serviceProvider, IConfiguration configuration, HealthCheckerFactory healthCheckerFactory, HAListener haListener)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
            _configuration = configuration.Get<Configuration>();
            _healthCheckerFactory = healthCheckerFactory;
            _haListener = haListener;

            _haListener.ToggleStateChanged += ToggleStateChanged;
            _cameras = new List<Camera>();
        }
        #endregion

        #region Methods
        /// <summary>
        /// Invoked when the max camera retries are reached.
        /// </summary>
        private async void CameraMaxRetriesReached(object sender, EventArgs e)
        {
            _logger.LogCritical("Shutting down");
            await StopAsync(new CancellationToken());
            Environment.Exit(1);
        }

        /// <summary>
        /// Invoked when the toggle entity changes state.
        /// </summary>
        private void ToggleStateChanged(object sender, HassClient.Models.StateChangedEvent e)
        {
            if (e?.NewState?.State?.Equals("on", StringComparison.CurrentCultureIgnoreCase) ?? false)
            {
                // Turn cameras on
                _logger.LogInformation("Toggle entity enabled. Starting camera stream(s).");
                _cameras.ForEach((camera) => camera.Start());
            }
            else
            {
                // Turn cameras off
                _logger.LogInformation("Toggle entity disabled. Stopping camera stream(s)");
                _cameras.ForEach((camera) => camera.Stop());
            }
        }

        /// <summary>
        /// Starts the controller.
        /// </summary>
        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Start HA Listener
            await _haListener.Start();
            // Get initial toggle state
            var isToggleEnabled = await _haListener.GetToggleState();

            // Initialize cameras
            foreach (var cameraConfig in _configuration.Cameras)
            {
                var camera = ActivatorUtilities.CreateInstance<Camera>(_serviceProvider, _healthCheckerFactory.GetHealthChecker(cameraConfig), cameraConfig);
                camera.MaxRetriesReached += CameraMaxRetriesReached;
                if (isToggleEnabled)
                {
                    camera.Start();
                }
                _cameras.Add(camera);
            }
        }

        /// <summary>
        /// Stops the controller.
        /// </summary>
        public async Task StopAsync(CancellationToken cancellationToken)
        {
            _cameras.ForEach((camera) => { camera.Stop(); });
            await _haListener.Stop();
        }
        #endregion
    }
}
