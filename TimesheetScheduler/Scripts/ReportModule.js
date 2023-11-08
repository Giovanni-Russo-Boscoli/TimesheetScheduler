var users = [];
var emptyBoxUSerSelection = "List is empty!"
var countBoxUserSelection = 0;

$(document).ready(function () {
    init();
});

function init() {
    loadTeams();
    users = [];
    countBoxUserSelection = 0;
    LoadMonths2();
    LoadYears2();
    $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);
    readJsonUserFile(function (data) {
        users = data;
    });

    loadDefaultPathToSaveExcelFile();
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

    var _month = getMonthFromPage2() + 1;
    var _year = getYearFromPage2();

    var selectedOptions = [];
    $("#boxUserSelection option:selected").each(function (index, value) {
        selectedOptions.push(parseInt($(value).val()));
    });

    var jsonObject = {
        "Month": _month,
        "SelectedMembersId": selectedOptions,
        "Year": parseInt(_year)
    };

    //if (jsonObject) {
    //    $.ajax({
    //        url: "/Home/ConsolidatedReportData",
    //        type: "POST",
    //        //contentType: "application/json; charset=utf-8",
    //        data: jsonObject,
    //        dataType: 'json',
    //        success: function (data) {
    //            loadConsolidatedReport(data.Members);
    //            deselectAll();
    //            loadFigures(data);
    //        },
    //        function(error) {
    //            ajaxErrorHandler("ConsolidatedReportData", error);
    //        }
    //    });
    //}

    if (jsonObject) {
        $.ajax({
            url: "/Home/CreateWordDoc",
            type: "POST",
            //contentType: "application/json; charset=utf-8",
            data: jsonObject,
            dataType: 'json',
            success: function (data) {
                loadConsolidatedReport(data.Members);
                deselectAll();
                loadFigures(data);
            },
            error: function (error) {
                //alert(JSON.stringify(error));
                ajaxErrorHandler("SaveExcelFile", error);
                //$("#saveExcelFilePathModal").modal("hide");
            }
        });
    }

}



function loadUsers(teamId) {
    $.ajax({
        url: "/User/SelectUsersByTeamDivision",
        type: "POST",
        data: {
            teamId: teamId
        },
        success: function (data) {

            data.forEach(function (value, index) {
                value.TeamDivision = value.TeamDivision.replaceAll(" ", ""); //spaces in the group option cause errors (it uses the first string fragment when find a space)
            });

            var _data = {
                "TeamDivisionList": data
            };

            loadMustacheTemplate("teamMembersTemplate", "targetTeamMembers", _data);

            if (data.length > 0) {
                $("#btnSelectAllBoxSelection").removeAttr('disabled');
            } else {
                $("#btnSelectAllBoxSelection").attr('disabled', 'disabled');
                $("#btnExportExcelBulk").attr('disabled', 'disabled');
                $("#btnGetConsolidatedReportMonthly").attr('disabled', 'disabled');
            }

            hideTableRateMonthly();
            bindBtnUserSelection();
            bindBoxUserSelection();
        },
        function(error) {
            ajaxErrorHandler("ConsolidatedReportData", error);
        }
    });
}

//function SortByName(a, b) {
//    var aName = a.Name.toLowerCase();
//    var bName = b.Name.toLowerCase();
//    return ((aName < bName) ? -1 : ((aName > bName) ? 1 : 0));
//}

function bindBoxUserSelection() {
    //http://loudev.com/#project
    $('#boxUserSelection').multiSelect(
        {
            afterSelect: function (value) {
                countBoxUserSelection = countItemsBoxUserSelection();
                if (countBoxUserSelection > 0) {

                    $("#btnExportExcelBulk").removeAttr('disabled');
                    $("#btnExportExcelBulk").prop("title", "Export Excel File (" + countBoxUserSelection + " file(s))");

                    $("#btnGetConsolidatedReportMonthly").removeAttr('disabled');
                    $("#btnGetConsolidatedReportMonthly").prop("title", "Show Monthly Team Rates Report (" + countBoxUserSelection + " member(s))");

                    $("#btnSelectAllBoxSelection").text("Deselect All");

                    if (!$("#btnSelectAllBoxSelection").hasClass("allSelected")) {
                        $("#btnSelectAllBoxSelection").addClass("allSelected");
                    }
                }
            },
            afterDeselect: function (value) {
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
            },
            selectableOptgroup: true
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

function confirmationSavePath_Old() {
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

function confirmationSavePath() {

    var strPath = $("#txtDefault2PathToSaveExcelFile").val();

    if ($('#flexRadioDefault2').is(':checked')) {
        if (!strPath) {
            toastrMessage("Please add the folder path or select the default path above", "warning");
            return;
        }

        //console.log(strPath.substr(strPath.length - 2) + "------" + strPath.substr(strPath.length - 2).replace('\\\\', ''));

        if (strPath.substr(strPath.length - 2).replace('\\\\', '').length > 0) {
            strPath += "\\\\";
        }

        $.when(
            $.ajax({
                url: "/Home/PathExists",
                type: "GET",
                async: false,
                //data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage(), path: strPath },
                data: { path: strPath },
                error: function (error) {
                    ajaxErrorHandler("Not saved. (confirmationSavePath function) \n", error);
                    return false;
                }
            })
        ).then(function (data, textStatus, jqXHR) {
            if (data) {
                BulkSaveExcelFile(strPath);
            }
        });
    }
    else if ($('#flexRadioDefault1').is(':checked')) {

        $.when(
            $.ajax({
                url: "/Home/TimesheetSaveLocation",
                type: "GET",
                async: false,
                //data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
                error: function (error) {
                    ajaxErrorHandler("Not saved. (confirmationSavePath function) \n", error);
                    return false;
                }
            })
        ).then(function (data, textStatus, jqXHR) {
            BulkSaveExcelFile(data);
        });
    }
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
                //data: JSON.stringify(jsonObject),
                data: JSON.stringify({ userData: jsonObject, folderPath: strPath }),
                cache: false,
                success: function (data) {
                    toastrMessage(data, "success");
                    $('#boxUserSelection').multiSelect('deselect_all');
                    $("#btnExportExcelBulk").attr('disabled', 'disabled');
                    $("#btnExportExcelBulk").prop("title", emptyBoxUSerSelection);
                    //$("#saveExcelFilePathModal").hide();
                    $("#saveExcelFilePathModal").modal("hide");
                },
                error: function (error) {
                    ajaxErrorHandler("BulkSaveExcelFile", error);
                    //$("#saveExcelFilePathModal").modal("hide");
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

    $("#monthTimesheet").on("change", function () {
        hideTableRateMonthly();
    });
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

    $("#yearTimesheet").on("change", function () {
        hideTableRateMonthly();
    });
}

function getYearFromPage2() {
    return $("#yearTimesheet").val();
}

function loadConsolidatedReport(_data) {

    $('.dataRateMonthlyHidden').addClass("displayTable");
    $('#tableConsolidatedReport').DataTable().destroy();
    var table = $('#tableConsolidatedReport').DataTable({
        data: _data,
        "order": [[6, "asc"], [1, "desc"], [0, "asc"]], //ordered first by Active and then by Name
        "bPaginate": false,
        "bLengthChange": false,
        "bFilter": true,
        "bInfo": false,
        "bAutoWidth": false,
        "searching": false,
        "columns": [
            { data: "MemberName", className: "memberNameCol" },
            { data: "RateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className: "rateExcVatCol" },
            { data: "RateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className: "rateIncVatCol" },
            { data: "DaysWorked", className: "daysWorkedCol" },
            { data: "DayRateExcVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className: "dayRateExcVatCol" },
            { data: "DayRateIncVat", render: $.fn.dataTable.render.number(',', '.', 2, '€'), className: "dayRateIncVatCol" },
            { data: "TeamDivision", className: "teamDivisionCol" },
        ],
        "columnDefs": [
            { "className": "dt-center", "targets": "_all" }
        ]
    });

    highlightRow("tableConsolidatedReport");
}

function highlightRow(elementId) {

    $('#' + elementId + ' tbody tr').each(function () {
        this.setAttribute('title', "Click to highlight this row");
    });

    //var table = $('#' + elementId).DataTable();
    $('#' + elementId + ' tbody').on('click', 'tr', function () {
        $(this).closest("table").addClass("highlightedTable");
        $(this).toggleClass("highlightedRow");
        $(".divResetTableBackground").addClass("displayResetTableBackgroundBtn");
        if ($(".highlightedRow").length === 0) {
            resetTableBackground();
        }

        if ($(this).hasClass("highlightedRow")) {
            this.setAttribute('title', "Click to unhighlight this row");
        } else {
            this.setAttribute('title', "Click to highlight this row");
        }

        
        //var data = table.row(this).data();
        //console.log(data);
        //alert('You clicked on ' + data.MemberName + '\'s row');
    });
}

function hideTableRateMonthly() {
    $('.dataRateMonthlyHidden').removeClass("displayTable");
    $('#tableConsolidatedReport').DataTable().destroy();
}

function loadFigures(_data) {

    var data = {
        "FiguresByTeamDivision": _data.FiguresByTeamDivision,
        "FiguresIndexes": _data.FiguresByTeamDivision,
        "TeamName": _data.TeamName,
        "VatApplied": _data.VatApplied,
        "PeriodSearched": _data.PeriodSearchedString,
        "TotalExclVat": _data.TotalExclVat,
        "TotalInclVat": _data.TotalInclVat,
    };

    loadMustacheTemplate("figuresTemplate","targetFigures",data);

    currencyMask('.currencyFigures', "##,##0.00", "€");

    //$(".figuresByTeamDivision .div-wrap-figure").each(function (index, value) {
    //    var lblText = $(value).find("label");
    //    if (lblText && $(lblText).text().indexOf("Total Incl VAT:") > -1) {
    //        $(this).addClass("figureIncVat");
    //    }
    //    if (lblText && $(lblText).text().indexOf("Total Excl VAT:") > -1) {
    //        $(this).addClass("figureExcVat");
    //    }
    //});
}

function resetTableBackground() {
    $("#tableConsolidatedReport").removeClass("highlightedTable");
    $(".highlightedRow").removeClass("highlightedRow");
    $(".divResetTableBackground").removeClass("displayResetTableBackgroundBtn");
}

function loadTeams() {
    $.ajax({
        url: "/User/ReadJsonProjectIterationFile",
        type: "GET",
        contentType: "application/json; charset=utf-8",
        success: function (data) {
            formatDataLoadTeams(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function formatDataLoadTeams(data) {
    var _data = {
        "teams": data
    };
    loadMustacheTemplate('selectTeamsTemplate', 'targetSelectTeams', _data);
}

function loadMembersByTeam(_this) {
    console.log($("#selectTeamsElement option:selected").val());
    loadUsers($("#selectTeamsElement option:selected").val());
}

function loadDefaultPathToSaveExcelFile() {
    $("#btnExportExcelBulk").on("click", function () {
        $.when(
            $.ajax({
                url: "/Home/TimesheetSaveLocation",
                type: "GET",
                async: false,
                //data: { userName: getUserNameFromPage(), _month: getMonthFromPage() + 1, _year: getYearFromPage() },
                error: function (error) {
                    ajaxErrorHandler("Not saved. (confirmationSavePath function) \n", error);
                    return false;
                }
            })
        ).then(function (data, textStatus, jqXHR) {

            $("#lblDefaultPathToSaveExcelFile").text(data);

            $("#txtDefault2PathToSaveExcelFile").on("input", function () {
                if (!$(this).val()) {//EMPTY
                    $("#flexRadioDefault2").attr("disabled", true);
                    $("#flexRadioDefault1").prop("checked", true);
                }
                else { //WITH VALUE
                    $("#flexRadioDefault2").attr("disabled", false);
                    $("#flexRadioDefault2").prop("checked", true);
                }
            });

            $("input[type=radio][name=flexRadioDefault]").on("change", function () {
                if (this.id == "flexRadioDefault1") {
                    $("#txtDefault2PathToSaveExcelFile").val("");
                    $("#flexRadioDefault2").attr("disabled", true);
                }
            });

            $("#saveExcelFilePathModal").modal();

            //to resolve problems when trying to do stuff when the modal is loading
            //$("#saveExcelFilePathModal").on('shown.bs.modal', function () {
            //    //$('#pathFolderExcelFileId').focus();
            //});

            //$("#saveExcelFilePathModal").on('hide.bs.modal', function () {
            //    //$("#eventModal").css("display", "block");
            //});

            return false;
        });
    });
}