using System;
using System.Diagnostics;
using System.Threading;
using Nano.Web.Core;
using Nano.Web.Core.Host.HttpListener;

namespace Nano.Demo.SelfHost
{
    internal class Program
    {
        private static void Main( string[] args )
        {
            const string url = "http://localhost:4545";

            var exitEvent = new ManualResetEvent( false );

            Console.CancelKeyPress += ( sender, eventArgs ) =>
            {
                eventArgs.Cancel = true;
                exitEvent.Set();
            };

            var config = new NanoConfiguration();

            // When the Debugger is attached, map two folders up so that you can live edit files in Visual Studio without having to restart
            // your application to get the files copied to your bin directory.
            config.AddDirectory( "/", Debugger.IsAttached ? "../../www" : "www", returnHttp404WhenFileWasNotFound: true );
            config.AddMethods<Customer>();
            config.AddMethods<Customer2>();
            config.AddFunc( "/hi", context => "Hello World!" );
            
            config.AddBackgroundTask( "Test", 30000, () =>
            {
                string result = "Hi, the time is now: " + DateTime.Now;
                Console.WriteLine( result );
                return result;
            } );

            using ( HttpListenerNanoServer.Start( config, url ) )
            {
                if ( Debugger.IsAttached )
                    Process.Start( url + "/ApiExplorer/" );

                Console.WriteLine( "Nano Server is running on: " + url );
                Console.WriteLine( "Press Ctrl+C to exit." );
				Console.WriteLine( config.ToString() );
                exitEvent.WaitOne();
            }
        }
    }
}