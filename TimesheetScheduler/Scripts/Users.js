var users = [];
var roles = [];
var projectNames = [];

$(document).ready(function () {

    initUsersTab(false);

    $("#tabMenuUsers").on("click", function () {
        initUsersTab(true);
    });

    $("#tabMenuRoles").on("click", function () {
        initRolesTab(true);
    });

    $("#tabMenuTfsReference").on("click", function () {
        initTfsReferenceTab(true);
    });

    $("#tabVatReference").on("click", function () {
        initVatTab(true);
    });
    //sendEmail();
});

function initUsersTab(destroyDataTable) {
    if (destroyDataTable) {
        $('#userTable').DataTable().destroy();
    }
    users = [];
    roles = [];
    callbackDataTablesUsers();
    readJsonRatesAndRolesFile(function () { });
    readJsonProjectNameTFSFile(function () { });
    cleanEditUserModal();
}

function initRolesTab(destroyDataTable) {
    if (destroyDataTable) {
        $('#rolesTable').DataTable().destroy();
    }
    users = [];
    roles = [];
    readJsonUserFile(callbackDataTablesRatesAndRoles);
    cleanEditRatesAndRolesModal();
}

function initTfsReferenceTab(destroyDataTable) {
    if (destroyDataTable) {
        $('#tfsReferenceTable').DataTable().destroy();
    }
    projectNames = [];
    callbackDataTablesTfsReference();
    cleanEditTfsReferenceModal();
}

function initTeamDivisionTab(destroyDataTable) {
    if (destroyDataTable) {
        $('#teamDivisionTable').DataTable().destroy();
    }

    var data = $('#tfsReferenceTable').DataTable().row($("#hiddenTeamSelected").val()).data();
    getTeamDivisionByTeamId(data, loadTeamDivisionTable);
    cleanEditTeamDivisionModal();
}

function initVatTab(destroyDataTable) {
    if (destroyDataTable) {
        $('#vatTable').DataTable().destroy();
    }
    readJsonVATFile(loadVatTab);
}

function GetResult_AddNewUser(data) {
    if (data === "True" || data === true) {
        $('#editCreateUserModal').modal('hide');
        toastrMessage("User save successfully!", "success");
        initUsersTab(true);
    }
    else {
        toastrMessage("(GetResult_AddNewUser) Something went wrong!", "error");
    }
}

function GetResult_AddNewRole(data) {
    if (data === "True" || data === true) {
        $('#editCreateRolesModal').modal('hide');
        toastrMessage("Role save successfully!", "success");
        initRolesTab(true);
    }
    else {
        toastrMessage("(GetResult_AddNewRole) Something went wrong! \n " + data, "error");
    }
}

function GetResult_AddNewTFSProject(data) {
    if (data === "True" || data === true) {
        $('#editCreateTFSReferenceModal').modal('hide');
        toastrMessage("TFS Project save successfully!", "success");
        initTfsReferenceTab(true);
    }
    else {
        toastrMessage("(GetResult_AddNewTFSProject) Something went wrong! \n " + data, "error");
    }
}

function GetResult_AddNewTeamDivision(data) {
    if (data === "True" || data === true) {
        $('#editCreateTeamDivisionModal').modal('hide');
        toastrMessage("Team Division added successfully!", "success");
        initTeamDivisionTab(true);
    }
    else {
        toastrMessage("(GetResult_AddNewTeamDivision) Something went wrong! \n " + data, "error");
    }
}

function AddNewUserFailure(data) {
    ajaxErrorHandler("(AddNewUserFailure) Something went wrong!", data);
}

function AddNewRoleFailure(data) {
    ajaxErrorHandler("(AddNewRoleFailure) Something went wrong!", data);
}

function AddNewTFSProjectFailure(data) {
    ajaxErrorHandler("(AddNewTFSProjectFailure) Something went wrong!", data);
}

function AddNewTeamDivisionFailure(data) {
    ajaxErrorHandler("(AddNewTeamDivisionFailure) Something went wrong!", data);
}

function readJsonUserFile(callback) {
    $.ajax({
        url: "/User/ReadJsonUserFile",
        type: "GET",
        dataType: "json",
        success: function (data) {
            users = data;
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function readJsonRatesAndRolesFile(callback) {
    $.ajax({
        url: "/User/ReadJsonRatesAndRolesFile",
        type: "GET",
        dataType: "json",
        success: function (data) {
            roles = data;
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function readJsonProjectNameTFSFile(callback) {
    $.ajax({
        url: "/User/ReadJsonProjectIterationFile",
        type: "GET",
        dataType: "json",
        success: function (data) {
            projectNames = data;
            callback(data);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function callbackDataTablesUsers() {
    readJsonUserFile(function (jsonData) {
        //console.log(jsonData);
        $('#userTable').DataTable({
            data: jsonData,
            //"order": [[1, "desc"], [2, "desc"], [3, "asc"], [4, "asc"], [5,"asc"]], //ordered first by Active and then by Name
            "order": [[1, "desc"], [2, "desc"], [3, "asc"], [4, "asc"], [9,"desc"], [5, "asc"]], //ordered first by Active and then by Name
            "columns": [
                {
                    data: "Id",
                    className: "userId"
                },
                {
                    data: "Active",
                    render: function (data, type, row, meta) {
                        var activeStatus = "dotGreenActive";
                        if (data === false || data === "False") {
                            activeStatus = "dotRedActive";
                        }
                        //return "<span class='" + activeStatus + "' onclick='toggleActveStatus(" + row.Id + ");'></span>" + "<span class='hiddenSpan'>" + data + "</span>";
                        return "<span class='" + activeStatus + "'></span>" + "<span class='hiddenSpan'>" + data + "</span>";
                        //hiddenSpan this hidden span is to enable ordering by this columns once we dont have a content to get based on to order
                    }
                },
                {
                    data: "Chargeable",
                    render: function (data) {
                        var chargeableStatus = "dotGreenChargeable";
                        if (data === false || data === "False") {
                            chargeableStatus = "dotRedChargeable";
                        }
                        return "<span class='" + chargeableStatus + "'></span>";
                    }
                },
                { data: "Project.TeamName" },
                { data: "TeamDivision" },
                { data: "Name" },
                { data: "Role" },
                { data: "Email" },
                //{ data: "ProjectNameTFS" },
                //{ data: "IterationPathTFS" },
                { data: "Access" },
                {
                    data: "Rate",
                    render: $.fn.dataTable.render.number(',', '.', 2, '€')
                },
                {
                    data: function (data, type, row, meta) {
                        //var _name = data.Name.replaceAll("'", "''");
                        var _name = data.Name.replace(/'/g, '');
                        console.log(_name);
                        return "<i class='fa fa-pencil-square-o editUser' title='Edit User' onclick='return confirm(" + "\"Do you want to edit: " + _name + " ?\") ? openEditUserModal(" + meta.row + "," + false + ") : \"\"'></i> " +
                            "<i class='fa fa-trash-o deleteUser' title='Delete User' onclick='return confirm(" + "\"Delete " + _name + " ?\") ? deleteUser(" + data.Id + ") : \"\"'></i>";
                    }
                }
            ],
            "columnDefs": [
                { "className": "dt-center", "targets": "_all" },
                {
                    "targets": [10], //EDIT/DELETE BUTTONS
                    "orderable": false
                },
                {
                    "targets": [0], //Id
                    "visible": false
                },
                {
                    "targets": 6, //ROLE
                    "createdCell": function (td, cellData, rowData, row, col) {
                        $(this).attr('title', rowData.Rate);
                    }
                }
            ]
        });

        hideColumnsForNonAdminRole_UserTable();
    });
}

function callbackDataTablesRatesAndRoles() {
    readJsonRatesAndRolesFile(function (jsonData) {
        $('#rolesTable').DataTable({
            data: jsonData,
            "autoWidth": false,
            "order": [[1, "asc"], [3, "asc"]], //ordered first by Active and then by Name
            "columns": [
                {
                    data: "Id",
                    className: "roleId"
                },
                { data: "Role" },
                { data: "ShortName" },
                {
                    data: "Rate",
                    render: $.fn.dataTable.render.number(',', '.', 2, '€')
                },
                {
                    data: function (data, type, row, meta) {
                        return "<i class='fa fa-pencil-square-o editRole' title='Edit Role' onclick='openEditRoleModal(" + meta.row + "," + false + ")'></i> " +
                            "<i class='fa fa-trash-o deleteRole' title='Delete Role' onclick='return confirm(" + "\"Delete " + data.Role + " ?\") ? deleteRole(" + data.Id + ") : \"\"'></i>";
                    }
                }
            ],
            "columnDefs": [
                { "className": "dt-center", "targets": "_all" },
                {
                    "targets": [4],//edit and delete column
                    "orderable": false
                },
                {
                    "targets": [0], //Id
                    "visible": false
                },
                { "width": "40%", "targets": 1 },
                { "width": "25%", "targets": 2 },
                { "width": "25%", "targets": 3 },
                { "width": "10%", "targets": 4 }
            ]
        });

        hideColumnsForNonAdminRole_RolesTable();
    });
}

function callbackDataTablesTfsReference() {
    readJsonProjectNameTFSFile(function (jsonData) {
        var table = $('#tfsReferenceTable').DataTable({
            data: jsonData,
            "autoWidth": false,
            "order": [[1, "asc"]], //ordered first by Active and then by Name
            "columns": [
                {
                    data: "Id",
                    className: "tfsReferenceId"
                },
                { data: "TeamName" },
                { data: "ProjectNameTFS" },
                { data: "IterationPathTFS" },
                {
                    data: function (data, type, row, meta) {
                        return "<i class='fa fa-pencil-square-o editTFSProject' title='Edit TFS Project' onclick='openEditTFSReferenceModal(" + meta.row + "," + false + ")'></i> " +
                            "<i class='fa fa-trash-o deleteTFSProject' title='Delete TFS Project' onclick='return confirm(" + "\"Delete " + data.IterationPathTFS + " ?\") ? deleteTFSProject(" + data.Id + ") : \"\"'></i>";
                    }
                }
            ],
            "columnDefs": [
                { "className": "dt-center", "targets": "_all" },
                {
                    "targets": [4],
                    "orderable": false
                },
                {
                    "targets": [0], //Id
                    "visible": false
                },
                { "width": "25%", "targets": 1 },
                { "width": "25%", "targets": 2 },
                { "width": "25%", "targets": 3 },
                { "width": "25%", "targets": 4 }
            ]
        });

        hideColumnsForNonAdminRole_TFSReference(table);

        $("#tfsReferenceTable_wrapper").addClass("well");
    });
}

function bindRowClickTfsReference(table) {
    $('#tfsReferenceTable tbody').on('click', 'tr', function (value, index) {
        var trRow = table.row(this);
        hideTeamDivisionTable();
        $("#hiddenTeamSelected").val(trRow.index());
        getTeamDivisionByTeamId(trRow.data(), loadTeamDivisionTable);
    });
}

function hideColumnsForNonAdminRole_UserTable() {
    var userTable = $('#userTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        userTable.column(9).visible(_visibleColumns); //RATE
        userTable.column(10).visible(_visibleColumns); //EDIT/DELETE BUTTONS
        $("#btnNewUser").toggle(_visibleColumns);
    });
}

function hideColumnsForNonAdminRole_RolesTable() {
    var roleTable = $('#rolesTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        roleTable.column(3).visible(_visibleColumns); //RATE
        roleTable.column(4).visible(_visibleColumns); //EDIT/DELETE BUTTONS
        $("#btnRoleItem").toggle(_visibleColumns);
    });
}

function hideColumnsForNonAdminRole_TFSReference(table) {
    var tfsReference = $('#tfsReferenceTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        tfsReference.column(4).visible(_visibleColumns);
        $("#btnTFSReferenceItem").toggle(_visibleColumns);
        
        if (_visibleColumns) {
            bindRowClickTfsReference(table);
            $("#divMsg").show();//toggle label info 
        } else {
            $('#tfsReferenceTable tbody tr').off('click');//off click event
            $("#divMsg").hide();//toggle label info 
        }

    });
}

function toggleActveStatus(objId) {
    alert(objId);
}

function deleteUser(_userId) {
    $.ajax({
        url: "/User/DeleteUser",
        type: "POST",
        dataType: "text",
        data: { userId: _userId },
        success: function (data) {
            if (data.toString().toLowerCase() === "true") {
                //if (data === "True" || data === true) {
                toastrMessage("User deleted!", "success");
                initUsersTab(true);
            } else {
                toastrMessage("Something went wrong, user NOT deleted", "error");
            }
        },
        function(error) {
            ajaxErrorHandler("Something went wrong, user NOT deleted", error);
        }
    });
}

function deleteRole(_roleId) {
    $.ajax({
        url: "/User/DeleteRole",
        type: "POST",
        dataType: "json",
        data: { roleId: _roleId },
        success: function (data) {
            if (data === "True" || data === true) {
                toastrMessage("Role deleted!", "success");
                initRolesTab(true);
            } else {
                toastrMessage("Something went wrong, role NOT deleted. \n " + data, "error");
            }
        },
        function(error) {
            ajaxErrorHandler("Something went wrong, role NOT deleted", error);
        }
    });
}

function deleteTFSProject(_tfsProjectId) {
    $.ajax({
        url: "/User/DeleteTFSProject",
        type: "POST",
        dataType: "json",
        data: { tfsProjectId: _tfsProjectId },
        success: function (data) {
            if (data === "True" || data === true) {
                toastrMessage("TFS Project deleted!", "success");
                initTfsReferenceTab(true);
            } else {
                toastrMessage("Something went wrong, TFS Project NOT deleted. \n " + data, "error");
            }
        },
        function(error) {
            ajaxErrorHandler("Something went wrong, TFS Project NOT deleted", error);
        }
    });
}

function deleteTeamDivision(_teamDivisionId, _teamId) {
    $.ajax({
        url: "/User/DeleteTeamDivision",
        type: "POST",
        dataType: "json",
        data: { teamDivisionId: _teamDivisionId, teamId: _teamId },
        success: function (data) {
            if (data === "True" || data === true) {
                toastrMessage("Team Division deleted!", "success");
                initTeamDivisionTab(true);
            } else {
                toastrMessage("Something went wrong, Team Division NOT deleted. \n " + data, "error");
            }
        },
        function(error) {
            ajaxErrorHandler("Something went wrong, Team Division NOT deleted", error);
        }
    });
}

function openEditUserModal(rowIndex, create) {

    //CLEAN MODAL
    cleanEditUserModal();
    //OPEN MODAL
    //$("#editCreateUserModal").modal();

    if (!create) { //EDIT MODE

        var data = $('#userTable').DataTable().row(rowIndex).data();
        populateSelectEditUserModal(function () {

            //HEADER - NAME
            $("#headerLabelUserName").text("Edit User - " + data.Name);

            $("#hiddenUserId").val(data.Id);

            //ACTIVE
            $('#checkBoxContentActive').prop('checked', data.Active).change();

            //NAME
            $("#userNameTxt").val(data.Name);

            //EMAIL
            $("#userEmailTxt").val(data.Email);

            //ROLE
            $("#userRolesSelect option").filter(function () {
                return $(this).text() === data.Role;
            }).prop('selected', true);

            //PROJECT NAME TFS
            $.when(
                $("#userProjectNameTFSSelect option").filter(function () {
                    return $(this).text() === data.Project.TeamName;
                }).prop('selected', true))

                .then(function () {
                    $.when(populateTeamDivisionSelect($("#userProjectNameTFSSelect").children("option:selected").val(), function () {
                        $("#userCategorySelect option").filter(function () {
                            console.log($(this).text() + " === " + data.TeamDivision + ": " + ($(this).text() === data.TeamDivision));
                            return $(this).text() === data.TeamDivision;
                        }).prop('selected', true);
                    }));
                });
            //CHARGEABLE
            $('#checkBoxChargeable').prop('checked', data.Chargeable).change();

            //ACCESS
            $("#userAccessSelect").val(data.Access);

            //SAVE BUTTON
            $("#btnUpdateUser").show();
        });
    }
    else {
        populateSelectEditUserModal();
        //NEW USER
        //HEADER - NAME
        $("#headerLabelUserName").text("New User");

        $("#hiddenUserId").val(0);//send 0 for new users and avoid ModelState.IsValid fail

        //ACTIVE
        $('#checkBoxContentActive').prop('checked', true).change();

        //CHARGEABLE
        $('#checkBoxChargeable').prop('checked', true).change();

        //CREATE BUTTON
        $("#btnCreateUser").show();

       
    }
    ////OPEN MODAL
    $("#editCreateUserModal").modal();
}

function openEditRoleModal(rowIndex, create) {

    //CLEAN MODAL
    cleanEditRatesAndRolesModal();

    if (!create) { //EDIT MODE

        var data = $('#rolesTable').DataTable().row(rowIndex).data();

        //HEADER - NAME
        $("#headerLabelRolesName").text("Edit Role - " + data.Role);

        $("#hiddenRoleId").val(data.Id);

        //ROLE
        $("#roleTxt").val(data.Role);

        //SHORT NAME
        $("#shortNameTxt").val(data.ShortName);

        //RATE
        //var strRate = data.Rate;
        //strRate = strRate.replace(",", "");
        $("#rateTxt").val(parseFloat(data.Rate).toFixed(2));

        //SAVE BUTTON
        $("#btnUpdateRoles").show();
    }
    else {
        //NEW USER
        //HEADER - NAME
        $("#headerLabelRolesName").text("New Role");

        $("#hiddenRoleId").val(0);//send 0 for new users and avoid ModelState.IsValid fail

        //CREATE BUTTON
        $("#btnCreateRoles").show();
    }

    currencyMask('#rateTxt', '####.##');
    //OPEN MODAL
    $("#editCreateRolesModal").modal();
}

function openEditTFSReferenceModal(projectIndex, create) {

    //CLEAN MODAL
    cleanEditTfsReferenceModal();

    if (!create) { //EDIT MODE

        var data = $('#tfsReferenceTable').DataTable().row(projectIndex).data();

        //HEADER - NAME
        $("#headerLabelTFSName").text("Edit TFS Project - " + data.IterationPathTFS);

        $("#hiddenTFSId").val(data.Id);

        //PROJECT NAME TFS
        $("#projectNameTFSTxt").val(data.ProjectNameTFS);

        //SHORT NAME
        $("#iterationPathTFSTxt").val(data.IterationPathTFS);

        $("#teamNameTFSTxt").val(data.TeamName);

        //SAVE BUTTON
        $("#btnUpdateTFSProject").show();
    }
    else {
        //NEW USER
        //HEADER - NAME
        $("#headerLabelTFSName").text("New TFS Project");

        $("#hiddenTFSId").val(0);//send 0 for new users and avoid ModelState.IsValid fail

        //CREATE BUTTON
        $("#btnCreateTFSProject").show();
    }

    //currencyMask('#rateTxt', '####.##');
    //OPEN MODAL
    $("#editCreateTFSReferenceModal").modal();
}

function openEditTeamDivisionModal(projectIndex, create) {

    //CLEAN MODAL
    cleanEditTeamDivisionModal();

    if (!create) { //EDIT MODE

        var data = $('#teamDivisionTable').DataTable().row(projectIndex).data();

        //HEADER - NAME
        $("#headerLabelTeamDivision").text("Edit Team Division - " + data.TeamName);

        $("#hiddenTeamDivisionId").val(data.Id);
        $("#hiddenTeamId").val(data.TeamId);

        //TEAM DIVISION
        $("#teamDivisionTxt").val(data.TeamDivision);

        //SAVE BUTTON
        $("#btnUpdateTeamDivision").show();
    }
    else {
        //NEW DIVISION
        var data = $('#tfsReferenceTable').DataTable().row($("#hiddenTeamSelected").val()).data();
        $("#hiddenTeamId").val(data.Id);

        //HEADER - NAME
        $("#headerLabelTeamDivision").text("New Team Division");

        $("#hiddenTeamDivisionId").val(0);//send 0 for new users and avoid ModelState.IsValid fail

        //CREATE BUTTON
        $("#btnCreateTeamDivision").show();
    }

    //currencyMask('#rateTxt', '####.##');
    //OPEN MODAL
    $("#editCreateTeamDivisionModal").modal();
}

function populateSelectEditUserModal(callback) {

    //ACTIVE
    $('#checkBoxContentActive').bootstrapToggle({
        on: '',
        off: ''
    });

    //CHARGEABLE
    $('#checkBoxChargeable').bootstrapToggle({
        on: '',
        off: ''
    });

    //ROLES
    var _optionsRoles = "<option></option>";
    $(roles).each(function (index, value) {
        _optionsRoles += "<option value='" + value.Role + "' title='" + value.Rate + "'>" + value.Role + "</option>";
    });
    $("#userRolesSelect").html(_optionsRoles);

    //PROJECT NAME TFS
    var _optionsProjectNameTfs = "<option></option>";
    $(projectNames).each(function (index, value) {
        _optionsProjectNameTfs += "<option value='" + value.Id + "' title='" + value.IterationPathTFS + "'>" + value.TeamName + "</option>";
    });
    $("#userProjectNameTFSSelect").html(_optionsProjectNameTfs);

    $("#userProjectNameTFSSelect").off().on("change", function () {
        populateTeamDivisionSelect($(this).children("option:selected").val());
    });

    $("#btnUpdateUser").hide();
    $("#btnCreateUser").hide();

    if (callback) callback();
}

function populateTeamDivisionSelect(_teamId, callback) {
    $.ajax({
        url: "/User/GetTeamDivisionByTeamId",
        type: "POST",
        dataType: "json",
        cache:false,
        data: { teamId: _teamId },
        success: function (data) {
            var _optionsTeamDivision = "<option></option>";
            $(data).each(function (index, value) {
                _optionsTeamDivision += "<option value='" + value.Id + "'>" + value.Division + "</option>";
            });
            $("#userCategorySelect").html(_optionsTeamDivision);
            if (callback) callback();
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function cleanEditUserModal() {
    $("#headerLabelUserName").text("");
    $('#checkBoxContentActive').prop('checked', false).change();
    $("#userNameTxt").val("");
    $("#userEmailTxt").val("");
    $("#userRolesSelect").val("");
    //category
    $('#checkBoxChargeable').prop('checked', false).change();
    $("#userProjectNameTFSSelect").val("");
    $("#userAccessSelect").val("User");

    $("#userProjectNameTFSSelect").html(""); //empty select
    $("#userCategorySelect").html("");//empty select

    clearJqValidErrors("#UserForm");//clean error from ValidationMessageFor
}

function cleanEditRatesAndRolesModal() {
    $("#headerLabelRolesName").text("");
    $("#roleTxt").val("");
    $("#shortNameTxt").val("");
    $("#rateTxt").val("");
    $("#btnUpdateRoles").hide();
    $("#btnCreateRoles").hide();
    //clearJqValidErrors("#RolesForm");//clean error from ValidationMessageFor
}

function cleanEditTfsReferenceModal() {
    $("#headerLabelTFSName").text("");
    $("#projectNameTFSTxt").val("");
    $("#iterationPathTFSTxt").val("");
    $("#btnUpdateTFSProject").hide();
    $("#btnCreateTFSProject").hide();
    $("#teamNameTFSTxt").val("");
}

function cleanEditTeamDivisionModal() {
    $("#headerLabelTeamDivision").text("");
    $("#hiddenTeamDivisionId").text("");
    $("#teamDivisionTxt").text("");
    $("#teamDivisionTxt").val("");
    $("#btnUpdateTeamDivision").hide();
    $("#btnCreateTeamDivision").hide();
}

function readJsonVATFile(callback) {
    $.ajax({
        url: "/User/ReadJsonVATFile",
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

function loadVatTab(_data) {

    $('#vatTable').DataTable().destroy();
    var table = $('#vatTable').DataTable({
        data: _data,
        "order": [[2, "asc"]], //ordered by StartPeriod
        "bPaginate": false,
        "bLengthChange": false,
        "bFilter": true,
        "bInfo": false,
        "bAutoWidth": false,
        "searching": false,
        "columns": [
            { data: "Id" },
            { data: "VATText" },
            {
                data: "StartPeriod",
                render: function (data, type, row, meta) {
                    return fromJsonDateToDateStringFormatted(data);
                }
            },
            {
                data: "EndPeriod",
                render: function (data, type, row, meta) {
                    return fromJsonDateToDateStringFormatted(data);
                }
            },
            {
                data: "Active",
                render: function (data, type, row, meta) {
                    var activeStatus = "dotGreenActive";
                    if (data === false || data === "False") {
                        activeStatus = "dotRedActive";
                    }
                    //return "<span class='" + activeStatus + "' onclick='toggleActveStatus(" + row.Id + ");'></span>" + "<span class='hiddenSpan'>" + data + "</span>";
                    return "<span class='" + activeStatus + "'></span>" + "<span class='hiddenSpan'>" + data + "</span>";
                    //hiddenSpan this hidden span is to enable ordering by this columns once we dont have a content to get based on to order
                }
            },
        ],
        "columnDefs": [
            { "className": "dt-center", "targets": "_all" },
            { "targets": [0], "visible": false }, //Id
        ]
    });
}

function loadTeamDivisionTable(jsonData, tfsObj) {
    var _data = [];
    $(jsonData).each(function (index, value) {
        _data.push({ "Id": value.Id, "TeamName": tfsObj.TeamName, "TeamDivision": value.Division, "TeamId": tfsObj.Id });
    });

    $("#teamDivisionTable").addClass("showDivisionTable");//make table visible
    $("#btnTeamDivision").addClass("showDivisionTable");//make button "new" visible

    $('#teamDivisionTable').DataTable().destroy();
    var table = $('#teamDivisionTable').DataTable({
        data: _data,
        "autoWidth": false,
        "order": [[1, "asc"]], //ordered first by Active and then by Name
        "bPaginate": false,
        "bLengthChange": false,
        "bFilter": true,
        "bInfo": false,
        "bAutoWidth": false,
        "searching": false,
        "columns": [
            { data: "Id" },
            { data: "TeamId" },
            { data: "TeamName" },
            { data: "TeamDivision" },
            {
                data: function (data, type, row, meta) {
                    return "<i class='fa fa-pencil-square-o editTeamDivision' title='Edit Team Division' onclick='openEditTeamDivisionModal(" + meta.row + "," + false + ")'></i> " +
                        "<i class='fa fa-trash-o deleteTeamDivision' title='Delete Team Division' onclick='return confirm(" + "\"Delete " + data.TeamDivision + " ?\") ? deleteTeamDivision(" + data.Id + "," + data.TeamId + ") : \"\"'></i>";
                }
            }
        ],
        "columnDefs": [
            { "className": "dt-center", "targets": "_all" },
            {
                "targets": [0, 1], //Id
                "visible": false
            }
        ]
    });
}

function getTeamDivisionByTeamId(tfsObj, callback) {
    $.ajax({
        url: "/User/GetTeamDivisionByTeamId",
        type: "POST",
        dataType: "json",
        data: { teamId: (tfsObj ? tfsObj.Id : 0) },
        success: function (data) {
            callback(data, tfsObj);
        },
        function(error) {
            ajaxErrorHandler(error);
        }
    });
}

function hideTeamDivisionTable() {
    //$('#teamDivisionTable').DataTable().destroy();
    $("#teamDivisionTable").removeClass("showDivisionTable");
    $("#btnTeamDivision").removeClass("showDivisionTable");
}