(function ($) {
    'use strict';

    function csrfToken() {
        return $('meta[name="csrf-token"]').attr('content') || '';
    }

    window.HrmsAdmin = {
        submitAjaxForm: function (formSelector, url, modalSelector) {
            const $form = $(formSelector);
            if ($form.valid && !$form.valid()) return;

            $('#globalLoadingOverlay').removeClass('d-none');
            $.post(url, $form.serialize())
                .done(function (res) {
                    if (res && res.success) {
                        if (modalSelector) {
                            bootstrap.Modal.getInstance(document.querySelector(modalSelector))?.hide();
                        }
                        Swal.fire('Success', res.message, 'success').then(function () {
                            window.location.reload();
                        });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Operation failed.');
                        Swal.fire('Validation Error', msg, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'An unexpected server error occurred.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        deleteEntity: function (url, title) {
            Swal.fire({
                title: title || 'Confirm deletion?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post(url).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Delete failed.', 'error');
                    }
                });
            });
        },

        // Notifications
        markRead: function (id) {
            $.post('/Notifications/MarkRead/' + id).done(function (res) {
                if (res && res.success) window.location.reload();
            });
        },

        markAllRead: function () {
            $.post('/Notifications/MarkAllRead').done(function (res) {
                if (res && res.success) window.location.reload();
            });
        },

        toggleBroadcastAudience: function () {
            const scope = $('#bcTargetScope').val();
            $('#bcRoleGroup').toggleClass('d-none', scope !== 'Role');
            $('#bcDeptGroup').toggleClass('d-none', scope !== 'Department');
            $('#bcUserGroup').toggleClass('d-none', scope !== 'SpecificUser');
        },

        openTemplateModal: function (data) {
            const t = data || {
                templateId: 0, templateCode: '', templateName: '', moduleName: 'Leave',
                eventTrigger: 'OnApproved', subjectTemplate: '', bodyTemplate: '',
                defaultType: 'Info', defaultPriority: 'Normal', sendInApp: true, sendEmailDigest: false, isActive: true
            };
            $('#ntId').val(t.templateId || 0);
            $('#ntCode').val(t.templateCode || '');
            $('#ntName').val(t.templateName || '');
            $('#ntModule').val(t.moduleName || 'Leave');
            $('#ntTrigger').val(t.eventTrigger || 'OnApproved');
            $('#ntSubject').val(t.subjectTemplate || '');
            $('#ntBody').val(t.bodyTemplate || '');
            $('#ntType').val(t.defaultType || 'Info');
            $('#ntPriority').val(t.defaultPriority || 'Normal');
            $('#ntInApp').prop('checked', t.sendInApp !== false);
            $('#ntEmail').prop('checked', !!t.sendEmailDigest);
            $('#ntActive').prop('checked', t.isActive !== false);
            $('#ntModalTitle').text(t.templateId ? 'Edit Notification Template' : 'New Notification Template');
            new bootstrap.Modal(document.getElementById('notifTemplateModal')).show();
        },

        // Workflows
        openWorkflowModal: function (data) {
            const w = data || {
                workflowId: 0, workflowCode: '', workflowName: '', moduleName: 'Leave',
                departmentId: '', minAmountThreshold: '', autoEscalateHours: 48, description: '', isActive: true
            };
            $('#wfId').val(w.workflowId || 0);
            $('#wfCode').val(w.workflowCode || '');
            $('#wfName').val(w.workflowName || '');
            $('#wfModule').val(w.moduleName || 'Leave');
            $('#wfDept').val(w.departmentId || '');
            $('#wfThreshold').val(w.minAmountThreshold ?? '');
            $('#wfEscalate').val(w.autoEscalateHours || 48);
            $('#wfDesc').val(w.description || '');
            $('#wfActive').prop('checked', w.isActive !== false);
            $('#wfModalTitle').text(w.workflowId ? 'Edit Workflow Definition' : 'New Workflow Definition');
            new bootstrap.Modal(document.getElementById('workflowDefModal')).show();
        },

        openStepModal: function (workflowId, data) {
            const s = data || {
                workflowStepId: 0, workflowId: workflowId || 0, stepOrder: 1, stepName: '',
                approverType: 'ReportingManager', approverRoleId: '', approverEmployeeId: '',
                escalationRoleId: '', slaHours: 24, canDelegate: true, isMandatory: true
            };
            $('#wsId').val(s.workflowStepId || 0);
            $('#wsWorkflowId').val(s.workflowId || workflowId || '');
            $('#wsOrder').val(s.stepOrder || 1);
            $('#wsName').val(s.stepName || '');
            $('#wsApproverType').val(s.approverType || 'ReportingManager');
            $('#wsRoleId').val(s.approverRoleId || '');
            $('#wsEmpId').val(s.approverEmployeeId || '');
            $('#wsEscRoleId').val(s.escalationRoleId || '');
            $('#wsSla').val(s.slaHours || 24);
            $('#wsDelegate').prop('checked', s.canDelegate !== false);
            $('#wsMandatory').prop('checked', s.isMandatory !== false);
            HrmsAdmin.toggleApproverFields();
            $('#wsModalTitle').text(s.workflowStepId ? 'Edit Approval Step' : 'Add Approval Step');
            new bootstrap.Modal(document.getElementById('workflowStepModal')).show();
        },

        toggleApproverFields: function () {
            const type = $('#wsApproverType').val();
            $('#wsRoleGroup').toggleClass('d-none', type !== 'RoleBased' && type !== 'ReportingManager' && type !== 'DepartmentHead');
            $('#wsEmpGroup').toggleClass('d-none', type !== 'SpecificEmployee');
        },

        openAdvanceModal: function (instanceId, refNum, defaultAction) {
            $('#advInstanceId').val(instanceId);
            $('#advRefDisplay').text(refNum);
            $('#advAction').val(defaultAction || 'Approved');
            $('#advComments').val('');
            new bootstrap.Modal(document.getElementById('advanceWorkflowModal')).show();
        },

        // System Settings
        openSettingModal: function (data) {
            const s = data || { settingKey: '', settingValue: '', category: 'General', dataType: 'Text', description: '' };
            $('#stKey').val(s.settingKey || '').prop('readonly', !!s.settingKey);
            $('#stValue').val(s.settingValue || '');
            $('#stCategory').val(s.category || 'General');
            $('#stDataType').val(s.dataType || 'Text');
            $('#stDesc').val(s.description || '');
            $('#stModalTitle').text(s.settingKey ? 'Edit System Setting' : 'Add System Setting');
            new bootstrap.Modal(document.getElementById('systemSettingModal')).show();
        },

        // Access Control
        openUserModal: function (data) {
            const u = data || {
                userId: 0, employeeId: '', branchId: '', username: '', email: '', fullName: '',
                primaryRoleId: '', isActive: true, mustChangePassword: true
            };
            $('#usrId').val(u.userId || 0);
            $('#usrEmployeeId').val(u.employeeId || '');
            $('#usrBranchId').val(u.branchId || '');
            $('#usrUsername').val(u.username || '');
            $('#usrEmail').val(u.email || '');
            $('#usrFullName').val(u.fullName || '');
            $('#usrPassword').val('').prop('required', !u.userId);
            $('#usrPasswordGroup').toggleClass('d-none', !!u.userId);
            $('#usrRoleId').val(u.primaryRoleId || '');
            $('#usrActive').prop('checked', u.isActive !== false);
            $('#usrMustChange').prop('checked', u.mustChangePassword !== false);
            $('#usrModalTitle').text(u.userId ? 'Edit User Account' : 'Create User Account');
            new bootstrap.Modal(document.getElementById('adminUserModal')).show();
        },

        toggleUserStatus: function (userId, action, username) {
            if (action === 'ResetPassword') {
                Swal.fire({
                    title: 'Reset password for ' + username,
                    input: 'password',
                    inputLabel: 'Temporary Password (minimum 8 characters)',
                    inputAttributes: { minlength: 8 },
                    showCancelButton: true,
                    confirmButtonText: 'Reset Password'
                }).then(function (res) {
                    if (!res.isConfirmed || !res.value) return;
                    $.post('/SystemAdmin/ToggleUserStatus', { id: userId, statusAction: 'ResetPassword', newPassword: res.value })
                        .done(function (apiRes) {
                            if (apiRes && apiRes.success) Swal.fire('Success', apiRes.message, 'success').then(() => window.location.reload());
                            else Swal.fire('Error', (apiRes && apiRes.message) || 'Password reset failed.', 'error');
                        });
                });
                return;
            }

            Swal.fire({
                title: action + ' user "' + username + '"?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Confirm'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/SystemAdmin/ToggleUserStatus', { id: userId, statusAction: action })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) window.location.reload();
                        else Swal.fire('Error', (apiRes && apiRes.message) || 'Action failed.', 'error');
                    });
            });
        },

        loadRoleMatrix: function () {
            const roleId = Number($('#matrixRoleSelect').val() || 0);
            const map = window.HRMS_ROLE_PERMISSION_MAP || {};
            const assigned = new Set(map[roleId] || []);
            $('.perm-checkbox').each(function () {
                $(this).prop('checked', assigned.has(Number($(this).val())));
            });
        },

        saveRoleMatrix: function () {
            const roleId = Number($('#matrixRoleSelect').val() || 0);
            if (!roleId) return;

            const permIds = [];
            $('.perm-checkbox:checked').each(function () {
                permIds.push(Number($(this).val()));
            });

            $('#globalLoadingOverlay').removeClass('d-none');
            $.ajax({
                url: '/SystemAdmin/SaveRolePermissions',
                type: 'POST',
                contentType: 'application/json',
                headers: { 'X-CSRF-TOKEN': csrfToken() },
                data: JSON.stringify({ roleId: roleId, permissionIds: permIds })
            }).done(function (res) {
                if (res && res.success) {
                    Swal.fire('Saved', res.message, 'success').then(() => window.location.reload());
                } else {
                    Swal.fire('Error', (res && res.message) || 'Unable to save permissions.', 'error');
                }
            }).always(function () {
                $('#globalLoadingOverlay').addClass('d-none');
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-admin-datatable').length) {
            $('.hrms-admin-datatable').DataTable({
                pageLength: 20,
                responsive: true,
                order: [],
                language: { search: '', searchPlaceholder: 'Search records...' }
            });
        }

        if ($('#matrixRoleSelect').length) {
            HrmsAdmin.loadRoleMatrix();
        }
    });
})(jQuery);
