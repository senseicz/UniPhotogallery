using System.ComponentModel.DataAnnotations;

namespace UniPhotoGallery.DomainModel.ViewModels.Admin
{
    public class VMOwner : BaseViewModel
    {
        public int OwnerId { get; set; }
        public string OwnerName { get; set; }
        [Required]
        public string OwnerDirectory { get; set; }
        public int UserId { get; set; }
    }
}
