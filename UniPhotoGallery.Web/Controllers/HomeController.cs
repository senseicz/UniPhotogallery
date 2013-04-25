using System.Web.Mvc;

namespace UniPhotoGallery.Controllers
{
    public class HomeController : ControllerBase
    {
        public ActionResult Index()
        {
            ViewBag.UserSession = base.UserSession;

            var userName = RouteData.Values["username"].ToString();
            if (!string.IsNullOrEmpty(userName) && UserService.IsUserOwner(userName))
            {
                return RedirectToActionPermanent("Index", "Gallery");
            }

            return View();
        }

    }
}
