using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ServiceStack.Text;
using UniPhotoGallery.DomainModel.Auth;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.Services
{
    public interface IUserService
    {
        List<Owner> GetAllOwners();
        Owner GetOwnerById(int id);
        Owner GetOwnerByDirectory(string directory);
        int GetOwnerIdByUserId(int userId);
        int InsertOwner(Owner owner);
        void UpdateOwner(Owner owner);

        List<User> GetAllUsers();
        User GetUser(int userId);
        bool IsUserOwner(int userId);
    }
    
    public class UserService : IUserService
    {
        public IBaseService _baseService { get; set; } //injected
        
        public const string OWNERS_ALL = "OWNERS";
        public const string OWNER_ID = "OWNER_{0}"; //ownerId

        public List<Owner> GetAllOwners()
        {
            return _baseService.Cacher.Get(OWNERS_ALL, () => _baseService.UserRepo.GetAll());
        }

        public Owner GetOwnerById(int id)
        {
            return _baseService.Cacher.Get(OWNER_ID.Fmt(id), () => _baseService.UserRepo.GetById(id));
        }

        public Owner GetOwnerByDirectory(string directory)
        {
            var owners = GetAllOwners();
            if (owners != null && owners.Any(o => o.OwnerDirectory.ToLower() == directory.ToLower()))
            {
                return owners.First(o => o.OwnerDirectory.ToLower() == directory.ToLower());
            }

            return null;
        }

        public int InsertOwner(Owner owner)
        {
            var newOwnerId = _baseService.UserRepo.Insert(owner);
            owner.OwnerId = newOwnerId;

            _baseService.Cacher.RemoveAll(new List<string> { OWNERS_ALL, OWNER_ID.Fmt(newOwnerId) });
            return newOwnerId;
        }

        public void UpdateOwner(Owner owner)
        {
            _baseService.UserRepo.Update(owner);
            _baseService.Cacher.RemoveAll(new List<string> { OWNERS_ALL, OWNER_ID.Fmt(owner.OwnerId) });
        }

        public int GetOwnerIdByUserId(int userId)
        {
            var owner = _baseService.UserRepo.GetOwnerByUserId(userId);
            if (owner != null)
            {
                return owner.OwnerId;
            }

            return 0;
        }

        public List<User> GetAllUsers()
        {
            return _baseService.UserRepo.GetAllUsers();
        }

        public User GetUser(int userId)
        {
            return _baseService.UserRepo.GetUser(userId);
        }

        public bool IsUserOwner(int userId)
        {
            var owners = GetAllOwners();
            if (owners != null)
            {
                return owners.Any(o => o.UserId == userId);
            }
            return false;
        }
    }
}
