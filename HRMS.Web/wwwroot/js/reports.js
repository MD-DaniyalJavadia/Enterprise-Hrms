(function ($) {
    'use strict';

    let deptCostChart = null;
    let finTrendChart = null;
    let attritionChart = null;
    let payrollDeptChart = null;

    function textColor() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? '#cbd5e1' : '#475569';
    }

    function destroyHubCharts() {
        [deptCostChart, finTrendChart, attritionChart].forEach(function (c) {
            if (c) c.destroy();
        });
    }

    window.HrmsReports = {
        renderHubCharts: function (data) {
            if (!data || !document.getElementById('repDeptCostChart')) return;
            destroyHubCharts();

            const color = textColor();
            const depts = data.departmentStats || [];
            deptCostChart = new Chart(document.getElementById('repDeptCostChart'), {
                type: 'bar',
                data: {
                    labels: depts.map(x => x.departmentName),
                    datasets: [
                        {
                            label: 'Active Headcount',
                            data: depts.map(x => x.activeHeadcount),
                            backgroundColor: '#2563eb',
                            borderRadius: 5,
                            yAxisID: 'y'
                        },
                        {
                            label: 'Monthly Net Payroll',
                            data: depts.map(x => x.latestMonthlyNetPay),
                            backgroundColor: '#10b981',
                            borderRadius: 5,
                            yAxisID: 'y1'
                        }
                    ]
                },
                options: {
                    responsive: true,
                    plugins: { legend: { labels: { color: color } } },
                    scales: {
                        x: { ticks: { color: color } },
                        y: { type: 'linear', position: 'left', beginAtZero: true, ticks: { precision: 0, color: color } },
                        y1: { type: 'linear', position: 'right', beginAtZero: true, grid: { drawOnChartArea: false }, ticks: { color: color } }
                    }
                }
            });

            const trends = data.financialTrends || [];
            finTrendChart = new Chart(document.getElementById('repFinTrendChart'), {
                type: 'line',
                data: {
                    labels: trends.map(x => x.periodLabel),
                    datasets: [
                        {
                            label: 'Gross Payroll',
                            data: trends.map(x => x.totalGrossPay),
                            borderColor: '#2563eb',
                            backgroundColor: 'rgba(37, 99, 235, 0.12)',
                            fill: true,
                            tension: 0.35
                        },
                        {
                            label: 'Net Payroll',
                            data: trends.map(x => x.totalNetPay),
                            borderColor: '#16a34a',
                            backgroundColor: 'rgba(22, 163, 74, 0.08)',
                            fill: true,
                            tension: 0.35
                        },
                        {
                            label: 'Approved Expenses',
                            data: trends.map(x => x.approvedExpenseAmount),
                            borderColor: '#f59e0b',
                            backgroundColor: 'rgba(245, 158, 11, 0.1)',
                            fill: false,
                            tension: 0.35
                        }
                    ]
                },
                options: {
                    responsive: true,
                    plugins: { legend: { labels: { color: color } } },
                    scales: {
                        x: { ticks: { color: color } },
                        y: { beginAtZero: true, ticks: { color: color } }
                    }
                }
            });

            const attr = data.attritionBreakdown || [];
            attritionChart = new Chart(document.getElementById('repAttritionChart'), {
                type: 'doughnut',
                data: {
                    labels: attr.length ? attr.map(x => x.reasonCategory) : ['No Separations'],
                    datasets: [{
                        data: attr.length ? attr.map(x => x.caseCount) : [1],
                        backgroundColor: attr.length ? ['#ef4444', '#f59e0b', '#3b82f6', '#8b5cf6', '#14b8a6', '#ec4899'] : ['#94a3b8'],
                        borderWidth: 0
                    }]
                },
                options: {
                    responsive: true,
                    cutout: '64%',
                    plugins: { legend: { position: 'bottom', labels: { color: color } } }
                }
            });
        },

        renderPayrollDeptChart: function (rows) {
            const canvas = document.getElementById('repPayrollDeptChart');
            if (!canvas) return;
            if (payrollDeptChart) payrollDeptChart.destroy();

            const grouped = {};
            (rows || []).forEach(function (r) {
                const key = r.departmentName || 'Other';
                if (!grouped[key]) grouped[key] = { net: 0, tax: 0, contrib: 0 };
                grouped[key].net += Number(r.netPay || 0);
                grouped[key].tax += Number(r.incomeTax || 0);
                grouped[key].contrib += Number(r.employerContributions || 0);
            });

            const labels = Object.keys(grouped);
            const color = textColor();
            payrollDeptChart = new Chart(canvas, {
                type: 'bar',
                data: {
                    labels: labels,
                    datasets: [
                        { label: 'Net Pay', data: labels.map(k => grouped[k].net), backgroundColor: '#2563eb', borderRadius: 4 },
                        { label: 'Income Tax', data: labels.map(k => grouped[k].tax), backgroundColor: '#f59e0b', borderRadius: 4 },
                        { label: 'Employer Contrib.', data: labels.map(k => grouped[k].contrib), backgroundColor: '#10b981', borderRadius: 4 }
                    ]
                },
                options: {
                    responsive: true,
                    plugins: { legend: { labels: { color: color } } },
                    scales: {
                        x: { stacked: true, ticks: { color: color } },
                        y: { stacked: true, beginAtZero: true, ticks: { color: color } }
                    }
                }
            });
        },

        refreshHub: function () {
            const params = {
                branchId: $('#hubBranchId').val() || '',
                departmentId: $('#hubDepartmentId').val() || '',
                fromDate: $('#hubFromDate').val() || '',
                toDate: $('#hubToDate').val() || ''
            };

            $('#globalLoadingOverlay').removeClass('d-none');
            $.get('/Reports/GetAnalyticsData', params)
                .done(function (res) {
                    if (res && res.success && res.data) {
                        const k = res.data.kpi || {};
                        $('#kpiHeadcount').text(k.activeHeadcount ?? 0);
                        $('#kpiProbation').text((k.probationCount ?? 0) + ' on probation');
                        $('#kpiTurnover').text((k.turnoverRatePercent ?? 0) + '%');
                        $('#kpiSeparations').text((k.activeSeparationCases ?? 0) + ' active exit cases');
                        $('#kpiPayroll').text('$' + Number(k.monthlyPayrollCost || 0).toLocaleString(undefined, { maximumFractionDigits: 0 }));
                        $('#kpiLoans').text('Loans: $' + Number(k.outstandingLoanPortfolio || 0).toLocaleString(undefined, { maximumFractionDigits: 0 }));
                        $('#kpiAttendance').text((k.attendanceCompliancePercent ?? 0) + '%');
                        $('#kpiLeaveDays').text((k.approvedLeaveDaysInPeriod ?? 0) + ' approved leave days');
                        $('#kpiOpenReqs').text(k.openRequisitionsCount ?? 0);
                        $('#kpiAppraisals').text('Appraisals: ' + (k.appraisalCompletionPercent ?? 0) + '% done');
                        $('#kpiAssetValue').text('$' + Number(k.activeAssetPortfolioValue || 0).toLocaleString(undefined, { maximumFractionDigits: 0 }));

                        window.HRMS_REPORTS_HUB_DATA = res.data;
                        HrmsReports.renderHubCharts(res.data);
                    }
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        openTemplateModal: function (data) {
            const t = data || {
                templateId: 0, templateName: '', reportKey: 'Workforce',
                branchId: $('#hubBranchId').val() || '',
                departmentId: $('#hubDepartmentId').val() || '',
                dateRangeDays: 30, statusFilter: '', notes: '', isShared: true
            };
            $('#repTplId').val(t.templateId || 0);
            $('#repTplName').val(t.templateName || '');
            $('#repTplKey').val(t.reportKey || 'Workforce');
            $('#repTplBranch').val(t.branchId || '');
            $('#repTplDept').val(t.departmentId || '');
            $('#repTplDays').val(t.dateRangeDays || 30);
            $('#repTplStatus').val(t.statusFilter || '');
            $('#repTplNotes').val(t.notes || '');
            $('#repTplShared').prop('checked', t.isShared !== false);
            $('#repTplModalTitle').text(t.templateId ? 'Edit Report Preset' : 'Save Report Preset');
            new bootstrap.Modal(document.getElementById('reportTemplateModal')).show();
        },

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
                            : ((res && res.message) || 'Unable to save report preset.');
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

        deleteTemplate: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Reports/DeleteTemplate/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete preset.', 'error');
                });
            });
        },

        printReport: function (reportKey, reportTitle, recordCount, filterSummary) {
            if (reportKey) {
                $.post('/Reports/LogPrintExport', {
                    reportKey: reportKey,
                    reportTitle: reportTitle,
                    recordCount: recordCount || 0,
                    filterSummary: filterSummary || ''
                });
            }
            window.print();
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-rep-datatable').length) {
            $('.hrms-rep-datatable').DataTable({
                pageLength: 20,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if (window.HRMS_REPORTS_HUB_DATA) {
            HrmsReports.renderHubCharts(window.HRMS_REPORTS_HUB_DATA);
            $(document).on('hrms:themeChanged', function () {
                HrmsReports.renderHubCharts(window.HRMS_REPORTS_HUB_DATA);
            });
        }

        if (window.HRMS_PAYROLL_REPORT_ROWS) {
            HrmsReports.renderPayrollDeptChart(window.HRMS_PAYROLL_REPORT_ROWS);
            $(document).on('hrms:themeChanged', function () {
                HrmsReports.renderPayrollDeptChart(window.HRMS_PAYROLL_REPORT_ROWS);
            });
        }
    });
})(jQuery);
