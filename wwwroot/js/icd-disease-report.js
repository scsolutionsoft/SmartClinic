document.addEventListener('DOMContentLoaded', () => {
    const panel = document.querySelector('.icd-disease-report-panel');
    const grid = panel?.querySelector('.icd-disease-report-grid');
    if (!panel || !grid) return;

    const cards = [...grid.querySelectorAll('.icd-disease-report-card')];
    if (!cards.length) return;

    const pageSize = 4;
    let currentPage = 1;
    let filteredCards = cards;

    cards.forEach((card, index) => {
        const header = card.querySelector(':scope > header');
        if (!header) return;
        const sequence = document.createElement('span');
        sequence.className = 'icd-report-sequence';
        sequence.innerHTML = `<small>#</small><strong>${index + 1}</strong>`;
        header.prepend(sequence);
    });

    const filters = document.createElement('form');
    filters.className = 'icd-report-filter';
    filters.innerHTML = `<label><span>ค้นหาโรค</span><input type="search" data-report-disease placeholder="รหัส ICD-10 ชื่อไทย หรือชื่ออังกฤษ"></label><label><span>ค้นหายาที่เชื่อมโยง</span><input type="search" data-report-drug placeholder="ชื่อยา ชื่อการค้า บริษัท หรือความแรง"></label><button type="submit" class="btn btn-primary">กรองรายงาน</button><button type="button" class="btn btn-light" data-report-clear>ล้างตัวกรอง</button>`;
    grid.before(filters);

    const empty = document.createElement('div');
    empty.className = 'icd-report-filter-empty';
    empty.textContent = 'ไม่พบรายงานโรคหรือรายการยาที่ตรงกับตัวกรอง';
    empty.hidden = true;
    grid.after(empty);

    const nav = document.createElement('nav');
    nav.className = 'icd-report-pagination';
    nav.setAttribute('aria-label', 'หน้ารายงานยาตามโรค');
    panel.append(nav);

    const normalize = (value) => String(value || '').trim().toLocaleLowerCase('th');
    function applyFilters() {
        const diseaseTerm = normalize(filters.querySelector('[data-report-disease]').value);
        const drugTerm = normalize(filters.querySelector('[data-report-drug]').value);
        filteredCards = cards.filter((card) => {
            const headerText = normalize(card.querySelector(':scope > header')?.textContent);
            const drugText = normalize(card.querySelector('.icd-report-drug-list')?.textContent);
            return (!diseaseTerm || headerText.includes(diseaseTerm)) && (!drugTerm || drugText.includes(drugTerm));
        });
        currentPage = 1;
        render();
    }

    function render() {
        const totalPages = Math.max(1, Math.ceil(filteredCards.length / pageSize));
        currentPage = Math.min(currentPage, totalPages);
        const start = (currentPage - 1) * pageSize;
        const visible = new Set(filteredCards.slice(start, start + pageSize));
        cards.forEach((card) => { card.hidden = !visible.has(card); });
        empty.hidden = filteredCards.length > 0;

        if (!filteredCards.length) {
            nav.hidden = true;
            return;
        }
        nav.hidden = false;
        const from = start + 1;
        const to = Math.min(start + pageSize, filteredCards.length);
        const pageButtons = Array.from({ length: totalPages }, (_, index) => index + 1)
            .map((page) => `<button type="button" data-report-page="${page}" class="${page === currentPage ? 'active' : ''}" aria-label="หน้าที่ ${page}">${page}</button>`).join('');
        nav.innerHTML = `<div><strong>${from}–${to}</strong><span>จาก ${filteredCards.length} โรค</span></div><section><button type="button" data-report-prev ${currentPage === 1 ? 'disabled' : ''} aria-label="หน้าก่อน">‹</button>${pageButtons}<button type="button" data-report-next ${currentPage === totalPages ? 'disabled' : ''} aria-label="หน้าถัดไป">›</button></section>`;
        nav.querySelector('[data-report-prev]')?.addEventListener('click', () => goTo(currentPage - 1));
        nav.querySelector('[data-report-next]')?.addEventListener('click', () => goTo(currentPage + 1));
        nav.querySelectorAll('[data-report-page]').forEach((button) => button.addEventListener('click', () => goTo(Number(button.dataset.reportPage))));
    }

    function goTo(page) {
        const totalPages = Math.max(1, Math.ceil(filteredCards.length / pageSize));
        currentPage = Math.max(1, Math.min(totalPages, page));
        render();
        panel.scrollIntoView({ behavior: 'smooth', block: 'start' });
    }

    filters.addEventListener('submit', (event) => { event.preventDefault(); applyFilters(); });
    filters.querySelector('[data-report-clear]').addEventListener('click', () => { filters.reset(); applyFilters(); });
    filters.querySelectorAll('input').forEach((input) => input.addEventListener('input', applyFilters));
    render();
});
