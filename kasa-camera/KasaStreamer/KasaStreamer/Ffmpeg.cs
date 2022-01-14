using System;
using System.Diagnostics;
using System.Net;
using System.Threading.Tasks;
using KasaStreamer.Data;
using Microsoft.Extensions.Logging;

namespace KasaStreamer
{
    public class Ffmpeg
    {
        #region Fields
        private readonly CameraConfig _cameraConfig;
        private readonly ILogger _logger;
        private Process _ffmpeg;
        #endregion

        #region Initializers
        public Ffmpeg(ILogger<Ffmpeg> logger, Data.CameraConfig cameraConfig)
        {
            _logger = logger;
            _cameraConfig = cameraConfig;
        }
        #endregion

        #region Methods
        /// <summary>
        /// Starts the Ffmpeg process (in the background).
        /// </summary>
        /// <param name="audioPort">The TCP port that FFmpeg will receive audio data on.</param>
        /// <param name="videoPort">The TCP port that FFmpeg will receive video data on.</param>
        public void Start(int audioPort, int videoPort)
        {
            Task.Run(() =>
            {
                try
                {
                    _ffmpeg = new Process();
                    _ffmpeg.StartInfo.UseShellExecute = false;
                    _ffmpeg.StartInfo.CreateNoWindow = true;
                    _ffmpeg.StartInfo.WorkingDirectory = AppContext.BaseDirectory;
                    _ffmpeg.StartInfo.FileName = "ffmpeg";
                    _ffmpeg.StartInfo.RedirectStandardOutput = true;
                    _ffmpeg.StartInfo.RedirectStandardInput = true;
                    _ffmpeg.StartInfo.RedirectStandardError = true;
                    _ffmpeg.EnableRaisingEvents = true;
                    _ffmpeg.OutputDataReceived += LogDataReceived;
                    _ffmpeg.ErrorDataReceived += LogDataReceived;
                    _ffmpeg.StartInfo.Arguments = BuildCommandArguments(audioPort, videoPort);
                    _ffmpeg.Start();
                    _ffmpeg.BeginErrorReadLine();
                    _ffmpeg.BeginOutputReadLine();

                    _logger.LogInformation($"[{_cameraConfig.CameraName}] Ffmpeg started");
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            });
        }

        /// <summary>
        /// Produce a command string for Ffmpeg.
        /// </summary>
        private string BuildCommandArguments(int audioPort, int videoPort)
        {
            return
                     $"-use_wallclock_as_timestamps 1 " +
                     // VIDEO INPUT
                     $"-r 15 " +
                     $"-f h264 " +
                     $"-thread_queue_size 1024 " +
                     $"-vsync 1 " +
                     $"-i tcp://{IPAddress.Loopback}:{videoPort} " +
                     // AUDIO INPUT
                     $"-f mulaw " +
                     $"-ar 8000 " +
                     $"-async 1 " +
                     $"-i tcp://{IPAddress.Loopback}:{audioPort} " +
                     // VIDEO OUTPUT
                     $"-map 0:v:0 " +
                     $"-map 1:a:0 " +
                     $"-vcodec libx264 " +
                     $"-acodec aac " +
                     $"-f flv " +
                     (_cameraConfig.VideoFilter == null ? string.Empty : $"-vf {_cameraConfig.VideoFilter} ") +
                     $"rtmp://localhost:1935/live/{_cameraConfig.CameraName} " +
                     // SNAPSHOT OUTPUT
                     $"-map 0:v:0 " +
                     $"-r 1/5 " +
                     $"-update 1 " +
                     $"-y " +
                     (_cameraConfig.VideoFilter == null ? string.Empty : $"-vf {_cameraConfig.VideoFilter} ") +
                     $"/tmp/streaming/thumbnails/{_cameraConfig.CameraName}.jpg";
        }

        /// <summary>
        /// Write ffmpeg console logs to our console.
        /// </summary>
        private void LogDataReceived(object sender, DataReceivedEventArgs e)
        {
            var message = e?.Data?.Trim();
            if (string.IsNullOrWhiteSpace(message)) return;
            if (message.StartsWith("frame="))
            {
                // FFmpeg outputs a line containing FPS, drop rate, etc.. about every second. These should be trace only.
                _logger.LogTrace($"[Ffmpeg][{_cameraConfig.CameraName}] {message.Trim()}");
            }
            else
            {
                _logger.LogDebug($"[Ffmpeg][{_cameraConfig.CameraName}] {message.Trim()}");
            }
        }

        /// <summary>
        /// Stop the ffmpeg process.
        /// </summary>
        public void Stop()
        {
            _ffmpeg?.Kill();
            _logger.LogInformation($"[{_cameraConfig.CameraName}] Ffmpeg stopped");
        }
        #endregion
    }
}
