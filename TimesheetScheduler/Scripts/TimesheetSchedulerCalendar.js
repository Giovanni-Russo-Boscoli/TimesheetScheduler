var _bypassTFS = false;
var totalChargeableHours = 0;
var totalNonChargeableHours = 0;
var timerID = 0;

$(document).ready(function ($) {
    registerTriggerAjax();
    LoadMonths();
    LoadYears();
    bindUserNameDropdown();
    bindMonthDropdown();
    closeAllTasksCurrentMonth_Tooltip();
    getUserName(LoadUserNames); //this method call ConnectTFS() - async method [need select the user name from windows authentication defore retrieve the events]
    saveEvent();
    applyBtnClassesInActionsSelect();
    btnActionClick();
    reminderNoEventCreationForToday();
    copyTask();
    enterKeySaveEvent();

    //$('#body-row .collapse').collapse('hide');

    // Collapse/Expand icon
    $('#collapse-icon').addClass('fa-angle-double-left');

    // Collapse click
    $('[data-toggle=sidebar-colapse]').click(function () {
        SidebarCollapse();
    });


});

function SidebarCollapse() {
    $('.menu-collapsed').toggleClass('d-none');
    $('.sidebar-submenu').toggleClass('d-none');
    $('.submenu-icon').toggleClass('d-none');
    $('#sidebar-container').toggleClass('sidebar-expanded sidebar-collapsed');

    // Treating d-flex/d-none on separators with title
    var SeparatorTitle = $('.sidebar-separator-title');
    if (SeparatorTitle.hasClass('d-flex')) {
        SeparatorTitle.removeClass('d-flex');
    } else {
        SeparatorTitle.addClass('d-flex');
    }

    // Collapse/Expand icon
    $('#collapse-icon').toggleClass('fa-angle-double-left fa-angle-double-right');
}

function clearMonthInfoVariables() {
    totalChargeableHours = 0;
    totalNonChargeableHours = 0;
}

function registerTriggerAjax() {
    jQuery.ajaxSetup({
        beforeSend: function () {
            //console.log("show...");
            $('.modalPleaseWait').show();
        },
        complete: function () {
            //console.log("hide...");
            $('.modalPleaseWait').hide();
        },
        success: function () {
        }
    });
}

function eventsCalendar(_events, dateCalendar) {

    var dayEvent = "";
    //var _chargeableHoursPerDay = 0;
    //var _nonChargeableHoursPerDay = 0;

    //var _currentChargeableHours = 0.0;
    //var _currentNonChargeableHours = 0.0;
    var totalChargeableHoursRemaining = 7.5;

    clearMonthInfoVariables();
    $('#calendar').fullCalendar('destroy');
    $('#calendar').fullCalendar({
        defaultView: 'month',
        firstDay: 1,
        height: 800,
        contentHeight: "auto",
        weekMode: 'liquid',
        weekends: false,
        fixedWeekCount: true,
        header: { left: 'title', center: ' ', right: 'month, listMonth' },
        defaultDate: _formatDate(dateCalendar, "yyyymmdd", "-"),
        events: _events,
        eventLimit: 10,
        eventClick: function (event) {
            ModalEvent(event, false);
        },
        eventMouseover: function (event) {

        },
        eventRender: function (event, element) {
            $(element).attr("data-html", "true");
            $(element).attr("data-container", "body");
            $(element).tooltip({
                title: event.title +
                    "<br> Chargeable Hours: " + event.chargeableHours +
                    "<br> Non-Chargeable Hours:" + event.nonchargeableHours +
                    "<br> Description: " + event.description +
                    "<br> Work Items Linked: " + event.comments +
                    "<br> State: " + event.state,
                placement: "bottom"
            });

            //--------- [INIT] -----  CALCULATING INFO MONTH - CHARGEABLE HOURS HOURS / NON-CHARGEABLE HOURS HOURS / DAYS WORKED ------

            if (dayEvent.toString() === event.start.toString() || !dayEvent) {
                //same day - more than one event
                if (totalChargeableHoursRemaining > 0) {
                    if (totalChargeableHoursRemaining >= event.chargeableHours) {
                        totalChargeableHoursRemaining = totalChargeableHoursRemaining - event.chargeableHours;
                        totalChargeableHours += event.chargeableHours;
                        totalNonChargeableHours += event.nonchargeableHours;
                    }
                    else {
                        totalNonChargeableHours += (event.chargeableHours - totalChargeableHoursRemaining) + event.nonchargeableHours;
                        totalChargeableHours += totalChargeableHoursRemaining;
                        totalChargeableHoursRemaining = 0;
                    }
                }
                else {
                    totalNonChargeableHours += event.chargeableHours + event.nonchargeableHours;
                }
            } else {
                //only one event on the day
                totalChargeableHoursRemaining = 7.5;
                totalChargeableHoursRemaining = totalChargeableHoursRemaining - event.chargeableHours;
                totalChargeableHours += event.chargeableHours;
                totalNonChargeableHours += event.nonchargeableHours;
            }

            dayEvent = event.start;

            //--------- [END] -----  CALCULATING INFO MONTH - CHARGEABLE HOURS HOURS / NON-CHARGEABLE HOURS HOURS / DAYS WORKED ------

        },
        dayRender: function (date, cell) {
            if (date._d.setHours(0, 0, 0, 0) < new Date($.now()).setHours(0, 0, 0, 0)) { //ONLY FOR PAST DAYS
                cell.append('<div class="dayOutOfTheOffice">Out Of The Office</div>');
            }
        },
        eventAfterAllRender: function (view) {
            $('#calendar').fullCalendar('clientEvents', function (event) {
                var td = $('td.fc-day[data-date="' + event.start.format('YYYY-MM-DD') + '"]');
                td.find('div:first').remove();
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
    $(".dayOutOfTheOffice").parent().css("background-color", "#FFF0F1"); //change the color for days without event
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
        }
        case "ddmmyyyy": {
            return day + separator + month + separator + year;
        }
        default: {
            return day + separator + month + separator + year;
        }
    }
}

function formatDate(date) {
    return date.getDate() + '/' + (date.getMonth() + 1) + '/' + date.getFullYear();
}

// From /Date(1560330289910)/ to DD/MM/YYYY
function fromJsonDateToDateStringFormatted(strDate) {
    return _formatDate(new Date(parseInt(strDate.substr(6))).toDateString(), "/");
}

function generateRandomNumber(min, max) {
    return Math.random() * (+max - +min) + +min;
}

function formatTFSEventsForCalendar(_obj) {
    var _calendarEvents = [];
    for (i = 0; i < _obj.length; i++) {
        var _startDate = new Date(parseInt(_obj[i].StartDate.substr(6))).toDateString();
        //var _startDate = fromJsonDateToDateStringFormatted(_obj[i].StartDate); //DOES NOT WORK
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
            //description: $($.parseHTML(_obj[i].Description)).text(),
            description: removeHTMLTagsFromString(_obj[i].Description),
            chargeableHours: _chargeableHours,
            nonchargeableHours: _nonchargeableHours,
            comments: _obj[i].WorkItemsLinked,
            state: _obj[i].State,
            color: returnEventColor(_obj[i].State),
            linkUrl: _obj[i].LinkUrl
            //url: _obj[i].LinkUrl
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
            return '#3a87ad';
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
        "Doireann Hanley",
        "Renan Camara",
        "Disha Virk"];

    names.sort();//ordering A-Z

    var options = "";
    for (i = 0; i < names.length; i++) {
        options += "<option value='" + (i + 1) + "'>" + names[i] + "</option>";
    }
    $("#userNameTimesheet").append(options);

    //select the user logged as default
    $("#userNameTimesheet option").filter(function () {
        return $(this).text() === _userName;
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
    return $('#userNameTimesheet').find(":selected").text();
}

function getMonthFromPage() {
    return $("#monthTimesheet").val() - 1;
}

function getNameSelectedMonthFromPage() {
    return $("#monthTimesheet").children("option:selected").text();
}

function getYearFromPage() {
    return $("#yearTimesheet").val();
}

function bindUserNameDropdown() {
    $("#userNameTimesheet").on("change", function () {
        connectToTFS();
    });
}

function bindMonthDropdown() {
    $("#monthTimesheet").on("change", function () {
        connectToTFS();
        closeAllTasksCurrentMonth_Tooltip();
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
    var date = new Date($("#yearTimesheet").val(), $("#monthTimesheet").val() - 1);
    var firstDay = new Date(date.getFullYear(), date.getMonth(), 1);
    return firstDay;
}

function getLastDayMonthFullDateFromPage() {
    var date = new Date($("#yearTimesheet").val(), $("#monthTimesheet").val() - 1);
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay;
}

function getLastDayMonthFromPage() {
    var date = new Date($("#yearTimesheet").val(), $("#monthTimesheet").val() - 1);
    var lastDay = new Date(date.getFullYear(), date.getMonth() + 1, 0);
    return lastDay.getDate();
}

function dateMaskById(id, mask) {
    if (!mask) {
        mask = 'DD/MM/YYYY';
    }
    $("#" + id).mask("99/99/9999", { placeholder: mask });
}

function maskWorkItem(id) {
    var mask = '999999';
    $("#" + id).mask(mask, { placeholder: mask });
}

function IsWeekend(date) {
    var day = date.getDay();
    return day === 6 || day === 0;
}

function Info() {
    $("#infoModal").modal();
    if ($(".infoMonthLabel").length < 1) {
        $(".H4tTitleInfoModal").text("Info - " + $(".fc-left").find("h2").text());
        $(".H4tTitleInfoModal").addClass("infoMonthLabel");
    }

    $("#totalHoursInfoTxt").text(totalChargeableHours + totalNonChargeableHours);
    $("#dayWorkedInfoTxt").text((totalChargeableHours / 7.5).toFixed(2));
    $("#chargeableHoursInfoTxt").text(totalChargeableHours);
    $("#nonchargeableHoursInfoTxt").text(totalNonChargeableHours);
    closeModalActions();
}

function applyBtnClassesInActionsSelect() {
    $(".dropdownActions input").each(function (index, value) {
        if (index % 2 === 0) {
            $(value).addClass("btn-success");
        } else {
            $(value).addClass("btn-primary");
        }
    });
}

function ModalEvent(event, eventCreation) {
    cleanModal();
    $("#eventModal").modal();
    if (!eventCreation) {
        //EDIT
        unsavedForm(true);
        $("#dayTimesheet").prop("disabled", false);
        dateMaskById("dayTimesheet");
        $("#userNameModal").val(getUserNameFromPage());
        $("#workItemTimesheet").val(event.workItem);
        $("#dayTimesheet").val(_formatDate(event.day, "ddmmyyyy", "/"));
        $("#titleTimesheet").val(event.titleOriginal);
        $("#chargeableTimesheet").val(event.chargeableHours);
        $("#nonchargeableTimesheet").val(event.nonchargeableHours);
        $("#descriptionTimesheet").val(event.description);
        $("#workItemsLinkedTimesheet").val(event.comments);
        $(".urlLinkTfs").removeClass("displayNone");
        $("#linkOriginalUrlTimesheet").attr("href", event.linkUrl);
        populateStateTask(event.state);
        setModalTitle("Event Update");
    } else {
        //NEW/CREATE
        unsavedForm(false);
        $("#btnCopyEvent").removeClass("displayNone");
        $("#userNameModal").val(getUserNameFromPage());
        $("#dayTimesheet").prop("disabled", true);
        $("#dayTimesheet").val(_formatDate(event, "ddmmyyyy", "/"));
        $("#titleTimesheet").val("Timesheet - "); //TODO - make it variable
        $("#chargeableTimesheet").val(7.5);
        $("#nonchargeableTimesheet").val(0);
        $(".urlLinkTfs").addClass("displayNone");
        populateStateTask(event.state);
        setModalTitle("Event Creation");
        //to resolve problems when trying to do stuff when the modal is loading
        $("#eventModal").on('shown.bs.modal', function () {
            $('#titleTimesheet').focus();
        });
    }
}

function populateStateTask(state) {
    switch (state) {
        case 'New': { addOptionToTaskState("New|Active|Closed"); break; }
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
    $(".fieldChanged").removeClass("fieldChanged");//clean up all changed fields
    $("#userNameModal").val("");
    $("#wrapperCreationDate").hide();
    $("#creationDateTimesheet").val("");
    $("#dayTimesheet").val("");
    $("#workItemTimesheet").val("");
    $("#titleTimesheet").val("");
    $("#chargeableTimesheet").val("");
    $("#nonchargeableTimesheet").val("");
    $("#workItemsLinkedTimesheet").val("");
    $("#descriptionTimesheet").val("");
    $("#linkOriginalUrlTimesheet").attr("href", "");
    $("#closeTaskTimesheet").find("option").remove();
    $("#closeTaskTimesheet").attr("disabled", false);
    $("#btnCopyEvent").addClass("displayNone");

}

function setModalTitle(title) {
    $("#eventModal .modal-title").text(title);
}

function bindCopyTaskBtn() {
    $("#btnCopyEvent").on("click", function () {
        $("#copyTaskModal").modal();
        $("#workItemIdCopyTask").val("");
        maskWorkItem("workItemIdCopyTask");
        //to resolve problems when trying to do stuff when the modal is loading
        $("#copyTaskModal").on('shown.bs.modal', function () {
            $('#workItemIdCopyTask').focus();
        });
    });
}

function copyTaskByWorkItemNumber() {
    $("#btnCopyTask").on("click", function () {
        //retrieve work item by id
        getWorkItemBy_Id($("#workItemIdCopyTask").val(), function (data) {
            var _chargeableHours = data.CompletedHours > 7.5 ? 7.5 : (data.CompletedHours !== null ? data.CompletedHours : 0);
            var _nonchargeableHours = data.CompletedHours > 7.5 ? data.CompletedHours - 7.5 : 0;
            var _description = removeHTMLTagsFromString(data.Description);
            $("#titleTimesheet").val(data.Title); //TODO - make it variable
            $("#chargeableTimesheet").val(_chargeableHours);
            $("#nonchargeableTimesheet").val(_nonchargeableHours);
            $("#descriptionTimesheet").val(_description);
            closeModalCopyTask();
        });
    });
}

function enableCopyTaskBtn() {
    $("#workItemIdCopyTask").on("keyup", function () {
        if ($(this).val().length === 6) {
            $("#btnCopyTask").attr("disabled", false);
        } else {
            $("#btnCopyTask").attr("disabled", true);
        }
    });
}

function enterKeyForCopyTask() {
    $('#workItemIdCopyTask').keypress(function (e) {
        var key = e.which;
        if (key === 13)  // the enter key code
        {
            $("#btnCopyTask").click();
            return false;
        }
    });
}

function enterKeySaveEvent() {
    $("#dayTimesheet, #titleTimesheet, #chargeableTimesheet, #nonchargeableTimesheet, #descriptionTimesheet").keypress(function (e) {
        var key = e.which;
        if (key === 13)  // the enter key code
        {
            $("#btnSaveEvent").click();
            return false;
        }
    });
}

function copyTask() {
    bindCopyTaskBtn();
    copyTaskByWorkItemNumber();
    enableCopyTaskBtn();
    enterKeyForCopyTask();
}

function saveEvent() {

    $("#btnSaveEvent").on("click", function () {

        var _nonValidFields = validationSaveEvent();

        if (_nonValidFields) {
            toastrMessage("Please fill out the following fields: </br>" + _nonValidFields, "warning");
            return false;
        }

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
                    title: $("#titleTimesheet").val(),
                    chargeableHours: $("#chargeableTimesheet").val(),
                    nonchargeableHours: $("#nonchargeableTimesheet").val(),
                    state: $("#closeTaskTimesheet").children("option:selected").val(),
                    description: $("#descriptionTimesheet").val()
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
                    title: $("#titleTimesheet").val(),
                    state: $("#closeTaskTimesheet").children("option:selected").val(),
                    description: $("#descriptionTimesheet").val(),
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

function validationSaveEvent() {
    var _nonValidFields = "";

    if (!$("#dayTimesheet").val()) {
        _nonValidFields += "Day </br>";
    }
    if (!$("#titleTimesheet").val()) {
        _nonValidFields += " - Title </br>";
    }
    if (!$("#chargeableTimesheet").val()) {
        _nonValidFields += " - Chargeable Hours </br>";
    }
    if (!$("#nonchargeableTimesheet").val()) {
        _nonValidFields += " - Non Chargeable Hours </br>";
    }

    return _nonValidFields;
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
    var dateCalendar = new Date(getYearFromPage(), getMonthFromPage(), 1);
    $.ajax({
        url: "/Home/ConnectTFS",
        type: "GET",
        dataType: "json",
        data: { bypassTFS: _bypassTFS, userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
        success: function (data) {
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
    $("#divStartDateNotDefined").empty();
    $(eventsStartDateNotDefined).each(function (index, value) {
        //var _creationDate = _formatDate(new Date(parseInt(value.CreationDate.substr(6))).toDateString(), "/");
        var _creationDate = fromJsonDateToDateStringFormatted(value.CreationDate);
        var _item = "<div class='eventStartDateNofDefined'>" +
            "<label class='mainLbl'>" + "[" + value.Id + "] - " + value.Title + "</label>" +
            "<label class='lblTooltip lblStartDateNotDefinedTitle'>" + "[" + value.Id + "] - " + value.Title + "</label>" +
            "<label class='lblTooltip lblStartDateNotDefinedState'>" + value.State + "</label>" +
            "<label class='lblTooltip lblStartDateNotDefinedDateCreated'>" + _creationDate + "</label>" +
            "<label class='lblTooltip lblStartDateNotDefinedWorkItem'>" + value.Id + "</label>" +
            "</div>";
        $("#divStartDateNotDefined").append(_item);
    });
    $(".spanCountStartDateNotDefined").empty().append("(" + eventsStartDateNotDefined.length + ")");
    tooltipStartDateNotDefined();
    onClickEventsStartDateNotDefined();
    collapseDivStartDateNotDefined(eventsStartDateNotDefined.length);
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

function onClickEventsStartDateNotDefined() {
    $(".eventStartDateNofDefined").on("click", function () {
        getWorkItemBy_Id($(this).find(".lblStartDateNotDefinedWorkItem").text(), ModalEventWithoutStartDate);
    });
}

function ModalEventWithoutStartDate(event) {
    var _chargeableHours = event.CompletedHours > 7.5 ? 7.5 : (event.CompletedHours !== null ? event.CompletedHours : 0);
    var _nonchargeableHours = event.CompletedHours > 7.5 ? event.CompletedHours - 7.5 : 0;
    //var _creationDate = _formatDate(new Date(parseInt(event.CreationDate.substr(6))).toDateString(), "/");
    var _creationDate = fromJsonDateToDateStringFormatted(event.CreationDate);
    var _description = removeHTMLTagsFromString(event.Description);

    cleanModal();
    $("#eventModal").modal();
    $("#dayTimesheet").prop("disabled", false);
    dateMaskById("dayTimesheet");
    $("#userNameModal").val(getUserNameFromPage());
    $("#workItemTimesheet").val(event.Id);
    $("#wrapperCreationDate").show();
    $("#creationDateTimesheet").val(_creationDate);
    $("#titleTimesheet").val(event.Title);
    $("#chargeableTimesheet").val(_chargeableHours);
    $("#nonchargeableTimesheet").val(_nonchargeableHours);
    $("#descriptionTimesheet").val(_description);
    $("#workItemsLinkedTimesheet").val(event.WorkItemsLinked);
    $(".urlLinkTfs").removeClass("displayNone");
    $("#linkOriginalUrlTimesheet").attr("href", event.LinkUrl);
    populateStateTask(event.State);
    setModalTitle("Event Without Start Date");
    $("#eventModal").on('shown.bs.modal', function () {
        $('#dayTimesheet').focus();
    });
}

function collapseDivStartDateNotDefined(eventsCount) {
    $(".collapseDivStartDateNotDefined").off("click");
    $("#divStartDateNotDefined").hide();
    $(".collapseDivStartDateNotDefined").on("click", function () {
        if (eventsCount > 0) {
            $("#divStartDateNotDefined").toggle();
        } else {
            toastrMessage("No events without 'Start Date'!", "warning");
        }
    });
}

function removeHTMLTagsFromString(str) {
    return $($.parseHTML(str)).text();
}

function confirmationSavePath() {
    $.when(
        $.ajax({
            url: "/Home/TimesheetSaveLocationAndFileName",
            type: "GET",
            async: false,
            data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
            error: function (error) {
                toastrMessage("Not saved. (confirmationSavePath function) \n" + error, "warning");
                return false;
            }
        })
    ).then(function (data, textStatus, jqXHR) {
        SaveExcelFile(data);
    });
}

function SaveExcelFile(strPath) {
    var msgPath = "The Excel file will be saved in the following directory: " + strPath + ".xls";
    if (confirm(msgPath)) {
        closeModalActions();
        $.ajax({
            url: "/Home/SaveExcelFile",
            type: "GET",
            cache: false,
            data: { userName: getUserNameFromPage(), _bypassTFS: _bypassTFS, _month: getMonthFromPage() + 1, _year: getYearFromPage() },
            success: function (data) {
                toastrMessage(data, "success");
            },
            error: function (error) {
                toastrMessage("error (SaveExcelFile function): " + JSON.stringify(error), "warning");
                closeModalActions();
            }
        });
    }
    else {
        toastrMessage("Not saved.", "warning");
    }
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

function btnActionClick() {
    $("#btnActions").on("click", function () {
        $("#actionsModal").modal();
        getUserName(function (data) {
            if (data !== getUserNameFromPage()) {
                $("#btnCloseAllTasks").off();
                $("#btnCloseAllTasks").on("click", function () {
                    toastrMessage("You are not able to close tasks from another user", "warning");
                });
            }
            else {
                $("#btnCloseAllTasks").off();
                $("#btnCloseAllTasks").on("click", function () {
                    closeAllTasksCurrentMonth();
                });
            }
        });
    });
}

function closeModalActions() {
    $("#actionsModal").modal('toggle');
}

function closeModalCopyTask() {
    $("#copyTaskModal").modal('toggle');
}

function closeAllTasksCurrentMonth() {
    var _month = getNameSelectedMonthFromPage();
    if (confirm("Would you like to close all " + _month + " tasks?")) {
        ajaxCloseAllTasks(function (data) {
            closeModalActions();
            connectToTFS();
            if (data === true || data === "True") {
                toastrMessage("All '" + _month + "' Tasks Were Closed", "success");
            }
        }, closeModalActions);
    }
}

function closeAllTasksCurrentMonth_Tooltip() {
    $("#btnCloseAllTasks").attr("title", "Close All  " + getNameSelectedMonthFromPage() + " Tasks");
}

function unsavedForm(onoff) {
    if (onoff) {
        $("#titleTimesheet, #chargeableTimesheet, #nonchargeableTimesheet, #descriptionTimesheet, #closeTaskTimesheet, #dayTimesheet").on("change", function () {
            $(this).addClass("fieldChanged");
        });
    }
    else {
        $("#titleTimesheet, #chargeableTimesheet, #nonchargeableTimesheet, #descriptionTimesheet, #closeTaskTimesheet, #dayTimesheet").off();
    }
}

function reminderNoEventCreationForToday() {
    var _today = new Date();
    var _interval = (60 * 1000) * 60; //60 min
    timerID = setInterval(function () {
        checkUserRequestedActionWithLoggedUser(function (sameUser) {
            if (sameUser) {
                getWorkItemByDay(_today, function (data) {
                    if (jQuery.isEmptyObject(data)) {
                        window.focus();
                        clearReminderInterval();
                        if (confirm("You don't have a event created for today, would like to create now?")) {
                            ModalEvent(_today, true);
                            reminderNoEventCreationForToday();
                        } else {
                            reminderNoEventCreationForToday();
                            return false;
                        }
                    }
                    else {
                        clearReminderInterval();
                    }
                });
            }
        });
       
    }, _interval);
}

//function reminderNoEventCreationForToday() {
//    var tomorrow = new Date();
//    tomorrow.setDate(tomorrow.getDate() + 1);    
//    var _interval = (60 * 100);
//    timerID = setInterval(function () {
//        checkUserRequestedActionWithLoggedUser(function (sameUser) {
//            if (sameUser) {
//                getWorkItemByDay(tomorrow, function (data) {
//                    if (jQuery.isEmptyObject(data)) {
//                        window.focus();
//                        clearReminderInterval();
//                        if (confirm("You don't have a event created for today, would like to create now?")) {
//                            ModalEvent(tomorrow, true);
//                            reminderNoEventCreationForToday();
//                        } else {
//                            reminderNoEventCreationForToday();
//                            return false;
//                        }
//                    }
//                    else {
//                        clearReminderInterval();
//                    }
//                });
//            }
//        });

//    }, _interval);
//}

function clearReminderInterval() {
    clearInterval(timerID);
}

function getWorkItemBy_Id(_workItemId, callback) {
    $.ajax({
        url: "/Home/GetWorkItemById",
        type: "GET",
        dataType: "json",
        data: { workItemId: _workItemId },
        success: function (data) {
            callback(data);
            return data;
        },
        //error: function (error) {
        //    toastrMessage("error (getWorkItemBy_Id): " + JSON.stringify(error), "warning");
        //},
        error: function (xhr, ajaxOptions, thrownError) {
            var dom_nodes = $($.parseHTML(xhr.responseText));
            toastrMessage(dom_nodes.filter('title').text(), "warning");
        }
    });
}

function getWorkItemByDay(_day, callback) {
    $.ajax({
        url: "/Home/GetWorkItemByDay",
        type: "GET",
        dataType: "json",
        data: { userName: getUserNameFromPage(), day: _day.toUTCString() },
        success: function (data) {
            callback(data);
        },
        error: function (error) {
            //alert(JSON.stringify(error));
            toastrMessage("error (getWorkItemById): " + JSON.stringify(error), "warning");
        }
    });
}

function ajaxCloseAllTasks(callback, errorCallback) {
    $.ajax({
        url: "/Home/CloseTasksMonth",
        type: "PUT",
        dataType: "text",
        data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage()},
        success: function (data) {
            callback(data);
        },
        error: function (error) {
            errorCallback();
            toastrMessage("error (ajaxCloseAllTasks): " + JSON.stringify(error), "warning");
        }
    });
}

function checkUserRequestedActionWithLoggedUser(callback) {
    var _userPage = getUserNameFromPage();
    getUserName(function (userLoggedName) {
        if (_userPage === userLoggedName) {
            callback(true);
        }
        callback(false);
    });
}