(function ($) {
    'use strict';

    window.HrmsAtt = {
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

        deleteRecord: function (url, label) {
            Swal.fire({
                title: 'Remove ' + label + '?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Remove'
            }).then(function (result) {
                if (!result.isConfirmed) return;
                $.post(url).done(function (res) {
                    if (res && res.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (res && res.message) || 'Unable to remove record.', 'error');
                    }
                });
            });
        },

        openManualModal: function (empId, dateStr, status, checkIn, checkOut, remarks) {
            $('#manEmployeeId').val(empId || '').trigger('change');
            $('#manAttendanceDate').val(dateStr);
            $('#manStatus').val(status || 'Present');
            $('#manCheckIn').val(checkIn || '');
            $('#manCheckOut').val(checkOut || '');
            $('#manRemarks').val(remarks || '');
            new bootstrap.Modal(document.getElementById('manualAttendanceModal')).show();
        },

        openAssignmentModal: function (data) {
            const m = data || { assignmentId: 0, employeeId: '', shiftId: '', effectiveFrom: '', effectiveTo: '', notes: '' };
            $('#saAssignmentId').val(m.assignmentId || 0);
            $('#saEmployeeId').val(m.employeeId || '').trigger('change');
            $('#saShiftId').val(m.shiftId || '');
            $('#saEffectiveFrom').val(m.effectiveFrom || new Date().toISOString().slice(0, 10));
            $('#saEffectiveTo').val(m.effectiveTo || '');
            $('#saNotes').val(m.notes || '');
            $('#saModalTitle').text(m.assignmentId ? 'Edit Shift Assignment' : 'Assign Employee Shift');
            new bootstrap.Modal(document.getElementById('shiftAssignmentModal')).show();
        },

        openHolidayModal: function (data) {
            const h = data || { holidayId: 0, branchId: '', holidayName: '', holidayDate: '', holidayType: 'Public', description: '', isActive: true };
            $('#holId').val(h.holidayId || 0);
            $('#holBranchId').val(h.branchId || '');
            $('#holName').val(h.holidayName || '');
            $('#holDate').val(h.holidayDate || '');
            $('#holType').val(h.holidayType || 'Public');
            $('#holDescription').val(h.description || '');
            $('#holIsActive').prop('checked', h.isActive !== false);
            $('#holModalTitle').text(h.holidayId ? 'Edit Holiday' : 'Add Holiday');
            new bootstrap.Modal(document.getElementById('holidayModal')).show();
        },

        reviewRegularization: function (id, decision, empName) {
            Swal.fire({
                title: decision + ' Regularization?',
                text: 'Employee: ' + empName,
                input: 'text',
                inputLabel: 'Reviewer Comments (optional)',
                icon: decision === 'Approved' ? 'question' : 'warning',
                showCancelButton: true,
                confirmButtonText: decision
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Attendance/ReviewRegularization', {
                    RegularizationId: id,
                    ApprovalStatus: decision,
                    ReviewerComments: res.value || ''
                }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update request.', 'error');
                    }
                });
            });
        },

        reviewOvertime: function (id, decision, requestedHours, empName) {
            Swal.fire({
                title: decision + ' Overtime Request?',
                text: empName + ' requested ' + requestedHours + 'h',
                input: decision === 'Approved' ? 'number' : 'text',
                inputValue: decision === 'Approved' ? requestedHours : '',
                inputLabel: decision === 'Approved' ? 'Approved Hours' : 'Rejection Reason',
                inputAttributes: decision === 'Approved' ? { step: '0.25', min: '0.25', max: '24' } : {},
                icon: decision === 'Approved' ? 'question' : 'warning',
                showCancelButton: true,
                confirmButtonText: decision
            }).then(function (res) {
                if (!res.isConfirmed) return;
                const payload = {
                    OvertimeRequestId: id,
                    ApprovalStatus: decision,
                    ApprovedHours: decision === 'Approved' ? parseFloat(res.value || requestedHours) : 0,
                    ReviewerComments: decision === 'Approved' ? 'Approved' : (res.value || '')
                };
                $.post('/Attendance/ReviewOvertime', payload).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update overtime request.', 'error');
                    }
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-att-datatable').length) {
            $('.hrms-att-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }
    });
})(jQuery);
