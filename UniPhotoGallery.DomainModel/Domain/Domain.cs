using System;
using System.Collections.Generic;
using System.Linq;
using ServiceStack.DataAnnotations;

namespace UniPhotoGallery.DomainModel.Domain
{
    public enum GalleryTypes
    {
        Root = 1,
        Trash = 2,
        Preview = 3,
        Content = 4
    }
    
    public class GalleryType
    {
        [Index(Unique = true)]
        public int GalleryTypeId { get; set; }
        public string GalleryTypeName { get; set; }
    }

    public class Owner
    {
        [Index(Unique = true), AutoIncrement]
        public int OwnerId { get; set; }
        public string OwnerName { get; set; }
        public string OwnerDirectory { get; set; }
        public int UserId { get; set; }
    }

    public class Gallery 
    {
        [Index(Unique = true), AutoIncrement]
        public int GalleryId { get; set; }

        [References(typeof(Owner))]
        public int OwnerId { get; set; }

        [References(typeof(GalleryType))]
        public int GalleryType { get; set; }
        
        public int? ParentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public DateTime DateCreated { get; set; }
        public int Order { get; set; }
        public string Year { get; set; }

        [Ignore]
        public List<GalleryPhoto> GalleryPhotos { get; set; } //lazy loaded by business logic

        public List<GalleryPhoto> PreviewPhotos { get; set; }

        [Ignore]
        public int PhotosCount 
        { 
            get
            {
                if (GalleryPhotos != null && GalleryPhotos.Any())
                {
                    return GalleryPhotos.Count;
                }

                return 0;
            }
        }
    }

    public class GalleryPhoto
    {
        [Index(Unique = true), AutoIncrement]
        public int GalleryPhotoId { get; set; }
        
        [References(typeof(Gallery))]
        public int GalleryId { get; set; }

        [References(typeof(Photo))]
        public int PhotoId { get; set; }

        public int Order { get; set; }

        [Ignore]
        public Photo Photo { get; set; }

    }

    public class Photo 
    {
        [Index(Unique = true), AutoIncrement]
        public int PhotoId { get; set; }

        [References(typeof(Owner))]
        public int OwnerId { get; set; }
        
        public string FileName { get; set; }
        public DateTime DateUploaded { get; set; }
        public DateTime DateCreated { get; set; }
        public string Description { get; set; }
        public List<PhotoType> PhotoTypes { get; set; }

        [Ignore]
        public Owner Owner { get; set; }

        [Ignore]
        public string BasePhotoVirtualPath { get; set; }

        public string GetPhotoUrl(int photoTypeId)
        {
            if (PhotoTypes != null && PhotoTypes.Any(t => t.PhotoTypeId == photoTypeId))
            {
                return string.Format("{0}/{1}/{2}/{3}", BasePhotoVirtualPath, Owner.OwnerDirectory, PhotoTypes.First(t => t.PhotoTypeId == photoTypeId).Directory, FileName);
            }
            return "";
        }

        public string GetPhotoUrl(string photoTypeSystemName)
        {
            if (PhotoTypes != null && PhotoTypes.Any(t => t.SystemName.ToLower() == photoTypeSystemName.ToLower()))
            {
                return string.Format("{0}/{1}/{2}/{3}", BasePhotoVirtualPath, Owner.OwnerDirectory, PhotoTypes.First(t => t.SystemName.ToLower() == photoTypeSystemName.ToLower()).Directory, FileName);
            }
            return "";
        }

    }

    public class PhotoType
    {
        [AutoIncrement]
        [Index(Unique = true)]
        public int PhotoTypeId { get; set; }

        public string Name { get; set; }
        public string SystemName { get; set; }
        public string Directory { get; set; }
        public int X { get; set; }
        public int? Y { get; set; }
    }
}
