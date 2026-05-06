// Global Modal Logic
function showDeleteModal(id, number, deleteUrlPrefix = '/invoices/delete/') {
    const modalNumber = document.getElementById('modalInvoiceNumber');
    const modalForm = document.getElementById('deleteModalForm');
    const backdrop = document.getElementById('deleteModalBackdrop');

    if (modalNumber) modalNumber.innerText = number;
    if (modalForm) modalForm.action = deleteUrlPrefix + id;
    if (backdrop) backdrop.classList.add('show');
}

function hideDeleteModal() {
    const backdrop = document.getElementById('deleteModalBackdrop');
    if (backdrop) backdrop.classList.remove('show');
}

// Close modal on outside click
window.addEventListener('click', function(event) {
    const backdrop = document.getElementById('deleteModalBackdrop');
    if (event.target === backdrop) {
        hideDeleteModal();
    }
});
