using System.Collections.Generic;
using UniPhotoGallery.DomainModel.Auth;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.DomainModel.ViewModels.Admin
{
    public class AdminList : BaseViewModel
    {
        public List<User> Users { get; set; } 
        public List<Owner> Owners { get; set; }
    }
}
