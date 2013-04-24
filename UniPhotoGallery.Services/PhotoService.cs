using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;
using ServiceStack.Text;
using UniPhotoGallery.Data;
using UniPhotoGallery.DomainModel;
using UniPhotoGallery.DomainModel.Domain;
using UniPhotoGallery.DomainModel.ViewModels;

namespace UniPhotoGallery.Services
{
    public interface IPhotoService
    {
        Photo GetPhoto(int photoId);
        Photo GetPhotoByFileName(string fileName);
        List<Photo> GetUserPhotos(int ownerId);

        int InsertPhoto(Photo photo);
        void UpdatePhoto(Photo photo);
        Photo AddPhotoTypeToPhoto(Photo photo, PhotoType photoType);
        Photo RemovePhotoTypeFromPhoto(Photo photo, int photoTypeId);
        string CreateTypeOfPhoto(Photo photo, int photoTypeId);
        string CreateTypeOfPhoto(Photo photo, PhotoType photoType);
        string GetPhotoURL(int photoId, string typeName);

        List<OrigPhotosWaiting> GetWaitingPhotos(Owner owner, string subDir);
        List<OrigPhotoSubDirectory> GetSubDirs(Owner owner, string subDir);
        int ProcessUploadedPhoto(int[] photoIds, string currentUploadPath);

        #region PhotoType
        bool PhotoTypeExist(Photo photo, PhotoType photoType);
        List<PhotoType> GetAllPhotoTypes();
        PhotoType GetByPhotoTypeId(int photoTypeId);
        PhotoType GetByPhotoTypeName(string name);
        int InsertPhotoType(PhotoType type);
        void UpdatePhotoType(PhotoType type);
        #endregion

        void DeleteUploadSubdirIfEmpty(string path);
    }

    public class PhotoService : IPhotoService
    {
        public IBaseService _baseService { get; set; } //injected
        public IPhotoRepository _photoRepo { get; set; } //injected
        public IImageProcessingService ImageProcessingService { get; set; } //injected
        public AppConfig AppConfig { get; set; } //injected
        public IUserService UserService { get; set; } //injected

        public const string PHOTO_ID = "PHOTOID_{0}"; //id
        public const string PHOTO_FILENAME = "PHOTOFN_{0}"; //fileName
        public const string PHOTOS_OWNER = "PHOTOS_OWNER_{0}"; //ownerId

        public Photo GetPhoto(int photoId)
        {
            var cacheKey = PHOTO_ID.Fmt(photoId);
            return _baseService.Cacher.Get(cacheKey, () => PostProcessPhoto(_photoRepo.GetPhoto(photoId)));
        }

        public Photo GetPhotoByFileName(string fileName)
        {
            var cacheKey = PHOTO_FILENAME.Fmt(fileName);
            return _baseService.Cacher.Get(cacheKey, () => PostProcessPhoto(_photoRepo.GetPhotoByFileName(fileName)));
        }

        private Photo PostProcessPhoto(Photo photo)
        {
            if (photo != null)
            {
                photo.BasePhotoVirtualPath = _baseService.AppConfig.GalleryImagesRoot;
                photo.Owner = UserService.GetOwnerById(photo.OwnerId);
                return photo;
            }

            return null;
            //throw new Exception("Photo is null!");
        }
        
        public List<Photo> GetUserPhotos(int ownerId)
        {
            var cacheKey = PHOTOS_OWNER.Fmt("ownerId");
            return _baseService.Cacher.Get(cacheKey, () =>
                {
                    var photos = _photoRepo.GetuserPhotos(ownerId);
                    if (photos != null && photos.Any())
                    {
                        photos.ForEach(p => PostProcessPhoto(p));
                    }
                    return photos;
                });
        }

        /*
        public List<GalleryPhoto> GetPhotos(Dictionary<ObjectId, int> photoIds)
        {
            if (photoIds != null && photoIds.Any())
            {
                var retColl = photoIds.Select(photoId => new GalleryPhoto(GetPhoto(photoId.Key), photoId.Value)).ToList();
                return retColl;
            }

            return null;
        }

        public void MovePhotoSiblingsToTrash(User owner)
        {
            var photos = _photos.Collection.FindAll();
            if (photos != null && photos.Any())
            {
                var galleryManager = new GalleryManager();
                var trashGallery = galleryManager.GetTrashGallery(owner);
                var siblings = (from photo in photos where !galleryManager.IsPhotoInGallery(photo.Id) select photo.Id.ToString()).ToList();

                if (siblings.Count > 0)
                {
                    galleryManager.AddPhotosToGallery(trashGallery.Id.ToString(), siblings.ToArray());
                }
            }
        }

         */ 
        public int InsertPhoto(Photo photo)
        {
            _baseService.Cacher.Remove(PHOTOS_OWNER.Fmt(photo.OwnerId));
            return _photoRepo.InsertPhoto(photo);
        }

        public void UpdatePhoto(Photo photo)
        {
            _photoRepo.UpdatePhoto(photo);
            _baseService.Cacher.RemoveAll(new List<string> {PHOTOS_OWNER.Fmt(photo.OwnerId), PHOTO_ID.Fmt(photo.PhotoId)});
        }

        /*

        public void Delete(Photo photo)
        {
            _photos.Collection.Remove(Query.EQ("_id", photo.Id));
        }

        public Photo CreateFotka(string ownerId, string fullFileName, DateTime dateUploaded)
        {
            string photoSystemName;

            try
            {
                photoSystemName = RenameToSystemFileName(fullFileName);
            }
            catch (Exception ex)
            {
                throw;
            }

            var photoToSave = new Photo { FileName = photoSystemName, OwnerId = new ObjectId(ownerId), DateUploaded = dateUploaded };
            Save(photoToSave);

            return GetPhoto(photoToSave.Id);
        }

         */ 
        
        public Photo AddPhotoTypeToPhoto(Photo photo, PhotoType photoType)
        {
            var doSave = false;

            if (photo.PhotoTypes == null)
            {
                photo.PhotoTypes = new List<PhotoType> { photoType };
                doSave = true;
            }
            else
            {
                if (photo.PhotoTypes.All(p => p.SystemName.ToLower() != photoType.SystemName.ToLower()))
                {
                    photo.PhotoTypes.Add(photoType);
                    doSave = true;
                }
            }

            if (doSave)
            {
                _photoRepo.UpdatePhoto(photo);
                _baseService.Cacher.RemoveAll(AllCacheKeys(photo));
            }

            return photo;
        }
        
        public string CreateTypeOfPhoto(Photo photo, int photoTypeId)
        {
            var type = GetByPhotoTypeId(photoTypeId);
            if (type != null)
            {
                return CreateTypeOfPhoto(photo, type);    
            }
            return "";
        }

        /// <summary>
        /// Will create specific photo type and returns it's path as "{0}/{1}", photoType.Directory, photo.FileName
        /// </summary>
        /// <param name="photo"></param>
        /// <param name="photoType"></param>
        /// <returns>Path as "{0}/{1}", photoType.Directory, photo.FileName</returns>
        public string CreateTypeOfPhoto(Photo photo, PhotoType photoType)
        {
            if (photoType.SystemName.ToLower() != "orig")
            {
                var origType = _photoRepo.GetBySystemName("orig");
                try
                {
                    ImageProcessingService.ResizeImage(photo, origType, photoType);
                    AddPhotoTypeToPhoto(photo, photoType);
                }
                catch
                {
                    throw;
                }
            }

            return string.Format("{0}/{1}", photoType.Directory, photo.FileName);
        }

        public string GetPhotoURL(int photoId, string typeName)
        {
            if (photoId > 0 && !string.IsNullOrEmpty(typeName))
            {
                var photo = GetPhoto(photoId);
                var photoType = GetByPhotoTypeName(typeName);

                if (photo != null && photoType != null)
                {
                    var owner = UserService.GetOwnerById(photo.OwnerId);
                    var photoSrc = "";
                    if (PhotoTypeExist(photo, photoType))
                    {
                        photoSrc = string.Format("{0}/{1}/{2}/{3}", AppConfig.GalleryImagesRoot, owner.OwnerDirectory,
                                                 photoType.Directory, photo.FileName);
                    }
                    else
                    {
                        var partialPath = CreateTypeOfPhoto(photo, photoType);
                        photoSrc = string.Format("{0}/{1}/{2}", AppConfig.GalleryImagesRoot, owner.OwnerDirectory, partialPath);
                    }
                    
                    return photoSrc;
                }
            }

            return "";
        }
         

        public Photo RemovePhotoTypeFromPhoto(Photo photo, int photoTypeId)
        {
            if (photo.PhotoTypes != null)
            {
                if (photo.PhotoTypes.Any(pt => pt.PhotoTypeId == photoTypeId))
                {
                    var ptIndex = 0;
                    for (int i = 0; i < photo.PhotoTypes.Count; i++)
                    {
                        if (photo.PhotoTypes[i].PhotoTypeId == photoTypeId)
                        {
                            ptIndex = i;
                            break;
                        }
                    }

                    var typeImagePath = photo.GetPhotoUrl(photoTypeId);
                    photo.PhotoTypes.RemoveAt(ptIndex);

                    try
                    {
                        DeletePhotoTypeImage(HttpContext.Current.Server.MapPath(typeImagePath));
                        _photoRepo.UpdatePhoto(photo);
                        _baseService.Cacher.RemoveAll(AllCacheKeys(photo));
                    }
                    catch
                    {
                        throw;
                    }
                }
            }

            return photo;
        }

        private void DeletePhotoTypeImage(string path)
        {
            if (File.Exists(path))
            {
                File.Delete(path);
            }
        }

        public int ProcessUploadedPhoto(int[] photoIds, string currentUploadPath)
        {
            var origPhotoType = _photoRepo.GetBySystemName("orig");
            var uploadPhotoType = _photoRepo.GetBySystemName("upload");

            var processedPhotos = 0;
            foreach (var photoId in photoIds)
            {
                var photo = GetPhoto(photoId);
                if (photo != null && PhotoTypeExist(photo, uploadPhotoType))
                {
                    string uploadPath;
                    if (!string.IsNullOrEmpty(currentUploadPath))
                    {
                        uploadPath = string.Format("{0}/{1}/{2}/{3}/{4}", photo.BasePhotoVirtualPath, photo.Owner.OwnerDirectory, uploadPhotoType.Directory, currentUploadPath, photo.FileName);
                    }
                    else
                    {
                        uploadPath = string.Format("{0}/{1}/{2}/{3}", photo.BasePhotoVirtualPath, photo.Owner.OwnerDirectory, uploadPhotoType.Directory,  photo.FileName);
                    }
                    
                    var uploadPhysicalPath = HttpContext.Current.Server.MapPath(uploadPath);

                    var origPath = string.Format("{0}/{1}/{2}/{3}", photo.BasePhotoVirtualPath, photo.Owner.OwnerDirectory, origPhotoType.Directory, photo.FileName);
                    var origPhysicalPath = HttpContext.Current.Server.MapPath(origPath);

                    try
                    {
                        MoveFile(uploadPhysicalPath, origPhysicalPath);
                    }
                    catch
                    {
                        throw;
                    }

                    RemovePhotoTypeFromPhoto(photo, uploadPhotoType.PhotoTypeId);
                    AddPhotoTypeToPhoto(photo, origPhotoType);

                    processedPhotos++;
                }
            }
            return processedPhotos;
        }

        public List<OrigPhotosWaiting> GetWaitingPhotos(Owner owner, string subDir)
        {
            var retColl = new List<OrigPhotosWaiting>();
            var currentUserDir = HttpContext.Current.Server.MapPath(string.Format("{0}/{1}",
                                                                    _baseService.AppConfig.GalleryImagesRoot, 
                                                                    owner.OwnerDirectory ));

            var uploadPath = string.Format(@"{0}\{1}", currentUserDir, _baseService.AppConfig.UploadDirName);

            if (!string.IsNullOrEmpty(subDir))
            {
                uploadPath = string.Format(@"{0}\{1}", uploadPath, subDir);
            }

            var files = GetFilesInDirectory(uploadPath);
            if (files.Length > 0)
            {
                var uploadType = _photoRepo.GetBySystemName("upload");
                var adminThumb = _photoRepo.GetBySystemName("adminthumb");
                uploadType.Directory = uploadType.Directory + @"\" + subDir;

                for (int i = 0; i < files.Length; i++)
                {
                    var photoAlreadyInDb = GetPhotoByFileName(files[i].Name);

                    string fileName;
                    int photoId;

                    if (photoAlreadyInDb == null) //fotku sme jeste nezpracovavali
                    {
                        fileName = RenameToSystemFileName(files[i].FullName);
                        var fotka = new Photo
                            {
                                FileName = fileName, 
                                OwnerId = owner.OwnerId, 
                                DateUploaded = files[i].CreationTime,
                                DateCreated = DateTime.Now,
                                PhotoTypes = new List<PhotoType>{uploadType},
                                Owner = owner,
                                BasePhotoVirtualPath = _baseService.AppConfig.GalleryImagesRoot
                            };

                        photoId = InsertPhoto(fotka);
                        fotka.PhotoId = photoId;

                        try
                        {
                            ImageProcessingService.ResizeImage(fotka, uploadType, adminThumb);
                            AddPhotoTypeToPhoto(fotka, adminThumb);
                        }
                        catch (Exception)
                        {
                            throw;
                        }
                    }
                    else
                    {
                        fileName = photoAlreadyInDb.FileName;
                        photoId = photoAlreadyInDb.PhotoId;
                    }

                    var photoWaiting = new OrigPhotosWaiting { FileName = fileName, UploadedDate = files[i].CreationTime, PhotoId = photoId};

                    //X,Y dimensions of originally uploaded photo - uncomment if you need to diplay it on ProcessUploadPhotos view.
                    
                    //var origPath = HttpContext.Current.Server.MapPath(string.Format("/{0}/{1}/{2}/{3}", ConfigurationManager.AppSettings["GalleryRootDirVirtualPath"], currentUserDir, uploadType.Adresar, fileName));
                    //var origDimensions = ImageProcessingManager.GetImageDimensions(origPath);
                    //photoWaiting.X = origDimensions[0];
                    //photoWaiting.Y = origDimensions[1];
                    

                    photoWaiting.ThumbPath = string.Format("{0}/{1}/{2}/{3}", _baseService.AppConfig.GalleryImagesRoot, owner.OwnerDirectory, adminThumb.Directory, fileName);

                    retColl.Add(photoWaiting);
                }
            }
            return retColl;
        }

        public List<OrigPhotoSubDirectory> GetSubDirs(Owner owner, string subDir)
        {
            var currentUserDir = HttpContext.Current.Server.MapPath(string.Format("{0}/{1}",
                                                                    _baseService.AppConfig.GalleryImagesRoot,
                                                                    owner.OwnerDirectory));

            var uploadPath = string.Format(@"{0}\{1}", currentUserDir, _baseService.AppConfig.UploadDirName);

            if (!string.IsNullOrEmpty(subDir))
            {
                uploadPath = string.Format(@"{0}\{1}", uploadPath, subDir);
            }

            var dirInfo = new DirectoryInfo(uploadPath);
            var retColl = new List<OrigPhotoSubDirectory>();
            var subDirs = dirInfo.GetDirectories();

            var innerSubDir = string.IsNullOrEmpty(subDir) ? "" : subDir + @"\";

            if (subDirs.Any())
            {
                //only show directories that have files or subdirs in it.
                foreach (var subdir in subDirs)
                {
                    var files = subdir.GetFiles();
                    var subdirs = subdir.GetDirectories();

                    if (files.Length > 0 || subdirs.Length > 0)
                    {
                        retColl.Add(new OrigPhotoSubDirectory
                            {
                                DirName = subdir.Name,
                                FullParentPath = innerSubDir + subdir.Name
                            });
                    }
                }
            }

            return retColl;
        }

        private IEnumerable<string> AllCacheKeys(Photo photo)
        {
            return new List<string>
                {
                    PHOTOS_OWNER.Fmt(photo.OwnerId),
                    PHOTO_FILENAME.Fmt(photo.FileName),
                    PHOTO_ID.Fmt(photo.PhotoId)
                };
        }

        #region Photo Type

        public List<PhotoType> GetAllPhotoTypes()
        {
            return _baseService.Cacher.Get("ALL.PHOTO.TYPES", () => _photoRepo.GetAllPhotoTypes());
        }

        public PhotoType GetByPhotoTypeId(int photoTypeId)
        {
            var types = GetAllPhotoTypes();
            if (types != null)
            {
                return types.FirstOrDefault(t => t.PhotoTypeId == photoTypeId);
            }

            return null;
        }

        public PhotoType GetByPhotoTypeName(string name)
        {
            var types = GetAllPhotoTypes();
            if (types != null)
            {
                return types.FirstOrDefault(t => t.SystemName.ToLower() == name.ToLower());
            }

            return null;
        }

        public bool PhotoTypeExist(Photo photo, PhotoType photoType)
        {
            return photo.PhotoTypes.Any(tf => tf.PhotoTypeId == photoType.PhotoTypeId);
        }

        public int InsertPhotoType(PhotoType type)
        {
            _baseService.Cacher.Remove("ALL.PHOTO.TYPES");
            return _photoRepo.InsertPhotoType(type);
        }

        public void UpdatePhotoType(PhotoType type)
        {
            _photoRepo.UpdatePhotoType(type);
            _baseService.Cacher.Remove("ALL.PHOTO.TYPES");
        }

        #endregion

        #region I/O operations
        public void DeleteUploadSubdirIfEmpty(string path)
        {
            throw new NotImplementedException("Sorry, not implemented yet");
        }

        private static void MoveFile(string sourceFileName, string targetFileName)
        {
            var srcFI = new FileInfo(sourceFileName);
            if (srcFI.Exists)
            {
                //double-check that target does not exist:
                var tarFI = new FileInfo(targetFileName);
                if (!tarFI.Exists)
                {
                    var targetDir = tarFI.Directory;
                    if (!targetDir.Exists)
                    {
                        targetDir.Create();
                    }

                    srcFI.MoveTo(targetFileName);
                }
                else
                {
                    throw new Exception("Target file already exist!");
                }
            }
        }

        private static string RenameToSystemFileName(string fullFileName)
        {
            var fi = new FileInfo(fullFileName);

            string systemName = "";

            if (fi.Exists)
            {
                var fileName = fi.Name.Substring(0, (fi.Name.Length - fi.Extension.Length));
                systemName = string.Format("{0}{1}", MakeSystemName(fileName), fi.Extension);
                fi.MoveTo(string.Format(@"{0}\{1}", fi.DirectoryName, systemName));
            }

            return systemName;
        }

        private static string MakeSystemName(string fileName)
        {
            const string allowedChars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ_1234567890";
            string fileNameUpperCase = fileName.ToUpper();
            string newFileName = "";

            for (int i = 0; i < fileNameUpperCase.Length; i++)
            {
                if (allowedChars.Contains(fileNameUpperCase[i].ToString()))
                {
                    newFileName = newFileName + fileNameUpperCase[i];
                }
            }

            newFileName = string.Format("{0}_{1}_{2}", DateTime.Now.ToString("ddMMyy"), DateTime.Now.ToString("HHmm"), newFileName);

            return newFileName;
        }


        private static FileInfo[] GetFilesInDirectory(string fullyMappedDirectory)
        {
            var di = new DirectoryInfo(fullyMappedDirectory);
            if (!di.Exists)
            {
                di.Create();
            }

            var extensions = new[] { ".jpg", ".png" };
            FileInfo[] files = di.EnumerateFiles().Where(f => extensions.Contains(f.Extension.ToLower())).ToArray();
            return files;
        }



        #endregion
    }
}
