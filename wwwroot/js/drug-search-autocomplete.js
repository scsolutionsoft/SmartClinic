document.addEventListener('DOMContentLoaded', () => {
    const form = document.querySelector('.drug-list-filter');
    const input = form?.querySelector('input[name="q"]');
    if (!form || !input) return;

    const shell = document.createElement('div');
    shell.className = 'drug-search-combobox';
    input.parentNode.insertBefore(shell, input);
    shell.append(input);

    const results = document.createElement('div');
    results.className = 'drug-search-results';
    results.hidden = true;
    results.setAttribute('role', 'listbox');
    shell.append(results);

    input.type = 'search';
    input.autocomplete = 'off';
    input.setAttribute('role', 'combobox');
    input.setAttribute('aria-autocomplete', 'list');
    input.setAttribute('aria-expanded', 'false');
    input.placeholder = 'พิมพ์ชื่อยา ชื่อการค้า บริษัท ความแรง TMT หรือทะเบียนยา';

    const cache = new Map();
    let timer = 0;
    let request = null;
    let activeIndex = -1;

    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, (character) => ({ '&':'&amp;','<':'&lt;','>':'&gt;',"'":'&#39;','"':'&quot;' })[character]);
    function close() { results.hidden = true; input.setAttribute('aria-expanded', 'false'); activeIndex = -1; }

    function select(item) {
        input.value = item.genericName || item.tradeName || '';
        close();
        form.requestSubmit();
    }

    function setActive(index) {
        const options = [...results.querySelectorAll('[role="option"]')];
        if (!options.length) return;
        activeIndex = Math.max(0, Math.min(index, options.length - 1));
        options.forEach((option, optionIndex) => option.classList.toggle('active', optionIndex === activeIndex));
        options[activeIndex].scrollIntoView({ block: 'nearest' });
    }

    function render(items, term) {
        results.replaceChildren();
        if (!items.length) {
            results.innerHTML = `<div class="drug-search-empty"><strong>ไม่พบรายการยา</strong><small>ลองค้นด้วยชื่อสามัญ ชื่อการค้า หรือบริษัทผู้ผลิต</small></div>`;
        } else {
            items.forEach((item) => {
                const button = document.createElement('button');
                button.type = 'button';
                button.className = 'drug-search-option';
                button.setAttribute('role', 'option');
                button.innerHTML = `<span class="drug-search-rx">Rx</span><span class="drug-search-copy"><strong>${escapeHtml(item.genericName)} <b>${escapeHtml(item.strength || '')}</b></strong><small>${escapeHtml(item.tradeName || 'ไม่ระบุชื่อการค้า')} · ${escapeHtml(item.manufacturerName || 'ไม่ระบุบริษัท')} · ${escapeHtml(item.dosageForm || '-')}</small><em>${item.tmtCode ? `TMT ${escapeHtml(item.tmtCode)}` : 'ไม่ระบุ TMT'}${item.registrationNumber ? ` · ทะเบียน ${escapeHtml(item.registrationNumber)}` : ''}</em></span><span class="drug-search-state ${item.isActive ? '' : 'inactive'}">${item.isActive ? 'ใช้งาน' : 'ปิดใช้'}</span>`;
                button.addEventListener('click', () => select(item));
                results.append(button);
            });
            results.insertAdjacentHTML('beforeend', `<footer>พบ ${items.length} รายการสำหรับ “${escapeHtml(term)}” · ใช้ ↑ ↓ และ Enter เพื่อเลือก</footer>`);
        }
        results.hidden = false;
        input.setAttribute('aria-expanded', 'true');
        activeIndex = -1;
    }

    async function search() {
        const term = input.value.trim();
        if (!term) { close(); return; }
        const key = term.toLocaleLowerCase('th');
        if (cache.has(key)) { render(cache.get(key), term); return; }
        request?.abort();
        request = new AbortController();
        results.innerHTML = '<div class="drug-search-loading"><span></span> กำลังค้นหารายการยา...</div>';
        results.hidden = false;
        input.setAttribute('aria-expanded', 'true');
        try {
            const response = await fetch(`/DrugCatalog/DrugSearchSuggestions?q=${encodeURIComponent(term)}&take=12`, { signal: request.signal, headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const items = await response.json();
            cache.set(key, items);
            render(items, term);
        } catch (error) {
            if (error.name === 'AbortError') return;
            results.innerHTML = '<div class="drug-search-empty"><strong>ค้นหาไม่สำเร็จ</strong><small>กดปุ่มค้นหาเพื่อดำเนินการต่อได้ตามปกติ</small></div>';
        }
    }

    input.addEventListener('input', () => { window.clearTimeout(timer); timer = window.setTimeout(search, 150); });
    input.addEventListener('focus', () => { if (input.value.trim()) search(); });
    input.addEventListener('keydown', (event) => {
        const options = [...results.querySelectorAll('[role="option"]')];
        if (event.key === 'ArrowDown' && options.length) { event.preventDefault(); setActive(activeIndex + 1); }
        else if (event.key === 'ArrowUp' && options.length) { event.preventDefault(); setActive(activeIndex <= 0 ? options.length - 1 : activeIndex - 1); }
        else if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); options[activeIndex].click(); }
        else if (event.key === 'Escape') close();
    });
    document.addEventListener('click', (event) => { if (!shell.contains(event.target)) close(); });
});
