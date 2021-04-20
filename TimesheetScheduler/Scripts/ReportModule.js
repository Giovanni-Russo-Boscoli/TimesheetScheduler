var users = [];
var emptyBoxUSerSelection = "List is empty!"
var countBoxUserSelection = 0;

$(document).ready(function () {
    init();
    renderHello();
});

function init() {
    users = [];
    countBoxUserSelection = 0;
    readJsonUserFile(function (data) {
        users = data;
        loadUsers();
        LoadMonths2();
        LoadYears2();
        bindBtnUserSelection();
        $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);
    });
}

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

function getConsolidatedReportMonthly() {
    
    var jsonObject = [];
    var _month = getMonthFromPage2() + 1;
    var _year = getYearFromPage2();

    $(".ms-selection .ms-list .ms-elem-selection").each(function (index, value) {
        if ($(value).is(":visible")) {
            var userData = getUserDataByName2($(value).find("span").text());
            var _selected = {
                "UserName": userData.Name,
                "ProjectNameTFS": userData.ProjectNameTFS,
                "IterationPathTFS": userData.IterationPathTFS,
                "Month": _month,
                "Year": _year
            }
            jsonObject.push(_selected);
        }
    });

    if (jsonObject.length > 0) {
        $.ajax({
            url: "/Home/ConsolidatedReportData",
            type: "POST",
            contentType: "application/json; charset=utf-8",
            data: JSON.stringify(jsonObject),
            success: function (data) {
                loadConsolidatedReport(data);
                deselectAll();
            },
            function(error) {
                ajaxErrorHandler("ConsolidatedReportData", error);
            }
        });
    }
}

function loadUsers() {
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
    //$("#boxUserSelection").append(options);
    $("#boxUserSelection").html(options);

    bindBoxUserSelection();
}

function bindBoxUserSelection() {
    //http://loudev.com/#project
    $('#boxUserSelection').multiSelect({
        afterSelect: function (values) {
            countBoxUserSelection = countItemsBoxUserSelection();
            $("#btnExportExcelBulk").removeAttr('disabled');
            $("#btnExportExcelBulk").prop("title", "Export Excel File (" + countBoxUserSelection + " file(s))");

            $("#btnGetConsolidatedReportMonthly").removeAttr('disabled');
            $("#btnGetConsolidatedReportMonthly").prop("title", "Show Monthly Team Rates Report (" + countBoxUserSelection + " member(s))");

            $("#btnSelectAllBoxSelection").text("Deselect All");

            if (!$("#btnSelectAllBoxSelection").hasClass("allSelected")) {
                $("#btnSelectAllBoxSelection").addClass("allSelected");
            }
        },
        afterDeselect: function (values) {
            countBoxUserSelection = countItemsBoxUserSelection();
            if (countBoxUserSelection === 0) {
                $("#btnExportExcelBulk").attr('disabled', 'disabled');
                $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);

                $("#btnGetConsolidatedReportMonthly").attr('disabled', 'disabled');
                $("#btnGetConsolidatedReportMonthly").prop("title", emptyBoxUSerSelection);

                $("#btnSelectAllBoxSelection").text("Select All");

                if ($("#btnSelectAllBoxSelection").hasClass("allSelected")) {
                    $("#btnSelectAllBoxSelection").removeClass("allSelected");
                }

            } else {
                $("#btnExportExcelBulk").prop("title", "Export Excel File (" + countBoxUserSelection + " file(s))");
                $("#btnGetConsolidatedReportMonthly").prop("title", "Show Monthly Team Rates Report (" + countBoxUserSelection + " member(s))");
            }
        }
    });
}

function countItemsBoxUserSelection() {
    var count = 0;
    $(".ms-selection .ms-list .ms-elem-selection").each(function (index, value) {
        if ($(value).is(":visible")) {
            count++;
        }
    });
    return count;
}

function bindBtnUserSelection() {

    $("#btnSelectAllBoxSelection").on("click", function () {
        if ($(this).hasClass("allSelected")) {
            deselectAll();
        }
        else {
            selectAll();
        }
    });

}

function deselectAll() {
    $('#boxUserSelection').multiSelect('deselect_all');
}

function selectAll() {
    $('#boxUserSelection').multiSelect('select_all');
}

function readSelected() {
    var count = 0;
    $(".ms-selection .ms-list .ms-elem-selection").each(function (index, value) {
        if ($(value).is(":visible")) {
            toastrMessage($(value).find("span").text(), "sucess");
            count++;
        }
    });
    if (count === 0) {
        toastrMessage("List is empty", "warning");
    }
}

function confirmationSavePath() {
    $.when(
        $.ajax({
            url: "/Home/TimesheetSaveLocation",
            type: "GET",
            async: false,
            data: {},
            error: function (error) {
                ajaxErrorHandler("Not saved. (confirmationSavePath function) \n", error);
                return false;
            }
        })
    ).then(function (data, textStatus, jqXHR) {
        BulkSaveExcelFile(data);
    });
}

function BulkSaveExcelFile(strPath) {
    //var msgPath = "The Excel files will be saved in the following directory: \n" + strPath + ".xls";
    var msgPath = "The Excel files will be saved in the following directory: \n" + strPath;

    if (confirm(msgPath)) {

        var jsonObject = [];
        var _month = getMonthFromPage2() + 1;
        var _year = getYearFromPage2();

        $(".ms-selection .ms-list .ms-elem-selection").each(function (index, value) {
            if ($(value).is(":visible")) {
                var userData = getUserDataByName2($(value).find("span").text());
                var _selected = {
                    "UserName": userData.Name,
                    "ProjectNameTFS": userData.ProjectNameTFS,
                    "IterationPathTFS": userData.IterationPathTFS,
                    "Month": _month,
                    "Year": _year
                }
                jsonObject.push(_selected);
            }
        });

        if (jsonObject.length > 0) {
            $.ajax({
                url: "/Home/BulkSaveExcelFile",
                type: "POST",
                contentType: "application/json; charset=utf-8",
                data: JSON.stringify(jsonObject),
                cache: false,
                success: function (data) {
                    toastrMessage(data, "success");
                    $('#boxUserSelection').multiSelect('deselect_all');
                    $("#btnExportExcelBulk").attr('disabled', 'disabled');
                    $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);
                },
                error: function (error) {
                    ajaxErrorHandler("BulkSaveExcelFile", error);
                }
            });
        }
    }
    else {
        toastrMessage("Not saved.", "warning");
    }
}

function getUserDataByName2(userName) {
    var userData;
    $(users).each(function (index, value) {
        if (value.Name === userName) {
            userData = value;
        }
    });
    return userData;
}

function LoadMonths2() {
    var months = ["January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December"];
    var options = "";
    for (i = 0; i < 12; i++) {
        options += "<option value='" + (i + 1) + "'>" + months[i] + "</option>";
    }
    $("#monthTimesheet").append(options);
    $('#monthTimesheet option[value="' + getCurrentMonth() + '"]').prop('selected', true);
}

function prevNextMonthBtn2(prevNext) {

    var _month = $("#monthTimesheet").children("option:selected").val();
    _month = parseInt(_month);

    if (prevNext === 0 && _month === 1) {
        _month = 12;
        $("#monthTimesheet").val(_month).trigger("change");
        return;
    }
    else if (prevNext === 1 && _month === 12) {
        _month = 1;
        $("#monthTimesheet").val(_month).trigger("change");
        return;
    }

    if (prevNext === 0) {
        _month = _month - 1;
    }
    else {
        _month = _month + 1;
    }

    $("#monthTimesheet").val(_month).trigger("change");

}

function getMonthFromPage2() {
    return $("#monthTimesheet").val() - 1;
}

function LoadYears2() {
    var _currentYear = getCurrentYear();
    $("#yearTimesheet").append("<option value='" + (_currentYear - 1) + "'>" + (_currentYear - 1) + "</option>");
    $("#yearTimesheet").append("<option value='" + _currentYear + "'>" + _currentYear + "</option>");
    $('#yearTimesheet option[value="' + _currentYear + '"]').prop('selected', true);
}

function getYearFromPage2() {
    return $("#yearTimesheet").val();
}

function loadConsolidatedReport(_data) {

    $('.dataRateMonthlyHidden').addClass("displayTable");
    $('#tableConsolidatedReport').DataTable().destroy();
    $('#tableConsolidatedReport').DataTable({
        data: _data,
        "order": [[6, "asc"], [1, "desc"], [0, "asc"]], //ordered first by Active and then by Name
        "bPaginate": false,
        "bLengthChange": false,
        "bFilter": true,
        "bInfo": false,
        "bAutoWidth": false,
        "searching": false,
        "columns": [
            { data: "MemberName", className:"memberNameCol" },
            { data: "RateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className:"rateExcVatCol" },
            { data: "RateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className:"rateIncVatCol" },
            { data: "DaysWorked", className:"daysWorkedCol" },
            { data: "DayRateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className:"dayRateExcVatCol" },
            { data: "DayRateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className:"dayRateIncVatCol"},
            { data: "TeamDivision", className:"teamDivisionCol" },
        ],
        "columnDefs": [
            { "className": "dt-center", "targets": "_all" }
        ]
    });
}

function hideTableRateMonthly() {
    $('.dataRateMonthlyHidden').removeClass("displayTable");
    $('#tableConsolidatedReport').DataTable().destroy();
}

function renderHello() {

    var data = {
        "figuresData": [
            { labelText: 'Vat Applied', valueText: '23%' },
            { labelText: 'Period Searched', valueText: 'March 2021' },
            { labelText: 'Total Core Excl. VAT', valueText: '€12,658.23' },
            { labelText: 'Total Drawdown Excl. VAT', valueText: '€10,174.95' },
            { labelText: 'Total Core Incl. VAT', valueText: '€16,988.58' },
            { labelText: 'Total Drawdown Incl. VAT', valueText: '€13,734.29' },
        ]
    };

    var template = document.getElementById('figuresTemplate').innerHTML;
    var rendered = Mustache.render(template, data);
    document.getElementById('targetFigures').innerHTML = rendered;
}