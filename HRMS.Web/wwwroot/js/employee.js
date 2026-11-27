(function ($) {
    'use strict';

    window.HrmsEmp = {
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
                        Swal.fire('Saved', res.message, 'success').then(function () {
                            window.location.reload();
                        });
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

        uploadDocument: function (formSelector, url) {
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
                        bootstrap.Modal.getInstance(document.querySelector('#documentModal'))?.hide();
                        Swal.fire('Uploaded', res.message, 'success').then(function () {
                            window.location.reload();
                        });
                    } else {
                        Swal.fire('Upload Failed', (res && res.message) || 'Invalid file.', 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'Unable to upload document.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        },

        deleteSubItem: function (url, label) {
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
                        Swal.fire('Error', (res && res.message) || 'Unable to remove item.', 'error');
                    }
                });
            });
        }
    };

    $(function () {
        if ($.fn.DataTable && $('.hrms-emp-datatable').length) {
            $('.hrms-emp-datatable').DataTable({
                pageLength: 10,
                responsive: true,
                order: [[0, 'asc']],
                language: { search: '', searchPlaceholder: 'Quick filter...' }
            });
        }

        if ($.fn.select2) {
            $('.hrms-select2').select2({ theme: 'bootstrap-5', width: '100%' });
        }
    });
})(jQuery);
