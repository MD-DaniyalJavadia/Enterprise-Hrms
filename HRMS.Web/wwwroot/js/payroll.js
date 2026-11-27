(function ($) {
    'use strict';

    function escapeHtml(text) {
        return $('<div>').text(text == null ? '' : String(text)).html();
    }

    window.HrmsPayroll = {
        submitAjaxForm: function (formSelector, url, modalSelector, redirectToDetails) {
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
                            if (redirectToDetails && res.data) {
                                window.location.href = '/Payroll/Details/' + res.data;
                            } else {
                                window.location.reload();
                            }
                        });
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

        updateRunStatus: function (runId, status, periodLabel) {
            Swal.fire({
                title: 'Mark Payroll as ' + status + '?',
                text: 'Period: ' + periodLabel,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Confirm ' + status
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Payroll/UpdateStatus', { payrollRunId: runId, status: status })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                        } else {
                            Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update payroll run.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        },

        deleteRun: function (runId, periodLabel) {
            Swal.fire({
                title: 'Delete Draft Payroll?',
                text: 'Remove draft payroll batch for ' + periodLabel + '?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete Draft'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Payroll/DeleteRun/' + runId).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Deleted', apiRes.message, 'success').then(function () { window.location.href = '/Payroll/Index'; });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete payroll run.', 'error');
                    }
                });
            });
        },

        openAdjustPayslipModal: function (data) {
            $('#adjPayslipId').val(data.payslipId);
            $('#adjPayslipEmpLabel').text(data.employeeCode + ' - ' + data.fullName);
            $('#adjBonus').val(data.bonusAmount);
            $('#adjOtherDed').val(data.otherDeductions);
            $('#adjPayStatus').val(data.paymentStatus || 'Pending');
            $('#adjNotes').val(data.notes || '');
            new bootstrap.Modal(document.getElementById('adjustPayslipModal')).show();
        },

        openComponentModal: function (data) {
            const c = data || {
                componentId: 0, componentCode: '', componentName: '', componentType: 'Earning',
                calculationType: 'Fixed', defaultValue: 0, isTaxable: true, isStatutory: false,
                displayOrder: 10, isActive: true
            };
            $('#compId').val(c.componentId || 0);
            $('#compCode').val(c.componentCode || '');
            $('#compName').val(c.componentName || '');
            $('#compType').val(c.componentType || 'Earning');
            $('#compCalcType').val(c.calculationType || 'Fixed');
            $('#compDefaultVal').val(c.defaultValue ?? 0);
            $('#compOrder').val(c.displayOrder ?? 10);
            $('#compTaxable').prop('checked', c.isTaxable !== false);
            $('#compStatutory').prop('checked', !!c.isStatutory);
            $('#compActive').prop('checked', c.isActive !== false);
            $('#compModalTitle').text(c.componentId ? 'Edit Salary Component' : 'Add Salary Component');
            new bootstrap.Modal(document.getElementById('componentModal')).show();
        },

        deleteComponent: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Payroll/DeleteComponent/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete component.', 'error');
                    }
                });
            });
        },

        openTaxSlabModal: function (data) {
            const s = data || {
                taxSlabId: 0, taxYear: new Date().getFullYear(), slabName: '',
                minAnnualIncome: 0, maxAnnualIncome: '', taxRatePercent: 0, fixedTaxAmount: 0, isActive: true
            };
            $('#slabId').val(s.taxSlabId || 0);
            $('#slabYear').val(s.taxYear || new Date().getFullYear());
            $('#slabName').val(s.slabName || '');
            $('#slabMin').val(s.minAnnualIncome ?? 0);
            $('#slabMax').val(s.maxAnnualIncome ?? '');
            $('#slabRate').val(s.taxRatePercent ?? 0);
            $('#slabFixed').val(s.fixedTaxAmount ?? 0);
            $('#slabActive').prop('checked', s.isActive !== false);
            $('#slabModalTitle').text(s.taxSlabId ? 'Edit Tax Bracket' : 'Add Tax Bracket');
            new bootstrap.Modal(document.getElementById('taxSlabModal')).show();
        },

        deleteTaxSlab: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Payroll/DeleteTaxSlab/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete tax slab.', 'error');
                    }
                });
            });
        },

        openStructureModal: function (employeeId) {
            $('#globalLoadingOverlay').removeClass('d-none');
            $.get('/Payroll/GetEmployeeStructure', { employeeId: employeeId })
                .done(function (res) {
                    if (!res || !res.success || !res.data) {
                        Swal.fire('Error', (res && res.message) || 'Unable to load salary structure.', 'error');
                        return;
                    }
                    const s = res.data;
                    $('#stEmployeeId').val(s.employeeId);
                    $('#stEmployeeLabel').text(s.employeeCode + ' - ' + s.fullName);
                    $('#stEffectiveFrom').val((s.effectiveFrom || '').substring(0, 10));
                    $('#stBasicMonthly').val(s.basicSalaryMonthly);
                    $('#stOtRate').val(s.hourlyOvertimeRate);
                    $('#stTaxRegime').val(s.taxRegime || 'Standard');
                    $('#stPayMethod').val(s.paymentMethod || 'BankTransfer');
                    $('#stRemarks').val(s.remarks || '');

                    const $tbody = $('#stItemsBody').empty();
                    (s.items || []).forEach(function (item) {
                        const row = $('<tr class="st-item-row"></tr>').data('componentId', item.componentId);
                        row.append('<td><input type="checkbox" class="form-check-input st-include" ' + (item.isIncluded ? 'checked' : '') + ' /></td>');
                        row.append('<td><span class="badge bg-light text-dark border me-1">' + escapeHtml(item.componentCode) + '</span>' + escapeHtml(item.componentName) + '</td>');
                        row.append('<td class="small">' + escapeHtml(item.componentType) + '</td>');
                        row.append(
                            '<td><select class="form-select form-select-sm st-calc-type">' +
                            '<option value="Fixed"' + (item.calculationType === 'Fixed' ? ' selected' : '') + '>Fixed</option>' +
                            '<option value="PercentOfBasic"' + (item.calculationType === 'PercentOfBasic' ? ' selected' : '') + '>% of Basic</option>' +
                            '</select></td>'
                        );
                        row.append('<td><input type="number" step="0.01" min="0" class="form-control form-control-sm st-amount" value="' + Number(item.amountOrPercent || 0) + '" /></td>');
                        row.append('<td class="text-end fw-semibold st-computed">0.00</td>');
                        $tbody.append(row);
                    });

                    HrmsPayroll.recalculateStructurePreview();
                    new bootstrap.Modal(document.getElementById('structureModal')).show();
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        recalculateStructurePreview: function () {
            const basic = parseFloat($('#stBasicMonthly').val()) || 0;
            let gross = basic;
            let ded = 0;

            $('#stItemsBody tr.st-item-row').each(function () {
                const $row = $(this);
                const included = $row.find('.st-include').is(':checked');
                const calcType = $row.find('.st-calc-type').val();
                const val = parseFloat($row.find('.st-amount').val()) || 0;
                const typeText = $row.find('td').eq(2).text().trim();

                const amt = calcType === 'PercentOfBasic' ? Math.round(basic * val) / 100 : val;
                $row.find('.st-computed').text(amt.toFixed(2));

                if (included) {
                    if (typeText === 'Earning') gross += amt;
                    else if (typeText === 'Deduction') ded += amt;
                }
            });

            $('#stPreviewGross').text(gross.toFixed(2));
            $('#stPreviewDed').text(ded.toFixed(2));
            $('#stPreviewNetEst').text(Math.max(0, gross - ded).toFixed(2));
        },

        saveStructure: function () {
            const items = [];
            $('#stItemsBody tr.st-item-row').each(function () {
                const $row = $(this);
                items.push({
                    componentId: $row.data('componentId'),
                    calculationType: $row.find('.st-calc-type').val(),
                    amountOrPercent: parseFloat($row.find('.st-amount').val()) || 0,
                    isIncluded: $row.find('.st-include').is(':checked')
                });
            });

            const payload = {
                employeeId: parseInt($('#stEmployeeId').val(), 10),
                effectiveFrom: $('#stEffectiveFrom').val(),
                basicSalaryMonthly: parseFloat($('#stBasicMonthly').val()) || 0,
                hourlyOvertimeRate: parseFloat($('#stOtRate').val()) || 0,
                taxRegime: $('#stTaxRegime').val(),
                paymentMethod: $('#stPayMethod').val(),
                remarks: $('#stRemarks').val(),
                items: items
            };

            $('#globalLoadingOverlay').removeClass('d-none');
            $.ajax({
                url: '/Payroll/SaveStructure',
                type: 'POST',
                contentType: 'application/json; charset=utf-8',
                data: JSON.stringify(payload)
            })
                .done(function (res) {
                    if (res && res.success) {
                        bootstrap.Modal.getInstance(document.getElementById('structureModal'))?.hide();
                        Swal.fire('Saved', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to save salary structure.');
                        Swal.fire('Validation Error', msg, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'Unable to save salary structure.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-payroll-datatable').length) {
            $('.hrms-payroll-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        $(document).on('input change', '#stBasicMonthly, .st-include, .st-calc-type, .st-amount', function () {
            HrmsPayroll.recalculateStructurePreview();
        });
    });
})(jQuery);
