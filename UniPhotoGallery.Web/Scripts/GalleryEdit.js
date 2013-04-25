function updateSorter(ulElemName) {
    var sortedString = '';
    $("#" + ulElemName + " li").each(function () {
        sortedString += $(this).attr('data-photoId') + ',';
    });

    if (ulElemName == "photos") {
        $("#hdnPhotos").val(sortedString);
    }

    if (ulElemName == "preview-photos") {
        $("#hdnPreviewPhotos").val(sortedString);
    }
}

function checkIfPhotoAlreadyIn(destHidden, photoName) {
    var photosIn = destHidden.attr("value");

    if (photosIn.indexOf(photoName) >= 0) {
        return true;
    }
    return false;
}

function GetPreviewPhotoCount() {
    var hdnPreview = $("#hdnPreviewPhotos");
    var vals = hdnPreview.val().split(",");
    return vals.length;
}

function CreatePhotoLi(targetContainerId, photoId, addClickCallback) {
    var targetContainer = $("#" + targetContainerId);
    if (targetContainer) {
        $.getJSON('/admin/GetPhotoThumb/' + photoId, function (data) {
            var li = '<li class="ui-widget-content" data-photoId="' + data.id + '"><img src="' + data.path + '" /></li>';
            targetContainer.append(li);
            if (addClickCallback) {
                var elemLi = $("#PhotoDescriptionEdit_Preview li[data-photoId=" + photoId + "]");
                elemLi.click(function() {
                    DeletePhotoLiDescription(photoId);
                });
                
                SetupDescriptionForm();
            }
        });
    }
}

function DeletePhotoLi(targetContainerId) {
    var targetContainer = $("#" + targetContainerId + " li");
    if (targetContainer) {
        targetContainer.remove(".ui-selected");
    }
}

function DeletePhotoLiDescription(photoId) {
    var container = $("#PhotoDescriptionEdit_Preview");
    container.children("li[data-photoId=" + photoId + "]").remove();
    SetupDescriptionForm();
}

function GetGalleryThumbs(galleryId, targetContainerId) {
    var targetContainer = $("#" + targetContainerId);
    if (targetContainer) {
        $.getJSON('/admin/getgallerythumbs/' + galleryId, function (data) {
            $.each(data, function (i, item) {
                var li = '<li class="ui-widget-content" data-photoId="' + item.Id + '"><img src="' + item.Path + '" /></li>';
                targetContainer.append(li);
            });
        });
    }
}

function ClearThumbs(targetContainerId) {
    var targetContainer = $("#" + targetContainerId);
    if (targetContainer) {
        targetContainer.empty();
    }
}

function SetupDescriptionForm() {
    var photosCount = GetPhotoIdsInDescriptionForm();

    if (photosCount.length > 0) {
        if ($("#PhotoDescriptionEdit").hasClass("Hidden")) {
            $("#PhotoDescriptionEdit").removeClass("Hidden");
        }
        
        if (photosCount.length == 1) {
            if (!$("#PhotoDescriptionEdit_Warning").hasClass("Hidden")) {
                $("#PhotoDescriptionEdit_Warning").addClass("Hidden");
            }
        } else {
            //if ($("#PhotoDescriptionEdit_Warning").hasClass("Hidden")) {
                $("#PhotoDescriptionEdit_Warning").removeClass("Hidden");
            //}
        }
    } else {
        ClearDescriptionForm();
    }
}

function SetPhotoDescriptionText(photoId) {
    $.getJSON('/admin/GetPhotoDescription/' + photoId, function (data) {
            $("#PhotoDescriptionEdit_Description").val(data.Description);
        });
}

function ClearDescriptionForm() {
    $("#PhotoDescriptionEdit_PhotoId").val('');
    $("#PhotoDescriptionEdit_Preview").html('');
    $("#PhotoDescriptionEdit_Description").val('');
    if(!$("#PhotoDescriptionEdit").hasClass("Hidden")) { $("#PhotoDescriptionEdit").addClass("Hidden"); }
    if (!$("#PhotoDescriptionEdit_Warning").hasClass("Hidden")) { $("#PhotoDescriptionEdit_Warning").removeClass("MessageWarning").addClass("Hidden"); }
}

function GetPhotoIdsInDescriptionForm() {
    var photoIds = new Array();
    $("#PhotoDescriptionEdit_Preview li").each(function () {
        photoIds.push($(this).attr("data-photoId"));
    });

    return photoIds;
}

var previewSelected;
var gallerySelected;
var trashSelected;

//READY:
$(function () {
    $("#preview-photos").selectable({
        stop: function () {
            previewSelected = $(".ui-selected", this);

            var btnRemFromPreview = $("#btnRemoveFromPreview");
            if (previewSelected.length > 1) {
                if (btnRemFromPreview.attr("disabled")) {
                    btnRemFromPreview.removeAttr("disabled");
                }
            } else {
                if (!btnRemFromPreview.attr("disabled")) {
                    btnRemFromPreview.attr("disabled", "disabled");
                }
            }
        }
    });

    $("#photos").selectable({
        stop: function () {
            gallerySelected = $(".ui-selected", this);

            var btnAddToPreview = $("#btnAddToPreview");
            var btnRemoveFromGallery = $("#btnRemoveFromGallery");
            var btnEditDescription = $("#btnEditDescription");

            if (gallerySelected.length > 1) {
                if (GetPreviewPhotoCount() <= 5 && btnAddToPreview.attr("disabled")) {
                    btnAddToPreview.removeAttr("disabled");
                }

                if (btnRemoveFromGallery.attr("disabled")) {
                    btnRemoveFromGallery.removeAttr("disabled");
                }
                
                if (btnEditDescription.attr("disabled")) {
                    btnEditDescription.removeAttr("disabled");
                }
            } else {
                if (!btnAddToPreview.attr("disabled")) {
                    btnAddToPreview.attr("disabled", "disabled");
                }

                if (!btnRemoveFromGallery.attr("disabled")) {
                    btnRemoveFromGallery.attr("disabled", "disabled");
                }
                
                if (!btnEditDescription.attr("disabled")) {
                    btnEditDescription.attr("disabled", "disabled");
                }
            }
        }
    });

    $("#trash").selectable({
        stop: function () {
            trashSelected = $(".ui-selected", this);

            var btnAddToGallery = $("#btnAddToGallery");
            if (trashSelected.length > 1) {
                if (btnAddToGallery.attr("disabled")) {
                    btnAddToGallery.removeAttr("disabled");
                }
            } else {
                if (!btnAddToGallery.attr("disabled")) {
                    btnAddToGallery.attr("disabled", "disabled");
                }
            }
        }
    });

    $("#btnRemoveFromPreview").click(function () {
        var hdnPreview = $("#hdnPreviewPhotos");
        var vals = hdnPreview.val().split(",");

        previewSelected.each(function () {
            var photoId = $(this).attr("data-photoId");
            if (photoId) {
                if (vals.indexOf(photoId) > -1) {
                    vals.splice(vals.indexOf(photoId), 1);
                }
            }
        });
        DeletePhotoLi("preview-photos");

        hdnPreview.val(vals.toString());
        $("#hdnPreviewPhotosShadow").val(vals.toString());
        $(this).attr("disabled", "disabled");
    });

    $("#btnAddToPreview").click(function () {
        var hdnPreview = $("#hdnPreviewPhotos");
        var vals = hdnPreview.val().split(",");

        gallerySelected.each(function () {
            var photoId = $(this).attr("data-photoId");
            if (photoId) {
                if (vals.indexOf(photoId) < 0 && vals.length <= 5) {
                    vals.push(photoId);
                    CreatePhotoLi("preview-photos", photoId, false);
                }
            }
            $(this).removeClass("ui-selected");
        });
        hdnPreview.val(vals.toString());
        $("#hdnPreviewPhotosShadow").val(vals.toString());
        $(this).attr("disabled", "disabled");
    });


    $("#btnRemoveFromGallery").click(function () {
        var hdnPhotos = $("#hdnPhotos");
        var photoVals = hdnPhotos.val().split(",");
        var hdnTrash = $("#hdnTrash");
        var trashVals = hdnTrash.val().split(",");

        gallerySelected.each(function () {
            var photoId = $(this).attr("data-photoId");
            if (photoId) {
                if (trashVals.indexOf(photoId) < 0) {
                    //add to trash
                    trashVals.push(photoId);
                    CreatePhotoLi("trash", photoId, false);

                    //remove from photos
                    photoVals.splice(photoVals.indexOf(photoId), 1);
                }
            }
        });
        DeletePhotoLi("photos");

        hdnPhotos.val(photoVals.toString());
        hdnTrash.val(trashVals.toString());

        $("#hdnPhotosShadow").val(photoVals.toString());
        $("#hdnTrashShadow").val(trashVals.toString());
        $(this).attr("disabled", "disabled");
    });

    $("#btnAddToGallery").click(function () {
        var hdnPhotos = $("#hdnPhotos");
        var photoVals = hdnPhotos.val().split(",");
        var hdnTrash = $("#hdnTrash");
        var trashVals = hdnTrash.val().split(",");

        trashSelected.each(function () {
            var photoId = $(this).attr("data-photoId");
            if (photoId) {
                if (photoVals.indexOf(photoId) < 0) {
                    //add to photos
                    photoVals.push(photoId);
                    CreatePhotoLi("photos", photoId, false);

                    //removeFromTrash
                    trashVals.splice(trashVals.indexOf(photoId), 1);
                }
            }
        });
        DeletePhotoLi("trash");

        hdnPhotos.val(photoVals.toString());
        hdnTrash.val(trashVals.toString());

        $("#hdnPhotosShadow").val(photoVals.toString());
        $("#hdnTrashShadow").val(trashVals.toString());
        $(this).attr("disabled", "disabled");
    });

    $("#galId").change(function () {
        var galleryId = $(this).find(":selected").attr("value");
        if (galleryId && galleryId.length > 0) {
            ClearThumbs("photos");
            GetGalleryThumbs(galleryId, "photos");
        }
    });
    
    $("#btnEditDescription").click(function () {
        var counter = 0;
        var firstPhotoId;
        
        gallerySelected.each(function () {
            var photoId = $(this).attr("data-photoId");
            if (photoId) {
                var photosInDescriptionList = GetPhotoIdsInDescriptionForm();
                if (photosInDescriptionList.length == 0) {
                    firstPhotoId = photoId;
                    CreatePhotoLi("PhotoDescriptionEdit_Preview", photoId, true);
                    counter++;
                } else {
                    //add only photos that are not already there.
                    if (photosInDescriptionList.indexOf(photoId) == -1) {
                        if (counter == 0) {
                            firstPhotoId = photoId;
                        }
                        CreatePhotoLi("PhotoDescriptionEdit_Preview", photoId, true);
                        counter++;
                    }
                }
            }
            $(this).removeClass("ui-selected");
        });

        SetPhotoDescriptionText(firstPhotoId);
        $(this).attr("disabled", "disabled");
    });

    $("#PhotoDescriptionEdit_Button").click(function () {
        var photoIds = GetPhotoIdsInDescriptionForm();
        var description = $("#PhotoDescriptionEdit_Description").val();
        
        $.ajax({
            type: "POST",
            url: "/admin/PhotoDescriptionEdit",
            data: { strPhotoIds: photoIds.join(), description: description }
        }).done(function (msg) {
            toastr.success(msg, 'Úspěch');
            ClearDescriptionForm();
        });
    });
});