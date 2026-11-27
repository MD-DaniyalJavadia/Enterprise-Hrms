(function ($) {
    'use strict';

    window.HrmsExit = {
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

        openSubmitModal: function () {
            const today = new Date().toISOString().substring(0, 10);
            const lwdDate = new Date();
            lwdDate.setDate(lwdDate.getDate() + 30);

            $('#sepEmployeeId').val('').trigger('change');
            $('#sepType').val('Resignation');
            $('#sepReason').val('BetterOpportunity');
            $('#sepSubDate').val(today);
            $('#sepNoticeDays').val(30);
            $('#sepLwd').val(lwdDate.toISOString().substring(0, 10));
            $('#sepWaived').prop('checked', false);
            $('#sepInitApproved').prop('checked', false);
            $('#sepEmpComments').val('');
            $('#sepHrComments').val('');
            new bootstrap.Modal(document.getElementById('submitSeparationModal')).show();
        },

        syncLwdFromNotice: function () {
            const subVal = $('#sepSubDate').val();
            const days = parseInt($('#sepNoticeDays').val() || '0', 10);
            if (!subVal || isNaN(days)) return;
            const d = new Date(subVal + 'T00:00:00');
            d.setDate(d.getDate() + Math.max(0, days));
            $('#sepLwd').val(d.toISOString().substring(0, 10));
        },

        openReviewModal: function (separationId, action, label, defaultLwd, isNoticeWaived, isRehireEligible) {
            $('#revSepId').val(separationId);
            $('#revAction').val(action);
            $('#revLabel').text(label || ('Separation #' + separationId));
            $('#revLwd').val(defaultLwd ? String(defaultLwd).substring(0, 10) : '');
            $('#revWaived').prop('checked', !!isNoticeWaived);
            $('#revRehire').prop('checked', isRehireEligible !== false);
            $('#revComments').val('');

            const isHrApprove = action === 'HrApprove';
            const isManagerApprove = action === 'ManagerApprove';
            const showDate = isHrApprove || isManagerApprove;
            $('#revLwdGroup').toggleClass('d-none', !showDate);
            $('#revHrOptionsGroup').toggleClass('d-none', !isHrApprove);

            const titleMap = {
                ManagerApprove: 'Manager Approval',
                HrApprove: 'HR Approval & Initiate Clearance',
                Reject: 'Reject Separation Request',
                Withdraw: 'Withdraw Separation Request'
            };
            $('#revModalTitle').text(titleMap[action] || 'Review Separation');

            const $btn = $('#revSubmitBtn');
            $btn.removeClass('btn-primary btn-success btn-danger btn-warning');
            if (action === 'Reject') $btn.addClass('btn-danger').text('Confirm Rejection');
            else if (action === 'Withdraw') $btn.addClass('btn-warning').text('Confirm Withdrawal');
            else if (action === 'HrApprove') $btn.addClass('btn-success').text('Approve & Start Clearance');
            else $btn.addClass('btn-primary').text('Approve Request');

            new bootstrap.Modal(document.getElementById('reviewSeparationModal')).show();
        },

        withdrawSeparation: function (separationId, sepNumber) {
            Swal.fire({
                title: 'Withdraw "' + sepNumber + '"?',
                text: 'You can withdraw your resignation before clearance processing begins.',
                input: 'text',
                inputPlaceholder: 'Optional reason for withdrawal...',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#d97706',
                confirmButtonText: 'Withdraw Request'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Exit/Review', {
                    SeparationId: separationId,
                    Action: 'Withdraw',
                    Comments: res.value || ''
                })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Withdrawn', apiRes.message, 'success').then(function () { window.location.reload(); });
                        } else {
                            Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to withdraw request.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        },

        finalizeExit: function (separationId, empName) {
            Swal.fire({
                title: 'Finalize Exit for ' + empName + '?',
                text: 'This marks the employee as separated, records employment history, and deactivates portal login access. Mandatory clearances, asset returns, and settlement approval are required.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Finalize Exit'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Exit/FinalizeExit/' + separationId)
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Exit Finalized', apiRes.message, 'success').then(function () { window.location.reload(); });
                        } else {
                            Swal.fire('Cannot Finalize', (apiRes && apiRes.message) || 'Prerequisites not met.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        },

        openClearanceModal: function (item) {
            $('#clrItemId').val(item.clearanceItemId || 0);
            $('#clrItemLabel').text((item.departmentScope || '') + ' — ' + (item.itemName || ''));
            $('#clrStatus').val(item.clearanceStatus || 'Cleared');
            $('#clrRecovery').val(item.recoveryAmount ?? 0);
            $('#clrRemarks').val(item.remarks || '');
            new bootstrap.Modal(document.getElementById('clearanceItemModal')).show();
        },

        openSettlementModal: function (separationId, settlement, readiness) {
            const s = settlement || {};
            const r = readiness || {};
            $('#setSepId').val(separationId);
            $('#setWorkedDays').val(s.workedDaysInFinalMonth ?? '');
            $('#setLeaveDays').val(s.encashableLeaveDays ?? (r.encashableLeaveDays ?? ''));
            $('#setGratuity').val(s.gratuityOrSeverance ?? 0);
            $('#setBonus').val(s.bonusOrOtherEarnings ?? 0);
            $('#setLoanRec').val(s.outstandingLoanRecovery ?? (r.activeLoanBalance ?? ''));
            $('#setAssetRec').val(s.assetOrNoticeRecovery ?? '');
            $('#setTax').val(s.taxAndOtherDeductions ?? 0);
            $('#setNotes').val(s.notes || '');
            new bootstrap.Modal(document.getElementById('settlementCalcModal')).show();
        },

        approveSettlement: function (settlementId, settlementNo) {
            Swal.fire({
                title: 'Approve Settlement ' + settlementNo + '?',
                text: 'Approving locks in the settlement figures for disbursement.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonColor: '#16a34a',
                confirmButtonText: 'Approve Settlement'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Exit/UpdateSettlementStatus', {
                    SettlementId: settlementId,
                    SettlementStatus: 'Approved'
                })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Approved', apiRes.message, 'success').then(function () { window.location.reload(); });
                        } else {
                            Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to approve settlement.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        },

        openSettlementPayModal: function (settlementId, settlementNo, netAmount) {
            $('#paySetId').val(settlementId);
            $('#paySetLabel').text(settlementNo + ' — Net Payable: ' + Number(netAmount || 0).toLocaleString(undefined, { minimumFractionDigits: 2, maximumFractionDigits: 2 }));
            $('#payRef').val('');
            $('#payDate').val(new Date().toISOString().substring(0, 10));
            new bootstrap.Modal(document.getElementById('settlementPayModal')).show();
        },

        openExitInterviewModal: function (separationId, data, defaultLabel) {
            const iv = data || {
                separationId: separationId || '',
                interviewDate: new Date().toISOString().substring(0, 10),
                overallSatisfactionRating: 4,
                managerSupportRating: 4,
                workLifeBalanceRating: 4,
                compensationRating: 3,
                wouldRecommendCompany: true,
                wouldConsiderRejoining: true,
                primaryLeavingReason: '',
                likedMost: '',
                improvementSuggestions: '',
                interviewerNotes: '',
                isConfidential: true
            };

            if ($('#ivSepSelect').length) {
                $('#ivSepSelect').val(iv.separationId || separationId || '').trigger('change');
            }
            if ($('#ivSepIdHidden').length) {
                $('#ivSepIdHidden').val(iv.separationId || separationId || '');
            }
            if ($('#ivLabel').length) {
                $('#ivLabel').text(defaultLabel || (iv.employeeName ? (iv.separationNumber + ' — ' + iv.employeeName) : ''));
            }

            $('#ivDate').val(iv.interviewDate ? String(iv.interviewDate).substring(0, 10) : new Date().toISOString().substring(0, 10));
            $('#ivOverall').val(iv.overallSatisfactionRating || 4);
            $('#ivManager').val(iv.managerSupportRating || 4);
            $('#ivWlb').val(iv.workLifeBalanceRating || 4);
            $('#ivComp').val(iv.compensationRating || 3);
            $('#ivRecommend').prop('checked', iv.wouldRecommendCompany !== false);
            $('#ivRejoin').prop('checked', iv.wouldConsiderRejoining !== false);
            $('#ivReason').val(iv.primaryLeavingReason || '');
            $('#ivLiked').val(iv.likedMost || '');
            $('#ivImprove').val(iv.improvementSuggestions || '');
            $('#ivNotes').val(iv.interviewerNotes || '');
            $('#ivConf').prop('checked', iv.isConfidential !== false);
            new bootstrap.Modal(document.getElementById('exitInterviewModal')).show();
        },

        openTemplateModal: function (data) {
            const t = data || {
                templateId: 0,
                departmentScope: 'IT',
                itemName: '',
                description: '',
                isMandatory: true,
                displayOrder: 1,
                isActive: true
            };
            $('#tplId').val(t.templateId || 0);
            $('#tplScope').val(t.departmentScope || 'IT');
            $('#tplName').val(t.itemName || '');
            $('#tplDesc').val(t.description || '');
            $('#tplMandatory').prop('checked', t.isMandatory !== false);
            $('#tplOrder').val(t.displayOrder || 1);
            $('#tplActive').prop('checked', t.isActive !== false);
            $('#tplModalTitle').text(t.templateId ? 'Edit Clearance Template' : 'Add Clearance Template');
            new bootstrap.Modal(document.getElementById('clearanceTemplateModal')).show();
        },

        deleteTemplate: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                text: 'Existing separation checklists will retain their copied items, while future separations will no longer include this item.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Exit/DeleteTemplate/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete template.', 'error');
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-exit-datatable').length) {
            $('.hrms-exit-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }

        $('#sepSubDate, #sepNoticeDays').on('change', HrmsExit.syncLwdFromNotice);
    });
})(jQuery);
