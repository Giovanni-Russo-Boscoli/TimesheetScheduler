

$(document).ready(function () {
    LoadMonths();
    LoadYears();
    bindMonthDropdown();
    Info();
    UnselecRadio();
    renderMustacheTableTemplate(new Date());
    RowSelected();
    ShowHiddenTimesheetCalendarView();
    toggleView();
    saveEvent();
    connectToTFS();
});

function ShowHiddenTimesheetCalendarView() {
    $("#btnInputColor").on("click", function () {
        $("#inputColor").click();
    });

    $("#inputColor").change(
        function (e) {
            $(".fc-day-grid-event").css("background-color", e.target.value);
        }
    );
}

function eventsCalendar(_events, dateCalendar) {
    $('#calendar').fullCalendar('destroy');
    $('#calendar').fullCalendar({
        defaultView: 'month',
        firstDay: 1,
        height: 700,
        contentHeight: "auto",
        weekMode: 'liquid',
        weekends: false,
        fixedWeekCount: true,
        //header: {
        //    left: 'prev,next today',
        //    center: 'title',
        //    right: 'month,agendaWeek,agendaDay,listWeek'
        //},
        header: { left: 'title', center: ' ', right: 'month, listMonth' },
        defaultDate: _formatDate(dateCalendar), //'2019-09-16',
        //navLinks: true,
        //eventLimitClick: function () {
        //    $('[data-toggle="tooltip"]').tooltip({ container: 'body', html: true }); // re-init tooltips
        //    return "popover";
        //},
        events: _events,
        eventLimit: 4, // for all non-TimeGrid views
        eventClick: function (event) {
            //if (event.url) {
            //    window.open(event.url, "_blank");
            //    return false;
            //}
            ModalEvent(event);
            //alert(event.chargeableHours);
        },
        eventMouseover: function (event) {

        },
        eventRender: function (event, element) {
            $(element).attr("data-html", "true");
            $(element).attr("data-container","body");
            $(element).tooltip({
                title: "ChargeableHours: " + event.chargeableHours + " <br> Non-ChargeableHours:" + event.nonchargeableHours + "<br> Description: " + event.description,
                placement: "bottom",
            });
        }

    });

    $(".fc-other-month").each(function () {
        $(this).html("");
    });

    calculateLoadBarEvents(_events);
}

function toggleClass(_class) {
    $(_class).toggle();
}

function renderMustacheTableTemplate(dateCalendar) {
    var template = $('#templateTimesheetTable').html();
    Mustache.parse(template);   // optional, speeds up future uses
    var obj = fakeDataMustache();
    eventsCalendar(formatForCalendarEvents(obj), dateCalendar);
    var rendered = Mustache.render(template, obj);
    $('#targetTable').html(rendered);
}

function _formatDate(date) {
    var d = new Date(date),
        month = '' + (d.getMonth() + 1),
        day = '' + d.getDate(),
        year = d.getFullYear();

    if (month.length < 2) month = '0' + month;
    if (day.length < 2) day = '0' + day;

    return year + "-" + month + "-" + day;
}

function fakeDataMustache() {
    var _date;
    var _isWeekend;
    var values =
    {
        rows: [],
        name: function () {
            return this.tooltipDay + " " + this.classRow;
        }
    }

    for (i = 0; i < getLastDayMonthFromPage(); i++) {
        _date = new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i);
        _isWeekend = IsWeekend(_date);
        if (i % 2 == 0) {
            values.rows.push({
                Id: (i + 1),
                tooltipDay: _isWeekend ? "WEEKEND" : "",
                classRow: _isWeekend ? "weekendRow" : "weekdayRow",
                disableFlag: _isWeekend ? "disabled" : "",
                dayShortFormat: formatDate(new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i)),
                day: new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i).toDateString(),
                workItem: "34500" + (i + 1),
                description: "description... " + (i + 1),
                chargeableHours: i % 2 == 0 ? "8.0" : "6.4",
                nonchargeableHours: "2.0",
                comments: "comments... " + (i + 1)
            });
            values.rows.push({
                Id: (i + 1),
                tooltipDay: _isWeekend ? "WEEKEND" : "",
                classRow: _isWeekend ? "weekendRow" : "weekdayRow",
                disableFlag: _isWeekend ? "disabled" : "",
                dayShortFormat: formatDate(new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i)),
                day: new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i).toDateString(),
                workItem: "34500" + (i + 1),
                description: "description... " + (i + 1),
                chargeableHours: i % 2 == 0 ? "8.0" : "6.4",
                nonchargeableHours: "2.0",
                comments: "comments... " + (i + 1)
            });
        }
        else {
            values.rows.push({
                Id: (i + 1),
                tooltipDay: _isWeekend ? "WEEKEND" : "",
                classRow: _isWeekend ? "weekendRow" : "weekdayRow",
                disableFlag: _isWeekend ? "disabled" : "",
                dayShortFormat: formatDate(new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i)),
                day: new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i).toDateString(),
                workItem: "34500" + (i + 1),
                description: "description... " + (i + 1),
                chargeableHours: i % 2 == 0 ? "8.0" : "6.4",
                nonchargeableHours: "2.0",
                comments: "comments... " + (i + 1)
            });
        }
    }
    return values;
}

function formatForCalendarEvents(_obj) {
    var _calendarEvents = [];
    for (i = 0; i < _obj.rows.length; i++) {
        _calendarEvents.push({
            title: _obj.rows[i].workItem + " - " + _obj.rows[i].description,
            start: _obj.rows[i].day,
            end: _obj.rows[i].day,
            allDay: false,
            day: _obj.rows[i].day,
            workItem: _obj.rows[i].workItem,
            description: _obj.rows[i].description,
            chargeableHours: _obj.rows[i].chargeableHours,
            nonchargeableHours: _obj.rows[i].nonchargeableHours,
            comments: _obj.rows[i].comments
            //url: 'http://google.com/'
        }); 
    }
    return _calendarEvents;
}

function calculateLoadBarEvents(calendarEvents) {

    var days = [];
    var _chargeableHours = [];

    $(calendarEvents).each(function (index, value) {
        days.push($.format.date(new Date(value.day), "yyyy-MM-dd"));
        _chargeableHours.push((value.chargeableHours / 7.5) * 100); //percentage worked (7.5 = 100%)
    });

    var percentageIndex = 0, lastIndex = 0, countChargeableHours = 0;

    $(".fc-day-top:not(.fc-other-month)").each(function (index, value) {
        countChargeableHours = 0;
        percentageIndex = jQuery.inArray($(this).attr("data-date"), days);
        if ((percentageIndex - lastIndex) > 1) {//more then one event in the same day
            for (i = percentageIndex; i > lastIndex; i--) {
                countChargeableHours += _chargeableHours[i-1];
            }
        }
        else {
            countChargeableHours = _chargeableHours[percentageIndex-1];
        }

        lastIndex = percentageIndex;
        //TODO: sum all chargeableHours for each event in the same day
        var _class = "";
        if (countChargeableHours > 100) {
            _class = "overloadedLoadBar";
        }
        var _loadBar = "<div class='loadBarContainer'>    " +
            "  <div class='progress progress-striped' style='--loadbar-percent:" + countChargeableHours + "%'>" +
            "    <div class='progress-bar " + _class + "'>" +
            "    </div>                      " +
            "  </div> " +
            "</div>";


        $(this).append(_loadBar);
        $(this).find(".progress-bar")
            .tooltip({
                title: parseFloat(countChargeableHours).toFixed(2) + "%",
            placement: "bottom",
        });
    });
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
    if (getCurrentMonth() == 12) {
        $("#yearTimesheet").append("<option value='" + (_currentYear - 1) + "'>" + (_currentYear - 1) + "</option>");
    }
    $("#yearTimesheet").append("<option value='" + _currentYear + "'>" + _currentYear + "</option>");
    $('#yearTimesheet option[value="' + _currentYear + '"]').prop('selected', true);
}

//return int e.g.: January = 12 / December = 12
function getCurrentMonth() {
    return new Date().getMonth() + 1;
}

function getCurrentYear() {
    return (new Date).getFullYear();
}

function RowSelected() {
    $(".chk-day").click(function () {
        $(".chk-day").each(function (index, value) {
            $(value).closest("tr").addClass("row-inactive");
            $(this).closest("tr").removeClass("row-active");
        });
        $(this).closest("tr").removeClass("row-inactive");
        $(this).closest("tr").addClass("row-active");
        ChangeWorkItem($(this).attr("id"));
        returnTopPage();
        //highlightRecordOnTopTape($(this).attr("id"));
        $("#dayTimesheet").prop('disabled', true);
    });
}

function getDayFromPage() {
    //var strDate = $("#dayTimesheet" + dataId).text().split("/");
    //var dateBase = new Date(strDate[2], strDate[1] - 1, strDate[0]);
    //return dateBase;
}

function ChangeWorkItem(dataId) {
    //$("#dayTimesheet").val($("#dayTimesheet" + dataId).text());
    //$("#workItemTimesheet").val($("#workitem" + dataId).text());
    //$("#descriptionTimesheet").val($("#description" + dataId).text());
    //$("#chargeableTimesheet").val($("#chargeablehours" + dataId).text());
    //$("#nonchargeableTimesheet").val($("#nonchargeablehours" + dataId).text());
    //$("#commentsTimesheet").val($("#comments" + dataId).text());
}

function getMonthFromPage() {
    return ($("#monthTimesheet").val() - 1);
}

function getYearFromPage() {
    return $("#yearTimesheet").val();
}

function bindMonthDropdown() {
    $("#monthTimesheet").on("change", function () {
        renderMustacheTableTemplate(new Date(getYearFromPage(), getMonthFromPage(), 01));
        //$('#calendar').fullCalendar('addEventSource', events);
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

function IsWeekend(date) {
    var day = date.getDay();
    return (day === 6) || (day === 0);
}

//BUTTONS
function Info() {
    $("#btnInfo").click(function () {
        $("#infoModal").modal();
        $("#dayWorkedInfoTxt").text(calculateDaysWorked());
        $("#chargeableHoursInfoTxt").text(getChargeableHours());
        $("#nonchargeableHoursInfoTxt").text(getNonChargeableHours());
    });
}

function ModalEvent(event) {
    $("#eventModal").modal();
    $("#dayTimesheet").val($.format.date(new Date(event.day), "dd/MM/yyyy")), //_formatDate(event.day);
    $("#workItemTimesheet").val(event.workItem);
    $("#descriptionTimesheet").val(event.description);
    $("#chargeableTimesheet").val(event.chargeableHours);
    $("#nonchargeableTimesheet").val(event.nonchargeableHours);
    $("#commentsTimesheet").val(event.comments);
       //title: _obj.rows[i].workItem + " - " + _obj.rows[i].description,
       //     start: _obj.rows[i].day,
       //     end: _obj.rows[i].day,
       //     allDay: false,
       //     workItem: _obj.rows[i].workItem,
       //     description: _obj.rows[i].description,
       //     chargeableHours: _obj.rows[i].chargeableHours,
       //     nonchargeableHours: _obj.rows[i].nonchargeableHours,
       //     comments: _obj.rows[i].comments
}

function getChargeableHours() {
    var chargeableHours = 0;
    $(".chargeablehoursCls").each(function () {
        if (!$(this).closest("tr").hasClass("weekendRow")) {
            chargeableHours += parseInt($(this).text());
        }
    });
    return chargeableHours;
}

function getNonChargeableHours() {
    var nonchargeableHours = 0;
    $(".nonchargeablehoursCls").each(function () {
        if (!$(this).closest("tr").hasClass("weekendRow")) {
            nonchargeableHours += parseInt($(this).text());
        }
    });
    return nonchargeableHours;
}

function calculateDaysWorked() {
    return (getChargeableHours() / (7.5)).toFixed(2);
}

function UnselecRadio() {
    $("#btnAction").on("click", function () {
        $(".chk-day").prop("checked", false);
        $("tr").addClass("row-intial ");
        $("tr").removeClass("row-inactive");
        $("tr").removeClass("row-active");

        //$("#dayTimesheet").prop('disabled', false);
        //$("#dayTimesheet").val("");
        //$("#workItemTimesheet").val("");
        //$("#descriptionTimesheet").val("");
        //$("#chargeableTimesheet").val("");
        //$("#nonchargeableTimesheet").val("");
        //$("#commentsTimesheet").val("");
    });
}

function returnTopPage() {
    window.scrollTo(0, 0);
}

function toggleView() {
    var listUrl = "/Images/list.png";
    var calendarUrl = "/Images/calendarView.png";
    
    $("#btnView").on("click", function () {
        if ($(this).attr("src") == listUrl) {
            $(this).attr("src", calendarUrl);
            $(this).attr("title", "Change for Calendar View");
        } else {
            $(this).attr("src", listUrl);
            $(this).attr("title", "Change for List View");
        }
        $("#calendar").toggle();
        $("#targetTable").toggle();
    });
}

function saveEvent() {
    $("#btnSaveEvent").on("click", function () {
        toastrMessage("Saved", "success");
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
        //contentType: "application/json; charset=utf-8",
        //dataType: "json",
        //data: JSON.stringify({ controlData: _controlData, _form: $form, idAction: favouriteActionId }),
        //data: JSON.stringify({ controlData: _controlData, form: _form}),
        //data: JSON.stringify({ controlData: _controlData }),
        success: function (data) {
            alert("data[0]: " + JSON.stringify(data[0]));
            alert("data[1]: " + JSON.stringify(data[1]));
        },
        error: function (error) {
            alert("error: " + JSON.stringify(error));
        }
    });
}