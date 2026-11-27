(function ($) {
    'use strict';

    window.HrmsPerfTrn = {
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

        initializeCycle: function (cycleId, cycleName) {
            if (!cycleId) {
                Swal.fire('Select Cycle', 'Please filter or pick a cycle to initialize.', 'info');
                return;
            }
            Swal.fire({
                title: 'Initialize Appraisals?',
                text: 'Create appraisal records for all active employees in "' + cycleName + '"?',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Initialize'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Performance/InitializeCycle', { cycleId: cycleId })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Initialized', apiRes.message, 'success').then(function () { window.location.reload(); });
                        } else {
                            Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to initialize cycle.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        },

        openSelfAppraisalModal: function (data) {
            $('#selfAppId').val(data.appraisalId);
            $('#selfAppLabel').text(data.employeeName + ' — ' + data.cycleName);
            $('#selfRating').val(data.selfRating ?? 4.0);
            $('#selfComments').val(data.selfComments || '');
            new bootstrap.Modal(document.getElementById('selfAppraisalModal')).show();
        },

        openManagerReviewModal: function (data) {
            $('#mgrAppId').val(data.appraisalId);
            $('#mgrAppLabel').text(data.employeeName + ' (' + data.cycleCode + ')');
            $('#mgrRating').val(data.managerRating ?? data.selfRating ?? 4.0);
            $('#mgrFinalRating').val(data.finalRating ?? data.managerRating ?? data.selfRating ?? 4.0);
            $('#mgrStrengths').val(data.strengths || '');
            $('#mgrImprovements').val(data.improvementAreas || '');
            $('#mgrComments').val(data.managerComments || '');
            $('#mgrIncrement').val(data.recommendedIncrementPercent ?? '');
            $('#mgrFinalize').prop('checked', true);
            new bootstrap.Modal(document.getElementById('managerReviewModal')).show();
        },

        openCycleModal: function (data) {
            const c = data || {
                cycleId: 0, cycleCode: '', cycleName: '', reviewYear: new Date().getFullYear(),
                startDate: '', endDate: '', selfReviewDeadline: '', managerReviewDeadline: '',
                cycleStatus: 'Active', description: ''
            };
            $('#cycId').val(c.cycleId || 0);
            $('#cycCode').val(c.cycleCode || '');
            $('#cycName').val(c.cycleName || '');
            $('#cycYear').val(c.reviewYear || new Date().getFullYear());
            $('#cycStart').val(c.startDate ? String(c.startDate).substring(0, 10) : '');
            $('#cycEnd').val(c.endDate ? String(c.endDate).substring(0, 10) : '');
            $('#cycSelfDue').val(c.selfReviewDeadline ? String(c.selfReviewDeadline).substring(0, 10) : '');
            $('#cycMgrDue').val(c.managerReviewDeadline ? String(c.managerReviewDeadline).substring(0, 10) : '');
            $('#cycStatus').val(c.cycleStatus || 'Active');
            $('#cycDesc').val(c.description || '');
            $('#cycModalTitle').text(c.cycleId ? 'Edit Appraisal Cycle' : 'Create Appraisal Cycle');
            new bootstrap.Modal(document.getElementById('cycleModal')).show();
        },

        deleteCycle: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Performance/DeleteCycle/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete cycle.', 'error');
                    }
                });
            });
        },

        openGoalModal: function (data) {
            const g = data || {
                goalId: 0, cycleId: '', employeeId: '', goalTitle: '', kpiMetric: '',
                weightPercent: 25, targetValue: '', achievedValue: '', progressPercent: 0,
                goalStatus: 'InProgress', managerNotes: ''
            };
            $('#goalId').val(g.goalId || 0);
            $('#goalCycle').val(g.cycleId || '');
            $('#goalEmp').val(g.employeeId || '');
            $('#goalTitle').val(g.goalTitle || '');
            $('#goalMetric').val(g.kpiMetric || '');
            $('#goalWeight').val(g.weightPercent ?? 25);
            $('#goalTarget').val(g.targetValue || '');
            $('#goalAchieved').val(g.achievedValue || '');
            $('#goalProgress').val(g.progressPercent ?? 0);
            $('#goalStatus').val(g.goalStatus || 'InProgress');
            $('#goalNotes').val(g.managerNotes || '');
            $('#goalModalTitle').text(g.goalId ? 'Edit KPI / Goal' : 'Assign Performance Goal');
            new bootstrap.Modal(document.getElementById('goalModal')).show();
        },

        openGoalProgressModal: function (data) {
            $('#gpGoalId').val(data.goalId);
            $('#gpLabel').text(data.goalTitle + ' (' + data.employeeName + ')');
            $('#gpAchieved').val(data.achievedValue || '');
            $('#gpProgress').val(data.progressPercent ?? 0);
            $('#gpStatus').val(data.goalStatus || 'InProgress');
            new bootstrap.Modal(document.getElementById('goalProgressModal')).show();
        },

        deleteGoal: function (id, title) {
            Swal.fire({
                title: 'Delete "' + title + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Performance/DeleteGoal/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete goal.', 'error');
                    }
                });
            });
        },

        openProgramModal: function (data) {
            const p = data || {
                programId: 0, programCode: '', programTitle: '', category: 'Technical',
                deliveryMode: 'Workshop', durationHours: 8, trainerOrProvider: '',
                startDate: '', endDate: '', maxParticipants: 30, locationOrUrl: '',
                isMandatory: false, programStatus: 'Planned', description: ''
            };
            $('#prgId').val(p.programId || 0);
            $('#prgCode').val(p.programCode || '');
            $('#prgTitle').val(p.programTitle || '');
            $('#prgCat').val(p.category || 'Technical');
            $('#prgMode').val(p.deliveryMode || 'Workshop');
            $('#prgHours').val(p.durationHours ?? 8);
            $('#prgTrainer').val(p.trainerOrProvider || '');
            $('#prgStart').val(p.startDate ? String(p.startDate).substring(0, 10) : '');
            $('#prgEnd').val(p.endDate ? String(p.endDate).substring(0, 10) : '');
            $('#prgMax').val(p.maxParticipants ?? 30);
            $('#prgLoc').val(p.locationOrUrl || '');
            $('#prgMandatory').prop('checked', !!p.isMandatory);
            $('#prgStatus').val(p.programStatus || 'Planned');
            $('#prgDesc').val(p.description || '');
            $('#prgModalTitle').text(p.programId ? 'Edit Training Program' : 'Create Training Program');
            new bootstrap.Modal(document.getElementById('programModal')).show();
        },

        deleteProgram: function (id, title) {
            Swal.fire({
                title: 'Delete "' + title + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Training/DeleteProgram/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete training program.', 'error');
                    }
                });
            });
        },

        openOutcomeModal: function (data) {
            $('#outEnrId').val(data.enrollmentId);
            $('#outLabel').text(data.employeeName + ' — ' + data.programTitle);
            $('#outStatus').val(data.attendanceStatus === 'Enrolled' ? 'Completed' : data.attendanceStatus);
            $('#outScore').val(data.assessmentScore ?? '');
            $('#outCert').val(data.certificateNumber || '');
            $('#outDate').val(data.completionDate ? String(data.completionDate).substring(0, 10) : '');
            $('#outRating').val(data.feedbackRating ?? '');
            $('#outNotes').val(data.feedbackNotes || '');
            new bootstrap.Modal(document.getElementById('outcomeModal')).show();
        },

        deleteEnrollment: function (id, label) {
            Swal.fire({
                title: 'Remove enrollment for "' + label + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Remove'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Training/DeleteEnrollment/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to remove enrollment.', 'error');
                    }
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-pt-datatable').length) {
            $('.hrms-pt-datatable').DataTable({
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
