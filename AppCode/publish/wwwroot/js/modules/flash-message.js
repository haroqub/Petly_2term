document.addEventListener('click', (event) => {
    const dismissButton = event.target.closest('[data-dismiss-flash]');
    if (!dismissButton) {
        return;
    }

    const flashMessage = dismissButton.closest('.flash-message');
    if (flashMessage) {
        flashMessage.remove();
    }
});
