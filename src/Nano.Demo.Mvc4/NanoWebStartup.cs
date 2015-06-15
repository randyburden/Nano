using System.Linq;
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
                context.Items.Add( "Goodbye", "World" );
                context.Response.HeaderParameters.Add( "x-randy", "hi" );
            } );

            eventHandler.UnhandledExceptionHandlers.Add( ( exception, context ) =>
            {
                context.Items.Add( "Oops", exception );
            } );

            

            config.AddMethods<Customer>( "/api/customer/", eventHandler );

            config.AddFile( "/home", @"\www\home\index.html" );

            config.AddFunc( "/hi", x => "Hello World! " + x.Request.Url );

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

            config.AddFunc( "/Nano.Demo.Mvc4/getbaseurl", context =>
            {
                var baseUrl = GetBaseUrl();
                return baseUrl;
            } );

            config.EnableCors();
            
            config.AddFunc( "/probe", context => true );
            config.AddFunc( "/monitoring/probe", context => true );

            config.AddDirectory( "/", @"\www\" );

            SystemWebNanoServer.Start( httpApplication, config );
        }

        public static string GetBaseUrl()
        {
            var request = HttpContext.Current.Request;
            var appUrl = HttpRuntime.AppDomainAppVirtualPath;

            if( !string.IsNullOrWhiteSpace( appUrl ) ) appUrl += "/";

            var baseUrl = string.Format( "{0}://{1}{2}", request.Url.Scheme, request.Url.Authority, appUrl );

            return baseUrl;
        }

        public class Person
        {
            public int PersonId;
            public string FirstName;
            public string LastName;
        }
    }
}