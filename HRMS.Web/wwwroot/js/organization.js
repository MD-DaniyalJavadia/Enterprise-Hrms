(function ($) {
    'use strict';

    window.HrmsOrg = {
        initDataTable: function (selector) {
            if ($.fn.DataTable && $(selector).length) {
                return $(selector).DataTable({
                    pageLength: 10,
                    responsive: true,
                    order: [],
                    language: {
                        search: '',
                        searchPlaceholder: 'Quick filter...'
                    }
                });
            }
            return null;
        },

        confirmDelete: function (url, entityLabel, onSuccess) {
            Swal.fire({
                title: 'Delete ' + entityLabel + '?',
                text: 'This action will deactivate and archive the record.',
                icon: 'warning',
                showCancelButton: true,
                confirmButtonColor: '#dc2626',
                confirmButtonText: 'Yes, delete it'
            }).then(function (result) {
                if (!result.isConfirmed) return;

                $('#globalLoadingOverlay').removeClass('d-none');
                $.post(url)
                    .done(function (res) {
                        if (res && res.success) {
                            Swal.fire('Deleted', res.message, 'success').then(function () {
                                if (typeof onSuccess === 'function') {
                                    onSuccess(res);
                                } else {
                                    window.location.reload();
                                }
                            });
                        } else {
                            Swal.fire('Cannot Delete', (res && res.message) || 'Delete failed.', 'error');
                        }
                    })
                    .fail(function () {
                        Swal.fire('Error', 'Unable to complete delete request.', 'error');
                    })
                    .always(function () {
                        $('#globalLoadingOverlay').addClass('d-none');
                    });
            });
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
                        Swal.fire('Saved', res.message, 'success').then(function () {
                            window.location.reload();
                        });
                    } else {
                        const errList = res && res.errors && res.errors.length
                            ? res.errors.join(' ')
                            : ((res && res.message) || 'Validation or save failed.');
                        Swal.fire('Validation Error', errList, 'error');
                    }
                })
                .fail(function () {
                    Swal.fire('Error', 'An unexpected error occurred while saving.', 'error');
                })
                .always(function () {
                    $('#globalLoadingOverlay').addClass('d-none');
                });
        }
    };

    $(function () {
        if ($.fn.select2) {
            $('.hrms-select2').select2({
                theme: 'bootstrap-5',
                width: '100%'
            });
        }

        HrmsOrg.initDataTable('.hrms-datatable');
    });
})(jQuery);
