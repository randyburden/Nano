using System.Web.Http;

#pragma warning disable 1591
namespace Nano.Demo.Mvc4
{
    public static class WebApiConfig
    {
        public static void Register( HttpConfiguration config )
        {
            config.Routes.MapHttpRoute(
                name: "DefaultApi",
                routeTemplate: "api/{controller}/{id}",
                defaults: new { id = RouteParameter.Optional }
            );
        }
    }
}
