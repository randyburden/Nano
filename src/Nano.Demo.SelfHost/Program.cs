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
            config.AddDirectory( "/", "www", null, true );
            config.AddMethods<Customer>( "/api/customer/" );
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