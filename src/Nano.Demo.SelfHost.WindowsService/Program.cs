using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Nano.Web.Core.Host.HttpListener;

namespace Nano.Demo.SelfHost.WindowsService
{
    internal class Program
    {
        /*
        Windows Service Install, Start, Stop, Query, Delete Commands:
        =============================================================
        sc create "Nano.Demo.SelfHost.WindowsService" binPath= "C:\PathToExecutable\Nano.Demo.SelfHost.WindowsService.exe ApplicationName Nano.Demo.SelfHost.WindowsService Uri http://localhost:8686" start= auto
        sc start "Nano.Demo.SelfHost.WindowsService"
        sc stop "Nano.Demo.SelfHost.WindowsService"
        sc query "Nano.Demo.SelfHost.WindowsService"
        sc delete "Nano.Demo.SelfHost.WindowsService"
        */
        private static void Main( string[] args )
        {
            var applicationName = args.GetArgumentValue( "ServiceName" ) ?? args.GetArgumentValue( "ApplicationName" ) ?? AppDomain.CurrentDomain.FriendlyName;
            var url = args.GetArgumentValue( "Url" ) ?? args.GetArgumentValue( "Uri" ) ?? "http://localhost:4545";

            WindowsService.Start( applicationName, () => Startup.Start( url, applicationName ), Startup.Stop );
        }

        private static class Startup
        {
            private static HttpListenerNanoServer _server;

            public static void Start( string url, string applicationName )
            {
                var config = NanoConfigurationHelper.GetNanoConfiguration();

                // Specify your application name. A reasonable default is automatically used if not 
                // supplied but it's definitely recommended to supply one.
                config.ApplicationName = applicationName ?? "Nano.Demo.SelfHost.WindowsService";

                _server = HttpListenerNanoServer.Start( config, url );

                url = _server.HttpListenerConfiguration.GetFirstUrlBeingListenedOn();

                if ( Debugger.IsAttached )
                    Process.Start( url + "ApiExplorer/" );

                Console.WriteLine( "Nano Server is running on: " + url );
            }

            public static void Stop()
            {
                if ( _server.HttpListenerConfiguration.HttpListener.IsListening )
                {
                    _server.HttpListenerConfiguration.HttpListener.Stop();
                }
            }
        }
    }

    /// <summary>
    /// Command-line argument helper.
    /// </summary>
    public static class CommandLineArgumentHelper
    {
        /// <summary>
        /// Gets an argument value where the argument name and value are separated by either 
        /// an equal ( = ) sign, a colon ( : ), or a space.
        /// </summary>
        /// <param name="arguments">Argument list.</param>
        /// <param name="argumentName">Argument name.</param>
        /// <returns>Argument value.</returns>
        public static string GetArgumentValue( this string[] arguments, string argumentName )
        {
            for ( int i = 0; i < arguments.Length; i++ )
            {
                var argument = arguments[ i ].ToLower();

                if ( argument.ToLower().Contains( argumentName.ToLower() ) )
                {
                    // Note that it's important to check for '=' before ':' because "key=http://" is expected
                    if ( argument.IndexOf( '=' ) != -1 )
                    {
                        // If there is a '=' in the argument that we thought was the key, then assume everything up to the first colon is the key and everything else is the value
                        var indexOfFirstEqual = arguments[ i ].IndexOf( '=' );
                        var value = arguments[ i ].Substring( indexOfFirstEqual + 1 );
                        return value;
                    }
                    if ( argument.IndexOf( ':' ) != -1 )
                    {
                        // If there is a ':' in the argument that we thought was the key, then assume everything up to the first colon is the key and everything else is the value
                        var indexOfFirstColon = arguments[ i ].IndexOf( ':' );
                        var value = arguments[ i ].Substring( indexOfFirstColon + 1 );
                        return value;
                    }

                    // Key and value are space delimited so take the next element as the value but make sure value is supplied
                    if ( arguments.Length > i + 1 )
                    {
                        return arguments[ i + 1 ];
                    }

                    throw new Exception( "Command line parameters aren't in the expected format.  Should be \"-key value\", \"-key:value\" or \"-key=value\" " );
                }
            }

            return null;
        }
    }

    /// <summary>
    /// Windows Service.
    /// </summary>
    public class WindowsService : System.ServiceProcess.ServiceBase
    {
        private readonly Action _onStart;
        private readonly Action _onStop;

        /// <summary>
        /// Windows Service.
        /// </summary>
        /// <param name="serviceName">Service name.</param>
        /// <param name="onStart">Function to execute on start of Windows Service.</param>
        /// <param name="onStop">Function to execute on stop of Windows Service.</param>
        public WindowsService(string serviceName, Action onStart, Action onStop)
        {
            SetCurrentDirectory();
            _onStart = onStart;
            _onStop = onStop;
            ServiceName = serviceName;
            EventLog.Log = "Application";
            CanStop = true;
        }

        /// <summary>
        /// Executed on start.
        /// </summary>
        /// <param name="args">Arguments supplied upon execution.</param>
        protected override void OnStart(string[] args)
        {
            _onStart();
        }

        /// <summary>
        /// Executed on stop.
        /// </summary>
        protected override void OnStop()
        {
            _onStop();
        }

        /// <summary>
        /// Starts the Windows Service.
        /// </summary>
        /// <param name="serviceName">Service name.</param>
        /// <param name="onStart">Function to execute on start of Windows Service.</param>
        /// <param name="onStop">Function to execute on stop of Windows Service.</param>
        public static void Start(string serviceName, Action onStart, Action onStop)
        {
            if (!Environment.UserInteractive)
            {
                // Running as a Windows Service
                using (var service = new WindowsService(serviceName, onStart, onStop))
                {
                    System.ServiceProcess.ServiceBase.Run(service);
                }
            }
            else
            {
                var exitEvent = new ManualResetEvent(false);

                // Running as Console application
                Console.CancelKeyPress += (sender, eventArgs) =>
                {
                    eventArgs.Cancel = true;
                    exitEvent.Set();
                };

                onStart();

                Console.WriteLine("Press Ctrl+C to exit.");
                exitEvent.WaitOne();

                onStop();
            }
        }

        /// <summary>
        /// Sets the current directory to the executing assemblies location because when running
        /// as a Windows Service the current directory will often not be the correct location.
        /// </summary>
        private static void SetCurrentDirectory()
        {
            // Set the current directory to the executable's location
            string path = System.Reflection.Assembly.GetExecutingAssembly().Location;

            path = System.IO.Path.GetDirectoryName(path);

            if (path == null)
            {
                // Gracefully terminate the service and write a message to the EventViewer log
                Environment.FailFast("Unable to locate executable location");
            }

            Directory.SetCurrentDirectory(path);
        }
    }
}