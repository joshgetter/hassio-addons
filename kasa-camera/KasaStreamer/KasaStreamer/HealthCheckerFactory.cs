using System;
using KasaStreamer.Data;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class HealthCheckerFactory
    {
        #region Fields
        private readonly IServiceProvider _serviceProvider;
        private readonly int? _retryLimit;
        #endregion

        public HealthCheckerFactory(IServiceProvider serviceProvider, Configuration config)
        {
            _serviceProvider = serviceProvider;
            _retryLimit = config.RetryLimit;
        }

        /// <summary>
        /// Create a new Health Checker instance.
        /// </summary>
        /// <param name="cameraConfig">The camera's configuration.</param>
        /// <returns>A health checker instance.</returns>
        public HealthChecker GetHealthChecker(CameraConfig cameraConfig)
        {
            return new HealthChecker(_serviceProvider.GetService<ILogger<HealthChecker>>(), _retryLimit, cameraConfig);
        }
    }
}
