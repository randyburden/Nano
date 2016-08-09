using System.Linq;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;
using Nano.Web.Core.Host.SystemWeb;

#pragma warning disable 1591
namespace Nano.Demo.AspNet
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public override void Init()
        {
            StartupNano(this);
            base.Init();
        }

        public static void StartupNano(HttpApplication httpApplication)
        {
            // Get NanoConfiguration used by the demo projects. Replace this with your own code.
            var config = NanoConfigurationHelper.GetNanoConfiguration();

            config.RequestHandlers.Remove(config.RequestHandlers.First(handler => handler.UrlPath == "/"));
            config.AddDirectory("/", "www", returnHttp404WhenFileWasNotFound: false);

            // Specify your application name. A reasonable default is automatically used if not 
            // supplied but it's definitely recommended to supply one.
            config.ApplicationName = "Nano.Demo.AspNet";

            SystemWebNanoServer.Start(httpApplication, config);
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            WebApiConfig.Register( GlobalConfiguration.Configuration );
            FilterConfig.RegisterGlobalFilters( GlobalFilters.Filters );
            RouteConfig.RegisterRoutes( RouteTable.Routes );
        }
    }
}