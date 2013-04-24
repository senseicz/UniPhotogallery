$(function () {
    var uploadedPhotos = $("#photos");
    uploadedPhotos.selectable();

    $(".btnAddNewGallery").click(function() {
        var clickedbutton = $(this);
        var galleryId = clickedbutton.attr("data-parentGalleryId");
        var galleryName = clickedbutton.attr("data-parentGalleryName");

        //clickedbutton.click(function () {
            $("#AddNewGalleryDescription").text("Nová galerie pod rodičovskou galerii " + galleryName);
            $("#ParentGalleryId").val(galleryId);
            $("#galleryAddHolder").removeClass("Hidden");
        //});
    });
});

function addSelectedPhotosToGallery(destinationGalleryId) {
    //var destArea = $("#txt-" + destinationGalleryId);
    var destHidden = $("#addedPhotos-" + destinationGalleryId);

    var photosAdded = 0;

    $("li.ui-selected").each(function () {
        var fotoId = $(this).attr("data-photoId");

        if (!checkIfPhotoAlreadyIn(destHidden, fotoId)) {
            var photosIn = destHidden.attr("value");
            photosIn = photosIn + fotoId + ",";
            destHidden.attr("value", photosIn);
            photosAdded++;
        } else {
            //photo is already assigned to a gallery
            //alert("ALREADY IN");
        }
        $(this).removeClass("ui-selected");
    });

    if (photosAdded > 0) {
        //updateCounter(destinationGalleryId, photosAdded);
        $("#processUploadedPhotosForm").submit();
    }
}

function updateCounter(galleryId, count) {
    var existingCount = $("#gal-newCounter-" + galleryId).html();

    var exCountInt = parseInt(existingCount);
    var newCount = parseInt(count);

    var totalCount = exCountInt + newCount;

    $("#gal-newCounter-" + galleryId).empty().html(totalCount);
}

function checkIfPhotoAlreadyIn(destHidden, photoName) {
    var photosIn = destHidden.attr("value");

    if (photosIn.indexOf(photoName) >= 0) {
        return true;
    }

    return false;
}