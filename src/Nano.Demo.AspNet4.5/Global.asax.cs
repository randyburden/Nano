using System;
using System.Linq;
using System.Web;
using Nano.Web.Core.Host.SystemWeb;

#pragma warning disable 1591

namespace Nano.Demo.AspNet4._5
{
    public class Global : System.Web.HttpApplication
    {
        public override void Init()
        {
            StartupNano( this );
            base.Init();
        }

        public static void StartupNano( HttpApplication httpApplication )
        {
            var config = NanoConfigurationHelper.GetNanoConfiguration();

            config.RequestHandlers.Remove( config.RequestHandlers.First( handler => handler.UrlPath == "/" ) );
            config.AddDirectory( "/", "www", returnHttp404WhenFileWasNotFound: false );

            // Specify your application name. A reasonable default is automatically used if not 
            // supplied but it's definitely recommended to supply one.
            config.ApplicationName = "Nano.Demo.AspNet4.5";

            SystemWebNanoServer.Start( httpApplication, config );
        }

        protected void Application_Start( object sender, EventArgs e )
        {

        }

        protected void Session_Start( object sender, EventArgs e )
        {

        }

        protected void Application_BeginRequest( object sender, EventArgs e )
        {

        }

        protected void Application_AuthenticateRequest( object sender, EventArgs e )
        {

        }

        protected void Application_Error( object sender, EventArgs e )
        {

        }

        protected void Session_End( object sender, EventArgs e )
        {

        }

        protected void Application_End( object sender, EventArgs e )
        {

        }
    }
}