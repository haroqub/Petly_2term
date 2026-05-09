document.addEventListener('click', (event) => {
    const button = event.target.closest('[data-toggle-password]');
    if (!button) {
        return;
    }

    const selector = button.getAttribute('data-toggle-password');
    const input = selector ? document.querySelector(selector) : null;
    if (!input) {
        return;
    }

    const isPassword = input.getAttribute('type') === 'password';
    input.setAttribute('type', isPassword ? 'text' : 'password');
    button.textContent = isPassword ? 'Сховати' : 'Показати';
});
