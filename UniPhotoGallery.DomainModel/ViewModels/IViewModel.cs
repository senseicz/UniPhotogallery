namespace UniPhotoGallery.DomainModel.ViewModels
{
    public interface IViewModel
    {
        string ToastrMessage { get; }
        ToastrType ToastrType { get; }
    }
}
