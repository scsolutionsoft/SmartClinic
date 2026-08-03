document.querySelectorAll('[data-audit-search]').forEach((input) => {
    input.addEventListener('input', () => {
        const body = document.getElementById(input.dataset.auditSearch);
        if (!body) return;
        const term = input.value.trim().toLocaleLowerCase('th');
        body.querySelectorAll('tr').forEach((row) => {
            row.hidden = term.length > 0 && !row.textContent.toLocaleLowerCase('th').includes(term);
        });
    });
});
