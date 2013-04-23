using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ServiceStack.Text;
using UniPhotoGallery.Data;
using UniPhotoGallery.DomainModel.Domain;
using System.Web;
using UniPhotoGallery.DomainModel.ViewModels;
using UniPhotoGallery.DomainModel.ViewModels.UserGallery;

namespace UniPhotoGallery.Services
{
    public interface IGalleryService
    {
        List<Gallery> GetAll();
        Gallery GetById(int galleryId);
        List<Gallery> GetGalleriesForUser(int ownerId);
        Gallery GetRootGallery(int ownerId);
        Gallery GetTrashGallery(int ownerId);
        List<Gallery> Find(string keyword);
        List<Gallery> GetRootOrPreviewGalleries(int ownerId);
        List<Gallery> GetGalleryChildrens(int ownerId, int parentGalleryId);

        int Insert(Gallery gallery);
        void Update(Gallery gallery);
        void Delete(Gallery gallery, bool movePhotosToTrash);

        void AddPhotosToGallery(int galleryId, int[] photoIds);
        void AddPhotosToGallery(List<GalleryPhoto> photos);
        void UpdateGalleryPhotos(int galleryId, List<GalleryPhoto> photos);
        int GetGalleryMaxOrder(int galleryId);

        void ClearTrashGallery(int ownerId);
        bool IsPhotoInGallery(int photoId, int galleryId);
        void EnsurePhotoTypes(Gallery gallery, string[] types);

        int InsertOwner(Owner owner);
        void EnsureOwnerSetup(int ownerId);

        List<GalleryBreadcrumb> GenerateGalleryBreadcrumb(Gallery gallery);

            #region GalleryPhoto
        List<GalleryPhoto> GetGalleryPhotos(Gallery gallery);
        string GetPreviewGalleryCyclerJson(int galleryId);
        #endregion
    }
    
    
    public class GalleryService : IGalleryService
    {
        public IBaseService _baseService { get; set; } //injected
        public IGalleryRepository _galleryRepo { get; set; } //injected
        public IPhotoService PhotoService { get; set; } //injected
        public IUserService UserService { get; set; } //injected
        
        public const string GALLERIES_ALL = "GALLERIES";
        public const string GALLERY_BY_ID = "GALLERY_{0}"; //galleryId
        public const string GALLERIES_BY_OWNERID = "GALLERIES_BY_OWNERID_{0}"; //ownerId
        public const string ROOT_GALLERY_BY_OWNERID = "ROOT_BY_OWNERID_{0}"; //ownerId
        public const string TRASH_GALLERY_BY_OWNERID = "TRASH_BY_OWNERID_{0}"; //ownerId
        
        public List<Gallery> GetAll()
        {
            return _baseService.Cacher.Get(GALLERIES_ALL, () =>
            {
                var gals = _galleryRepo.GetAll();
                if (gals != null && gals.Any())
                {
                    gals.ForEach(g => g.GalleryPhotos = GetGalleryPhotos(g));
                }

                return gals;
            });
        }

        public List<Gallery> GetGalleriesForUser(int ownerId)
        {
            var cacheKey = GALLERIES_BY_OWNERID.Fmt(ownerId);
            return _baseService.Cacher.Get(cacheKey, () =>
                {
                    var userGals = _galleryRepo.GetAll(ownerId);
                    if (userGals != null && userGals.Any())
                    {
                        userGals.ForEach(g => g.GalleryPhotos = GetGalleryPhotos(g));
                    }

                    return userGals;
                });
        }

        public List<Gallery> Find(string keyword)
        {
            return _galleryRepo.Find(keyword);
        }

        public Gallery GetById(int galleryId)
        {
            var cacheKey = GALLERY_BY_ID.Fmt(galleryId);
            return _baseService.Cacher.Get(cacheKey, () =>
            {
                var gal = _galleryRepo.GetById(galleryId);
                if (gal != null)
                {
                    gal.GalleryPhotos = GetGalleryPhotos(gal);
                }

                return gal;
            });
        }

        public Gallery GetRootGallery(int ownerId)
        {
            var cacheKey = ROOT_GALLERY_BY_OWNERID.Fmt(ownerId);
            return _baseService.Cacher.Get(cacheKey, () => _galleryRepo.GetRootGallery(ownerId));

            //var userGalleries = GetGalleriesForUser(ownerId);
            //if (userGalleries != null && userGalleries.Any())
            //{
            //    return userGalleries.FirstOrDefault(g => g.GalleryType == (int)GalleryTypes.Root);
            //}

            //return null;
        }

        public Gallery GetTrashGallery(int ownerId)
        {
            var cacheKey = TRASH_GALLERY_BY_OWNERID.Fmt(ownerId);
            return _baseService.Cacher.Get(cacheKey, () => _galleryRepo.GetTrashGallery(ownerId));

            //var userGalleries = GetGalleriesForUser(ownerId);
            //if (userGalleries != null && userGalleries.Any())
            //{
            //    return userGalleries.FirstOrDefault(g => g.GalleryType == (int)GalleryTypes.Trash);
            //}

            //return null;
        }

        private bool IsRootGalleryExistForUser(int ownerId)
        {
            return GetRootGallery(ownerId) != null;
        }

        private bool IsTrashGalleryExistForUser(int ownerId)
        {
            return GetTrashGallery(ownerId) != null;
        }

        private void CreateRootGallery(Owner owner)
        {
            var rootGal = new Gallery
                {
                    DateCreated = DateTime.Now,
                    Description = "Root galerie uživatele {0}.".Fmt(owner.OwnerName),
                    GalleryType = (int)GalleryTypes.Root,
                    Name = "Root galerie uživatele {0}.".Fmt(owner.OwnerName),
                    Order = 0,
                    OwnerId = owner.OwnerId,
                    ParentId = null,
                    Year = DateTime.Now.Year.ToString()
                };

            _galleryRepo.InsertGallery(rootGal);
        }

        private void CreateTrashGallery(Owner owner)
        {
            var trashGal = new Gallery
            {
                DateCreated = DateTime.Now,
                Description = "Trash galerie uživatele {0}.".Fmt(owner.OwnerName),
                GalleryType = (int)GalleryTypes.Trash,
                Name = "Trash galerie uživatele {0}.".Fmt(owner.OwnerName),
                Order = 9999,
                OwnerId = owner.OwnerId,
                ParentId = null,
                Year = DateTime.Now.Year.ToString()
            };

            _galleryRepo.InsertGallery(trashGal);
        }
        
        public List<Gallery> GetRootOrPreviewGalleries(int ownerId)
        {
            var userGalleries = GetGalleriesForUser(ownerId);
            if (userGalleries != null && userGalleries.Any())
            {
                return userGalleries.Where(g => g.GalleryType != (int)GalleryTypes.Trash && g.GalleryType != (int)GalleryTypes.Content).ToList();
            }

            return null;
        }

        public List<Gallery> GetGalleryChildrens(int ownerId, int parentGalleryId)
        {
            return _galleryRepo.GetGalleryChildrens(parentGalleryId);

            //var userGalleries = GetGalleriesForUser(ownerId);
            //if (userGalleries != null && userGalleries.Any(g => g.ParentId == parentGalleryId))
            //{
            //    return userGalleries.Where(g => g.ParentId == parentGalleryId).ToList();
            //}
            //return null;
        }

        public void ClearTrashGallery(int ownerId)
        {
            var trash = GetTrashGallery(ownerId);
            _galleryRepo.CleanGallery(trash.GalleryId);
        }

        public bool IsPhotoInGallery(int photoId, int galleryId)
        {
            var photosInGallery = _galleryRepo.GetGalleryPhotos(galleryId);
            if (photosInGallery != null)
            {
                return photosInGallery.Any(p => p.PhotoId == photoId);
            }
            return false;
        }

        public int Insert(Gallery gallery)
        {
            _baseService.Cacher.RemoveAll(new []{GALLERIES_ALL, GALLERIES_BY_OWNERID.Fmt(gallery.OwnerId)});
            return _galleryRepo.InsertGallery(gallery);
        }

        public void Update(Gallery gallery)
        {
            _baseService.Cacher.RemoveAll(new[] { GALLERIES_ALL, GALLERIES_BY_OWNERID.Fmt(gallery.OwnerId), GALLERY_BY_ID.Fmt(gallery.GalleryId) });
            UpdateGalleryPhotos(gallery.GalleryId, gallery.GalleryPhotos);
            _galleryRepo.UpdateGallery(gallery);
        }

        public void Delete(Gallery gallery, bool movePhotosToTrash)
        {
            var galleryPhotos = _galleryRepo.GetGalleryPhotos(gallery.GalleryId);
            if (galleryPhotos != null && galleryPhotos.Any())
            {
                var trashGallery = GetTrashGallery(gallery.OwnerId);
                var photoIdsToRemove = new List<int>();
                foreach (var galleryPhoto in galleryPhotos)
                {
                    galleryPhoto.GalleryId = trashGallery.GalleryId;
                    photoIdsToRemove.Add(galleryPhoto.PhotoId);
                }

                if (movePhotosToTrash)
                {
                    _galleryRepo.AddPhotosToGallery(galleryPhotos);
                }
                _galleryRepo.RemovePhotosFromGallery(photoIdsToRemove, gallery.GalleryId);
            }

            DeleteGallery(gallery);
        }

        private void DeleteGallery(Gallery gallery)
        {
            _galleryRepo.DeleteGallery(gallery);
        }

        public int GetGalleryMaxOrder(int galleryId)
        {
            return _galleryRepo.GetGalleryMaxOrder(galleryId);
        }

        public void AddPhotosToGallery(int galleryId, int[] photoIds)
        {
            var maxOrder = GetGalleryMaxOrder(galleryId);
            
            if (photoIds != null && photoIds.Length > 0)
            {
                var gallery = GetById(galleryId);
                var galleryPhotos = new List<GalleryPhoto>();
                for (int i = 0; i < photoIds.Length; i++)
                {
                    //check whether photois already in gallery, if so, do NOT add it again.
                    if (gallery.GalleryPhotos.All(p => p.PhotoId != photoIds[i]))
                    {
                        galleryPhotos.Add(new GalleryPhoto
                            {
                                GalleryId = galleryId,
                                Order = ++maxOrder,
                                PhotoId = photoIds[i]
                            });
                    }
                }

                AddPhotosToGallery(galleryPhotos);
            }
        }

        public void AddPhotosToGallery(List<GalleryPhoto> photos)
        {
            if (photos != null && photos.Any())
            {
                _galleryRepo.AddPhotosToGallery(photos);
                var galleryId = photos[0].GalleryId;
                var gallery = GetById(galleryId);
                _baseService.Cacher.RemoveAll(new[] { GALLERIES_ALL, GALLERY_BY_ID.Fmt(galleryId), GALLERIES_BY_OWNERID.Fmt(gallery.OwnerId)});
            }
        }

        public void UpdateGalleryPhotos(int galleryId, List<GalleryPhoto> photos)
        {
            _galleryRepo.CleanGallery(galleryId);
            AddPhotosToGallery(photos);
        }

        public void EnsurePhotoTypes(Gallery gallery, string[] types)
        {
            if (gallery != null && gallery.GalleryPhotos != null && gallery.GalleryPhotos.Any())
            {
                foreach (var type in types)
                {
                    var photoType = PhotoService.GetByPhotoTypeName(type);
                    foreach (var photo in gallery.GalleryPhotos)
                    {
                        if (!PhotoService.PhotoTypeExist(photo.Photo, photoType))
                        {
                            PhotoService.CreateTypeOfPhoto(photo.Photo, photoType);
                        }
                    }
                }
            }
        }

        public int InsertOwner(Owner owner)
        {
            var newOwnerId = UserService.InsertOwner(owner);
            _baseService.Cacher.RemoveAll(new List<string> { GALLERIES_ALL, GALLERIES_BY_OWNERID.Fmt(newOwnerId) });
            EnsureOwnerSetup(newOwnerId);
            return newOwnerId;
        }

        public void EnsureOwnerSetup(int ownerId)
        {
            var owner = UserService.GetOwnerById(ownerId);

            if (owner != null)
            {
                if (!IsRootGalleryExistForUser(ownerId))
                {
                    CreateRootGallery(owner);
                }

                if (!IsTrashGalleryExistForUser(ownerId))
                {
                    CreateTrashGallery(owner);
                }

                CreateOwnerRootDirectory(owner.OwnerDirectory);
            }
        }

        public List<GalleryBreadcrumb> GenerateGalleryBreadcrumb(Gallery gallery)
        {
            var retColl = new List<GalleryBreadcrumb>();
            var currentGallery = gallery;
            var owner = UserService.GetOwnerById(gallery.OwnerId);
            var baseDir = owner.OwnerDirectory;

            while (currentGallery.GalleryType != (int)GalleryTypes.Root)
            {
                retColl.Add(new GalleryBreadcrumb
                    {
                        GalleryId = currentGallery.GalleryId,
                        GalleryName = currentGallery.Name,
                        UserBaseDir = baseDir,
                        ViewType = currentGallery.GalleryType == (int) GalleryTypes.Content ? "show" : "preview"
                    });

                if (currentGallery.ParentId.HasValue)
                {
                    currentGallery = GetById(currentGallery.ParentId.Value);
                }
            } 

            //now currentGallery must be root, but check just to be sure:
            if (currentGallery.GalleryType == (int) GalleryTypes.Root)
            {
                retColl.Add(new GalleryBreadcrumb
                {
                    GalleryId = currentGallery.GalleryId,
                    GalleryName = currentGallery.Name,
                    UserBaseDir = baseDir,
                    ViewType = currentGallery.GalleryType == (int)GalleryTypes.Content ? "show" : "preview"
                });
            }

            return retColl;
        }

        #region GalleryPhoto
        public List<GalleryPhoto> GetGalleryPhotos(Gallery gallery)
        {
            var galleryPhotos = _galleryRepo.GetGalleryPhotos(gallery.GalleryId);
            if (galleryPhotos != null && galleryPhotos.Any())
            {
                galleryPhotos.ForEach(gp => { gp.Photo = PhotoService.GetPhoto(gp.PhotoId); });
            }

            if (gallery.PreviewPhotos != null && gallery.PreviewPhotos.Any())
            {
                gallery.PreviewPhotos.ForEach(p => p.Photo = PhotoService.GetPhoto(p.PhotoId));
            }

            return galleryPhotos;
        }

        public string GetPreviewGalleryCyclerJson(int galleryId)
        {
            var cycler = new GalleryCycler();

            var gallery = GetById(galleryId);

            if (gallery != null)
            {
                cycler.Name = "root";
                cycler.Id = galleryId.ToString();

                if (gallery.PreviewPhotos != null && gallery.PreviewPhotos.Any())
                {
                    var photoType = "";
                    if (gallery.GalleryType == (int) GalleryTypes.Root)
                    {
                        photoType = "w1000";
                    }
                    else
                    {
                        photoType = "square200";
                    }

                    var imageUrls = new List<string>();
                    gallery.PreviewPhotos.ForEach(p => imageUrls.Add(PhotoService.GetPhotoURL(p.PhotoId, photoType)));
                    cycler.Images = imageUrls;
                }
            }

            return JsonSerializer.SerializeToString(cycler);
        }

        #endregion
        
        #region Private utils
        private void CreateOwnerRootDirectory(string dirname)
        {
            var dirInfo = new DirectoryInfo(string.Format(@"{0}\{1}", HttpContext.Current.Server.MapPath(_baseService.AppConfig.GalleryImagesRoot), dirname));
            if (!dirInfo.Exists)
            {
                dirInfo.Create();
            }
        }
        #endregion
    }
}
