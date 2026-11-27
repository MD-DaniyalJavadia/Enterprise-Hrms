(function ($) {
    'use strict';

    window.HrmsAssetDoc = {
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
            const $form = $(formSelector);
            if ($form.valid && !$form.valid()) return;

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
                        Swal.fire('Success', res.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        const msg = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Unable to upload document.');
                        Swal.fire('Validation Error', msg, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'An unexpected server error occurred during upload.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        // --- Asset Modals & Actions ---
        openAssetModal: function (data) {
            const a = data || {
                assetId: 0, categoryId: '', branchId: '', assetTag: '', assetName: '',
                brand: '', modelNumber: '', serialNumber: '', purchaseDate: '',
                purchaseCost: 0, warrantyExpiryDate: '', vendorName: '',
                conditionStatus: 'Good', assetStatus: 'Available', notes: ''
            };
            $('#astId').val(a.assetId || 0);
            $('#astCat').val(a.categoryId || '').trigger('change');
            $('#astBranch').val(a.branchId || '').trigger('change');
            $('#astTag').val(a.assetTag || '');
            $('#astName').val(a.assetName || '');
            $('#astBrand').val(a.brand || '');
            $('#astModel').val(a.modelNumber || '');
            $('#astSerial').val(a.serialNumber || '');
            $('#astPurDate').val(a.purchaseDate ? String(a.purchaseDate).substring(0, 10) : '');
            $('#astCost').val(a.purchaseCost ?? 0);
            $('#astWarranty').val(a.warrantyExpiryDate ? String(a.warrantyExpiryDate).substring(0, 10) : '');
            $('#astVendor').val(a.vendorName || '');
            $('#astCondition').val(a.conditionStatus || 'Good');
            $('#astStatus').val(a.assetStatus || 'Available');
            $('#astNotes').val(a.notes || '');
            $('#astModalTitle').text(a.assetId ? 'Edit Asset Record' : 'Register New Asset');
            new bootstrap.Modal(document.getElementById('assetModal')).show();
        },

        deleteAsset: function (id, label) {
            Swal.fire({
                title: 'Delete "' + label + '"?',
                text: 'This removes the asset from the active register.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Asset/DeleteAsset/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        window.location.reload();
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete asset.', 'error');
                    }
                });
            });
        },

        openAssignModal: function (assetId, assetLabel, conditionStatus) {
            $('#asgAssetId').val(assetId || '');
            $('#asgAssetLabel').text(assetLabel || 'Select asset');
            $('#asgEmployeeId').val('').trigger('change');
            $('#asgDate').val(new Date().toISOString().substring(0, 10));
            $('#asgExpReturn').val('');
            $('#asgCondition').val(conditionStatus || 'Good');
            $('#asgRemarks').val('');
            new bootstrap.Modal(document.getElementById('assignAssetModal')).show();
        },

        openReturnModal: function (allocationId, assetLabel, empName) {
            $('#retAllocId').val(allocationId);
            $('#retLabel').text(assetLabel + ' — ' + empName);
            $('#retDate').val(new Date().toISOString().substring(0, 10));
            $('#retCondition').val('Good');
            $('#retStatus').val('Returned');
            $('#retRemarks').val('');
            new bootstrap.Modal(document.getElementById('returnAssetModal')).show();
        },

        acknowledgeAllocation: function (allocationId, assetLabel) {
            Swal.fire({
                title: 'Acknowledge Asset Custody?',
                text: 'Confirm you have received "' + assetLabel + '" in the documented condition.',
                icon: 'question',
                showCancelButton: true,
                confirmButtonText: 'Confirm Receipt'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Asset/AcknowledgeAllocation', { allocationId: allocationId }).done(function (apiRes) {
                    if (apiRes && apiRes.success) {
                        Swal.fire('Acknowledged', apiRes.message, 'success').then(function () { window.location.reload(); });
                    } else {
                        Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to acknowledge custody.', 'error');
                    }
                });
            });
        },

        openMaintenanceModal: function (data, defaultAssetId) {
            const m = data || {
                maintenanceId: 0, assetId: defaultAssetId || '', maintenanceType: 'Repair',
                serviceDate: new Date().toISOString().substring(0, 10), completionDate: '',
                vendorOrTechnician: '', cost: 0, maintenanceStatus: 'Scheduled', description: ''
            };
            $('#mntId').val(m.maintenanceId || 0);
            $('#mntAssetId').val(m.assetId || '').trigger('change');
            $('#mntType').val(m.maintenanceType || 'Repair');
            $('#mntServiceDate').val(m.serviceDate ? String(m.serviceDate).substring(0, 10) : '');
            $('#mntCompletionDate').val(m.completionDate ? String(m.completionDate).substring(0, 10) : '');
            $('#mntVendor').val(m.vendorOrTechnician || '');
            $('#mntCost').val(m.cost ?? 0);
            $('#mntStatus').val(m.maintenanceStatus || 'Scheduled');
            $('#mntDesc').val(m.description || '');
            $('#mntModalTitle').text(m.maintenanceId ? 'Edit Maintenance Log' : 'Schedule / Log Maintenance');
            new bootstrap.Modal(document.getElementById('maintenanceModal')).show();
        },

        deleteMaintenance: function (id) {
            Swal.fire({
                title: 'Delete Maintenance Log?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Asset/DeleteMaintenance/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete log.', 'error');
                });
            });
        },

        openAssetCategoryModal: function (data) {
            const c = data || {
                categoryId: 0, categoryCode: '', categoryName: '', assetClass: 'IT',
                requiresSerialNumber: true, defaultDepreciationMonths: 36, description: '', isActive: true
            };
            $('#acatId').val(c.categoryId || 0);
            $('#acatCode').val(c.categoryCode || '');
            $('#acatName').val(c.categoryName || '');
            $('#acatClass').val(c.assetClass || 'IT');
            $('#acatSerial').prop('checked', c.requiresSerialNumber !== false);
            $('#acatDepr').val(c.defaultDepreciationMonths ?? '');
            $('#acatDesc').val(c.description || '');
            $('#acatActive').prop('checked', c.isActive !== false);
            $('#acatModalTitle').text(c.categoryId ? 'Edit Asset Category' : 'Add Asset Category');
            new bootstrap.Modal(document.getElementById('assetCategoryModal')).show();
        },

        deleteAssetCategory: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Asset/DeleteCategory/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete category.', 'error');
                });
            });
        },

        // --- Document Modals & Actions ---
        toggleDepartmentField: function () {
            const vis = $('#docVisibility').val();
            if (vis === 'DepartmentSpecific') {
                $('#docDepartmentGroup').removeClass('d-none');
            } else {
                $('#docDepartmentGroup').addClass('d-none');
                $('#docDepartmentId').val('');
            }
        },

        openCompanyDocumentModal: function (data) {
            const d = data || {
                documentId: 0, categoryId: '', departmentId: '', documentCode: '',
                title: '', versionNumber: '1.0', visibility: 'AllEmployees',
                requiresAcknowledgment: false, effectiveDate: new Date().toISOString().substring(0, 10),
                expiryDate: '', description: '', documentStatus: 'Published'
            };
            $('#docId').val(d.documentId || 0);
            $('#docCategoryId').val(d.categoryId || '');
            $('#docDepartmentId').val(d.departmentId || '');
            $('#docCode').val(d.documentCode || '');
            $('#docTitle').val(d.title || '');
            $('#docVersion').val(d.versionNumber || '1.0');
            $('#docVisibility').val(d.visibility || 'AllEmployees');
            $('#docAck').prop('checked', !!d.requiresAcknowledgment);
            $('#docEffective').val(d.effectiveDate ? String(d.effectiveDate).substring(0, 10) : '');
            $('#docExpiry').val(d.expiryDate ? String(d.expiryDate).substring(0, 10) : '');
            $('#docDesc').val(d.description || '');
            $('#docStatus').val(d.documentStatus || 'Published');
            $('#docFile').val('');
            $('#docFileHelp').text(d.documentId ? 'Optional when editing (leave blank to keep current file).' : 'Required (.pdf, .doc, .docx, .xlsx, .pptx, .png/.jpg, max 15 MB).');
            $('#docModalTitle').text(d.documentId ? 'Update Company Document' : 'Publish Company Document');
            HrmsAssetDoc.toggleDepartmentField();
            new bootstrap.Modal(document.getElementById('companyDocModal')).show();
        },

        deleteCompanyDocument: function (id, title) {
            Swal.fire({
                title: 'Delete "' + title + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Document/DeleteDocument/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete document.', 'error');
                });
            });
        },

        openAcknowledgeModal: function (docId, title, version) {
            $('#ackDocId').val(docId);
            $('#ackDocLabel').text(title + ' (v' + version + ')');
            $('#ackRemarks').val('');
            new bootstrap.Modal(document.getElementById('acknowledgeDocModal')).show();
        },

        openDocCategoryModal: function (data) {
            const c = data || {
                categoryId: 0, categoryCode: '', categoryName: '', scopeType: 'CompanyPolicy',
                requiresAcknowledgment: true, defaultVisibility: 'AllEmployees',
                retentionMonths: 60, description: '', isActive: true
            };
            $('#dcatId').val(c.categoryId || 0);
            $('#dcatCode').val(c.categoryCode || '');
            $('#dcatName').val(c.categoryName || '');
            $('#dcatScope').val(c.scopeType || 'CompanyPolicy');
            $('#dcatAck').prop('checked', !!c.requiresAcknowledgment);
            $('#dcatVis').val(c.defaultVisibility || 'AllEmployees');
            $('#dcatRet').val(c.retentionMonths ?? '');
            $('#dcatDesc').val(c.description || '');
            $('#dcatActive').prop('checked', c.isActive !== false);
            $('#dcatModalTitle').text(c.categoryId ? 'Edit Document Category' : 'Add Document Category');
            new bootstrap.Modal(document.getElementById('docCategoryModal')).show();
        },

        deleteDocCategory: function (id, name) {
            Swal.fire({
                title: 'Delete "' + name + '"?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Delete'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Document/DeleteCategory/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete category.', 'error');
                });
            });
        },

        deleteEmployeeVaultDoc: function (id, title) {
            Swal.fire({
                title: 'Remove "' + title + '" from Vault?',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Remove'
            }).then(function (res) {
                if (!res.isConfirmed) return;
                $.post('/Document/DeleteEmployeeDocument/' + id).done(function (apiRes) {
                    if (apiRes && apiRes.success) window.location.reload();
                    else Swal.fire('Error', (apiRes && apiRes.message) || 'Unable to delete employee document.', 'error');
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-ad-datatable').length) {
            $('.hrms-ad-datatable').DataTable({
                pageLength: 15,
                responsive: true,
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }

        $('#docVisibility').on('change', HrmsAssetDoc.toggleDepartmentField);
    });
})(jQuery);
