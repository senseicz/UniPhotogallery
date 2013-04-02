using System.Collections.Generic;
using System.Data;
using System.Linq;
using ServiceStack.OrmLite;
using UniPhotoGallery.DomainModel.Auth;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Data
{
    public interface IUserRepository
    {
        List<Owner> GetAll();
        Owner GetById(int id);
        Owner GetByName(string name);
        Owner GetOwnerByUserId(int userId);

        int Insert(Owner owner);
        void Update(Owner owner);

        List<User> GetAllUsers();
        User GetUser(int userId);
    }
    
    public class UserRepository : BaseRepository, IUserRepository
    {
        public List<Owner> GetAll()
        {
            return GetAll<Owner>();
        }

        public Owner GetById(int id)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.FirstOrDefault<Owner>(o => o.OwnerId == id);
            }
        }

        public Owner GetByName(string name)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<Owner>(o => o.OwnerName == name).FirstOrDefault();
            }
        }

        public Owner GetOwnerByUserId(int userId)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                return conn.Select<Owner>(o => o.UserId == userId).FirstOrDefault();
                
            }
        }

        public int Insert(Owner owner)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Insert(owner);
                return (int)conn.GetLastInsertId();
            }
        }

        public void Update(Owner owner)
        {
            using (IDbConnection conn = _db.OpenDbConnection())
            {
                conn.Update(owner);
            }
        }

        public List<User> GetAllUsers()
        {
            return GetAll<User>();
        }

        public User GetUser(int userId)
        {
            return GetAllUsers().FirstOrDefault(u => u.Id == userId);
        }
    }
}
