var _bypassTFS = true;

$(document).ready(function ($) {
    registerTriggerAjax();
    LoadMonths();
    LoadYears();
    bindUserNameDropdown();
    bindMonthDropdown();
    Info();
    SaveExcelFile();
    getUserName(LoadUserNames); //this method call ConnectTFS() - async method [need select the user name from windows authentication defore retrieve the events]
    saveEvent();
});

function registerTriggerAjax() {
    jQuery.ajaxSetup({
        beforeSend: function () {
            $('.modalPleaseWait').show();
        },
        complete: function () {
            $('.modalPleaseWait').hide();
        },
        success: function () { }
    });
}

function eventsCalendar(_events, dateCalendar) {
    $('#calendar').fullCalendar('destroy');
    $('#calendar').fullCalendar({
        defaultView: 'month',
        firstDay: 1,
        height: 750,
        contentHeight: "auto",
        weekMode: 'liquid',
        weekends: false,
        fixedWeekCount: true,
        header: { left: 'title', center: ' ', right: 'month, listMonth' },
        defaultDate: _formatDate(dateCalendar, "yyyymmdd", "-"), //'2019-09-16',
        events: _events,
        eventLimit: 4, // for all non-TimeGrid views
        eventClick: function (event) {
            //if (event.url) {
            //    window.open(event.url, "_blank");
            //    return false;
            //}
            ModalEvent(event, false);
        },
        eventMouseover: function (event) {

        },
        eventRender: function (event, element) {
            event.state
            $(element).attr("data-html", "true");
            $(element).attr("data-container", "body");
            $(element).tooltip({
                title: event.title +
                    "<br> Chargeable Hours: " + event.chargeableHours +
                    "<br> Non-Chargeable Hours:" + event.nonchargeableHours +
                    "<br> Description: " + event.description +
                    "<br> Work Items Linked: " + event.comments + 
                    "<br> State: " + event.state,
                placement: "bottom",
            });
        },
        dayClick: function (date, jsEvent, view) {
            prevTime = typeof currentTime === 'undefined' || currentTime === null
                ? new Date().getTime() - 1000000
                : currentTime;
            currentTime = new Date().getTime();

            if (currentTime - prevTime < 500) {
                //DOUBLE CLICK
                ModalEvent(date, true);
            }
        },
        viewRender: function (view, element) {
            calculateLoadBarEvents(_events);
        }

    });

    $(".fc-other-month").each(function () {
        $(this).html("");
    });
}

function toggleClass(_class) {
    $(_class).toggle();
}

function tooltipDaysListView() {
    $(".dayListView").each(function (index, value) {
        $(value).tooltip({
            title: $(value).attr("--title"),
            placement: "bottom"
        });
    });
}

function _formatDate(date, format, separator) {

    if (!separator) {
        separator = "/";
    }

    var d = new Date(date),
        month = '' + (d.getMonth() + 1),
        day = '' + d.getDate(),
        year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    switch (format) {
        case "yyyymmdd": {
            return year + separator + month + separator + day;
            //break;
        }
        case "ddmmyyyy": {
            return day + separator + month + separator + year;
            //break;
        }
        default: {
            return day + separator + month + separator + year;
            //break;
        }
    }
}

function generateRandomNumber(min, max) {
    return Math.random() * (+max - +min) + +min;
}

function formatTFSEventsForCalendar(_obj) {
    var _calendarEvents = [];
    for (i = 0; i < _obj.length; i++) {
        var _startDate = new Date(parseInt(_obj[i].StartDate.substr(6))).toDateString();
        var _chargeableHours = _obj[i].CompletedHours > 7.5 ? 7.5 : (_obj[i].CompletedHours !== null ? _obj[i].CompletedHours : 0);
        var _nonchargeableHours = _obj[i].CompletedHours > 7.5 ? _obj[i].CompletedHours - 7.5 : 0;
        _calendarEvents.push({
            title: "[" + _obj[i].Id + "] " + " - " + _obj[i].Title,
            titleOriginal: _obj[i].Title,
            start: _startDate,
            end: _startDate,
            allDay: false,
            day: _startDate,
            workItem: _obj[i].Id,
            description: _obj[i].Description,
            chargeableHours: _chargeableHours,
            nonchargeableHours: _nonchargeableHours,
            comments: _obj[i].WorkItemsLinked,
            state: _obj[i].State,
            color: returnEventColor(_obj[i].State)
            //url: 'http://google.com/'
        });
    }
    return _calendarEvents;
}

function returnEventColor(state) {
    switch (state) {
        case 'Closed':
            return 'green';
        case 'Active':
            return '#045177';
        case 'New':
            return '#3a87ad';
        default:
            return '#3a87ad'
    }
}

function calculateLoadBarEvents(calendarEvents) {

    var days = [];
    var _chargeableHours = [];

    $(calendarEvents).each(function (index, value) {
        days.push($.format.date(new Date(value.day), "yyyy-MM-dd"));
        _chargeableHours.push(value.chargeableHours); //percentage worked (7.5 = 100%)
    });

    var countChargeableHours = 0;
    $(".fc-day-top").each(function (index, value) {
        countChargeableHours = 0;
        $(days).each(function (_index, _value) {
            if ($(value).attr("data-date") === _value) {
                countChargeableHours += _chargeableHours[_index] === null ? 0 : parseFloat(_chargeableHours[_index]);
            }
        });

        var _class = "";
        var _totalHours = countChargeableHours / 7.5 * 100;
        if (_totalHours > 100) {
            _class = "overloadedLoadBar";
        }
        if (_totalHours < 100) {
            _class = "underloadedLoadBar";
        }
        var _loadBar = "<div class='loadBarContainer'>    " +
            "  <div class='progress progress-striped' style='--loadbar-percent:" + _totalHours + "%'>" +
            "    <div class='progress-bar " + _class + "'>" +
            "    </div>                      " +
            "  </div> " +
            "</div>";

        $(this).append(_loadBar);
        if (!countChargeableHours) {
            progressBarTooltipNull($(this));
        }
        else {
            $(this).find(".progress-bar")
                .tooltip({
                    title: parseFloat(_totalHours).toFixed(2) + "% - Hours: " + parseFloat(countChargeableHours).toFixed(2),
                    placement: "bottom"
                });
        }
    });
}

function listViewActive(_events) {
    $(".fc-listMonth-button:not(.fc-state-active)").click(function () {
        calculateLoadBarEventsForListView(_events);
    });
}

function progressBarTooltipNull(obj) {
    $(obj).find(".progress")
        .tooltip({
            title: "0% - Hours: 0",
            placement: "bottom"
        });
}

function calculateLoadBarEventsForListView(calendarEvents) {

    var days = [];
    var _chargeableHours = [];

    $(calendarEvents).each(function (index, value) {
        days.push($.format.date(new Date(value.day), "yyyy-MM-dd"));
        _chargeableHours.push(value.chargeableHours); //percentage worked (7.5 = 100%)
    });

    $(".fc-list-heading").each(function (index, value) {
        countChargeableHours = 0;
        $(days).each(function (_index, _value) {
            if ($(value).attr("data-date") === _value) {
                countChargeableHours += _chargeableHours[_index] === null ? 0 : parseFloat(_chargeableHours[_index]);
            }
        });

        var _class = "";
        var _totalHours = countChargeableHours / 7.5 * 100;
        if (_totalHours > 100) {
            _class = "overloadedLoadBar";
        }
        if (_totalHours < 100) {
            _class = "underloadedLoadBar";
        }
        var _loadBar = "<tr><td colspan='7' style='padding:0 !important;'><div class='loadBarContainerListView'>    " +
            "  <div class='progress progress-striped' style='--loadbar-percent:" + _totalHours + "%'>" +
            "    <div class='progress-bar " + _class + "'>" +
            "    </div>                      " +
            "  </div> " +
            "</div></td></tr>";

        $(this).find("td").last().append(_loadBar);
        $(this).find(".progress-bar")
            .tooltip({
                title: parseFloat(_totalHours).toFixed(2) + "% - Hours: " + parseFloat(countChargeableHours).toFixed(2),
                placement: "bottom"
            });
    });
}

function getUserName(callback) {
    $.ajax({
        url: "/Home/GetUserName",
        type: "GET",
        success: function (data) {
            callback(data);
        },
        error: function (error) {
            alert("error when trying to retrieve User Name (getUserName()): " + JSON.stringify(error));
        }
    });
}

function LoadUserNames(_userName) {
    var names = [
        "Giovanni Boscoli",
        "Amy Kelly",
        "Eoin OToole",
        "Ian O'Brien",
        "Niall Murphy",
        "Doireann Hanley"];

    names.sort();//ordering A-Z

    var options = "";
    for (i = 0; i < names.length; i++) {
        options += "<option value='" + (i + 1) + "'>" + names[i] + "</option>";
    }
    $("#userNameTimesheet").append(options);

    //select the user logged as default
    $("#userNameTimesheet option").filter(function () {
        return $(this).text() == _userName;
    }).prop('selected', true);

    connectToTFS();
}

function LoadMonths() {
    var months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    var options = "";
    for (i = 0; i < 12; i++) {
        options += "<option value='" + (i + 1) + "'>" + months[i] + "</option>";
    }
    $("#monthTimesheet").append(options);
    $('#monthTimesheet option[value="' + getCurrentMonth() + '"]').prop('selected', true);
}

function LoadYears() {
    var _currentYear = getCurrentYear();
    if (getCurrentMonth() === 12) {
        $("#yearTimesheet").append("<option value='" + (_currentYear - 1) + "'>" + (_currentYear - 1) + "</option>");
    }
    $("#yearTimesheet").append("<option value='" + _currentYear + "'>" + _currentYear + "</option>");
    $('#yearTimesheet option[value="' + _currentYear + '"]').prop('selected', true);
}

//return int e.g.: January = 1 / December = 12
function getCurrentMonth() {
    return new Date().getMonth() + 1;
}

function getCurrentYear() {
    return (new Date).getFullYear();
}

function getUserNameFromPage() {
    //return $("#userNameTimesheet").text();
    return $('#userNameTimesheet').find(":selected").text();
}

function getMonthFromPage() {
    return ($("#monthTimesheet").val() - 1);
}

function getYearFromPage() {
    return $("#yearTimesheet").val();
}

function bindUserNameDropdown() {
    $("#userNameTimesheet").on("change", function () {
        //$body = $("body");
        //$(document).on({
        //    ajaxStart: function () { $body.addClass("loading"); },
        //    ajaxStop: function () { $body.removeClass("loading"); }
        //});
        connectToTFS();
    });
}

function bindMonthDropdown() {
    $("#monthTimesheet").on("change", function () {
        connectToTFS();
    });
}

function getFirstDayMonth(year, month) {
    var date = new Date(year, month);
    var firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
    return firstDay;
}

function getLastDayMonth(year, month) {
    var date = new Date(year, month);
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay.getDate();
}

function getLastDayMonthFullDate(year, month) {
    var date = new Date(year, month);
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay;
}

function getFirstDayMonthFromPage() {
    var date = new Date($("#yearTimesheet").val(), ($("#monthTimesheet").val() - 1));
    var firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
    return firstDay;
}

function getLastDayMonthFullDateFromPage() {
    var date = new Date($("#yearTimesheet").val(), ($("#monthTimesheet").val() - 1));
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay;
}

function getLastDayMonthFromPage() {
    var date = new Date($("#yearTimesheet").val(), ($("#monthTimesheet").val() - 1));
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay.getDate();
}

function formatDate(date) {
    return (date.getDate() + '/' + (date.getMonth() + 1) + '/' + date.getFullYear());
}

function dateMaskById(id, mask) {
    if (!mask) {
        mask = 'DD/MM/YYYY';
    }
    $("#" + id).mask("99/99/9999", { placeholder: mask });
}

function IsWeekend(date) {
    var day = date.getDay();
    return (day === 6) || (day === 0);
}

function Info() {
    $("#btnInfo").click(function () {
        $("#infoModal").modal();
        if ($(".infoMonthLabel").length < 1) {
            $(".H4tTitleInfoModal").text($(".H4tTitleInfoModal").text() + " - " + $(".fc-left").find("h2").text());
            $(".H4tTitleInfoModal").addClass("infoMonthLabel");
        }

        $("#dayWorkedInfoTxt").text(calculateDaysWorked()); //populate this field while rendering events - (no needs to retrieve data again - modal is not being shown but is static html)
        $("#chargeableHoursInfoTxt").text(getChargeableHours()); //populate this field while rendering events -  (no needs to retrieve data again - modal is not being shown but is static html)
        $("#nonchargeableHoursInfoTxt").text(getNonChargeableHours()); //populate this field while rendering events -  (no needs to retrieve data again - modal is not being shown but is static html)
    });

}

function ModalEvent(event, eventCreation) {
    cleanModal();
    $("#eventModal").modal();
    if (!eventCreation) {
        //EDIT
        $("#dayTimesheet").prop("disabled", false);
        dateMaskById("dayTimesheet");
        $("#userNameModal").val(getUserNameFromPage());
        $("#workItemTimesheet").val(event.workItem);
        $("#dayTimesheet").val(_formatDate(event.day, "ddmmyyyy", "/")),        
        $("#descriptionTimesheet").val(event.titleOriginal);
        $("#chargeableTimesheet").val(event.chargeableHours);
        $("#nonchargeableTimesheet").val(event.nonchargeableHours);
        $("#workItemsLinkedTimesheet").val(event.comments);
        populateStateTask(event.state);
        setModalTitle("Event Info");
    } else {
        //NEW/CREATE
        $("#dayTimesheet").prop("disabled", true);
        $("#dayTimesheet").val(_formatDate(event, "ddmmyyyy", "/"));
        $("#descriptionTimesheet").val("Timesheet - "); //TODO - make it variable
        $("#chargeableTimesheet").val(7.5);
        $("#nonchargeableTimesheet").val(0);
        populateStateTask(event.state);
        setModalTitle("Event Creation");
        //to resolve problems trying to do stuff when the modal is loading
        $("#eventModal").on('shown.bs.modal', function () {
            $('#descriptionTimesheet').focus();
        });
    }
}

function populateStateTask(state) {
    switch (state) {
        case 'New': { addOptionToTaskState("New|Active"); break; }
        case 'Active': { addOptionToTaskState("Active|Closed"); break; }
        case 'Closed': { addOptionToTaskState("Closed"); $("#closeTaskTimesheet").attr("disabled", true); break; }
        default: { addOptionToTaskState("New"); $("#closeTaskTimesheet").attr("disabled", true); break; } //no event - creation
    }
}

function addOptionToTaskState(textOption) {
    textOption = textOption.split("|");
    for (i = 0; i < textOption.length; i++) {
        $("#closeTaskTimesheet").append("<option value='" + textOption[i] + "'>" + textOption[i] + "</option>");
    }
}

function cleanModal() {
    $("#userNameModal").val("");
    $("#dayTimesheet").val(""),
    $("#workItemTimesheet").val("");
    $("#descriptionTimesheet").val("");
    $("#chargeableTimesheet").val("");
    $("#nonchargeableTimesheet").val("");
    $("#workItemsLinkedTimesheet").val("");
    $("#closeTaskTimesheet").find("option").remove();
    $("#closeTaskTimesheet").attr("disabled", false);
}

function setModalTitle(title) {
    $("#eventModal .modal-title").text(title);
}

function getChargeableHours() {
    var chargeableHours = 0;
    //$(".chargeablehoursCls").each(function () {

    //USE CALLBACK IN THIS CALL => returnEventsFromTFS
    var _chargeableHours = _bypassTFS ? fakeTFSObj() : returnEventsFromTFS();

    $(_chargeableHours[0]).each(function () {
        chargeableHours += this.CompletedHours === null ? 0 : (this.CompletedHours > 7.5 ? 7.5 : this.CompletedHours);
    });
    return chargeableHours;
}

function getNonChargeableHours() {
    var nonchargeableHours = 0;

    //USE CALLBACK IN THIS CALL => returnEventsFromTFS
    var _nonchargeableHours = _bypassTFS ? fakeTFSObj() : returnEventsFromTFS();

    $(_nonchargeableHours[0]).each(function () {
        nonchargeableHours += (this.CompletedHours === null || this.CompletedHours <= 7.5) ? 0 : this.CompletedHours - 7.5;
    });
    return nonchargeableHours;
}

function calculateDaysWorked() {
    return (getChargeableHours() / (7.5)).toFixed(2);
}

function returnTopPage() {
    window.scrollTo(0, 0);
}

function saveEvent() {
    
    $("#btnSaveEvent").on("click", function () {

        var _workItemNumber = $("#workItemTimesheet").val();
        if (_workItemNumber) {
            //EDIT
            $.ajax({
                url: "/Home/EditTaskOnTFS",
                type: "POST",
                dataType: "json",
                data: {
                    workItemNumber: _workItemNumber,
                    startDate: $("#dayTimesheet").val(),
                    description: $("#descriptionTimesheet").val(),
                    chargeableHours: $("#chargeableTimesheet").val(),
                    nonchargeableHours: $("#nonchargeableTimesheet").val(),
                    state: $("#closeTaskTimesheet").children("option:selected").val()
                },
                success: function (data) {
                    toastrMessage("Saved -> Workitem: [" + data + "]", "success");
                    connectToTFS();
                },
                error: function (error) {
                    alert("error: " + JSON.stringify(error));
                }
            });
        } else {
            //CREATION
            $.ajax({
                url: "/Home/CreateTaskOnTFS",
                type: "POST",
                dataType: "json",
                data: {
                    userName: getUserNameFromPage(),
                    startDate: $("#dayTimesheet").val(),
                    description: $("#descriptionTimesheet").val(),
                    state: $("#closeTaskTimesheet").children("option:selected").val(),
                    chargeableHours: $("#chargeableTimesheet").val(),
                    nonchargeableHours: $("#nonchargeableTimesheet").val()                    
                },
                success: function (data) {
                    toastrMessage("Saved -> Workitem: [" + data + "]", "success");
                    connectToTFS();
                },
                error: function (error) {
                    alert("error: " + JSON.stringify(error));
                }
            });
        }
    });
}

function toastrMessage(msg, typeMessage) {
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

function changeColor() {
    $("#inputColor").change(function (e) { alert(e.target.value); });
}

function connectToTFS() {
    $.ajax({
        url: "/Home/ConnectTFS",
        type: "GET",
        dataType: "json",
        data: { bypassTFS: _bypassTFS, userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
        success: function (data) {

            var dateCalendar = new Date(getYearFromPage(), getMonthFromPage(), 1);
            var eventsTFSFormatted = formatTFSEventsForCalendar(data[0]);

            eventsCalendar(eventsTFSFormatted, dateCalendar);
            eventsCalendarStartDateNotDefined(data[1]);
            listViewActive(eventsTFSFormatted);
            tooltipDaysListView();
        },
        error: function (error) {
            alert("error: " + JSON.stringify(error));
        }
    });
}

function eventsCalendarStartDateNotDefined(eventsStartDateNotDefined) {
    $("#divStatDateNotDefined").empty();
    $(eventsStartDateNotDefined).each(function (index, value) {
        var _creationDate = _formatDate(new Date(parseInt(value.CreationDate.substr(6))).toDateString(), "/");
        var _item = "<div class='eventStartDateNofDefined'>" +
            "<label class='mainLbl'>" + "[" + value.Id + "] - " + value.Title + "</label>" + 
            "<label class='lblTooltip lblStartDateNotDefinedTitle'>" + "[" + value.Id + "] - " + value.Title + "</label>" +
            "<label class='lblTooltip lblStartDateNotDefinedState'>" + value.State + "</label>" + 
            "<label class='lblTooltip lblStartDateNotDefinedDateCreated'>" + _creationDate + "</label>" + 
            "</div>";
        $("#divStatDateNotDefined").append(_item);
    });
    tooltipStartDateNotDefined();
}

function tooltipStartDateNotDefined() {
    $(".eventStartDateNofDefined").each(function (index, value) {
        $(this).attr("data-html", "true");
        $(this).tooltip({
            title: $(this).find(".lblStartDateNotDefinedTitle").text() + "<br>" +
                "State: " + $(this).find(".lblStartDateNotDefinedState").text() + "<br>" +
                "Date Created: " + $(this).find(".lblStartDateNotDefinedDateCreated").text(),
            placement: "bottom"
        });
    });
}

function returnEventsFromTFS() {
    $.ajax({
        url: "/Home/ConnectTFS",
        type: "GET",
        //contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: { bypassTFS: _bypassTFS, userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
        success: function (data) {
            return data;
        },
        error: function (error) {
            alert("error: " + JSON.stringify(error));
        }
    });
}

function SaveExcelFile() {
    $("#btnSaveExcelFile").on("click", function () {
        confirmationSavePath();
    });
}

function confirmationSavePath() {
    $.ajax({
        url: "/Home/TimesheetSaveLocationAndFileName",
        type: "GET",
        data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
        success: function (data) {
            if (confirm("The Excel file will be saved in the following directory: " + data + ".xlsx")) {
                $.ajax({
                    url: "/Home/SaveExcelFile",
                    type: "GET",
                    //contentType: "application/json; charset=utf-8",
                    //dataType: "json",
                    data: { userName: getUserNameFromPage(), _bypassTFS: _bypassTFS, _month: getMonthFromPage() + 1, _year: getYearFromPage() },
                    success: function (data) {
                        toastrMessage("File saved! " + data, "success");
                    },
                    error: function (error) {
                        alert("error: " + JSON.stringify(error));
                    }
                });
            }
            else {
                toastrMessage("Not saved.", "warning");
            }
        },
        error: function (error) {
            toastrMessage("Not saved.", "warning");
            alert("error: " + JSON.stringify(error));
            return false;
        }
    });
}

function fakeTFSObj() {
    var fakeTFS = [
        [
            {
                "Id": "352147",
                "Title": "Timesheet - UI Improvements ",
                "StartDate": "/Date(1567378800000)/",
                "Description": "",
                "CompletedHours": 5.5,
                "WorkItemsLinked": "#350973"
            },
            {
                "Id": "352779",
                "Title": "Timesheet - UI Improvements ",
                "StartDate": "/Date(1567465200000)/",
                "Description": "",
                "CompletedHours": 4.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "353270",
                "Title": "Timesheet - UI Improvements ",
                "StartDate": "/Date(1567551600000)/",
                "Description": "",
                "CompletedHours": 4.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "353573",
                "Title": "Timesheet - UI Improvements + Live bug",
                "StartDate": "/Date(1567638000000)/",
                "Description": "",
                "CompletedHours": 8.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "354065",
                "Title": "Timesheet - Error when opening standalone tables - offset().top (live issue)",
                "StartDate": "/Date(1567724400000)/",
                "Description": "",
                "CompletedHours": 10.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "354295",
                "Title": "Timesheet - Dropdown box not refilling immediately when error message is generated",
                "StartDate": "/Date(1567983600000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "354566",
                "Title": "Timesheet - Dropdown box not refilling immediately when error message is generated",
                "StartDate": "/Date(1568070000000)/",
                "Description": "",
                "CompletedHours": 8.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "354920",
                "Title": "Timesheet - Dialog box becomes very long if you select finder after error messsage appears ",
                "StartDate": "/Date(1568156400000)/",
                "Description": "",
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "355888",
                "Title": "Timesheet - Dialog box becomes very long if you select finder after error messsage appears ",
                "StartDate": "/Date(1568674800000)/",
                "Description": "",
                "CompletedHours": 9.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "356179",
                "Title": "Timesheet - Don't have collections minimise when dialog box is opened",
                "StartDate": "/Date(1568156400000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "356548",
                "Title": "Timesheet - Dropdown text overlapping icon  ",
                "StartDate": "/Date(1568847600000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "356789",
                "Title": "Timesheet - UI Improvements (Favourite Actions)",
                "StartDate": "/Date(1568934000000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "357133",
                "Title": "Timesheet - UI Improvements (Favourite Actions)",
                "StartDate": "/Date(1569193200000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            {
                "Id": "357588",
                "Title": "Timesheet - UI Improvements (Favourite Actions)",
                "StartDate": "/Date(1569279600000)/",
                "Description": "",
                "CompletedHours": 7.5,
                "WorkItemsLinked": null
            },
            //{
            //    "Id": "357930",
            //    "Title": "Timesheet - Freeze headers not working / Double scroll bars related ",
            //    "StartDate": "/Date(1569366000000)/",
            //    "Description": "",
            //    "CompletedHours": 7.5,
            //    "WorkItemsLinked": null
            //}
        ],
        [
            //WorkItemsWithoutStartDate
            {
                "Id": "331577",
                "Title": "Timesheet - International Posting - Implement Contributed Actions ",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "332683",
                "Title": "Timesheet - Implement Articles Dropdown ",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "333005",
                "Title": "Timesheet - Implement Add Contact from existing top 5 ",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "333378",
                "Title": "Timesheet - Add actions (edit) for new properties",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "334022",
                "Title": "Timesheet - Record Signed Action for all decisions",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "334291",
                "Title": "Timesheet - Improve add contact feature",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            },
            {
                "Id": "334597",
                "Title": "Timesheet - Refactor after merge with EESSI branch",
                "StartDate": null,
                "Description": null,
                "CompletedHours": null,
                "WorkItemsLinked": null
            }
        ]
    ];
    return fakeTFS;
}

//function ShowHiddenTimesheetCalendarView() {
//    $("#btnInputColor").on("click", function () {
//        $("#inputColor").click();
//    });

//    $("#inputColor").change(
//        function (e) {
//            $(".fc-day-grid-event").css("background-color", e.target.value);
//        }
//    );
//}