using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using KasaStreamer.Data;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class KasaStreamReader
    {
        #region Fields
        private readonly ILogger<KasaStreamReader> _logger;
        private readonly CameraConfig _cameraConfig;
        private readonly HttpClient _httpClient;
        private readonly TcpListener _audioListener;
        private readonly TcpListener _videoListener;
        private CancellationTokenSource _cancellationToken;
        private NetworkStream _audioStream;
        private NetworkStream _videoStream;
        #endregion

        #region Initializers
        public KasaStreamReader(ILogger<KasaStreamReader> logger, CameraConfig cameraConfig, HttpClient httpClient)
        {
            _logger = logger;
            _cameraConfig = cameraConfig;
            _httpClient = httpClient;

            // The listeners will find an available port
            _audioListener = new TcpListener(IPAddress.Loopback, 0);
            _videoListener = new TcpListener(IPAddress.Loopback, 0);
        }
        #endregion

        #region Methods
        /// <summary>
        /// Stop processing the camera stream.
        /// </summary>
        public void Stop()
        {
            if (_cancellationToken == null) throw new NullReferenceException("Cancellation token is null. Did you call Start() first?");
            _cancellationToken.Cancel();
        }

        /// <summary>
        /// Start processing the Camera stream.
        /// </summary>
        /// <returns>The audio/video TCP port numbers, once the reader is ready for incoming TCP connections.</returns>
        public (int audioPort, int videoPort) Start()
        {
            try
            {
                _cancellationToken = new CancellationTokenSource();

                // Reset connection state 
                _audioStream = _videoStream = null;

                // Start listening for incoming connections
                _audioListener.Start();
                _videoListener.Start();

                // Register connection listers (the bool tells the method which stream is connected).
                _audioListener.BeginAcceptSocket(ConnectionStarted, StreamType.Audio);
                _videoListener.BeginAcceptSocket(ConnectionStarted, StreamType.Video);

                // Start reading the camera stream. Don't await since we want this method to return once the reader is ready for connections (not when it's done reading the camera stream).
                StartSplittingStream(_cancellationToken.Token).ConfigureAwait(false);

                // Return the TCP ports that this camera will use. (Note these don't need to be exposed as they're only used locally in the docker container).
                return (((IPEndPoint)_audioListener.LocalEndpoint).Port, ((IPEndPoint)_videoListener.LocalEndpoint).Port);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, null);
                return (0, 0);
            }
        }

        /// <summary>
        /// Accepts a socket for an incoming TCP connection.
        /// </summary>
        /// <param name="asyncResult">Indicates the Stream Type that will be associated with the connection/socket.</param>
        private void ConnectionStarted(IAsyncResult asyncResult)
        {
            var streamType = (StreamType)asyncResult.AsyncState;
            switch (streamType)
            {
                case StreamType.Audio:
                    var audioSocket = _audioListener.EndAcceptSocket(asyncResult);
                    _audioStream = new NetworkStream(audioSocket);
                    _logger.LogDebug($"[{_cameraConfig.CameraName}] Ffmpeg connected to audio port.");
                    break;
                case StreamType.Video:
                    var videoSocket = _videoListener.EndAcceptSocket(asyncResult);
                    _videoStream = new NetworkStream(videoSocket);
                    _logger.LogDebug($"[{_cameraConfig.CameraName}] Ffmpeg connected to video port.");
                    break;
                default:
                    throw new ArgumentException("Invalid stream type provided");
            }
        }

        /// <summary>
        /// Reads the camera stream and splits the audio and video segments into individual streams.
        /// </summary>
        /// <param name="cancellationToken">Token to cancel the operation.</param>
        private async Task StartSplittingStream(CancellationToken cancellationToken)
        {
            try
            {
                // Get camera stream
                using var cameraStream = await _httpClient.GetStreamAsync($"https://{_cameraConfig.CameraIP}:19443/https/stream/mixed?video=h264&audio=g711&resolution=hd", cancellationToken);

                var reader = new MultipartReader("data-boundary--", cameraStream);

                _logger.LogDebug($"[{_cameraConfig.CameraName}] Started reading camera stream");

                var section = await reader.ReadNextSectionAsync();

                while (!cancellationToken.IsCancellationRequested && section != null)
                {
                    switch (section.ContentType)
                    {
                        case string contentType when contentType.Contains("audio", StringComparison.CurrentCultureIgnoreCase):
                            if (_audioStream != null)
                            {
                                await section.Body.CopyToAsync(_audioStream);
                            }
                            break;
                        case string contentType when contentType.Contains("video", StringComparison.CurrentCultureIgnoreCase):
                            if (_videoStream != null)
                            {
                                await section.Body.CopyToAsync(_videoStream);
                            }
                            break;
                        default:
                            Console.WriteLine("Received unknown multipart section. Dropping section.");
                            break;
                    }
                    section = await reader.ReadNextSectionAsync();
                }
                await _audioStream.FlushAsync();
                await _videoStream.FlushAsync();

                _logger.LogDebug($"[{_cameraConfig.CameraName}] Stopped reading camera stream");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[{_cameraConfig.CameraName}] An error occurred while reading camera stream.");
            }
        }
        #endregion
    }
}
