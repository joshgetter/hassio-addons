using System;
using System.Net.Http;
using System.Net.Security;
using KasaStreamer.Data;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.WebSockets;
using System.Net.WebSockets;
using HassClient.WS;

namespace KasaStreamer
{
    class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Starting KasaStreamer");
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args)
        {
            // Setup configuration
            var config = LoadConfiguration();
            var parsedConfig = config.Get<Configuration>();

            return Host.CreateDefaultBuilder(args)

                .ConfigureServices(services =>
                {
                    services.AddSingleton(parsedConfig);
                    services.AddSingleton(config);

                    // Add health checker factory
                    services.AddSingleton<HealthCheckerFactory>();


                    // Setup Http Client
                    services.AddHttpClient("KasaHttpClient", (client) =>
                     {
                         client.DefaultRequestHeaders.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Basic", parsedConfig.GetAuthorizationHeader());
                     }).ConfigurePrimaryHttpMessageHandler(() =>
                     {
                         var allowedCipherSuites = Enum.GetValues<TlsCipherSuite>();
                         return new SocketsHttpHandler
                         {
                             SslOptions = new SslClientAuthenticationOptions
                             {
                                 CipherSuitesPolicy = new CipherSuitesPolicy(allowedCipherSuites),
                                 // Don't validate the Kasa Camera cert (it's self signed).
                                 RemoteCertificateValidationCallback = (sender, cert, chain, errs) =>
                                 {
                                     return true;
                                 }
                             }
                         };
                     });

                    // HA Listener
                    services.AddTransient<HassWSApi>();
                    services.AddTransient<HAListener>();

                    // Setup Controller
                    services.AddHostedService<Controller>();
                }
                
                // Setup Logging
                ).ConfigureLogging((sp, logging) =>
                {
                    logging.ClearProviders();
                    logging.AddConsole();
                    logging.SetMinimumLevel((LogLevel)(parsedConfig.LogLevel ?? 2));
                });
        }

        private static IConfiguration LoadConfiguration()
        {
            var configuration = new ConfigurationBuilder()
#if DEBUG
                .SetBasePath(AppContext.BaseDirectory)
#else
                .SetBasePath("/data")
#endif
                .AddJsonFile("options.json")
                           .AddEnvironmentVariables()
                           .Build();

            return configuration;
        }
    }
}
