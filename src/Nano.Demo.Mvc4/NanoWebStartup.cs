using System.Web;
using Nano.Web.Core;
using Nano.Web.Core.Host.SystemWeb;

#pragma warning disable 1591
namespace Nano.Demo.Mvc4
{
    public static class NanoWebStartup
    {
        public static void Start( HttpApplication httpApplication )
        {
            var config = new NanoConfiguration();

            config.GlobalEventHandler.PreInvokeHandlers.Add( context =>
            {
            } );

            config.GlobalEventHandler.UnhandledExceptionHandlers.Add((exception, context) =>
            {
            });

            var eventHandler = new EventHandler();
            eventHandler.PreInvokeHandlers.Add( context =>
            {
            } );

            eventHandler.PostInvokeHandlers.Add( context =>
            {
            } );

            eventHandler.UnhandledExceptionHandlers.Add( ( exception, context ) =>
            {
            } );

            config.AddMethods<Customer>(); // methods will be added under '/api/customer/'
            config.AddMethods<Customer2>();

            config.AddFile( "/home", @"\www\home\index.html" );

            config.AddFunc( "/hi", x => "Hello World! " + x.Request.Url );
            
            config.AddFunc( "/howdy", x =>
            {
                var model = x.Bind<Person>( "person" ); // Looks for a complex request parameter named 'person' to bind to a 'Person' class
                return model;
            } );

            config.EnableCors();
            
            config.AddFunc( "/probe", context => true );
            config.AddFunc( "/monitoring/probe", context => true );
            
            config.AddDirectory( "/", @"\www\" );

            SystemWebNanoServer.Start( httpApplication, config );
        }

        /// <summary>
        /// A person.
        /// </summary>
        public class Person
        {
            /// <summary>
            /// The person id.
            /// </summary>
            public int PersonId;

            /// <summary>
            /// The first name.
            /// </summary>
            public string FirstName;

            /// <summary>
            /// The last name.
            /// </summary>
            public string LastName;
        }
    }
}