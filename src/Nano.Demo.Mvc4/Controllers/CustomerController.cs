using System.Web.Mvc;

#pragma warning disable 1591
namespace Nano.Demo.Mvc4.Controllers
{
    public class CustomerController : Controller
    {
        public ActionResult Index()
        {
            return Json( "I come from MVC", JsonRequestBehavior.AllowGet );
        }

        public ActionResult HelloWorld()
        {
            return Json( "I come from MVC", JsonRequestBehavior.AllowGet );
        }
    }
}