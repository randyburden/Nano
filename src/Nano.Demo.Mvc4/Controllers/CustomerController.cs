using System.Web.Mvc;

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