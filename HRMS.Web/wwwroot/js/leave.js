(function ($) {
    'use strict';

    window.HrmsLeave = {
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
                            : ((res && res.message) || 'Unable to process request.');
                        Swal.fire('Validation Error', msg, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'An unexpected error occurred.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        submitMultipartForm: function (formSelector, url, modalSelector) {
            const formEl = document.querySelector(formSelector);
            const formData = new FormData(formEl);

            $('#globalLoadingOverlay').removeClass('d-none');
            $.ajax({
                url: url,
                type: 'POST',
                data: formData,
                processData: false,
                contentType: false
            })
                .done(function (res) {
                    if (res && res.success) {
                        if (modalSelector) {
                            bootstrap.Modal.getInstance(document.querySelector(modalSelector))?.hide();
                        }
                        Swal.fire('Submitted', res.message, 'success').then(function () {
                            window.location.reload();
                        });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to submit leave application.');
                        Swal.fire('Validation Error', msg, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'Unable to submit leave request.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        reviewApplication: function (id, decision, empName) {
            Swal.fire({
                title: decision + ' Leave Application?',
                text: 'Employee: ' + empName,
                input: 'text',
                inputLabel: 'Reviewer Comments (optional)',
                icon: decision === 'Approved' ? 'question' : 'warning',
                showCancelButton: true,
                confirmButtonText: decision
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Leave/Review', {
                    LeaveApplicationId: id,
                    ApprovalStatus: decision,
                    ReviewerComments: res.value || ''
                }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to review application.', 'error');
                    }
                });
            });
        },

        cancelApplication: function (id, empName) {
            Swal.fire({
                title: 'Cancel Leave Request?',
                text: 'Balance and synced attendance will be restored for ' + empName + '.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Cancel Leave'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Leave/Cancel/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Cancelled', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to cancel leave.', 'error');
                    }
                });
            });
        },

        openLeaveTypeModal: function (data) {
            const t = data || {
                leaveTypeId: 0, leaveCode: '', leaveName: '', annualQuota: 10, isPaid: true,
                allowCarryForward: false, maxCarryForwardDays: 0, allowEncashment: false,
                requiresDocument: false, maxConsecutiveDays: '', isHalfDayAllowed: true,
                genderRestriction: '', colorHex: '#2563eb', description: '', isActive: true
            };
            $('#ltId').val(t.leaveTypeId || 0);
            $('#ltCode').val(t.leaveCode || '');
            $('#ltName').val(t.leaveName || '');
            $('#ltQuota').val(t.annualQuota ?? 10);
            $('#ltIsPaid').prop('checked', t.isPaid !== false);
            $('#ltCarryForward').prop('checked', !!t.allowCarryForward);
            $('#ltMaxCarry').val(t.maxCarryForwardDays ?? 0);
            $('#ltEncashment').prop('checked', !!t.allowEncashment);
            $('#ltReqDoc').prop('checked', !!t.requiresDocument);
            $('#ltMaxConsec').val(t.maxConsecutiveDays || '');
            $('#ltHalfDay').prop('checked', t.isHalfDayAllowed !== false);
            $('#ltGender').val(t.genderRestriction || '');
            $('#ltColor').val(t.colorHex || '#2563eb');
            $('#ltDescription').val(t.description || '');
            $('#ltIsActive').prop('checked', t.isActive !== false);
            $('#ltModalTitle').text(t.leaveTypeId ? 'Edit Leave Policy' : 'Add Leave Policy');
            new bootstrap.Modal(document.getElementById('leaveTypeModal')).show();
        },

        deleteLeaveType: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Leave/DeleteLeaveType/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete leave type.', 'error');
                    }
                });
            });
        },

        openAdjustBalanceModal: function (b) {
            $('#adjBalanceId').val(b.leaveBalanceId);
            $('#adjEmployeeLabel').text(b.employeeCode + ' - ' + b.fullName + ' (' + b.leaveName + ')');
            $('#adjOpening').val(b.openingBalance);
            $('#adjAccrued').val(b.accruedDays);
            $('#adjCarry').val(b.carryForwardDays);
            new bootstrap.Modal(document.getElementById('adjustBalanceModal')).show();
        },

        reviewEncashment: function (id, decision, empName) {
            Swal.fire({
                title: decision + ' Leave Encashment?',
                text: 'Employee: ' + empName,
                input: 'text',
                inputLabel: 'Reviewer Comments (optional)',
                icon: decision === 'Approved' ? 'question' : 'warning',
                showCancelButton: true,
                confirmButtonText: decision
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Leave/ReviewEncashment', {
                    EncashmentId: id,
                    ApprovalStatus: decision,
                    ReviewerComments: res.value || ''
                }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update encashment.', 'error');
                    }
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-leave-datatable').length) {
            $('.hrms-leave-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }

        $('#chkHalfDay').on('change', function () {
            const isHalf = $(this).is(':checked');
            $('#halfDaySessionWrap').toggleClass('d-none', !isHalf);
            if (isHalf) {
                $('#leaveEndDate').val($('#leaveStartDate').val());
            }
        });
    });
})(jQuery);
