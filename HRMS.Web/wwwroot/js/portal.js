(function ($) {
    'use strict';

    window.HrmsPortal = {
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
                        Swal.fire('Success', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to process request.');
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

        submitMultipartForm: function (formSelector, url, modalSelector) {
            const formEl = document.querySelector(formSelector);
            if (!formEl) return;

            const fd = new FormData(formEl);
            $('#globalLoadingOverlay').removeClass('d-none');
            $.ajax({
                url: url,
                type: 'POST',
                data: fd,
                processData: false,
                contentType: false
            })
                .done(function (res) {
                    if (res && res.success) {
                        if (modalSelector) {
                            bootstrap.Modal.getInstance(document.querySelector(modalSelector))?.hide();
                        }
                        Swal.fire('Success', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to submit request.');
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

        quickPunch: function (actionUrl, employeeId) {
            $('#globalLoadingOverlay').removeClass('d-none');
            $.post(actionUrl, { EmployeeId: employeeId })
                .done(function (res) {
                    if (res && res.success) {
                        Swal.fire('Attendance Recorded', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Notice', (res && res.message) || 'Unable to record attendance.', 'warning');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'Failed to contact attendance service.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        cancelLeave: function (id) {
            Swal.fire({
                title: 'Cancel Leave Application?',
                text: 'Pending leave days will be restored to your leave balance.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Yes, Cancel'
            }).then(function (r) {
                if (!r.isConfirmed) return;
                $.post('/SelfService/CancelLeave/' + id).done(function (res) {
                    if (res && res.success) {
                        Swal.fire('Cancelled', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (res && res.message) || 'Unable to cancel leave.', 'error');
                    }
                });
            });
        },

        cancelProfileRequest: function (id) {
            Swal.fire({
                title: 'Withdraw Profile Change Request?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Withdraw'
            }).then(function (r) {
                if (!r.isConfirmed) return;
                $.post('/SelfService/CancelProfileRequest/' + id).done(function (res) {
                    if (res && res.success) {
                        Swal.fire('Withdrawn', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (res && res.message) || 'Unable to withdraw request.', 'error');
                    }
                });
            });
        },

        toggleProfileFields: function () {
            const reqType = $('#essReqType').val();
            $('.ess-prof-group').addClass('d-none');
            if (reqType === 'ContactInfo') $('#essProfContact').removeClass('d-none');
            else if (reqType === 'Address') $('#essProfAddress').removeClass('d-none');
            else if (reqType === 'BankDetails') $('#essProfBank').removeClass('d-none');
            else if (reqType === 'EmergencyContact') $('#essProfEmergency').removeClass('d-none');
        },

        openGoalProgressModal: function (data) {
            $('#essGpGoalId').val(data.goalId);
            $('#essGpTitle').text(data.goalTitle);
            $('#essGpAchieved').val(data.achievedValue || '');
            $('#essGpProgress').val(data.progressPercent ?? 0);
            $('#essGpStatus').val(data.goalStatus || 'InProgress');
            new bootstrap.Modal(document.getElementById('essGoalProgressModal')).show();
        },

        openSelfAppraisalModal: function (data) {
            $('#essSelfAppId').val(data.appraisalId);
            $('#essSelfCycleLabel').text(data.cycleName + ' (' + data.cycleCode + ')');
            $('#essSelfRating').val(data.selfRating ?? 4.0);
            $('#essSelfComments').val(data.selfComments || '');
            new bootstrap.Modal(document.getElementById('essSelfAppraisalModal')).show();
        },

        openManagerDecisionModal: function (item, defaultDecision) {
            $('#mgrDecCategory').val(item.category);
            $('#mgrDecRequestId').val(item.requestId);
            $('#mgrDecDecision').val(defaultDecision || 'Approved');
            $('#mgrDecTitle').text((defaultDecision === 'Rejected' ? 'Reject ' : 'Approve ') + item.category + ' Request');
            $('#mgrDecSubtitle').text(item.employeeName + ' (' + item.employeeCode + ') — ' + item.title + ' • ' + item.summary);
            $('#mgrDecComments').val('');

            const $numWrap = $('#mgrDecNumericWrap');
            const $numInput = $('#mgrDecNumeric');
            const $numLabel = $('#mgrDecNumericLabel');
            if (item.category === 'Overtime') {
                $numWrap.removeClass('d-none');
                $numLabel.text('Approved Overtime Hours');
                $numInput.val(item.numericValue ?? 1.0);
            } else if (item.category === 'Expense') {
                $numWrap.removeClass('d-none');
                $numLabel.text('Approved Reimbursement Amount');
                $numInput.val(item.numericValue ?? '');
            } else {
                $numWrap.addClass('d-none');
                $numInput.val('');
            }

            new bootstrap.Modal(document.getElementById('mgrDecisionModal')).show();
        },

        openTeamAppraisalReviewModal: function (pa) {
            $('#mgrRevAppId').val(pa.appraisalId);
            $('#mgrRevLabel').text(pa.employeeName + ' (' + pa.cycleCode + ')');
            $('#mgrRevRating').val(pa.managerRating ?? pa.selfRating ?? 4.0);
            $('#mgrRevFinal').val(pa.finalRating ?? pa.managerRating ?? pa.selfRating ?? 4.0);
            $('#mgrRevStrengths').val(pa.strengths || '');
            $('#mgrRevImprovements').val(pa.improvementAreas || '');
            $('#mgrRevComments').val(pa.managerComments || '');
            $('#mgrRevIncrement').val(pa.recommendedIncrementPercent ?? '');
            $('#mgrRevFinalize').prop('checked', true);
            new bootstrap.Modal(document.getElementById('mgrTeamAppraisalModal')).show();
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-portal-datatable').length) {
            $('.hrms-portal-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($('#essReqType').length) {
            window.HrmsPortal.toggleProfileFields();
            $('#essReqType').on('change', window.HrmsPortal.toggleProfileFields);
        }

        if ($('#essLeaveHalfDay').length) {
            $('#essLeaveHalfDay').on('change', function () {
                $('#essLeaveHalfSessionWrap').toggleClass('d-none', !$(this).is(':checked'));
            });
        }
    });
})(jQuery);
