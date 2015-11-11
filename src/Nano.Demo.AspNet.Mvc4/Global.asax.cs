using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Optimization;
using System.Web.Routing;
using Nano.Web.Core.Host.SystemWeb;

namespace Nano.Demo.AspNet.Mvc4
{
    public class MvcApplication : System.Web.HttpApplication
    {
        public override void Init()
        {
            StartupNano( this );
            base.Init();
        }

        protected void Application_Start()
        {
            AreaRegistration.RegisterAllAreas();
            FilterConfig.RegisterGlobalFilters(GlobalFilters.Filters);
            RouteConfig.RegisterRoutes(RouteTable.Routes);
            BundleConfig.RegisterBundles(BundleTable.Bundles);
        }

        public static void StartupNano(HttpApplication httpApplication)
        {
            var config = NanoConfigurationHelper.GetNanoConfiguration();

            config.RequestHandlers.Remove(config.RequestHandlers.First(handler => handler.UrlPath == "/"));
            config.AddDirectory("/", "www", returnHttp404WhenFileWasNotFound: false);

            // Specify your application name. A reasonable default is automatically used if not 
            // supplied but it's definitely recommended to supply one.
            config.ApplicationName = "Nano.Demo.AspNet.Mvc4";

            SystemWebNanoServer.Start(httpApplication, config);
        }
    }
}
