/**********************************************************
Function to retrieve GET params
**********************************************************/
$.urlParam = function (name) {
    var results = new RegExp('[\\?&]' + name + '=([^&#]*)').exec(window.location.href);
    if (results)
        return results[1];
    else
        return 0;
};

/**********************************************************
Form action settings
**********************************************************/
$.setFormAction = function () {
    // ?mode= will be set using the radio buttons at the top of the page
    var fileConnector = '../../connectors/' + lang + '/filemanager.' + lang;

    // attach action to form tag
    $('#save-image').attr('action', fileConnector);
};

/**********************************************************
Resize input action
**********************************************************/
$.resizeInputs = function (pResize, pOrgImgWidth, pOrgImgHeight, lg) {
    // set current image size (visible to user)
    $("#changeWidth").val(pOrgImgWidth);
    $("#changeHeight").val(pOrgImgHeight);

    // set dimensions on input (not visible to user)
    // also used by crop logic
    $("input[name='width']").val(pOrgImgWidth);
    $("input[name='height']").val(pOrgImgHeight);

    // Set help text
    switch ($("input:radio[name=mode]:checked").val()) {
        case 'resize':
            $('#help').html(lg.help_resize); 
            break;
        case 'flip-horizontal':
            $('#help').html(lg.help_flip_horizontal); 
            break;
        default:
            $('#help').html(lg.help_crop); 
            break;
    }

    if (pResize) {
        // activate resize logic
        $("#changeWidth, #changeHeight").keyup(function () {
            var changed = $(this).attr("name"),
                changedSize = $("input[name='" + changed + "']").val(),
                newWidth,
                newHeight;

            if (changed == 'resize-width') {
                // changed width
                newWidth = changedSize,
                newHeight = Math.round(pOrgImgHeight * (changedSize / pOrgImgWidth));

                // set input height to new valuw
                $('#changeHeight').val(newHeight);
            } else {
                // changed height
                newWidth = Math.round(pOrgImgWidth * (changedSize / pOrgImgHeight)),
                newHeight = changedSize;

                // set input width to new valuw
                $('#changeWidth').val(newWidth);
            }

            // set new dimensions
            $('#photo').width(newWidth)
                        .height(newHeight);

            // set dimensions on input
            $("input[name='width']").val(newWidth);
            $("input[name='height']").val(newHeight);

            // disable save button
            $('#save').attr('disabled', false);
        });
    } else {
        // used in crop /flip mode

        // reset image dimensions
        $("#photo").width(pOrgImgWidth)
                    .height(pOrgImgHeight);

        // reset hidden (crop) inputs
        $("input[name='width']").val('');
        $("input[name='height']").val('');
        $("input[name='x1']").val('');
        $("input[name='y1']").val('');
    }
};