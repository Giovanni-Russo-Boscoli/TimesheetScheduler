var accessGranted = "Access granted!";
var accessDenied = "Access denied!";

$(document).ready(function () {

});


function capsLockOn(e) {
    if (e.getModifierState("CapsLock")) {
        //$('.formCapsLock').css("visibility", "visible");
        $('.formCapsLock').show();
    } else {
        //$('.formCapsLock').css("visibility", "hidden");
        $('.formCapsLock').hide();
    }
}