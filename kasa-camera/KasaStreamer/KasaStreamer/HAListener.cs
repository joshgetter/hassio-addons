using System;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using HassClient.Models;
using HassClient.WS;
using KasaStreamer.Data;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class HAListener
    {
        #region Fields
        private readonly ILogger<HAListener> _logger;
        private readonly HassWSApi _webSocket;
        private readonly Configuration _config;
        private readonly bool _toggleEntityProvided;
        #endregion

        #region Events
        /// <summary>
        /// Raised when the toggle entity changes state.
        /// </summary>
        public event EventHandler<StateChangedEvent> ToggleStateChanged;
        #endregion

        #region Initializers
        public HAListener(ILogger<HAListener> logger, HassWSApi webSocket, Configuration config)
        {
            _logger = logger;
            _webSocket = webSocket;
            _config = config;
            _toggleEntityProvided = !string.IsNullOrWhiteSpace(config?.ToggleEntity);

        }
        #endregion

        #region Methods
        /// <summary>
        /// Start observing HA for state change events.
        /// </summary>
        public async Task Start()
        {
            if (_toggleEntityProvided)
            {
                var connectionParameters = ConnectionParameters.CreateForAddonConnection();
                await _webSocket.ConnectAsync(connectionParameters);
                _webSocket.StateChagedEventListener.SubscribeEntityStatusChanged(_config.ToggleEntity, ToggleStateChangedInternal);
            }
        }

        /// <summary>
        /// Stop observing HA for state change events.
        /// </summary>
        public async Task Stop()
        {
            if (_toggleEntityProvided)
            {
                await _webSocket.CloseAsync();
            }
        }

        /// <summary>
        /// Gets the initial toggle state.
        /// </summary>
        /// <returns>A boolean representing the current toggle entity's state.</returns>
        public async Task<bool> GetToggleState()
        {
            if (_toggleEntityProvided)
            {
                var states = await _webSocket.GetStatesAsync();
                var toggleState = states.FirstOrDefault(entity => entity.EntityId == _config.ToggleEntity)?.State?.Equals("on", StringComparison.CurrentCultureIgnoreCase) ?? true;
                _logger.LogInformation($"Initial toggle state: {(toggleState ? "Enabled" : "Disabled")}");
                return toggleState;
            }
            else
            {
                // Return true if the toggle entity isn't provided.
                return true;
            }
        }

        /// <summary>
        /// Invoked when the toggle state changes.
        /// </summary>
        private void ToggleStateChangedInternal(object sender, StateChangedEvent eventData)
        {
            ToggleStateChanged?.Invoke(this, eventData);
        }
        #endregion
    }
}
