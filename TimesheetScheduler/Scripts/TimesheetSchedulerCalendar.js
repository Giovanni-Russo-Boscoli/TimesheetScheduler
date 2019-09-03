

$(document).ready(function () {
    LoadMonths();
    LoadYears();
    bindMonthDropdown();
    Info();
    UnselecRadio();
    renderMustacheTableTemplate(new Date());
    RowSelected();
    ShowHiddenTimesheetCalendarView();

});

function ShowHiddenTimesheetCalendarView() {
    $("#btnUpdate").on("click", function () {
        $("#calendar").toggle();
        $("#targetTable").toggle();
    });
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
            if (event.url) {
                window.open(event.url, "_blank");
                return false;
            }
        }

    });

    $(".fc-other-month").each(function () {
        $(this).html("");
    });

    calculateLoadBarEvents();
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
        values.rows.push({
            Id: (i + 1),
            tooltipDay: _isWeekend ? "WEEKEND" : "",
            classRow: _isWeekend ? "weekendRow" : "weekdayRow",
            disableFlag: _isWeekend ? "disabled" : "",
            dayShortFormat: formatDate(new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i)),
            day: new Date(getYearFromPage(), getMonthFromPage(), new Date(getLastDayMonthFromPage()).getDate() + i).toDateString(),
            workItem: "34500" + (i + 1),
            description: "description... " + (i + 1),
            chargeableHours: "7.0",
            nonchargeableHours: "2.0",
            comments: "comments... " + (i + 1)
        });
    }
    return values;
}

function formatForCalendarEvents(_obj) {
    var _calendarEvents = [];
    for (i = 0; i < _obj.rows.length; i++) {
        _calendarEvents.push({ title: _obj.rows[i].workItem, start: _obj.rows[i].day, end: _obj.rows[i].day, allDay: false, url: 'http://google.com/' });
    }
    return _calendarEvents;
}

function calculateLoadBarEvents() {
    $(".fc-day-top:not(.fc-other-month)").each(function (index, value) {
        var _loadBar = "<div class='loadBarContainer'>    " +
            "  <div class='progress progress-striped'>" +
            "    <div class='progress-bar'>" +
            "    </div>                      " +
            "  </div> " +
            "</div>";

        $(this).append(_loadBar);
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
