
/************* TODO LIST *************

Nano.Server - open source on GitHub?
    - OnPreApiMethod, OnPostApiMethod, OnError event callbacks
    - support POST (get params from body?) 
       - header support?
    - nano host metadata (complete JSON metadata for a hosted API)
      - Nano.MetaData
        - Host this at a /metadata endpoint at both the root of the app and at every api
        - Let's suck in the .xml comments if found as optional metadata
        - Let's suck in the standard .net attributes as well? e.g. [Description]
    - WebAdminPage (aka Help Page) - list of apis/methods, method invocation web UI, and proxy emitter (Javascript and C#)
    - custom WebAdminPage request handler - would be used for wiring up a stats page in Ambit.Nano
    - better exception/error response handling

Ambit.Nano - default/common Ambit implementation helper for Nano
    - security implementation via OnPreApiMethod event callback
    - local request/response logging functions for OnPreApiMethod/OnPostApiMethod event callbacks
    - local error logging functions for OnError event callbacks
    - logging page as implmentation of custom WebAdminPage
    - logging help extension
    - heartbeat task (hardware configuration, CPU usage, memory usage, etc)
    - service registration?
    - log shipping task - will have its own filesystem log for storing shipping activity
    - helper method for auto registering all static class types in some folder (or attribute decorated types)

SSL Support Testing
 -- Hunt down the how to, the registration of SSL.. the whole she-bang
LOCALHOST and Machine IP Address and Machine Name on port testing
*/

using Nano.Core;
using Nano.Example.Api;
using Nano.Host;

namespace Nano.Example.Console
{
    class Program
    {
        static void Main( string[] args )
        {
            var config = new NanoServerConfiguration( "http://+:80/NanoExampleApi/" );

            config.AddWebApi<EmployeeRepository>(); // Example: http://localhost/nanoexampleapi/EmployeeRepository/AddEmployee?firstname=Clark&lastname=Kent

            config.AddWebApi<TestApi>(); // Example: http://localhost/nanoexampleapi/TestApi/VoidMethodWithNoInputOrOutput

            config.AddStaticFileServer( "www", "web" ); // Example: http://localhost/nanoexampleapi/web/helloworld.html

            // start server
            NanoServer.Start( config );

            System.Console.WriteLine();
            System.Console.WriteLine( "Press any key to quit." );
            System.Console.ReadKey();
        }
    }
}