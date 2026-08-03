document.addEventListener('DOMContentLoaded', () => {
  const escapeHtml = value => String(value ?? '').replace(/[&<>"']/g, char => ({ '&': '&amp;', '<': '&lt;', '>': '&gt;', '"': '&quot;', "'": '&#39;' }[char]));

  function enhanceIcdInput(input, options = {}) {
    if (!input || input.dataset.icdAutocomplete === 'ready') return;
    input.dataset.icdAutocomplete = 'ready';
    input.autocomplete = 'off';
    input.placeholder = options.compact ? 'กรอง ICD-10 หรือชื่อโรค' : 'ค้นหารหัส ชื่อโรคไทย หรือชื่ออังกฤษ';

    const wrapper = document.createElement('div');
    wrapper.className = `icd-autocomplete catalog-icd-autocomplete${options.compact ? ' compact' : ''}`;
    if (input.classList.contains('mb-3')) {
      input.classList.remove('mb-3');
      wrapper.classList.add('mb-3');
    }
    input.parentNode.insertBefore(wrapper, input);
    wrapper.append(input);

    const results = document.createElement('div');
    results.className = 'icd-results';
    results.hidden = true;
    const selected = document.createElement('div');
    selected.className = 'catalog-icd-selected';
    selected.hidden = true;
    wrapper.append(results, selected);

    let timer = 0;
    let request = null;
    let activeIndex = -1;

    function closeResults() {
      results.hidden = true;
      activeIndex = -1;
    }

    function showSelected(item) {
      if (options.compact) {
        selected.hidden = true;
        selected.replaceChildren();
        return;
      }
      if (!item?.code) {
        selected.hidden = true;
        selected.replaceChildren();
        return;
      }
      selected.innerHTML = `<b>${escapeHtml(item.code)}</b><span>${escapeHtml(item.thaiName || 'ไม่พบชื่อโรคภาษาไทย')}</span><small>${escapeHtml(item.englishName || '')}</small>`;
      selected.hidden = false;
    }

    function choose(item) {
      input.value = item.code || '';
      showSelected(item);
      closeResults();
      input.dispatchEvent(new Event('change', { bubbles: true }));
      input.focus();
    }

    function highlight(index) {
      const buttons = [...results.querySelectorAll('.icd-result-item')];
      if (!buttons.length) return;
      activeIndex = Math.max(0, Math.min(index, buttons.length - 1));
      buttons.forEach((button, itemIndex) => button.classList.toggle('active', itemIndex === activeIndex));
      buttons[activeIndex].scrollIntoView({ block: 'nearest' });
    }

    async function search(term, exactOnly = false) {
      const query = (term || '').trim();
      if (!query) {
        closeResults();
        showSelected(null);
        return;
      }
      request?.abort();
      request = new AbortController();
      try {
        const response = await fetch(`/MedicalRecords/SearchIcd10?q=${encodeURIComponent(query)}&take=15`, { signal: request.signal });
        if (!response.ok) throw new Error(`HTTP ${response.status}`);
        const items = await response.json();
        const exact = items.find(item => String(item.code).toUpperCase() === query.replaceAll('.', '').toUpperCase());
        if (exactOnly) {
          showSelected(exact || null);
          return;
        }
        results.replaceChildren();
        if (!items.length) {
          const empty = document.createElement('div');
          empty.className = 'icd-result-empty';
          empty.textContent = 'ไม่พบรหัส ICD-10 ที่ตรงกัน';
          results.append(empty);
        } else {
          items.forEach(item => {
            const button = document.createElement('button');
            button.type = 'button';
            button.className = 'icd-result-item';
            button.innerHTML = `<strong>${escapeHtml(item.code)}</strong><span>${escapeHtml(item.thaiName || item.englishName)}</span><small>${escapeHtml(item.englishName || item.version || '')}</small>`;
            button.addEventListener('click', () => choose(item));
            results.append(button);
          });
        }
        results.hidden = false;
        activeIndex = -1;
      } catch (error) {
        if (error.name !== 'AbortError') closeResults();
      }
    }

    input.addEventListener('input', () => {
      showSelected(null);
      window.clearTimeout(timer);
      timer = window.setTimeout(() => search(input.value), 180);
    });
    input.addEventListener('focus', () => {
      if (input.value.trim() && selected.hidden) search(input.value);
    });
    input.addEventListener('blur', () => {
      window.setTimeout(() => search(input.value, true), 160);
    });
    input.addEventListener('keydown', event => {
      if (results.hidden && event.key === 'ArrowDown') {
        search(input.value);
        return;
      }
      const buttons = [...results.querySelectorAll('.icd-result-item')];
      if (event.key === 'ArrowDown' && buttons.length) { event.preventDefault(); highlight(activeIndex + 1); }
      else if (event.key === 'ArrowUp' && buttons.length) { event.preventDefault(); highlight(activeIndex <= 0 ? buttons.length - 1 : activeIndex - 1); }
      else if (event.key === 'Enter' && activeIndex >= 0) { event.preventDefault(); buttons[activeIndex].click(); }
      else if (event.key === 'Escape') closeResults();
    });

    const modal = input.closest('.modal');
    modal?.addEventListener('shown.bs.modal', () => {
      closeResults();
      showSelected(null);
      if (input.value.trim()) search(input.value, true);
    });
    modal?.addEventListener('hidden.bs.modal', closeResults);
  }

  enhanceIcdInput(document.querySelector('#adviceForm [name="Icd10Code"]'));
  enhanceIcdInput(document.querySelector('#protocolModal [name="Icd10Code"]'));
  enhanceIcdInput(document.querySelector('#icdDrugSelectionModal [name="Icd10Code"]'));
  enhanceIcdInput(document.querySelector('.drug-list-filter [name="icd"]'), { compact: true });
  document.addEventListener('click', event => {
    if (!event.target.closest('.catalog-icd-autocomplete')) document.querySelectorAll('.catalog-icd-autocomplete .icd-results').forEach(item => item.hidden = true);
  });
});
