using System.Collections.Generic;
using System.Data;
using System.Linq;
using UniPhotoGallery.DomainModel.Domain;
using ServiceStack.OrmLite;

namespace UniPhotoGallery.Data
{
    public interface IGalleryRepository
    {
        List<Gallery> GetAll();
        List<Gallery> GetAll(int ownerId);

        Gallery GetById(int galleryId);
        List<Gallery> Find(string keyword);

        List<GalleryPhoto> GetGalleryPhotos(int galleryId); 
        //List<Photo> GetGalleryPhotos(int galleryId);
        int GetGalleryMaxOrder(int galleryId);

        int InsertGallery(Gallery gallery);
        void UpdateGallery(Gallery gallery);
        void DeleteGallery(Gallery gallery);


        void AddPhotosToGallery(List<GalleryPhoto> photos);
        void RemovePhotosFromGallery(List<int> photoIds, int galleryId);
        void CleanGallery(int galleryId);
    }
    
    public class GalleryRepository : BaseRepository, IGalleryRepository
    {
        public List<Gallery> GetAll()
        {
            return GetAll<Gallery>();
        }

        public List<Gallery> GetAll(int ownerId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<Gallery>(g => g.OwnerId == ownerId);
            }
        }

        public Gallery GetById(int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.First<Gallery>(g => g.GalleryId == galleryId);
            }
        }

        public List<Gallery> Find(string keyword)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<Gallery>(g => g.Name.Contains(keyword));
            }
        }

        public int GetGalleryMaxOrder(int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Scalar<int>("select max([order]) from galleryphoto where galleryid = {0}", galleryId);
            }
        }

        public List<GalleryPhoto> GetGalleryPhotos(int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<GalleryPhoto>(g => g.GalleryId == galleryId).OrderBy(g => g.Order).ToList();
            }
        }

        private IEnumerable<int> PhotoIdsInGallery(int galleryId)
        {
            var galleryPhotos = GetGalleryPhotos(galleryId);
            if (galleryPhotos != null && galleryPhotos.Any())
            {
                return galleryPhotos.Select(p => p.PhotoId).ToArray();
            }
            return new int[]{};
        }

        /*
        public List<Photo> GetGalleryPhotos(int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                var photos = conn.Select<Photo>(p => Sql.In(p.PhotoId, PhotoIdsInGallery(galleryId)));
                return photos;
            }
        }
        */

        public int InsertGallery(Gallery gallery)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Insert(gallery);
                return (int)conn.GetLastInsertId();
            }
        }

        public void UpdateGallery(Gallery gallery)
        {
            Update(gallery);
        }

        public void DeleteGallery(Gallery gallery)
        {
            Delete(gallery);
        }

        public void AddPhotosToGallery(List<GalleryPhoto> photos)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.InsertAll(photos);                
            }
        }
        
        public void RemovePhotosFromGallery(List<int> photoIds, int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                foreach (var photoId in photoIds)
                {
                    var deletedPhotoId = photoId;
                    conn.Delete<GalleryPhoto>(p => p.PhotoId == deletedPhotoId && p.GalleryId == galleryId);
                }
            }
        }

        public void CleanGallery(int galleryId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Delete<GalleryPhoto>(gp => gp.GalleryId == galleryId);
            }
        }
    }
}
