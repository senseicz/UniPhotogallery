using System.Web.Mvc;

namespace UniPhotoGallery.Controllers
{
    public class HomeController : ControllerBase
    {
        public ActionResult Index()
        {
            //Logger.Info("Info debug");
            //Logger.Debug("Debug Event Log Entry.");
            //Logger.Warn("Warning Event Log Entry.");

            //throw new Exception("This is test");

            ViewBag.UserSession = base.UserSession;

            return View();
        }

    }
}
