using System.Collections.Generic;
using System.Web.Mvc;
using UniPhotoGallery.DomainModel.ViewModels;

namespace UniPhotoGallery.Areas.Partials.Controllers
{
    public class GalleryBreadcrumbController : UniPhotoGallery.Controllers.ControllerBase
    {
        public ActionResult Index(List<GalleryBreadcrumb> breadcrumb)
        {
            return View(breadcrumb);
        }
    }
}
