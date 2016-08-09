using System.Diagnostics;
using System.Threading;
using Nano.Web.Core.Host.HttpListener;

namespace Nano.Demo.SelfHost.Console
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            const string url = "http://localhost:4545";

            var exitEvent = new ManualResetEvent( false );

            // Hook up the Ctrl+C to exit the application
            System.Console.CancelKeyPress += ( sender, eventArgs ) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            // Get NanoConfiguration used by the demo projects. Replace this with your own code.
            var config = NanoConfigurationHelper.GetNanoConfiguration();

            // Specify your application name. A reasonable default is automatically used if not 
            // supplied but it's definitely recommended to supply one.
            config.ApplicationName = "Nano.Demo.SelfHost.Console";

            using ( var server = HttpListenerNanoServer.Start( config, url ) )
            {
                if ( Debugger.IsAttached )
                    Process.Start( url + "/ApiExplorer/" );
                
                System.Console.WriteLine( "Nano Server is running on: " + url );
                System.Console.WriteLine( "Press Ctrl+C to exit." );
                System.Console.WriteLine( config.ToString() );
                exitEvent.WaitOne();
            }
        }
    }
}