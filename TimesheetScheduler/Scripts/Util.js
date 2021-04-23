Array.prototype.firstOrDefault = function (func) {
    return this.where(func)[0] || null;
};

Array.prototype.where = function (filter) {

    var collection = this;

    switch (typeof filter) {

        case 'function':
            return $.grep(collection, filter);

        case 'object':
            for (var property in filter) {
                if (!filter.hasOwnProperty(property))
                    continue; // ignore inherited properties

                collection = $.grep(collection, function (item) {
                    return item[property] === filter[property];
                });
            }
            return collection.slice(0); // copy the array 
        // (in case of empty object filter)

        default:
            throw new TypeError('func must be either a' +
                'function or an object of properties and values to filter by');
    }
};

//function groupByCustom(dataArray) {
//    //abcArr = [["A", 10], ["B", 20], ["A", 30], ["C", 40]];

//    var items = {}, base, key;
//    for (var i = 0; i < dataArray.length; i++) {
//        base = dataArray[i];
//        key = base[0];
//        // if not already present in the map, add a zeroed item in the map
//        if (!items[key]) {
//            items[key] = 0;
//        }
//        // add new item to the map entry
//        items[key] += base[1];
//    }

//    // Now, generate new array
//    var outputArr = [], temp;
//    for (key in items) {
//        // create array entry for the map value
//        temp = [key, items[key]]
//        // put array entry into the output array
//        outputArr.push(temp);
//    }

//    return outputArr;
//}

function toastrMessage(msg, typeMessage) {
    toastr.options = {
        "closeButton": false,
        "debug": false,
        "newestOnTop": false,
        "progressBar": true,
        "positionClass": "toast-bottom-right", //"toast-bottom-full-width",//
        "preventDuplicates": true,
        "onclick": null,
        "showDuration": "100",
        "hideDuration": "1000",
        "timeOut": "5000",
        "extendedTimeOut": "1000",
        "showEasing": "swing",
        "hideEasing": "linear",
        "showMethod": "show",
        "hideMethod": "hide",
        'body-output-type': 'trustedHtml'
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

function getUserName(callback) {
    $.ajax({
        url: "/Home/GetUserLoggedName",
        type: "GET",
        success: function (data) {
            //alert(data);
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

//return int e.g.: January = 1 / December = 12
function getCurrentMonth() {
    return new Date().getMonth() + 1;
}

function getCurrentYear() {
    return (new Date).getFullYear();
}

function getAccessUserByName(userName) {
    var access = "";
    $(users).each(function (index, value) {
        if (access !== "") return;
        if (value.Name === userName) {
            access = value.Access;
        }
    });
    return access.toUpperCase();
}

function isUserLoggedAdmin(callback) {
    getUserName(function (loggedUserName) {
        callback(getAccessUserByName(loggedUserName) === "ADMIN");
    });
}

function loadMustacheTemplate(templateId, targetId, data) {
    var template = document.getElementById(templateId).innerHTML;
    var rendered = Mustache.render(template, data);
    document.getElementById(targetId).innerHTML = rendered;
}

//function sendEmail() {
//    $.ajax({
//        url: "/EmailSender/SendEmail",
//        type: "post",
//        data: {
//            "from": "GiovanniRB",
//            "email": "boscoli.giovanni@gmail.com2",
//            "subject": "Test subject",
//            "comments": "Test comments"
//        },
//        success: function (data) {
//            alert(data);
//            console.log("Sucess");
//        },
//        function(error) {
//            ajaxErrorHandler(error);
//        }
//    });
//}

function sendEmail() {
    $.ajax({
        url: "/EmailSender/SendEmail",
        type: "GET",
        //data: {
        //    "from": "GiovanniRB",
        //    "email": "boscoli.giovanni@gmail.com2",
        //    "subject": "Test subject",
        //    "comments": "Test comments"
        //},
        success: function (data) {
            alert(data);
            console.log("Sucess");
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

//EmailSenderController

function clearJqValidErrors(formElement) {
    // NOTE: Internal "$.validator" is exposed through "$(form).validate()". By Travis J
    var validator = $(formElement).validate();

    // NOTE: Iterate through named elements inside of the form, and mark them as 
    // error free. By Travis J
    $(":input", formElement).each(function () {
        // NOTE: Get all form elements (input, textarea and select) using JQuery. By Questor
        // [Refs.: https://stackoverflow.com/a/12862623/3223785 , 
        // https://api.jquery.com/input-selector/ ]

        validator.successList.push(this); // mark as error free
        validator.showErrors(); // remove error messages if present
    });
    validator.resetForm(); // remove error class on name elements and clear history
    validator.reset(); // remove all error and success data

    // NOTE: For those using bootstrap, there are cases where resetForm() does not 
    // clear all the instances of ".error" on the child elements of the form. This 
    // will leave residual CSS like red text color unless you call ".removeClass()". 
    // By JLewkovich and Nick Craver
    // [Ref.: https://stackoverflow.com/a/2086348/3223785 , 
    // https://stackoverflow.com/a/2086363/3223785 ]
    $(formElement).find("label.error").hide();
    $(formElement).find(".error").removeClass("error");
}

function currencyMask(element, maskFormat, prefix) {
    $(element).unmask();
    $(element).mask(maskFormat, { reverse: true });
    if (prefix) {
        $(element).each(function (index, value) {
            $(value).val(prefix + $(value).val());
            $(value).text(prefix + $(value).text());
        }); 
    }
    //parseFloat($(element)).toFixed(2);
    //$(element).mask('###,###,###.##', { reverse: true });
}

//HOW TO USE
//var myMoney = 3543.75873;
//var formattedMoney = '$' + myMoney.formatMoney(2, ',', '.'); 
Number.prototype.formatMoney = function (decPlaces, thouSeparator, decSeparator) {
    var n = this;
    decPlaces = isNaN(decPlaces = Math.abs(decPlaces)) ? 2 : decPlaces;
    decSeparator = decSeparator === undefined ? "." : decSeparator;
    thouSeparator = thouSeparator === undefined ? "," : thouSeparator;
    var sign = n < 0 ? "-" : "",
        i = parseInt(n = Math.abs(+n || 0).toFixed(decPlaces)) + "",
        j = (j = i.length) > 3 ? j % 3 : 0;
    return sign + (j ? i.substr(0, j) + thouSeparator : "") + i.substr(j).replace(/(\d{3})(?=\d)/g, "$1" + thouSeparator) + (decPlaces ? decSeparator + Math.abs(n - i).toFixed(decPlaces).slice(2) : "");
};

//function equalsTrue(obj) {
//    var result = false;
//    if (obj && $(obj).toUpperCase() === "TRUE") {
//        result = true;
//    }
//    return result;
//    //return obj === "true" || obj === "True";
//}

// From /Date(1560330289910)/ to DD/MM/YYYY
function fromJsonDateToDateStringFormatted(strDate) {
    return _formatDate(new Date(parseInt(strDate.substr(6))).toDateString(), "/");
}


//USAGE
//var holidays = [
//    { 
//        "Year": "2020", 
//        "Dates": [
//              { 
//                  "Date": "01/01/2020"
//              },
//              { 
//                  "Date": "01/02/2020"
//              }
//          ] 
//    }];

//var _holidays = holidays.where({ Year: "2020" });

//Array.prototype.where = function (filter) {

//    var collection = this;

//    switch (typeof filter) {

//        case 'function':
//            return $.grep(collection, filter);

//        case 'object':
//            for (var property in filter) {
//                if (!filter.hasOwnProperty(property))
//                    continue; // ignore inherited properties

//                collection = $.grep(collection, function (item) {
//                    return item[property] === filter[property];
//                });
//            }
//            return collection.slice(0); // copy the array 
//        // (in case of empty object filter)

//        default:
//            throw new TypeError('func must be either a' +
//                'function or an object of properties and values to filter by');
//    }
//};

