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
    if (data === "True" || data === true){
        $('#editCreateTFSReferenceModal').modal('hide');
        toastrMessage("TFS Project save successfully!", "success");
        initTfsReferenceTab(true);
    }
    else {
        toastrMessage("(GetResult_AddNewTFSProject) Something went wrong! \n " + data, "error");
    }
}

//function RemoveMask_AddNewRole() {
//    //$("#rateTxt").val($("#rateTxt").unmask());
//    //$("#rateTxt").unmask();
//}

function AddNewUserFailure(data) {
    ajaxErrorHandler("(AddNewUserFailure) Something went wrong!", data);
}

function AddNewRoleFailure(data) {
    ajaxErrorHandler("(AddNewRoleFailure) Something went wrong!", data);
}

function AddNewTFSProjectFailure(data) {
    ajaxErrorHandler("(AddNewTFSProjectFailure) Something went wrong!", data);
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
        $('#userTable').DataTable({
            data: jsonData,
            "order": [[1, "desc"], [2, "asc"]], //ordered first by Active and then by Name
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
                        return "<span class='" + activeStatus + "' onclick='toggleActveStatus(" + row.Id + ");'></span>" + "<span class='hiddenSpan'>" + data + "</span>";
                        //hiddenSpan this hidden span is to enable ordering by this columns once we dont have a content to get based on to order
                    }
                },
                { data: "Name" },
                { data: "Role" },
                { data: "TeamDivision" },
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
                { data: "Email" },
                { data: "ProjectNameTFS" },
                { data: "IterationPathTFS" },
                { data: "Access" },
                {
                    data: "Rate",
                    render: $.fn.dataTable.render.number(',', '.', 2, '€')
                },
                {
                    data: function (data, type, row, meta) {
                        return "<i class='fa fa-pencil-square-o editUser' title='Edit User' onclick='return confirm(" + "\"Do you want to edit: " + data.Name + " ?\") ? openEditUserModal(" + meta.row + "," + false + ") : \"\"'></i> " +
                               "<i class='fa fa-trash-o deleteUser' title='Delete User' onclick='return confirm(" + "\"Delete " + data.Name + " ?\") ? deleteUser(" + data.Id + ") : \"\"'></i>";
                    }
                }                
            ],
            "columnDefs": [
                { "className": "dt-center", "targets": "_all" },
                {
                    "targets": [11], //EDIT/DELETE BUTTONS
                    "orderable": false
                },
                {
                    "targets": [0], //Id
                    "visible": false
                },
                {
                    "targets": 3,
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
        $('#tfsReferenceTable').DataTable({
            data: jsonData,
            "autoWidth": false,
            "order": [[1, "asc"]], //ordered first by Active and then by Name
            "columns": [
                {
                    data: "Id",
                    className: "tfsReferenceId"
                },
                { data: "ProjectNameTFS" },
                { data: "IterationPathTFS" },
                { data: "TeamName" },
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
                    "targets": [3],
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

        hideColumnsForNonAdminRole_TFSReference();
    });
}

function hideColumnsForNonAdminRole_UserTable() {
    var userTable = $('#userTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        userTable.column(10).visible(_visibleColumns); //RATE
        userTable.column(11).visible(_visibleColumns); //EDIT/DELETE BUTTONS
        $("#btnNewUser").toggle(_visibleColumns);
    });
}

function hideColumnsForNonAdminRole_RolesTable() {
    var userTable = $('#rolesTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        userTable.column(4).visible(_visibleColumns);
        $("#btnRoleItem").toggle(_visibleColumns);
    });
}

function hideColumnsForNonAdminRole_TFSReference() {
    var tfsReference = $('#tfsReferenceTable').DataTable();
    isUserLoggedAdmin(function (_visibleColumns) {
        tfsReference.column(4).visible(_visibleColumns);
        $("#btnTFSReferenceItem").toggle(_visibleColumns);
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
        data: { userId: _userId},
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

function openEditUserModal(rowIndex, create) {

    //CLEAN MODAL
    cleanEditUserModal();
    populateSelectEditUserModal();
    
    if (!create) { //EDIT MODE

        var data = $('#userTable').DataTable().row(rowIndex).data();

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

        //TeamDivision
        $("#userCategorySelect").val(data.TeamDivision);

       //CHARGEABLE
        $('#checkBoxChargeable').prop('checked', data.Chargeable).change();

        //PROJECT NAME TFS
        $("#userProjectNameTFSSelect option").filter(function () {
            return $(this).text() === data.ProjectNameTFS;
        }).prop('selected', true);

        //ACCESS
        $("#userAccessSelect").val(data.Access);

        //SAVE BUTTON
        $("#btnUpdateUser").show();
    }
    else {
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
    //OPEN MODAL
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

function populateSelectEditUserModal() {

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
        //_optionsProjectNameTfs += "<option value='" + value.ProjectNameTFS + "' title='" + value.IterationPathTFS + "'>" + value.ProjectNameTFS + "</option>";
        _optionsProjectNameTfs += "<option value='" + value.Id + "' title='" + value.IterationPathTFS + "'>" + value.TeamName + "</option>";
    });
    $("#userProjectNameTFSSelect").html(_optionsProjectNameTfs);

    $("#btnUpdateUser").hide();
    $("#btnCreateUser").hide();
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

function cleanEditTfsReferenceModal(){
    $("#headerLabelTFSName").text("");
    $("#projectNameTFSTxt").val("");
    $("#iterationPathTFSTxt").val("");
    $("#btnUpdateTFSProject").hide();
    $("#btnCreateTFSProject").hide();    
}
