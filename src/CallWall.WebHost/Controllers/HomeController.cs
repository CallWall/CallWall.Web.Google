using System;
using System.Web.Mvc;

namespace CallWall.WebHost.Controllers
{
    public class HomeController : Controller
    {
        [AllowAnonymous]
        public ActionResult Index()
        {
            //TODO: I think this can be done with Attributes? Is that what I want? Is that easy to test?
            if (User.Identity.IsAuthenticated)
            {
                return View();
            }
            return View("About");
        }

        [AllowAnonymous]
        public ActionResult About()
        {
            return View();
        }

        [AllowAnonymous]
        public ActionResult Contact()
        {
            Console.WriteLine(User.Identity);
            return View();
        }

        [AllowAnonymous]
        public ActionResult Download()
        {
            return View();
        }
    }
}