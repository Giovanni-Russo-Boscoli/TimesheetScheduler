var accessGranted = "Access granted!";
var accessDenied = "Access denied!";

$(document).ready(function () {

});


function capsLockOn(e) {
    if (e.getModifierState("CapsLock")) {
        $('.formCapsLock').css("visibility", "visible");
    } else {
        $('.formCapsLock').css("visibility", "hidden");
    }
}