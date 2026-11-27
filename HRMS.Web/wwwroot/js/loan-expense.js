(function ($) {
    'use strict';

    window.HrmsLoanExp = {
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
                        Swal.fire('Submitted', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to submit claim.');
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

        updateLoanEmiPreview: function () {
            const $sel = $('#loanTypeSelect option:selected');
            const maxAmt = parseFloat($sel.data('max')) || 0;
            const rate = parseFloat($sel.data('rate')) || 0;
            const maxTenure = parseInt($sel.data('tenure'), 10) || 24;
            const principal = parseFloat($('#loanPrincipal').val()) || 0;
            const tenure = parseInt($('#loanTenure').val(), 10) || 1;

            if (maxAmt > 0) {
                $('#loanPolicyHint').text('Policy max: ' + maxAmt.toLocaleString() + ' • Rate: ' + rate.toFixed(2) + '% p.a. • Max tenure: ' + maxTenure + ' mos');
            } else {
                $('#loanPolicyHint').text('');
            }

            if (principal > 0 && tenure >= 1) {
                const totalInterest = Math.round((principal * (rate / 100.0) * (tenure / 12.0)) * 100) / 100;
                const totalPayable = principal + totalInterest;
                const emi = Math.round((totalPayable / tenure) * 100) / 100;
                $('#loanEmiPreview').text(emi.toFixed(2) + ' / mo (Total: ' + totalPayable.toFixed(2) + ')');
            } else {
                $('#loanEmiPreview').text('-');
            }
        },

        openReviewLoanModal: function (loanId, loanLabel, defaultStart) {
            $('#revLoanId').val(loanId);
            $('#revLoanLabel').text(loanLabel);
            $('#revLoanStatus').val('Active');
            $('#revLoanStart').val(defaultStart ? String(defaultStart).substring(0, 10) : '');
            $('#revLoanComments').val('');
            $('#revLoanScheduleFields').removeClass('d-none');
            new bootstrap.Modal(document.getElementById('reviewLoanModal')).show();
        },

        openRepaymentModal: function (scheduleId, instNumber, amount) {
            $('#repScheduleId').val(scheduleId);
            $('#repInstLabel').text('Installment #' + instNumber + ' — Amount: ' + Number(amount).toFixed(2));
            $('#repRef').val('');
            new bootstrap.Modal(document.getElementById('repaymentModal')).show();
        },

        openLoanTypeModal: function (data) {
            const t = data || {
                loanTypeId: 0, typeCode: '', typeName: '', category: 'Loan',
                maxAmount: 25000, defaultInterestRate: 0, maxTenureMonths: 24,
                requiresGuarantor: false, description: '', isActive: true
            };
            $('#ltId').val(t.loanTypeId || 0);
            $('#ltCode').val(t.typeCode || '');
            $('#ltName').val(t.typeName || '');
            $('#ltCategory').val(t.category || 'Loan');
            $('#ltMaxAmt').val(t.maxAmount ?? 25000);
            $('#ltRate').val(t.defaultInterestRate ?? 0);
            $('#ltTenure').val(t.maxTenureMonths ?? 24);
            $('#ltGuarantor').prop('checked', !!t.requiresGuarantor);
            $('#ltActive').prop('checked', t.isActive !== false);
            $('#ltDesc').val(t.description || '');
            $('#ltModalTitle').text(t.loanTypeId ? 'Edit Loan Policy' : 'Add Loan / Advance Policy');
            new bootstrap.Modal(document.getElementById('loanTypeModal')).show();
        },

        deleteLoanType: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Loan/DeleteLoanType/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete loan policy.', 'error');
                    }
                });
            });
        },

        openExpenseReviewModal: function (claimId, claimLabel, currentStatus, claimAmount) {
            $('#expRevClaimId').val(claimId);
            $('#expRevLabel').text(claimLabel);
            if (currentStatus === 'Approved') {
                $('#expRevStatus').val('Reimbursed');
                $('#expRevApprovedAmt').val(Number(claimAmount).toFixed(2));
                $('#expRevReimburseFields').removeClass('d-none');
                $('#expRevApproveAmtWrap').addClass('d-none');
            } else {
                $('#expRevStatus').val('Approved');
                $('#expRevApprovedAmt').val(Number(claimAmount).toFixed(2));
                $('#expRevReimburseFields').addClass('d-none');
                $('#expRevApproveAmtWrap').removeClass('d-none');
            }
            $('#expRevComments').val('');
            $('#expRevPayRef').val('');
            new bootstrap.Modal(document.getElementById('reviewExpenseModal')).show();
        },

        openCategoryModal: function (data) {
            const c = data || {
                categoryId: 0, categoryCode: '', categoryName: '',
                maxLimitPerClaim: '', requiresReceipt: true, description: '', isActive: true
            };
            $('#ecId').val(c.categoryId || 0);
            $('#ecCode').val(c.categoryCode || '');
            $('#ecName').val(c.categoryName || '');
            $('#ecLimit').val(c.maxLimitPerClaim ?? '');
            $('#ecReceipt').prop('checked', c.requiresReceipt !== false);
            $('#ecActive').prop('checked', c.isActive !== false);
            $('#ecDesc').val(c.description || '');
            $('#ecModalTitle').text(c.categoryId ? 'Edit Expense Category' : 'Add Expense Category');
            new bootstrap.Modal(document.getElementById('expenseCategoryModal')).show();
        },

        deleteCategory: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Expense/DeleteCategory/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete category.', 'error');
                    }
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-le-datatable').length) {
            $('.hrms-le-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }

        $('#loanTypeSelect, #loanPrincipal, #loanTenure').on('change input', function () {
            window.HrmsLoanExp.updateLoanEmiPreview();
        });

        $('#revLoanStatus').on('change', function () {
            if ($(this).val() === 'Active') {
                $('#revLoanScheduleFields').removeClass('d-none');
            } else {
                $('#revLoanScheduleFields').addClass('d-none');
            }
        });

        $('#expRevStatus').on('change', function () {
            const st = $(this).val();
            $('#expRevApproveAmtWrap').toggleClass('d-none', st !== 'Approved');
            $('#expRevReimburseFields').toggleClass('d-none', st !== 'Reimbursed');
        });
    });
})(jQuery);
