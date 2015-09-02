using System;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

#pragma warning disable 1591

namespace Nano.Demo.Mvc4
{
    // Note: For instructions on enabling IIS6 or IIS7 classic mode, 
    // visit http://go.microsoft.com/?LinkId=9394801
    public class MvcApplication : HttpApplication
    {
        public override void Init()
        {
            NanoWebStartup.Start( this );
            base.Init();
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register( GlobalConfiguration.Configuration );
            FilterConfig.RegisterGlobalFilters( GlobalFilters.Filters );
            RouteConfig.RegisterRoutes( RouteTable.Routes );
        }

        protected void Application_BeginRequest( object sender, EventArgs e )
        {
        }

        void Application_Error( object sender, EventArgs e )
        {
        }
    }
}