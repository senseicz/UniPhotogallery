using System.Collections.Generic;
using System.Data;
using ServiceStack.OrmLite;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Data
{
    public interface IPhotoRepository
    {
        Photo GetPhoto(int photoId);
        Photo GetPhotoByFileName(string fileName);
        List<Photo> GetuserPhotos(int ownerId); 

        int InsertPhoto(Photo photo);
        void UpdatePhoto(Photo photo);

        #region PhotoType
        int InsertPhotoType(PhotoType type);
        void UpdatePhotoType(PhotoType type);
        List<PhotoType> GetAllPhotoTypes();
        PhotoType GetBySystemName(string name);

        #endregion
    }
    
    public class PhotoRepository : BaseRepository, IPhotoRepository
    {
        public Photo GetPhoto(int photoId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Id<Photo>(photoId);
            }
        }

        public Photo GetPhotoByFileName(string fileName)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.FirstOrDefault<Photo>(p => p.FileName.ToLower() == fileName.ToLower());
            }
        }

        public List<Photo> GetuserPhotos(int ownerId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<Photo>(p => p.OwnerId == ownerId);
            }
        }

        public int InsertPhoto(Photo photo)
        {
            return Insert(photo);
        }

        public void UpdatePhoto(Photo photo)
        {
            Update(photo);
        }


        #region PhotoType
        public List<PhotoType> GetAllPhotoTypes()
        {
            return GetAll<PhotoType>();
        }

        public int InsertPhotoType(PhotoType type)
        {
            return Insert(type);
        }

        public void UpdatePhotoType(PhotoType type)
        {
            Update(type);
        }

        public PhotoType GetBySystemName(string name)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.FirstOrDefault<PhotoType>(p => p.SystemName.ToLower() == name.ToLower());
            }
        }

        #endregion

    }
}
