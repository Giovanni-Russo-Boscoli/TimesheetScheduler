//http://pcgan0855:88/Home
var totalChargeableHours = 0;
var totalNonChargeableHours = 0;
var timerID = 0;
var users = [];
var holidays = [];

$(document).ready(function ($) {
    readJsonUserFile(function (data) {
        users = data;
        LoadMonths();
        LoadYears();
        bindUserNameDropdown();
        bindMonthDropdown();
        bindYearDropdown();
        closeAllTasksCurrentMonth_Tooltip();
        getUserName(LoadUserNames); //this method call ConnectTFS() - async method [need select the user name from windows authentication defore retrieve the events]
        saveEvent();
        //applyBtnClassesInActionsSelect();
        reminderNoEventCreationForToday();
        copyTask();
        linkWorkItem();
    });
});

function readJsonUserFile(callback) {
    $.ajax({
        url: "/User/ReadJsonUserFile",
        type: "GET",
        dataType: "json",
        success: function (data) {
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function readJsonHolidaysFile(callback) {
    $.ajax({
        url: "/Home/ReadJsonHolidaysFile",
        type: "GET",
        dataType: "json",
        success: function (data) {
            holidays = data;
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function clearMonthInfoVariables() {
    totalChargeableHours = 0;
    totalNonChargeableHours = 0;
}

function eventsCalendar(_events, dateCalendar) {

    readJsonHolidaysFile(function () { 

        //alert(fromJsonDateToDateStringFormatted(holidays[0].Date));

        var dayEvent = "";
        var totalChargeableHoursRemaining = 7.5;

        clearMonthInfoVariables();
        $('#calendar').fullCalendar('destroy');
        $('#calendar').fullCalendar({
            defaultView: 'month',
            firstDay: 1,
            height: 800,
            contentHeight: "auto",
            weekMode: 'liquid',
            weekends: true,
            fixedWeekCount: true,
            header: { left: 'title', center: ' ', right: 'month, listMonth' },
            defaultDate: _formatDate(dateCalendar, "yyyymmdd", "-"),
            events: _events,
            eventLimit: 10,
            eventClick: function (event) {
                ModalEvent(event, false);
            },
            eventMouseover: function (event) {

                var obj =
                    `<div class='objTooltip'>` +
                    `<br/> <span class='boldContent'>` + event.title + `</span>` +
                    `<br/> <span class='boldContent'> Chargeable Hours: </span>` + event.chargeableHours +
                    `<br/> <span class='boldContent'> Non-Chargeable Hours: </span>` + event.nonchargeableHours +
                    `<br/> <span class='boldContent'> Description: </span> <div class='wrapContent'>` + event.description + `</div>` +
                    `      <span class='boldContent'> Work Items Linked: </span>` + event.comments +
                    `<br/> <span class='boldContent'> State: </span>` + event.state +
                    `</div>`;


                $(this).addClass("backgroundColorEventTooltip");
                $("body").append(obj);
            },
            eventMouseout: function (event) {
                $(this).removeClass("backgroundColorEventTooltip");
                $(".objTooltip").remove();
            },
            eventRender: function (event, element) {
                //--------- [INIT] -----  CALCULATING INFO MONTH - CHARGEABLE HOURS HOURS / NON-CHARGEABLE HOURS HOURS / DAYS WORKED ------
                if (event.isWeekend) {
                    totalNonChargeableHours += event.chargeableHours + event.nonchargeableHours;
                } else {
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
                }
                dayEvent = event.start;

                //--------- [END] -----  CALCULATING INFO MONTH - CHARGEABLE HOURS HOURS / NON-CHARGEABLE HOURS HOURS / DAYS WORKED ------

            },
            dayRender: function (date, cell) {
                var _weekend = IsWeekend(date._d);
                if (_weekend) {
                    cell.append('<div class="weekendDay">Weekend</div>');
                } else if (date._d.setHours(0, 0, 0, 0) < new Date($.now()).setHours(0, 0, 0, 0)) {//ONLY FOR PAST DAYS
                    cell.append('<div class="dayOutOfTheOffice">Out Of The Office</div>');
                }

                $.each(holidays, function (index, value) {
                    var _date = _formatDate(date._d.setHours(0, 0, 0, 0), "ddmmyyyy", "/");
                    if (fromJsonDateToDateStringFormatted(value.Date) === _date) {
                        $(cell).find(".weekendDay").remove();
                        $(cell).find(".dayOutOfTheOffice").remove();
                        $(cell).attr("title", value.Description);
                        cell.append("<div class='hollidayEvent' title='" + value.Description + "'>Holliday</div>");
                    }
                });
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
        $(".dayOutOfTheOffice").parent().css({ "background-color": "#FFF0F1", "vertical-align": "middle"}); //change the color for days without event
        $(".weekendDay").parent().css({"background-color":"#F2F2F2", "vertical-align": "middle"}); //change the color for days without event
        $(".hollidayEvent").parent().css({ "background-color": "thistle", "vertical-align": "middle" }); //change the color for days without event

        $(".fc-content-skeleton tbody td").each(function (index, value) {
            if ($(value).html().length < 1) {
                $(value).css("z-index", "-1");
            }
        });

        Info();
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
            description: removeHTMLTagsFromString(_obj[i].Description),
            chargeableHours: _chargeableHours,
            nonchargeableHours: _nonchargeableHours,
            comments: _obj[i].WorkItemsLinked,
            state: _obj[i].State,
            color: returnEventColor(_obj[i].State),
            linkUrl: _obj[i].LinkUrl,
            isWeekend: _obj[i].IsWeekend
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

function getRateUserByName(userName) {
    var rate = 0;
    $(users).each(function (index, value) {
        if (rate !== 0) return;
        if (value.Name === userName && value.Chargeable) {
            rate = value.Rate.toFixed(2);
        }
    });
    return rate;
}

function getUserDataByName(userName) {
    var userData;
    $(users).each(function (index, value) {
        if (value.Name === userName) {
            userData = value;
        }
    });
    return userData;
}

function LoadUserNames(_userName) {

    var names = [];

    $(users).each(function (index, value) {
        if (value.Active) {
            names.push(value.Name);
        }
    });

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
    $("#yearTimesheet").append("<option value='" + (_currentYear - 1) + "'>" + (_currentYear - 1) + "</option>");
    $("#yearTimesheet").append("<option value='" + _currentYear + "'>" + _currentYear + "</option>");
    $('#yearTimesheet option[value="' + _currentYear + '"]').prop('selected', true);
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

function bindYearDropdown() {
    $("#yearTimesheet").on("change", function () {
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
    clearInfoValues();
    getUserName(function (loggedUserName) {
        if (getAccessUserByName(loggedUserName) === "ADMIN") { //identify by role "admin"
            $("#infoPanel").show();

            $("#totalHoursInfoTxt").text(totalChargeableHours + totalNonChargeableHours);
            $("#dayWorkedInfoTxt").text((totalChargeableHours / 7.5).toFixed(2));
            $("#chargeableHoursInfoTxt").text(totalChargeableHours);
            $("#nonchargeableHoursInfoTxt").text(totalNonChargeableHours);

            var rateExcVat = getRateUserByName(getUserNameFromPage());
            if (rateExcVat === 0) {
                toastrMessage("Rate is 0(zero)", "warning");
                //return;
            }

            var rateIncVat = (rateExcVat * 1.23).toFixed(2);
            var totalByDay = (totalChargeableHours / 7.5).toFixed(2);
            var totalExlVatInfoTxt = (totalByDay * rateExcVat).toFixed(2);
            var totalIncVatInfoTxt = (totalByDay * rateIncVat).toFixed(2);

            $("#rateExlVatInfoTxt").text(rateExcVat);
            $("#rateIncVatInfoTxt").text(rateIncVat);
            $("#totalExlVatInfoTxt").text(totalExlVatInfoTxt);
            $("#totalIncVatInfoTxt").text(totalIncVatInfoTxt);
            currencyMask('.currencyMask', '###,###,###.##');
        } else {
            $("#infoPanel").hide();
        }
    });
}

function clearInfoValues() {
    $("#totalHoursInfoTxt").text("");
    $("#dayWorkedInfoTxt").text("");
    $("#chargeableHoursInfoTxt").text("");
    $("#nonchargeableHoursInfoTxt").text("");
    $("#rateExlVatInfoTxt").text("");
    $("#rateIncVatInfoTxt").text("");
    $("#totalExlVatInfoTxt").text("");
    $("#totalIncVatInfoTxt").text("");
}

function ModalEvent(event, eventCreation) {
    cleanModal();
    $("#eventModal").modal();
    if (!eventCreation) {
        //EDIT
        unsavedForm(true);
        enterKeySaveEvent();
        $("#dayTimesheet").prop("disabled", false);
        dateMaskById("dayTimesheet");
        $("#userNameModal").val(getUserNameFromPage());
        $("#workItemTimesheet").val(event.workItem);
        $("#dayTimesheet").val(_formatDate(event.day, "ddmmyyyy", "/"));
        $("#titleTimesheet").val(event.titleOriginal);
        $("#chargeableTimesheet").val(event.chargeableHours);
        $("#nonchargeableTimesheet").val(event.nonchargeableHours);
        $("#descriptionTimesheet").val(event.description);
        populateWorkItemLinkedTable(event.comments);
        $(".urlLinkTfs").removeClass("displayNone");
        $("#linkOriginalUrlTimesheet").attr("href", event.linkUrl);
        populateStateTask(event.state);
        setModalTitle("Event Update");
    } else {
        //NEW/CREATE
        unsavedForm(false);
        enterKeySaveEvent();
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

function populateWorkItemLinkedTable(strWorkItemLinks) {
    if (!strWorkItemLinks) return false;
    var _items = trimSpaces(strWorkItemLinks);
    _items = _items.split("#");
    $(_items).each(function (index, value) {
        if (value) {
            getIdAndTitleWorkItemBy_Id(value, appendLinkWorkItemRow);
        }
    });
}

//removes all white spaces (NOT only begining/end)
function trimSpaces(str) {
    if (str) {
        return str.split(" ").join("");
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
    $("#linkWorkItemTable tbody").html("");
    $("#workItemLinkNotSaved").css("display", "none");
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

        //Toggle event modal behind
        $("#eventModal").css("display", "none");
        $("#copyTaskModal").on('hide.bs.modal', function () {
            $("#eventModal").css("display", "block");
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
            $("#titleTimesheet").val(data.Title);
            $("#chargeableTimesheet").val(_chargeableHours);
            $("#nonchargeableTimesheet").val(_nonchargeableHours);
            $("#descriptionTimesheet").val(_description);
            populateWorkItemLinkedTable(data.WorkItemsLinked);
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
    //, #descriptionTimesheet  -> removed to allow user to use enter/newline in the description field
    $("#dayTimesheet, #titleTimesheet, #chargeableTimesheet, #nonchargeableTimesheet").keypress(function (e) {
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
                    description: $("#descriptionTimesheet").val(),
                    workItemLink: getWorkItemsLinkFromPage()
                },
                success: function (data) {
                    toastrMessage("Saved -> Workitem: [" + data + "]", "success");
                    connectToTFS();
                },
                error: function (xhr) {
                    ajaxErrorHandler("EditTaskOnTFS", xhr);
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
                    workItemLink: getWorkItemsLinkFromPage(),
                    chargeableHours: $("#chargeableTimesheet").val(),
                    nonchargeableHours: $("#nonchargeableTimesheet").val()
                },
                success: function (data) {
                    toastrMessage("Saved -> Workitem: [" + data + "]", "success");
                    connectToTFS();
                },
                error: function (xhr) {
                    ajaxErrorHandler("CreateTaskOnTFS", xhr);
                }
            });
        }
    });
}

function getWorkItemsLinkFromPage() {
    var _workItems = "";
    $(".hiddenWorkItemId").each(function (index, value) {
        _workItems += "#" + $(value).text();
    });
    return _workItems;
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

//function changeColor() {
//    $("#inputColor").change(function (e) { alert(e.target.value); });
//}

function connectToTFS() {
    var dateCalendar = new Date(getYearFromPage(), getMonthFromPage(), 1);

    var userData = getUserDataByName(getUserNameFromPage());

    var jsonObject = {
        "UserName": userData.Name,
        "ProjectNameTFS": userData.ProjectNameTFS,
        "IterationPathTFS": userData.IterationPathTFS,
        "Month": getMonthFromPage() + 1,
        "Year": getYearFromPage()
    };

    $.ajax({
        url: "/Home/ConnectTFS",
        type: "POST",
        contentType: "application/json; charset=utf-8",
        dataType: "json",
        data: JSON.stringify(jsonObject),
        success: function (data) {
            var eventsTFSFormatted = formatTFSEventsForCalendar(data[0]);
            eventsCalendar(eventsTFSFormatted, dateCalendar);
            eventsCalendarStartDateNotDefined(data[1]);
            listViewActive(eventsTFSFormatted);
            tooltipDaysListView();
        },
        error: function (error) {
            ajaxErrorHandler("ConnectTFS", error);
        }
    });
}

function eventsCalendarStartDateNotDefined(eventsStartDateNotDefined) {

    $("#divStartDateNotDefined").empty();

    doSomethingIfIsTheSameUser(function (data) {
        if (data && eventsStartDateNotDefined.length > 0) {
            //only the own user can see this button
            $("#divStartDateNotDefined").append("<button id='btnApplyStartDate' class='btn btn-primary' title='Apply the \"Creation Date\" to the \"Start Date\" for all events'> Apply Start Date</button>");
            bind_btnApplyStartDate();
        } else {
            $(eventsStartDateNotDefined).each(function (index, value) {
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
    });
}

function bind_btnApplyStartDate() {
    $("#btnApplyStartDate").on("click", function () {
        if (confirm("It applies the \"Creation Date\" to \"Start Date\" for all events listed \n(undo is NOT available)")) {
            $.ajax({
                url: "/Home/ApplyCreationDateToStartDate",
                type: "POST",
                dataType: "text",
                data: { userName: getUserNameFromPage() },
                success: function (data) {
                    connectToTFS();
                    toastrMessage(data + " event(s) were applied \"Creation Date\" to \"Start Date\"", "success");
                },
                error: function (error) {
                    ajaxErrorHandler("ApplyCreationDateToStartDate", error);
                }
            });
        }
    });
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
    populateWorkItemLinkedTable(event.WorkItemsLinked);
    $(".urlLinkTfs").removeClass("displayNone");
    //console.log(event.LinkUrl);
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
                ajaxErrorHandler("Not saved. (confirmationSavePath function) \n", error);
                return false;
            }
        })
    ).then(function (data, textStatus, jqXHR) {
            SaveExcelFile(data);
    });
}

function SaveExcelFile(strPath) {
    var msgPath = "The Excel file will be saved in the following directory: \n" + strPath + ".xls";

    if (confirm(msgPath)) {

        var userData = getUserDataByName(getUserNameFromPage());

        var jsonObject = {
            "UserName": userData.Name,
            "ProjectNameTFS": userData.ProjectNameTFS,
            "IterationPathTFS": userData.IterationPathTFS,
            "Month": getMonthFromPage() + 1,
            "Year": getYearFromPage()
        };

        $.ajax({
            url: "/Home/SaveExcelFile",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(jsonObject),
            cache: false,
            success: function (data) {
                toastrMessage(data, "success");
            },
            error: function (error) {
                //alert(JSON.stringify(error));
                ajaxErrorHandler("SaveExcelFile", error);
            }
        });
    }
    else {
        toastrMessage("Not saved.", "warning");
    }
}

function doSomethingIfIsTheSameUser(callback) {
    getUserName(function (data) {
        if (data === getUserNameFromPage()) {
            callback(true);
        }
        callback(false);
    });
}

function closeModalCopyTask() {
    $("#copyTaskModal").modal('toggle');
}

function closeAllTasksCurrentMonth() {
    getUserName(function (data) {
        if (data !== getUserNameFromPage()) {
            toastrMessage("You are not authorized to close tasks from another user", "warning");
        }
        else {
            var _month = getNameSelectedMonthFromPage();
            if (confirm("Do you want close all " + _month + " tasks?")) {
                ajaxCloseAllTasks(function (data) {
                    connectToTFS();
                    if (data === true || data === "True") {
                        toastrMessage("All '" + _month + "' Tasks Were Closed", "success");
                    }
                });
            }
        }
    });
}

function closeAllTasksCurrentMonth_Tooltip() {
    $("#btnCloseAllTasks").attr("title", "Close All '" + getNameSelectedMonthFromPage() + "' Tasks");
}

function unsavedForm(onoff) {
    //#linkWorkItemTable managed in a different way (Work Item Link table)
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
        error: function (xhr) {
            ajaxErrorHandler("GetWorkItemById", xhr);
        }
    });
}

function getIdAndTitleWorkItemBy_Id(_workItemId, callback) {
    $.ajax({
        url: "/Home/GetIdAndTitleWorkItemById",
        type: "GET",
        dataType: "json",
        data: { workItemId: _workItemId },
        success: function (data) {
            callback(data);
            return data;
        },
        error: function (xhr) {
            ajaxErrorHandler("GetIdAndTitleWorkItemById", xhr);
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
            ajaxErrorHandler("GetWorkItemByDay" + error);
            //toastrMessage("error (getWorkItemById): " + JSON.stringify(error), "warning");
        }
    });
}

function ajaxCloseAllTasks(callback, errorCallback) {
    $.ajax({
        url: "/Home/CloseTasksMonth",
        type: "PUT",
        dataType: "text",
        data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
        success: function (data) {
            callback(data);
        },
        error: function (error) {
            errorCallback();
            //toastrMessage("error (ajaxCloseAllTasks): " + JSON.stringify(error), "warning");
            ajaxErrorHandler("ajaxCloseAllTasks", error);
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

// --------------------------------------   LINK WORK ITEM INIT ------------------------------------

function linkWorkItemClick() {
    $("#btnLinkWorkItem").on("click", function () {
        $("#linkWorkItemModal").modal();
        $("#linkWorkItemId").val("");

        //to resolve problems when trying to do stuff when the modal is loading
        $("#linkWorkItemModal").on('shown.bs.modal', function () {
            $('#linkWorkItemId').focus();
        });

        //Toggle event modal behind
        $("#eventModal").css("display", "none");
        $("#linkWorkItemModal").on('hide.bs.modal', function () {
            $("#eventModal").css("display", "block");
        });

        return false;
    });
}

function enableAddLinkBtn() {
    $("#linkWorkItemId").on("keyup", function () {
        if ($(this).val().length === 6) {
            $("#btnSaveLinkWorkItem").attr("disabled", false);
        } else {
            $("#btnSaveLinkWorkItem").attr("disabled", true);
        }
    });
}

function addLinkWorkItem() {
    $("#btnSaveLinkWorkItem").on("click", function () {
        var _workItem = $("#linkWorkItemId").val();
        getIdAndTitleWorkItemBy_Id(_workItem, function (data) {
            if (!isWorkItemLinked(_workItem)) {
                appendLinkWorkItemRow(data, true);
            }
            else {
                toastrMessage("Work Item already linked (" + _workItem + ")", "warning");
            }
            $("#linkWorkItemModal").modal('toggle');
        });
    });
}

function appendLinkWorkItemRow(event, addedByUser) {
    var newRow =
        "<tr>" +
        "<td> <a href='" + event.LinkUrl + "' target='_blank'>" + event.Id + " - " + event.Title + "</a>" +
        "<span class='hiddenWorkItemId'>" + event.Id + "</span>" +
        "</td><td>" +
        "<button class='form-control deleteLinkWorkItemTableRow' title='Link work item'></button>" +
        "</td>" +
        "</tr>";
    $("#linkWorkItemTable").append(newRow);
    bindDeleteWorkItemLinked();
    if (addedByUser) {
        $("#workItemLinkNotSaved").css("display", "block");
    }
}

function isWorkItemLinked(workItemId) {
    var _isLinked = false;
    $(".hiddenWorkItemId").each(function (index, value) {
        if ($(this).text() === workItemId) {
            _isLinked = true;
            return false;
        }
    });
    return _isLinked;
}

function bindDeleteWorkItemLinked() {
    $(".deleteLinkWorkItemTableRow").off().on("click", function () {
        $("#workItemLinkNotSaved").css("display", "block");
        $(this).closest("tr").remove();
        return false;
    });
}

function enterKeyAddWorkItem() {
    $("#linkWorkItemId").keypress(function (e) {
        var key = e.which;
        if (key === 13)  // the enter key code
        {
            $("#btnSaveLinkWorkItem").click();
            return false;
        }
    });
}

function linkWorkItem() {
    linkWorkItemClick();
    maskWorkItem("linkWorkItemId");
    addLinkWorkItem();
    enterKeyAddWorkItem();
    enableAddLinkBtn();
    bindDeleteWorkItemLinked();
}

// --------------------------------------   LINK WORK ITEM END ------------------------------------

function bindBtnMe() {
    //$("#btnMe").on("click", function () {
    //getUserName(function (_userName) {
    //    $("#userNameTimesheet option").filter(function () {
    //        return $(this).text() === _userName;
    //    }).prop('selected', true);//.trigger('change');
    //    connectToTFS();
    //    closeAllTasksCurrentMonth_Tooltip();
    //});
    //connectToTFS();
    //closeAllTasksCurrentMonth_Tooltip();
    //alert($(".loggedUser").text());
    $("#userNameTimesheet option").filter(function () {
        return $(this).text() === $(".loggedUser").text();
    }).prop('selected', true).trigger('change');
    //});
}

function prevNextMonthBtn(prevNext) {
    //("#btnPrevMonth").on("click", function () {
    var _month = $("#monthTimesheet").children("option:selected").val();
    if (prevNext === 0 && _month === 1 || prevNext === 1 && _month === 12) return; //First month - no previous month
    if (prevNext === 0) {
        _month = parseInt(_month) - 1;
    } else {
        _month = parseInt(_month) + 1;
    }
    $("#monthTimesheet").val(_month).trigger("change");
    //});
}

