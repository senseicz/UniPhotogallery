using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using UniPhotoGallery.DomainModel.Domain;

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

            bool showAddNew = false;

            var thisGallery = gals.FirstOrDefault(g => g.GalleryId == leafId);
            if (thisGallery != null)
            {
                showAddNew = (thisGallery.GalleryType == (int)GalleryTypes.Root ||
                              thisGallery.GalleryType == (int)GalleryTypes.Preview);
                ViewBag.ThisGalName = thisGallery.Name;
                ViewBag.ThisGalId = thisGallery.GalleryId;
            }

            ViewBag.ShowAddNew = showAddNew;

            if (gals != null && gals.Count > 0 && gals.Any(g => g.ParentId == leafId))
            {
                var retColl = gals.Where(g => g.ParentId == leafId).ToList();
                return View(retColl);
            }
            
            return View(new List<Gallery>());
        }
    }
}
