using System.Collections.Generic;
using System.Linq;
using ServiceStack.Text;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.DomainModel.ViewModels.UserGallery
{
    public class GalleryCycler
    {
        public string Name { get; set; }
        public string Id { get; set; }
        public List<string> Images { get; set; } 
    }
    
    
    public class IndexVM
    {
        public Gallery RootGallery { get; set; }
        public List<Gallery> RootGalleryChildrens { get; set; }
        public string OwnerSeoName { get; set; }
        
        public bool HasSomePreviewImage
        {
            get { return (RootGallery != null && RootGallery.PreviewPhotos != null && RootGallery.PreviewPhotos.Count > 0); }
        }

        public string FirstPreviewUrl { get; set; }
        public string GalleryCyclerJson { get; set; }
    }

    public class ChildGalleryMinimal
    {
        public string GalleryId { get; set; }
        public string Name { get; set; }
        public string ThumbUrl { get; set; }
        public int PhotoCount { get; set; }
        public int Order { get; set; }
        public string Year { get; set; }
        public string ThumbUrlsJson { get; set; } 
    }

    public class PreviewImage
    {
        public int ImageId { get; set; }
        public string ImagePath { get; set; }
    }

    public class Show
    {
        public string OwnerSeoName { get; set; }
        public Gallery Gallery { get; set; }
        public bool IsParentRootGallery { get; set; }
        public string ParentGalleryId { get; set; }
        public string ParentGalleryName { get; set; }
        public List<ChildGalleryMinimal> ChildGalleries { get; set; } 
    }
}
