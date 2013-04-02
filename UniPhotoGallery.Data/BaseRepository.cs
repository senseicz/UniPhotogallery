using System.Collections.Generic;
using System.Data;
using ServiceStack.OrmLite;

namespace UniPhotoGallery.Data
{
    public class BaseRepository
    {
        public IDbConnectionFactory _db { get; set; }

        public BaseRepository(){}

        public List<T> GetAll<T>() 
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<T>();
            }
        }

        public int Insert<T>(T objectToBeInserted) where T:new()
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Insert(objectToBeInserted);
                return (int)conn.GetLastInsertId();
            }
        }

        public void Update<T>(T objectToBeUpdated) where T:new() 
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Update(objectToBeUpdated);
            }
        }

        public void Delete<T>(T objectToBeDeleted) where T : new()
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Delete(objectToBeDeleted);
            }
        }
    }
}
