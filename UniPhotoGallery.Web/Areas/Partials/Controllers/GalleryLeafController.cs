using System.Linq;
using System.Web.Mvc;

namespace UniPhotoGallery.Areas.Partials.Controllers
{
    public class GalleryLeafController : UniPhotoGallery.Controllers.ControllerBase
    {
        public GalleryLeafController()
        {
        }
        
        public ActionResult GalleryLeaf(int leafId)
        {
            var gals = GalleryService.GetGalleriesForUser(UserSession.OwnerId);

            if(gals != null && gals.Count > 0 && gals.Any(g => g.ParentId == leafId))
            {
                var retColl = gals.Where(g => g.ParentId == leafId).ToList();
                return View(retColl);
            }

            return new EmptyResult();
        }

        public ActionResult GalleryLeafDroppable(int leafId)
        {
            
            var gals = GalleryService.GetGalleriesForUser(UserSession.OwnerId);

            if (gals != null && gals.Count > 0 && gals.Any(g => g.ParentId == leafId))
            {
                var retColl = gals.Where(g => g.ParentId == leafId).ToList();
                return View(retColl);
            }
            
            return new EmptyResult();
        }
    }
}
