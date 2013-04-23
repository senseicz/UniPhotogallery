using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using ServiceStack.Text;
using UniPhotoGallery.DomainModel.Domain;
using UniPhotoGallery.DomainModel.ViewModels;
using UniPhotoGallery.DomainModel.ViewModels.Admin;
using UniPhotoGallery.Globals;
using ServiceStack.Common;

namespace UniPhotoGallery.Controllers
{
    public class AdminController : ControllerBase
    {
        public AdminController()
        {
        }

        public ActionResult Index()
        {
            GalleryService.EnsureOwnerSetup(UserSession.OwnerId);
            var galleries = GalleryService.GetGalleriesForUser(UserSession.OwnerId);
            var trash = GalleryService.GetTrashGallery(UserSession.OwnerId);
            var retModel = new AdminVM { Galleries = galleries, Trash = trash };

            return View(retModel);
        }

        #region Gallery owners
        public ActionResult AdminList()
        {
            var model = new AdminList
                {
                    Owners = UserService.GetAllOwners(),
                    Users = UserService.GetAllUsers()
                };

            return View(model);
        }

        public ActionResult CreateOwner(string id)
        {
            
            int IdUser;

            if (!int.TryParse(id, out IdUser))
            {
                return RedirectToAction("AdminList");
            }

            ViewBag.Title = "Založení nového vlastníka galerie";

            var user = UserService.GetUser(IdUser);
            var ownerName = user != null ? user.UserName : "";
            var model = new VMOwner
                {
                    UserId = IdUser,
                    OwnerName = ownerName,
                    OwnerDirectory = SEO.ConvertTextForSEOURL(ownerName)
                };

            return View(model);
        }

        [HttpPost]
        public ActionResult CreateOwner(VMOwner model)
        {
            if (ModelState.IsValid)
            {
                if (model.OwnerId == 0) //create
                {
                    var owner = new Owner
                        {
                            OwnerName = model.OwnerName,
                            OwnerDirectory = model.OwnerDirectory,
                            UserId = model.UserId
                        };
                    
                    var newOwnerId = GalleryService.InsertOwner(owner);
                    model.OwnerId = newOwnerId;
                    model.AddOKMessage("Nový vlastník galerie úspěšně přidán.");
                }
            }

            return View(model);
        }
        #endregion

        #region Process uploaded photos

        public ActionResult ProcessUploadedPhotos(string path)
        {
            var model = TempData["result"] as ProcessUploadedPhotosVM ?? new ProcessUploadedPhotosVM(path);

            model.PhotosWaiting = PhotoService.GetWaitingPhotos(UserSession.Owner, model.CurrentPath);
            model.SubDirs = PhotoService.GetSubDirs(UserSession.Owner, model.CurrentPath);
            model.Galleries = GalleryService.GetGalleriesForUser(UserSession.OwnerId);

            return View(model);
        }
        
        [HttpPost]
        public ActionResult ProcessUploadedPhotosCustom(string currentPath)
        {
            var model = new ProcessUploadedPhotosVM(currentPath);
            var request = Request.Form;

            if (request.Keys.Count > 0)
            {
                foreach (var key in request.Keys)
                {
                    if (key.ToString().StartsWith("addedPhotos-") && request[key.ToString()].Length > 0)
                    {
                        var split = key.ToString().Split(new[] {'-'});
                        var galleryId = int.Parse(split[1]);
                        var values = request[key.ToString()];

                        if (values.EndsWith(","))
                        {
                            values = values.TrimEnd(new [] {','});
                        }

                        var strPhotoIdsArr = values.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries);

                        var photoIdsArr = new int[strPhotoIdsArr.Length];
                        if (strPhotoIdsArr.Length > 0)
                        {
                            for (int i = 0; i < strPhotoIdsArr.Length; i++)
                            {
                                photoIdsArr[i] = int.Parse(strPhotoIdsArr[i]);
                            }
                        }

                        GalleryService.AddPhotosToGallery(galleryId, photoIdsArr);
                        var photosAdded = PhotoService.ProcessUploadedPhoto(photoIdsArr, currentPath);

                        var gallery = GalleryService.GetById(galleryId);

                        if (photosAdded > 0)
                        {
                            model.AddOKMessage(string.Format("Přidáno {0} fotografií do galerie {1}", photosAdded, gallery.Name));
                        }
                    }
                }
            }

            TempData["result"] = model;

            return RedirectToAction("ProcessUploadedPhotos");
        }
        #endregion

        #region Gallery
         
        public ActionResult GalleryEdit(string Id)
        {
            GalleryEdit retModel;
            int galleryId;

            var tempModel = TempData["result"] as MessageTransferModel;

            if (int.TryParse(Id, out galleryId))
            {
                var gal = GalleryService.GetById(galleryId);
                retModel = MapGalleryToGalleryEdit(gal);
            }
            else
            {
                retModel = new GalleryEdit { GalleryList = GetListForGalleryInsert() };
            }

            if(tempModel != null)
            {
                retModel.CopyMessages(tempModel);
            }

            return View(retModel);
        }

        private GalleryEdit MapGalleryToGalleryEdit(Gallery gal)
        {
            var trashGalleryPhotos = GalleryService.GetTrashGallery(gal.OwnerId).GalleryPhotos;
            
            return new GalleryEdit
                {
                    GalleryId = gal.GalleryId,
                    Name = gal.Name,
                    Description = gal.Description,
                    ParentGalleryId = gal.ParentId.HasValue ? gal.ParentId.Value.ToString() : "0",
                    GalleryList = GetListForGalleryUpdate(gal),
                    GalleryListForPreviewGalleries = GetListForGalleryUpdate(),
                    Order = gal.Order,
                    Year = gal.Year,
                    PreviewGallery = gal.GalleryType == (int)GalleryTypes.Preview,
                    IsRootGallery = gal.GalleryType == (int)GalleryTypes.Root,
                    //Diaries = gal.Diaries,
                    Photos = gal.GalleryPhotos,
                    PreviewPhotos = gal.PreviewPhotos,
                    TrashPhotos = trashGalleryPhotos
                };
        }

        [HttpPost]
        public ActionResult GalleryEdit(GalleryEdit galEdit, string hdnPreviewPhotosShadow, string hdnPhotosShadow, string hdnTrashShadow, string hdnIsRootGallery)
        {
            var allOk = true;
            if (ModelState.IsValid)
            {
                try
                {
                    var gal = new Gallery
                        {
                            DateCreated = DateTime.Now,
                            Name = galEdit.Name,
                            Description = galEdit.Description,
                            Order = galEdit.Order,
                            Year = galEdit.Year,
                            OwnerId = UserSession.OwnerId,
                            GalleryType = (int)GalleryTypes.Content
                        };

                    if (galEdit.ParentGalleryId == "0")
                    {
                        gal.ParentId = null;
                    }
                    else
                    {
                        gal.ParentId = int.Parse(galEdit.ParentGalleryId);
                    }
                    
                    if (galEdit.PreviewGallery)
                    {
                        gal.GalleryType = (int) GalleryTypes.Preview;
                    }

                    if (hdnIsRootGallery.ToLower() == "true")
                    {
                        gal.GalleryType = (int) GalleryTypes.Root;
                    }

                    if (galEdit.GalleryId.HasValue) //UPDATE
                    {
                        gal.GalleryId = galEdit.GalleryId.Value;
                        gal = ProcessGalleryPhotos(gal, hdnPreviewPhotosShadow, hdnPhotosShadow, hdnTrashShadow);
                        GalleryService.Update(gal);
                        galEdit.AddOKMessage(string.Format("Update galerie {0} proběhl úspěšně.", gal.Name));
                    }
                    else //INSERT
                    {
                        var newId = GalleryService.Insert(gal);
                        galEdit.AddOKMessage(string.Format("Uložení nové galerie {0} proběhlo úspěšně.", gal.Name));
                        galEdit.GalleryId = newId;
                    }
                }
                catch (Exception ex)
                {
                    allOk = false;
                    galEdit.AddErrorMessage("Při ukládání galerie došlo k chybě: " + ex.Message);
                }
            }
            else
            {
                allOk = false;
                galEdit.AddErrorMessage("Některá povinná položka není vyplněná.");
            }

            GalleryEdit retModel;

            if (allOk)
            {
                var editedGallery = GalleryService.GetById(galEdit.GalleryId.Value);
                retModel = MapGalleryToGalleryEdit(editedGallery);
            }
            else
            {
                retModel = galEdit;
            }

            if(galEdit.ErrorMessages.Count > 0)
            {
                foreach (var errorMessage in galEdit.ErrorMessages)
                {
                    retModel.AddErrorMessage(errorMessage);    
                }
            }
            
            if(galEdit.OKMessages.Count > 0)
            {
                foreach(var okMessage in galEdit.OKMessages)
                {
                    retModel.AddOKMessage(okMessage);
                }
            }
            
            return View(retModel);
        }

        [HttpPost]
        public ActionResult GalleryEditCustom(int GalleryId, string hdnPreviewPhotos, string hdnPhotos, string hdnTrash)
        {
            var gallery = GalleryService.GetById(GalleryId);

            gallery = ProcessGalleryPhotos(gallery, hdnPreviewPhotos, hdnPhotos, hdnTrash);
            GalleryService.Update(gallery);

            var transferModel = new MessageTransferModel();
            transferModel.AddOKMessage("Úprava galerie úspěšně uložena.");

            TempData["result"] = transferModel;

            return RedirectToAction("GalleryEdit", new {Id = GalleryId});
        }

        private Gallery ProcessGalleryPhotos(Gallery gallery, string previewPhotos, string photos, string trash)
        {
            var previewPhotosList = new List<GalleryPhoto>();
            var photosList = new List<GalleryPhoto>();
            var separator = new[] {','};

            if (!string.IsNullOrEmpty(previewPhotos) && previewPhotos.Length > 1)
            {
                previewPhotos = previewPhotos.Trim(separator);
                var previewPhotosArr = previewPhotos.Split(separator);

                for (int i = 0; i < previewPhotosArr.Length; i++)
                {
                    previewPhotosList.Add(new GalleryPhoto{GalleryId = gallery.GalleryId, PhotoId = int.Parse(previewPhotosArr[i]), Order = i+1});
                }

                gallery.PreviewPhotos = previewPhotosList;
            }

            if (!string.IsNullOrEmpty(photos) && photos.Length > 1)
            {
                photos = photos.Trim(separator);
                var photosArr = photos.Split(separator);

                for (int i = 0; i < photosArr.Length; i++)
                {
                    photosList.Add(new GalleryPhoto{GalleryId = gallery.GalleryId, PhotoId = int.Parse(photosArr[i]), Order = i+1});
                }

                gallery.GalleryPhotos = photosList;
            }

            if (!string.IsNullOrEmpty(trash) && trash.Length > 1)
            {
                trash = trash.Trim(separator);
                var trashArr = trash.Split(separator, StringSplitOptions.RemoveEmptyEntries).Select(int.Parse).ToArray();

                if (trashArr.Length > 0)
                {
                    GalleryService.ClearTrashGallery(UserSession.OwnerId);
                    GalleryService.AddPhotosToGallery(GalleryService.GetTrashGallery(UserSession.OwnerId).GalleryId, trashArr);
                }
            }

            return gallery;
        }

        [HttpPost]
        public ActionResult PhotoDescriptionEdit(string strPhotoIds, string description)
        {
            if (!string.IsNullOrEmpty(strPhotoIds))
            {
                strPhotoIds = strPhotoIds.Trim(new[] {','});
                if (!string.IsNullOrEmpty(strPhotoIds))
                {
                    var arrPhotoIds = strPhotoIds.Split(new [] {','});

                    if (arrPhotoIds.Length > 0)
                    {
                        foreach (var strPhotoId in arrPhotoIds)
                        {
                            int photoId;
                            if (int.TryParse(strPhotoId, out photoId))
                            {
                                var photo = PhotoService.GetPhoto(photoId);
                                if (photo != null)
                                {
                                    photo.Description = description;
                                    PhotoService.UpdatePhoto(photo);
                                }
                            }
                        }

                        return Json(new { Result = "OK" }, JsonRequestBehavior.AllowGet);
                    }
                }
            }
            return Json(new { Result = "Popisek nebyl nastaven žádné fotografii!" }, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult GetPhotoDescription(string Id)
        {
            int photoId;
            if (int.TryParse(Id, out photoId))
            {
                var photo = PhotoService.GetPhoto(photoId);
                if (photo != null)
                {
                    return Json(new {photo.Description}, JsonRequestBehavior.AllowGet);
                }
            }
            return Json(new { Result = "Failed" }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetPhotoThumb(string Id)
        {
            var photo = PhotoService.GetPhoto(int.Parse(Id));
            if (photo != null)
            {
                var thumbPath = photo.GetPhotoUrl("adminthumb");

                if (string.IsNullOrEmpty(thumbPath))
                {
                    thumbPath = PhotoService.CreateTypeOfPhoto(photo, PhotoService.GetByPhotoTypeName("adminthumbs"));
                }

                return Json(new {id = Id, path = thumbPath}, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        public ActionResult GetGalleryThumbs(string Id)
        {
            var gallery = GalleryService.GetById(int.Parse(Id));
            if (gallery != null && gallery.GalleryPhotos != null && gallery.PhotosCount > 0)
            {
                var retColl =
                    gallery.GalleryPhotos.Select(
                    photo => new
                        {
                            Id = photo.PhotoId, 
                            photo.Order, 
                            Path = PhotoService.GetPhoto(photo.PhotoId).GetPhotoUrl("adminthumb")
                        }).ToList();

                return Json(retColl, JsonRequestBehavior.AllowGet);
            }

            return Json(null, JsonRequestBehavior.AllowGet);
        }

        [HttpGet]
        public ActionResult DeleteGallery(string Id)
        {
            var galerie = GalleryService.GetById(int.Parse(Id));
            if (galerie != null)
            {
                GalleryService.Delete(galerie, true);
            }

            return RedirectToAction("Index");
        }

        public ActionResult ClearTrash()
        {
            GalleryService.ClearTrashGallery(UserSession.OwnerId);
            return RedirectToAction("Index");
        }

        #endregion
         

        #region PhotoTypeEdit

        public ActionResult PhotoTypeList()
        {
            var photoTypes = PhotoService.GetAllPhotoTypes();
            var model = new PhotoTypeList() {PhotoTypes = photoTypes};
            return View(model);
        }

        public ActionResult PhotoTypeEdit(string Id)
        {
            PhotoTypeEdit retModel;
            int photoTypeId;

            if (int.TryParse(Id, out photoTypeId))
            {
                var typFotky = PhotoService.GetByPhotoTypeId(photoTypeId);
                retModel = typFotky.TranslateTo<PhotoTypeEdit>();
            }
            else
            {
                retModel = new PhotoTypeEdit();
            }

            return View(retModel);
        }

        [HttpPost]
        public ActionResult PhotoTypeEdit(PhotoTypeEdit ptEdit)
        {
            if (ModelState.IsValid)
            {
                try
                {
                    var photoType = new PhotoType
                        {
                            Name = ptEdit.Name, Directory = ptEdit.Directory, SystemName = ptEdit.SystemName, X = ptEdit.X
                        };
                    if (ptEdit.Y.HasValue)
                    {
                        photoType.Y = ptEdit.Y.Value;
                    }

                    if (ptEdit.PhotoTypeId.HasValue)
                    {
                        photoType.PhotoTypeId = ptEdit.PhotoTypeId.Value;
                        PhotoService.UpdatePhotoType(photoType);
                        ptEdit.AddOKMessage("Update typu proběhl úspěšně.");
                    }
                    else //Insert
                    {
                        int newId = PhotoService.InsertPhotoType(photoType);
                        ptEdit.AddOKMessage("Uložení nového typu proběhlo úspěšně, nové ID je {0}".Fmt(newId));
                    }
                }
                catch (Exception ex)
                {
                    ptEdit.AddErrorMessage("Při ukládání typu fotky došlo k chybě: " + ex.Message);
                }
            }
            else
            {
                ptEdit.AddErrorMessage("Některá povinná položka není vyplněná.");
            }

            return View(ptEdit);
        }
        #endregion

       

        #region Photos

        public ActionResult PhotosList()
        {
            var photos = PhotoService.GetUserPhotos(UserSession.OwnerId);
            var model = new PhotosList {Photos = photos};
            return View(model);
        }

        [HttpGet]
        public ActionResult PhotoListRemovePhotoType(int photoId, int photoTypeId)
        {
            var photo = PhotoService.GetPhoto(photoId);
            PhotoService.RemovePhotoTypeFromPhoto(photo, photoTypeId);
            return RedirectToAction("PhotosList");
        }

        #endregion


        #region SelectLists
        //INSERT
        private List<SelectListItem> GetListForGalleryInsert()
        {
            var gals = GalleryService.GetRootOrPreviewGalleries(UserSession.OwnerId);
            var retList = gals.Select(gal => new SelectListItem {Selected = false, Text = gal.Name, Value = gal.GalleryId.ToString()}).ToList();
            return retList;
        }

        //EDIT|UPDATE
        private List<SelectListItem> GetListForGalleryUpdate(Gallery gallery)
        {
            var gals = GalleryService.GetGalleriesForUser(UserSession.OwnerId);
            var retList = new List<SelectListItem>();

            foreach(var gal in gals)
            {
                retList.Add(new SelectListItem { Selected = gal.GalleryId == gallery.ParentId, Text = gal.Name, Value = gal.GalleryId.ToString() });
            }
            return retList;
        }

        //PREVIEW GALLERIES
        private List<SelectListItem> GetListForGalleryUpdate()
        {
            var gals = GalleryService.GetGalleriesForUser(UserSession.OwnerId);

            return gals.Where(g => g.GalleryType == (int)GalleryTypes.Content).Select(gal => new SelectListItem {Selected = false, Text = gal.Name, Value = gal.GalleryId.ToString()}).ToList();
        }
        #endregion
    }
}
