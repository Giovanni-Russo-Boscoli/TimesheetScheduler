﻿function toastrMessage(msg, typeMessage) {
    toastr.options = {
        "closeButton": false,
        "debug": false,
        "newestOnTop": false,
        "progressBar": true,
        "positionClass": "toast-bottom-right", //"toast-bottom-full-width",
        "preventDuplicates": true,
        "onclick": null,
        "showDuration": "100",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "show",
        "hideMethod": "hide"
    };

    switch (typeMessage) {
        case "info": {
            toastr.info(msg);
            break;
        }
        case "warning": {
            toastr.warning(msg);
            break;
        }
        case "success": {
            toastr.success(msg);
            break;
        }
        case "error": {
            toastr.error(msg);
            break;
        }
        default: {
            toastr.info(msg);
            break;
        }
    }
}

function ajaxErrorHandler(origin, xhrError) {
    if (xhrError.status === 401) {
        toastrMessage(origin + " - " + "Session expired, please login again.");
        setTimeout(function () { location.reload(); }, 5000);
    } else {
        var dom_nodes = $($.parseHTML(xhrError.responseText));
        toastrMessage(origin + " - " + dom_nodes.filter('title').text(), "error");
    }
}