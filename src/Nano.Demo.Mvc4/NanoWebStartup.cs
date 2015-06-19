using System.Web;
using Nano.Web.Core;
using Nano.Web.Core.Host.SystemWeb;

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

            config.AddMethods<Customer>( "/api/customer/", eventHandler );

            config.AddFile( "/home", @"\www\home\index.html" );

            config.AddFunc( "/hi", x => "Hello World! " + x.Request.Url );
            
            config.AddFunc( "/howdy", x =>
            {
                var model = x.Bind<Person>( "person" );
                return model;
            } );

            config.EnableCors();
            
            config.AddFunc( "/probe", context => true );
            config.AddFunc( "/monitoring/probe", context => true );

            config.AddDirectory( "/", @"\www\" );

            SystemWebNanoServer.Start( httpApplication, config );
        }

        public class Person
        {
            public int PersonId;
            public string FirstName;
            public string LastName;
        }
    }
}