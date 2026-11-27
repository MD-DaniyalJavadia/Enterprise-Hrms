(function ($) {
    'use strict';

    window.HrmsRec = {
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
                        Swal.fire('Saved', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to save record.');
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

        openRequisitionModal: function (data) {
            const r = data || {
                requisitionId: 0, requisitionCode: '', jobTitle: '', branchId: '', departmentId: '',
                designationId: '', jobPositionId: '', hiringManagerId: '', employmentType: 'Full-Time',
                workMode: 'On-Site', openingsCount: 1, minSalary: '', maxSalary: '',
                experienceMinYears: 0, experienceMaxYears: '', targetHireDate: '',
                requisitionStatus: 'Open', jobSummary: '', requiredSkills: ''
            };
            $('#reqId').val(r.requisitionId || 0);
            $('#reqCode').val(r.requisitionCode || '');
            $('#reqTitle').val(r.jobTitle || '');
            $('#reqBranch').val(r.branchId || '');
            $('#reqDept').val(r.departmentId || '');
            $('#reqDesig').val(r.designationId || '');
            $('#reqPos').val(r.jobPositionId || '');
            $('#reqMgr').val(r.hiringManagerId || '');
            $('#reqEmpType').val(r.employmentType || 'Full-Time');
            $('#reqWorkMode').val(r.workMode || 'On-Site');
            $('#reqOpenings').val(r.openingsCount || 1);
            $('#reqMinSal').val(r.minSalary ?? '');
            $('#reqMaxSal').val(r.maxSalary ?? '');
            $('#reqExpMin').val(r.experienceMinYears ?? 0);
            $('#reqExpMax').val(r.experienceMaxYears ?? '');
            $('#reqTargetDate').val(r.targetHireDate ? String(r.targetHireDate).substring(0, 10) : '');
            $('#reqStatus').val(r.requisitionStatus || 'Open');
            $('#reqSummary').val(r.jobSummary || '');
            $('#reqSkills').val(r.requiredSkills || '');
            $('#reqModalTitle').text(r.requisitionId ? 'Edit Job Requisition' : 'Create Job Requisition');
            new bootstrap.Modal(document.getElementById('requisitionModal')).show();
        },

        updateRequisitionStatus: function (id, status, title) {
            Swal.fire({
                title: 'Set Requisition to ' + status + '?',
                text: title,
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Confirm'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Recruitment/UpdateRequisitionStatus', { requisitionId: id, status: status }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update requisition.', 'error');
                    }
                });
            });
        },

        deleteRequisition: function (id, title) {
            Swal.fire({
                title: 'Delete "' + title + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Recruitment/DeleteRequisition/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete requisition.', 'error');
                    }
                });
            });
        },

        openCandidateModal: function (data) {
            const c = data || {
                candidateId: 0, firstName: '', lastName: '', email: '', phone: '',
                currentCompany: '', currentJobTitle: '', totalExperienceYears: 0,
                expectedSalary: '', skills: '', source: 'CareerPortal', linkedInUrl: '', notes: ''
            };
            $('#candId').val(c.candidateId || 0);
            $('#candFirst').val(c.firstName || '');
            $('#candLast').val(c.lastName || '');
            $('#candEmail').val(c.email || '');
            $('#candPhone').val(c.phone || '');
            $('#candCompany').val(c.currentCompany || '');
            $('#candTitle').val(c.currentJobTitle || '');
            $('#candExp').val(c.totalExperienceYears ?? 0);
            $('#candSalary').val(c.expectedSalary ?? '');
            $('#candSkills').val(c.skills || '');
            $('#candSource').val(c.source || 'CareerPortal');
            $('#candLinkedIn').val(c.linkedInUrl || '');
            $('#candNotes').val(c.notes || '');
            $('#candResume').val('');
            $('#candModalTitle').text(c.candidateId ? 'Edit Candidate Profile' : 'Add Candidate');
            new bootstrap.Modal(document.getElementById('candidateModal')).show();
        },

        deleteCandidate: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Recruitment/DeleteCandidate/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete candidate.', 'error');
                    }
                });
            });
        },

        openMoveStageModal: function (appId, candidateName, currentStage) {
            $('#mvAppId').val(appId);
            $('#mvCandidateLabel').text(candidateName);
            $('#mvStage').val(currentStage || 'Screening');
            $('#mvReason').val('');
            new bootstrap.Modal(document.getElementById('moveStageModal')).show();
        },

        openInterviewModal: function (data) {
            const iv = data || {
                interviewId: 0, applicationId: '', interviewRound: 'Technical Round',
                interviewMode: 'Video', scheduledStart: '', durationMinutes: 45,
                locationOrLink: '', interviewerEmployeeId: ''
            };
            $('#ivId').val(iv.interviewId || 0);
            $('#ivAppId').val(iv.applicationId || '');
            $('#ivRound').val(iv.interviewRound || 'Technical Round');
            $('#ivMode').val(iv.interviewMode || 'Video');
            $('#ivStart').val(iv.scheduledStart ? String(iv.scheduledStart).substring(0, 16) : '');
            $('#ivDuration').val(iv.durationMinutes || 45);
            $('#ivLocation').val(iv.locationOrLink || '');
            $('#ivInterviewer').val(iv.interviewerEmployeeId || '');
            $('#ivModalTitle').text(iv.interviewId ? 'Reschedule Interview' : 'Schedule Interview');
            new bootstrap.Modal(document.getElementById('interviewModal')).show();
        },

        openFeedbackModal: function (data) {
            $('#fbInterviewId').val(data.interviewId);
            $('#fbLabel').text(data.candidateName + ' — ' + data.interviewRound);
            $('#fbStatus').val(data.interviewStatus === 'Scheduled' ? 'Completed' : data.interviewStatus);
            $('#fbTech').val(data.technicalScore || 4);
            $('#fbComm').val(data.communicationScore || 4);
            $('#fbRec').val(data.overallRecommendation || 'Hire');
            $('#fbNotes').val(data.feedbackNotes || '');
            new bootstrap.Modal(document.getElementById('feedbackModal')).show();
        },

        openOfferModal: function (data) {
            const o = data || {
                offerId: 0, applicationId: '', offeredSalaryAnnual: '',
                joiningDate: '', offerExpiryDate: '', employmentType: 'Full-Time', benefitsSummary: ''
            };
            $('#ofrId').val(o.offerId || 0);
            $('#ofrAppId').val(o.applicationId || '');
            $('#ofrSalary').val(o.offeredSalaryAnnual ?? '');
            if (o.joiningDate) $('#ofrJoinDate').val(String(o.joiningDate).substring(0, 10));
            if (o.offerExpiryDate) $('#ofrExpiryDate').val(String(o.offerExpiryDate).substring(0, 10));
            $('#ofrEmpType').val(o.employmentType || 'Full-Time');
            $('#ofrBenefits').val(o.benefitsSummary || '');
            $('#ofrModalTitle').text(o.offerId ? 'Edit Job Offer' : 'Create Job Offer');
            new bootstrap.Modal(document.getElementById('offerModal')).show();
        },

        updateOfferStatus: function (offerId, status, candidateName) {
            Swal.fire({
                title: 'Mark Offer as ' + status + '?',
                text: 'Candidate: ' + candidateName,
                input: 'text',
                inputLabel: 'Notes (optional)',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Confirm ' + status
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Recruitment/UpdateOfferStatus', {
                    offerId: offerId,
                    status: status,
                    responseNotes: res.value || ''
                }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Updated', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to update offer.', 'error');
                    }
                });
            });
        },

        convertToEmployee: function (offerId, candidateName) {
            Swal.fire({
                title: 'Onboard ' + candidateName + '?',
                text: 'This creates an Employee Master record, employment history, and initial salary record.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Onboard Employee'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $('#globalLoadingOverlay').removeClass('d-none');
                $.post('/Recruitment/ConvertOfferToEmployee', { offerId: offerId })
                    .done(function (apiRes) {
                        if (apiRes && apiRes.success) {
                            Swal.fire('Onboarded!', apiRes.message, 'success').then(function () {
                                if (apiRes.data) {
                                    window.location.href = '/Employee/Details/' + apiRes.data;
                                } else {
                                    window.location.reload();
                                }
                            });
                        } else {
                            Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to onboard candidate.', 'error');
                        }
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-rec-datatable').length) {
            $('.hrms-rec-datatable').DataTable({
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
