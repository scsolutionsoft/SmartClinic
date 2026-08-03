document.addEventListener('DOMContentLoaded', () => {
    const collapseElement = document.getElementById('smartclinicMainNav');
    const links = [...document.querySelectorAll('.smartclinic-main-links .nav-link')];
    const activeLink = links.find((link) => link.classList.contains('active'));

    if (activeLink) {
        activeLink.setAttribute('aria-current', 'page');
        window.setTimeout(() => activeLink.scrollIntoView({ block: 'nearest', inline: 'center' }), 120);
    }

    links.forEach((link) => link.addEventListener('click', () => {
        if (window.innerWidth >= 1200 || !collapseElement?.classList.contains('show')) return;
        bootstrap.Collapse.getOrCreateInstance(collapseElement, { toggle: false }).hide();
    }));
});
