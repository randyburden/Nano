using System;
using System.Diagnostics;
using System.Threading;
using Nano.Web.Core;

namespace Nano.Demo
{
    /// <summary>
    /// Nano configuration helper.
    /// </summary>
    public static class NanoConfigurationHelper
    {
        /// <summary>
        /// Gets the <see cref="NanoConfiguration"/> used by all of the demo projects.
        /// </summary>
        /// <returns><see cref="NanoConfiguration"/> instance.</returns>
        public static NanoConfiguration GetNanoConfiguration()
        {
            DateTime startupDateTime = DateTime.Now;
            int requestCounter = 0;
            int errorCounter = 0;

            // Every Nano app begins with the creation of an instance of a NanoConfiguration.
            // This is *the* entry point into Nano and how all of Nano is configured.
            var config = new NanoConfiguration();
            
            config.GlobalEventHandler.UnhandledExceptionHandlers.Add((exception, context) =>
            {
                // Log your exception here, etc.

                Interlocked.Increment(ref errorCounter);
            });

            config.GlobalEventHandler.PreInvokeHandlers.Add(context =>
            {
                // Do stuff before an API method is called or file is accessed.
                // Examples: Logging requests, authentication, authorization, adding headers, starting timers, etc.

                Interlocked.Increment(ref requestCounter);
            });

            config.GlobalEventHandler.PostInvokeHandlers.Add(context =>
            {
                // Do stuff after an API method is called or file has been accessed.
                // Examples: Logging responses, writing cookies, adding headers, ending timers, etc.
            });

            // Serves up all methods in the Customer class under the URL: /api/customer/methodName
            config.AddMethods<Customer>();

            // We can also create event handlers that are not global.
            // This can be useful when certain APIs do or do not need logging, authentication, etc.
            var eventHandler = new Nano.Web.Core.EventHandler();

            // Let's add a custom header as a demonstration.
            eventHandler.PreInvokeHandlers.Add( context =>
            {
                context.Response.HeaderParameters.Add( "X-Message", "Hello World!" );
            });

            // Add all static methods in the Time class as well as specify to use custom event handler
            config.AddMethods<Time>( eventHandler: eventHandler );

            // Handles all requests for URL: /hi
            config.AddFunc("/hi", context => "Hello World!");

            // Handles all requests for URL: /howdy
            // Example: http://localhost:4545/howdy?person={"PersonId":1,"FirstName":"Clark","LastName":"Kent"}
            config.AddFunc("/howdy", x =>
            {
                // Looks for a complex request parameter named 'person' to bind to a 'Person' class
                var model = x.Bind<Customer.Person>("person");
                return model;
            });

            // Example of how to serve up a single file. It's much easier to map an entire directory though.
            config.AddFile( "/MultipartTester", @"\www\MultipartTester\index.html" );

            // When the Debugger is attached, map two folders up so that you can live edit files in Visual Studio 
            // without having to restart your application to get the files copied to your bin directory.
            config.AddDirectory("/", Debugger.IsAttached ? "../../www" : "www", returnHttp404WhenFileWasNotFound: true);

            // Enables CORS ( Cross-origin resource sharing ) requests
            config.EnableCors();

            // Configures a background task to run every 30 seconds that outputs some server stats like uptime and
            // the number of processed requests and errors encountered.
            config.AddBackgroundTask("Status Update", 30000, () =>
            {
                var result = string.Format("Uptime {0:dd\\.hh\\:mm\\:ss} | Requests Handled: {1} | Errors: {2}",
                    DateTime.Now - startupDateTime, requestCounter, errorCounter);

                Console.WriteLine(result);
                return result;
            });

            return config;
        }
    }
}