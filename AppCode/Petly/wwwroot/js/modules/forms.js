document.addEventListener('submit', (event) => {
    const form = event.target;
    if (!(form instanceof HTMLFormElement)) {
        return;
    }

    const submitButton = form.querySelector('button[type="submit"]');
    if (!(submitButton instanceof HTMLButtonElement)) {
        return;
    }

    submitButton.disabled = true;
    submitButton.dataset.originalText = submitButton.textContent ?? '';
    submitButton.textContent = 'Завантаження...';

    window.setTimeout(() => {
        submitButton.disabled = false;
        submitButton.textContent = submitButton.dataset.originalText ?? submitButton.textContent;
    }, 4000);
});
