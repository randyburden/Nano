using System.Linq;
using System.Web;
using Nano.Demo.SelfHost;
using Nano.Web.Core;

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
                context.Items.Add( "Goodbye", "World" );
                context.Response.HeaderParameters.Add( "x-randy", "hi" );
            } );

            eventHandler.UnhandledExceptionHandlers.Add( ( exception, context ) =>
            {
                context.Items.Add( "Oops", exception );
            } );

            config.AddDirectory( "/", @"\www\" );

            config.AddMethods<Customer>( "/api/customer/", eventHandler );

            config.AddFile( "/home", @"\www\home\index.html" );

            config.AddFunc( "/hi", x => "Hello World! " + x.Request.Uri );

            config.AddFunc( "/swagger/swagger.json", x => "Hello World!" );

            config.AddFunc( "/howdy", x =>
            {
                var model = x.Bind<Person>( "person" );

                return model;
            } );

            config.AddFunc( "/doit", x =>
            {
                var model = x.Bind<int>( "personid" );

                return model;
            } );

            config.AddFunc( "/metadata/getroutes", context =>
            {
                return context.NanoConfiguration.RequestHandlers.Select( x => new
                {
                    UrlPath = x.UrlPath,
                    HandlerType = x.GetType().Name
                } );
            } );

            config.EnableCors();

            Nano.Web.Core.Host.SystemWeb.SystemWebNanoServer.Start( httpApplication, config );
        }

        public class Person
        {
            public int PersonId;
            public string FirstName;
            public string LastName;
        }
    }
}