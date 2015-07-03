using System.Web.Mvc;

#pragma warning disable 1591
namespace Nano.Demo.Mvc4
{
    public class FilterConfig
    {
        public static void RegisterGlobalFilters( GlobalFilterCollection filters )
        {
            filters.Add( new HandleErrorAttribute() );
        }
    }
}