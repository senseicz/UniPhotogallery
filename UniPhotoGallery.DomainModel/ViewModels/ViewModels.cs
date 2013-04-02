using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Mvc;
using System.Linq;
using UniPhotoGallery.DomainModel.Domain;

namespace UniPhotoGallery.DomainModel.ViewModels
{
    public enum ToastrType
    {
        Warning,
        Info,
        Success,
        Error
    }

    [Serializable]
    public class BaseViewModel : IViewModel
    {
        private List<string> _errorMessages;
        private List<string> _okMessages;
        private ToastrType _toastrType;
        private bool _toastrTypeInitialized;

        [IgnoreDataMember]
        public string ToastrMessage
        {
            get
            {
                if(_errorMessages != null && _errorMessages.Any())
                {
                    _toastrType = ToastrType.Error;
                    _toastrTypeInitialized = true;
                    return FormattedErrorMessage;
                }

                if(_okMessages != null && _okMessages.Any())
                {
                    _toastrType = ToastrType.Success;
                    _toastrTypeInitialized = true;
                    return FormattedOKMessage;
                }

                _toastrType = ToastrType.Info;
                _toastrTypeInitialized = true;
                return "";
            }
        }

        [IgnoreDataMember]
        public ToastrType ToastrType
        {
            get
            {
                if (!_toastrTypeInitialized)
                {
                    var temp = ToastrMessage; //just make sure proper toastr type is initialized
                } 
                return _toastrType;
            }
        }

        public List<string> OKMessages 
        { 
            get { return _okMessages ?? new List<string>(); }
        }

        public List<string> ErrorMessages 
        {
            get { return _errorMessages ?? new List<string>(); }
        }

        [IgnoreDataMember]
        public string FormattedErrorMessage
        {
            get { return _errorMessages == null ? null : FormatMessageOutput(_errorMessages); }
        }

        [IgnoreDataMember]
        public string FormattedOKMessage
        {
            get { return _okMessages == null ? null : FormatMessageOutput(_okMessages); }
        }

        public void AddErrorMessage(string message)
        {
            if (_errorMessages == null)
            {
                _errorMessages = new List<string> { message };
            }
            else
            {
                _errorMessages.Add(message);
            }
        }

        public void AddOKMessage(string message)
        {
            if (_okMessages == null)
            {
                _okMessages = new List<string> { message };
            }
            else
            {
                _okMessages.Add(message);
            }
        }

        private string FormatMessageOutput(List<string> messages)
        {
            if(messages != null)
            {
                var count = messages.Count();
                if(count > 1)
                {
                    var sb = new StringBuilder("<ul>");
                    foreach (var message in messages)
                    {
                        sb.AppendFormat("<li>{0}</li>", message);
                    }
                    sb.Append("</ul>");
                    return sb.ToString();
                }
  
                return messages[0];
            }
            
            return "";
        }

        public void CopyMessages(BaseViewModel otherModel)
        {
            if(otherModel.ErrorMessages != null && otherModel.ErrorMessages.Any())
            {
                foreach (var errorMessage in otherModel.ErrorMessages)
                {
                    AddErrorMessage(errorMessage);
                }
            }

            if (otherModel.OKMessages != null && otherModel.OKMessages.Any())
            {
                foreach (var okMessage in otherModel.OKMessages)
                {
                    AddOKMessage(okMessage);
                }
            }
        }
    }

    [Serializable]
    public class MessageTransferModel : BaseViewModel
    {
    }

    [Serializable]
    public class Register : BaseViewModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
        [Required]
        public string PasswordAgain { get; set; }
        [Required]
        public string Name { get; set; }
    }

    [Serializable]
    public class Login : BaseViewModel
    {
        [Required]
        public string Email { get; set; }
        [Required]
        public string Password { get; set; }
    }

    [Serializable]
    public class GalleryEdit : BaseViewModel
    {
        public int? GalleryId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Description { get; set; }

        [Required]
        public string ParentGalleryId { get; set; }

        [Required]
        public int Order { get; set; }

        public string Year { get; set; }
        public bool PreviewGallery { get; set; }

        //public SelectList GalleryList { get; set; }
        //public SelectList GalleryListForPreviewGalleries { get; set; }

        public List<SelectListItem> GalleryList { get; set; }
        public List<SelectListItem> GalleryListForPreviewGalleries { get; set; }


        public List<GalleryPhoto> Photos { get; set; }
        //public List<Diary> Diaries { get; set; }
        public List<GalleryPhoto> PreviewPhotos { get; set; }
        public List<GalleryPhoto> TrashPhotos { get; set; }

        public string PreviewPhotoIds
        {
            get
            {
                var retString = "";
                if (PreviewPhotos != null && PreviewPhotos.Count > 0)
                {
                    retString = PreviewPhotos.OrderBy(p => p.Order).Aggregate(retString, (current, photo) => current + photo.PhotoId.ToString() + ",");
                }

                return retString.TrimEnd(new []{','});
            }
        }

        public string PhotoIds
        {
            get
            {
                var retString = "";
                if (Photos != null && Photos.Count > 0)
                {
                    retString = Photos.OrderBy(p => p.Order).Aggregate(retString, (current, photo) => current + photo.PhotoId.ToString() + ",");
                }

                return retString.TrimEnd(new[] { ',' }); 
            }
        }

        public string TrashPhotoIds
        {
            get
            {
                var retString = "";
                if (TrashPhotos != null && TrashPhotos.Count > 0)
                {
                    retString = TrashPhotos.OrderBy(p => p.Order).Aggregate(retString, (current, photo) => current + photo.PhotoId.ToString() + ",");
                }

                return retString.TrimEnd(new[] { ',' });
            }
        }
    }

    [Serializable]
    public class PhotoTypeList : BaseViewModel
    {
        public List<PhotoType> PhotoTypes { get; set; }
    }

    [Serializable]
    public class PhotoTypeEdit : BaseViewModel
    {
        public int? PhotoTypeId { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string SystemName { get; set; }

        [Required]
        public string Directory { get; set; }

        [Required]
        public int X { get; set; }

        public int? Y { get; set; }
    }

    [Serializable]
    public class PhotosList : BaseViewModel
    {
        public List<Photo> Photos { get; set; }
    }

    [Serializable]
    public class AdminVM : BaseViewModel
    {
        public List<Gallery> Galleries { get; set; }
        public Gallery Trash { get; set; }
    }

    [Serializable]
    public class ProcessUploadedPhotosVM : BaseViewModel
    {
        public List<OrigPhotosWaiting> PhotosWaiting { get; set; }
        public List<Gallery> Galleries { get; set; }
    }

    [Serializable]
    public class OrigPhotosWaiting : BaseViewModel
    {
        public int PhotoId { get; set; }
        public string FileName { get; set; }
        public DateTime UploadedDate { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public string ThumbPath { get; set; }
    }

}
