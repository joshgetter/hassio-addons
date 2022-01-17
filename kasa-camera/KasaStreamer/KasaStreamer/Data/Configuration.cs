using System;
namespace KasaStreamer.Data
{
    public class Configuration
    {
        public string KasaUsername { get; set; }
        public string KasaPassword { get; set; }
        public CameraConfig[] Cameras { get; set; }
        public int? RetryLimit { get; set; }
        public float? RetrySleep { get; set; }
        public string ToggleEntity { get; set; }
        public int? LogLevel { get; set; }
    }

    public class CameraConfig
    {
        public string CameraName { get; set; }
        public string CameraIP { get; set; }
        public string VideoFilter { get; set; }
    }
}
