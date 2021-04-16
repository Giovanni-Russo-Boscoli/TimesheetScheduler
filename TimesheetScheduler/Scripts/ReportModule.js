var users = [];
var emptyBoxUSerSelection = "List is empty!"
var countBoxUserSelection = 0;

$(document).ready(function () {
    init();
    loadConsolidatedReport();
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
        },
        afterDeselect: function (values) {
            countBoxUserSelection = countItemsBoxUserSelection();
            if (countBoxUserSelection === 0) {
                $("#btnExportExcelBulk").attr('disabled', 'disabled');
                $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);
            } else {
                $("#btnExportExcelBulk").prop("title", "Export Excel File (" + countBoxUserSelection + " file(s))");
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
            $('#boxUserSelection').multiSelect('deselect_all');
            $(this).text("Select All");
        }
        else {
            $('#boxUserSelection').multiSelect('select_all');
            $(this).text("Deselect All");
        }
        $(this).toggleClass("allSelected");
    });

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

function loadConsolidatedReport() {

    var jsonData = [
        {
            "Id": "id data",
            "Period": "period data",
            "Name": "name data",
            "RateExcVat": 995,
            "RateIncVat": 1223.85,
            "DaysWorked": 20,
            "DayRateExcVat": 3313.35,
            "DayRateIncVat": 4075.42,
        },
        {
            "Id": "id data",
            "Period": "period data",
            "Name": "name data",
            "RateExcVat": 515,
            "RateIncVat": 633.45,
            "DaysWorked": 21.5,
            "DayRateExcVat": 11075.50,
            "DayRateIncVat": 13619.17,
        }
    ]

    $('#tableConsolidatedReport').DataTable({
        data: jsonData,
        //"order": [[1, "desc"], [2, "asc"]], //ordered first by Active and then by Name
        "bPaginate": false,
        "bLengthChange": false,
        "bFilter": true,
        "bInfo": false,
        "bAutoWidth": false,
        //"searching": false,
        "columns": [
            { data: "Id" },
            //{ data: "Period" },
            { data: "Name" },
            { data: "RateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€') },
            { data: "RateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€') },
            { data: "DaysWorked" },
            { data: "DayRateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€') },
            { data: "DayRateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€') },
        ],
        "columnDefs": [
            { "className": "dt-center", "targets": "_all" },
            //{
            //    "targets": [11], //EDIT/DELETE BUTTONS
            //    "orderable": false
            //},
            {
                "targets": [0], //Id
                "visible": false
            },
            //{
            //    "targets": 3,
            //    "createdCell": function (td, cellData, rowData, row, col) {
            //        $(this).attr('title', rowData.Rate);
            //    }
            //}
        ]
    });
}