var dataTable;

$(document).ready(function () {
    dataTable = $('#tblData').DataTable({
        ajax: '/admin/user/getall',
        columns: [
            { data: 'name', width: "15%" },
            { data: 'email', width: "20%" },
            { data: 'phoneNumber', width: "12%" },
            { data: 'company.name', width: "20%" },
            { data: 'role', width: "10%" },
            {
                data: { id: 'id', lockoutEnd: 'lockoutEnd' },
                render: function (data) {

                    var today = new Date().getTime();
                    var lockout = new Date(data.lockoutEnd).getTime();

                    if (lockout > today) {
                        return `<div class="text-center">
                            <a onClick=LockUnlock("${data.id}") class="btn btn-danger text-white mx-1">
                                <i class="bi bi-lock-fill"></i> Lock
                            </a>
                            <a href="/Admin/User/RoleManagement?userId=${data.id}" class="btn btn-danger text-white mx-1">
                                <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        <div/>`
                    }
                    else {
                        return `<div class="text-center">
                            <a onClick=LockUnlock("${data.id}") class="btn btn-success text-white mx-1">
                                <i class="bi bi-unlock-fill"></i> Unlock
                            </a>
                            <a href="/Admin/User/RoleManagement?userId=${data.id}" class="btn btn-danger text-white mx-1">
                                <i class="bi bi-pencil-square"></i> Permission
                            </a>
                        <div/>`
                    }
                    
                },
                width: "23%"
            }
        ]
    });
});

function LockUnlock(id) {
    $.ajax({
        type: "POST",
        url: "/Admin/User/LockUnlock",
        data: JSON.stringify(id),
        contentType: "application/json",
        success: function (data) {
            if (data.success) {
                toastr.success(data.message);
                dataTable.ajax.reload();
            }
        }
    });
}
