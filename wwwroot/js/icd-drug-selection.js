(() => {
    const modal = document.getElementById('icdDrugSelectionModal');
    const codeInput = document.getElementById('icdDrugCode');
    const loadButton = document.getElementById('loadIcdDrugs');
    const state = document.getElementById('icdDrugState');
    const picker = document.getElementById('icdDrugPicker');
    const cards = document.getElementById('icdDrugCards');
    const search = document.getElementById('icdDrugSearch');
    const saveButton = document.getElementById('saveIcdDrugs');
    const reportModal = document.getElementById('icdDrugSaveReportModal');
    if (!modal || !codeInput || !loadButton || !state || !picker || !cards || !saveButton) return;

    const token = document.querySelector('input[name="__RequestVerificationToken"]')?.value || '';
    const esc = (value) => String(value ?? '').replace(/[&<>'"]/g, (character) => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;' })[character]);
    let activeCode = '';
    let changed = false;
    let activeDisease = null;
    let pendingReport = false;

    function updateCount() {
        const count = cards.querySelectorAll('[data-icd-drug]:checked').length;
        document.getElementById('icdDrugSelectedCount').textContent = `เลือก ${count} รายการ`;
        cards.querySelectorAll('.icd-drug-option').forEach((card) => card.classList.toggle('selected', card.querySelector('[data-icd-drug]').checked));
    }

    function render(data) {
        activeCode = data.icd10Code;
        activeDisease = data;
        const selected = new Set(data.selectedDrugIds || []);
        document.getElementById('icdDrugDiseaseName').innerHTML = `<b>${esc(data.icd10Code)}</b> ${esc(data.diseaseName)}`;
        cards.innerHTML = (data.drugs || []).map((drug) => {
            const isSelected = selected.has(drug.id);
            const usage = drug.usageText || [drug.doseAmount ? `ครั้งละ ${drug.doseAmount}` : '', drug.frequencyPerDay ? `วันละ ${drug.frequencyPerDay} ครั้ง` : '', drug.mealTiming || ''].filter(Boolean).join(' · ') || 'ยังไม่ได้ระบุวิธีใช้';
            const searchText = [drug.genericName, drug.tradeName, drug.manufacturerName, drug.strength, drug.dosageForm].join(' ').toLocaleLowerCase('th');
            return `<label class="icd-drug-option${isSelected ? ' selected' : ''}" data-search="${esc(searchText)}"><input type="checkbox" data-icd-drug value="${drug.id}"${isSelected ? ' checked' : ''}><span class="icd-drug-check">✓</span><span class="icd-drug-rx">Rx</span><span class="icd-drug-copy"><strong>${esc(drug.genericName)} <b>${esc(drug.strength)}</b></strong><small>${esc(drug.tradeName || 'ไม่ระบุชื่อการค้า')} · ${esc(drug.manufacturerName || 'ไม่ระบุบริษัท')} · ${esc(drug.dosageForm || '-')}</small><em>${esc(usage)}</em></span></label>`;
        }).join('') || '<div class="icd-drug-empty">ยังไม่มียาที่เปิดใช้งานในคลังยา</div>';
        cards.querySelectorAll('[data-icd-drug]').forEach((input) => input.addEventListener('change', updateCount));
        state.hidden = true;
        picker.hidden = false;
        saveButton.disabled = false;
        updateCount();
    }

    async function load() {
        const code = codeInput.value.trim().toUpperCase();
        if (code.length < 3) {
            state.hidden = false;
            state.className = 'icd-drug-state error';
            state.textContent = 'กรุณาเลือกรหัส ICD-10 ก่อน';
            return;
        }
        loadButton.disabled = true;
        picker.hidden = true;
        saveButton.disabled = true;
        state.hidden = false;
        state.className = 'icd-drug-state';
        state.textContent = 'กำลังโหลดรายการยาจากคลัง...';
        try {
            const response = await fetch(`/DrugCatalog/IcdDrugSelection?icdCode=${encodeURIComponent(code)}`, { headers: { Accept: 'application/json' } });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error || `HTTP ${response.status}`);
            render(result);
        } catch (error) {
            state.className = 'icd-drug-state error';
            state.textContent = `โหลดรายการยาไม่สำเร็จ: ${error.message}`;
        } finally {
            loadButton.disabled = false;
        }
    }

    loadButton.addEventListener('click', load);
    codeInput.addEventListener('change', () => { if (codeInput.value.trim().length >= 3) load(); });
    search?.addEventListener('input', () => {
        const term = search.value.trim().toLocaleLowerCase('th');
        cards.querySelectorAll('.icd-drug-option').forEach((card) => card.hidden = term.length > 0 && !card.dataset.search.includes(term));
    });
    document.getElementById('selectAllIcdDrugs')?.addEventListener('click', () => { cards.querySelectorAll('[data-icd-drug]').forEach((item) => { if (!item.closest('.icd-drug-option').hidden) item.checked = true; }); updateCount(); });
    document.getElementById('clearAllIcdDrugs')?.addEventListener('click', () => { cards.querySelectorAll('[data-icd-drug]').forEach((item) => item.checked = false); updateCount(); });
    saveButton.addEventListener('click', async () => {
        if (!activeCode) return;
        const drugIds = [...cards.querySelectorAll('[data-icd-drug]:checked')].map((item) => Number(item.value));
        const selectedDrugs = (activeDisease?.drugs || []).filter((drug) => drugIds.includes(drug.id));
        saveButton.disabled = true;
        saveButton.textContent = 'กำลังบันทึก...';
        try {
            const response = await fetch('/DrugCatalog/SaveIcdDrugSelection', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json', RequestVerificationToken: token, 'X-Requested-With': 'XMLHttpRequest' }, body: JSON.stringify({ icd10Code: activeCode, drugIds }) });
            const result = await response.json();
            if (!response.ok) throw new Error(result.error || `HTTP ${response.status}`);
            changed = true;
            document.getElementById('icdSaveReportCode').textContent = result.icd10Code;
            document.getElementById('icdSaveReportThai').textContent = result.diseaseName;
            document.getElementById('icdSaveReportEnglish').textContent = result.englishName || activeDisease?.englishName || '-';
            document.getElementById('icdSaveReportSelected').textContent = result.selected;
            document.getElementById('icdSaveReportAdded').textContent = result.added;
            document.getElementById('icdSaveReportRemoved').textContent = result.removed;
            document.getElementById('icdSaveReportDrugs').innerHTML = selectedDrugs.length
                ? selectedDrugs.map((drug) => `<div class="icd-save-drug-item"><span>Rx</span><div><strong>${esc(drug.genericName)} ${esc(drug.strength)}</strong><small>${esc(drug.tradeName || 'ไม่ระบุชื่อการค้า')} · ${esc(drug.manufacturerName || 'ไม่ระบุบริษัท')}</small></div></div>`).join('')
                : '<div class="icd-drug-empty">ไม่ได้เลือกรายการยา ระบบนำความสัมพันธ์เดิมออกตามที่ยืนยัน</div>';
            pendingReport = true;
            bootstrap.Modal.getOrCreateInstance(modal).hide();
        } catch (error) {
            state.hidden = false;
            state.className = 'icd-drug-state error';
            state.textContent = `บันทึกไม่สำเร็จ: ${error.message}`;
        } finally {
            saveButton.disabled = false;
            saveButton.textContent = 'บันทึกรายการยาของโรค';
        }
    });
    modal.addEventListener('show.bs.modal', (event) => {
        const reportButton = event.relatedTarget?.closest?.('[data-icd-report-edit]');
        if (!reportButton) return;
        codeInput.value = reportButton.dataset.icdReportEdit || '';
        window.setTimeout(load, 180);
    });
    modal.addEventListener('hidden.bs.modal', () => {
        if (pendingReport && reportModal) {
            pendingReport = false;
            bootstrap.Modal.getOrCreateInstance(reportModal).show();
        } else if (changed) location.reload();
    });
    reportModal?.addEventListener('hidden.bs.modal', () => { if (changed) location.reload(); });
})();
