using System;
using System.Text;
using KasaStreamer.Data;

namespace KasaStreamer
{
    public static class Extensions
    {
        /// <summary>
        /// Builds base64 encoded authorization string.
        /// </summary>
        public static string GetAuthorizationHeader(this Configuration config)
        {
            var encodedPassword = Convert.ToBase64String(Encoding.UTF8.GetBytes(config.KasaPassword));
            return Convert.ToBase64String(Encoding.UTF8.GetBytes($"{config.KasaUsername}:{encodedPassword}"));
        }
    }
}
