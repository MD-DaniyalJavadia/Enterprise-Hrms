(function ($) {
    'use strict';

    let attendanceChart = null;
    let genderChart = null;
    let departmentChart = null;
    let hiringChart = null;

    function getChartTextColor() {
        return document.documentElement.getAttribute('data-bs-theme') === 'dark' ? '#cbd5e1' : '#475569';
    }

    function destroyCharts() {
        [attendanceChart, genderChart, departmentChart, hiringChart].forEach(function (chart) {
            if (chart) chart.destroy();
        });
    }

    function renderCharts(data) {
        if (!data) return;
        destroyCharts();

        const textColor = getChartTextColor();

        const att = data.attendanceAnalytics || [];
        attendanceChart = new Chart(document.getElementById('attendanceAnalyticsChart'), {
            type: 'bar',
            data: {
                labels: att.map(x => x.dateLabel),
                datasets: [
                    { label: 'Present', data: att.map(x => x.presentCount), backgroundColor: '#16a34a', borderRadius: 4 },
                    { label: 'Late', data: att.map(x => x.lateCount), backgroundColor: '#f59e0b', borderRadius: 4 },
                    { label: 'Absent', data: att.map(x => x.absentCount), backgroundColor: '#dc2626', borderRadius: 4 },
                    { label: 'On Leave', data: att.map(x => x.onLeaveCount), backgroundColor: '#2563eb', borderRadius: 4 }
                ]
            },
            options: {
                responsive: true,
                plugins: { legend: { labels: { color: textColor } } },
                scales: {
                    x: { ticks: { color: textColor } },
                    y: { beginAtZero: true, ticks: { precision: 0, color: textColor } }
                }
            }
        });

        const gender = data.genderStats || [];
        genderChart = new Chart(document.getElementById('genderDistributionChart'), {
            type: 'doughnut',
            data: {
                labels: gender.map(x => x.gender),
                datasets: [{
                    data: gender.map(x => x.count),
                    backgroundColor: ['#2563eb', '#ec4899', '#8b5cf6', '#14b8a6'],
                    borderWidth: 0
                }]
            },
            options: {
                responsive: true,
                cutout: '66%',
                plugins: { legend: { position: 'bottom', labels: { color: textColor } } }
            }
        });

        const dept = data.departmentStats || [];
        departmentChart = new Chart(document.getElementById('departmentStatsChart'), {
            type: 'bar',
            data: {
                labels: dept.map(x => x.departmentName),
                datasets: [{
                    label: 'Employees',
                    data: dept.map(x => x.employeeCount),
                    backgroundColor: '#3b82f6',
                    borderRadius: 6
                }]
            },
            options: {
                indexAxis: 'y',
                responsive: true,
                plugins: { legend: { display: false } },
                scales: {
                    x: { beginAtZero: true, ticks: { precision: 0, color: textColor } },
                    y: { ticks: { color: textColor } }
                }
            }
        });

        const hiring = data.monthlyHiringStats || [];
        hiringChart = new Chart(document.getElementById('monthlyHiringChart'), {
            type: 'line',
            data: {
                labels: hiring.map(x => x.monthLabel),
                datasets: [
                    {
                        label: 'Hired',
                        data: hiring.map(x => x.hiredCount),
                        borderColor: '#2563eb',
                        backgroundColor: 'rgba(37, 99, 235, 0.15)',
                        fill: true,
                        tension: 0.35
                    },
                    {
                        label: 'Resigned',
                        data: hiring.map(x => x.resignedCount),
                        borderColor: '#ef4444',
                        backgroundColor: 'rgba(239, 68, 68, 0.1)',
                        fill: true,
                        tension: 0.35
                    }
                ]
            },
            options: {
                responsive: true,
                plugins: { legend: { labels: { color: textColor } } },
                scales: {
                    x: { ticks: { color: textColor } },
                    y: { beginAtZero: true, ticks: { precision: 0, color: textColor } }
                }
            }
        });
    }

    function updateKpis(summary) {
        if (!summary || !summary.kpis) return;
        const k = summary.kpis;
        $('#kpiTotalEmployees').text(k.totalEmployees);
        $('#kpiActiveEmployees').text(k.activeEmployees);
        $('#kpiInactiveEmployees').text(k.inactiveEmployees);
        $('#kpiNewHires').text(k.newHiresThisMonth);
        $('#kpiResigned').text(k.resignedEmployeesThisMonth);
        $('#kpiOnLeave').text(k.employeesOnLeaveToday);
        $('#kpiPresentToday').text(k.presentToday);
        $('#kpiLateToday').text(k.lateArrivalsToday);
        $('#kpiAbsentToday').text(k.absentToday);
        $('#kpiPendingLeaves').text(k.pendingLeaveRequests);
    }

    $(function () {
        let currentData = window.HRMS_INITIAL_DASHBOARD_DATA || {};
        renderCharts(currentData);

        $(document).on('hrms:themeChanged', function () {
            renderCharts(currentData);
        });

        $('#refreshDashboardBtn, #branchFilterSelect, #asOfDateFilter').on('change click', function (e) {
            if (this.id === 'refreshDashboardBtn' || e.type === 'change') {
                const branchId = $('#branchFilterSelect').val();
                const asOfDate = $('#asOfDateFilter').val();

                $('#globalLoadingOverlay').removeClass('d-none');
                $.get('/Dashboard/GetSummaryData', { branchId: branchId, asOfDate: asOfDate })
                    .done(function (res) {
                        if (res && res.success && res.data) {
                            currentData = res.data;
                            updateKpis(res.data);
                            renderCharts(res.data);
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            }
        });
    });
})(jQuery);
