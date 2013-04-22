using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using UniPhotoGallery.DomainModel.Domain;
using UniPhotoGallery.DomainModel.ViewModels.UserGallery;
using ServiceStack.Text;

namespace UniPhotoGallery.Controllers
{
    public class GalleryController : ControllerBase
    {
        public ActionResult Index()
        {
            var selectedUser = SelectedUser;
            
            if(selectedUser != null)
            {
                var retModel = new IndexVM { OwnerSeoName = selectedUser.OwnerDirectory };
                var userRootGallery = GalleryService.GetRootGallery(selectedUser.OwnerId);
                if (userRootGallery != null)
                {
                    var childGalleries = GalleryService.GetGalleryChildrens(selectedUser.OwnerId, userRootGallery.GalleryId);
                    retModel.RootGallery = userRootGallery;
                    retModel.RootGalleryChildrens = childGalleries;

                    if(retModel.HasSomePreviewImage)
                    {
                        var photo = retModel.RootGallery.PreviewPhotos.OrderBy(p => p.Order).First();
                        var url = PhotoService.GetPhotoURL(photo.PhotoId, "w1000");
                        retModel.FirstPreviewUrl = url;
                    }

                    retModel.GalleryCyclerJson = GalleryService.GetPreviewGalleryCyclerJson(userRootGallery.GalleryId);

                    return View(retModel);
                }
                
                return RedirectToAction("NotReady");
            }

            return RedirectToAction("Index", "Home");
        }

        public ActionResult NotReady()
        {
            return View();
        }
        
        public ActionResult Show(string Id)
        {
            var retModel = new Show();

            int galleryId;
            if (!int.TryParse(Id, out galleryId))
            {
                return RedirectToAction("Index", "Gallery");
            }

            var gallery = GalleryService.GetById(galleryId);
            if(gallery != null && gallery.ParentId.HasValue)
            {
                if(gallery.GalleryType == (int)GalleryTypes.Preview)
                {
                    return RedirectToAction("Preview", "Gallery", new {Id});
                }

                if (gallery.PhotosCount > 0)
                {
                    gallery.Breadcrumb = GalleryService.GenerateGalleryBreadcrumb(gallery);
                    
                    var owner = UserService.GetOwnerById(gallery.OwnerId);

                    retModel.OwnerSeoName = owner.OwnerDirectory;
                    retModel.Gallery = gallery;

                    GalleryService.EnsurePhotoTypes(gallery, new[] { "w1000", "adminthumb" });

                    var parentGal = GalleryService.GetById(gallery.ParentId.Value);
                    if (parentGal != null)
                    {
                        retModel.IsParentRootGallery = !parentGal.ParentId.HasValue;
                        retModel.ParentGalleryId = parentGal.GalleryId.ToString();
                        retModel.ParentGalleryName = parentGal.Name;
                    }

                    return View(retModel);
                }
            }

            return RedirectToAction("Index", "Gallery");
        }
        
        
        public ActionResult Preview(string Id)
        {
            var retModel = new Show();
            int galleryId;
            if (!int.TryParse(Id, out galleryId))
            {
                return RedirectToAction("Index", "Gallery");
            }

            var gallery = GalleryService.GetById(galleryId);
            if (gallery != null && gallery.ParentId.HasValue)
            {
                if (gallery.GalleryType != (int)GalleryTypes.Preview)
                {
                    return RedirectToAction("Show", "Gallery", new { Id });
                }

                gallery.Breadcrumb = GalleryService.GenerateGalleryBreadcrumb(gallery);
                
                var owner = UserService.GetOwnerById(gallery.OwnerId);

                retModel.OwnerSeoName = owner.OwnerDirectory;
                retModel.Gallery = gallery;

                GalleryService.EnsurePhotoTypes(gallery, new []{"w1000", "adminthumb"});

                var parentGal = GalleryService.GetById(gallery.ParentId.Value);
                if (parentGal != null)
                {
                    retModel.IsParentRootGallery = !parentGal.ParentId.HasValue;
                    retModel.ParentGalleryId = parentGal.GalleryId.ToString();
                    retModel.ParentGalleryName = parentGal.Name;
                }

                var childGalleries = GalleryService.GetGalleryChildrens(gallery.OwnerId, gallery.GalleryId);
                if(childGalleries != null && childGalleries.Count > 0)
                {
                    var childGals = new List<ChildGalleryMinimal>();

                    foreach (var childGallery in childGalleries)
                    {
                        var childGal = new ChildGalleryMinimal
                            {
                                GalleryId = childGallery.GalleryId.ToString(),
                                Name = childGallery.Name,
                                Order = childGallery.Order,
                                PhotoCount = childGallery.PhotosCount,
                                ThumbUrl =
                                    (childGallery.PreviewPhotos != null && childGallery.PreviewPhotos.Count > 0)
                                        ? PhotoService.GetPhotoURL(childGallery.PreviewPhotos[0].PhotoId, "square200")
                                        : "",
                                Year = childGallery.Year,
                                ThumbUrlsJson = GetPreviewUrlsJson(childGallery)
                            };

                        childGals.Add(childGal);
                    }
                    retModel.ChildGalleries = childGals;

                    return View(retModel);
                }
            }

            return RedirectToAction("Index", "Gallery");
        }

        private string GetPreviewUrlsJson(Gallery childGallery)
        {
            var retList = new List<PreviewImage>();
            if (childGallery.PreviewPhotos != null && childGallery.PreviewPhotos.Any())
            {
                foreach (var previewPhoto in childGallery.PreviewPhotos)
                {
                    retList.Add(new PreviewImage
                        {
                            ImageId = previewPhoto.PhotoId, 
                            ImagePath = PhotoService.GetPhotoURL(previewPhoto.PhotoId, "square200")
                        });
                }
            }

            return JsonSerializer.SerializeToString(retList);
        }
         
    }
}
