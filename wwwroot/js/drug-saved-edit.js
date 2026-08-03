(() => {
    const modal = document.getElementById('drugDetailsModal');
    const form = document.getElementById('drugDetailsForm');
    const loading = document.getElementById('drugDetailsLoading');
    const content = document.getElementById('drugDetailsContent');
    const protocolsHost = document.getElementById('drugDetailsProtocols');
    const saveButton = document.getElementById('saveDrugDetailsButton');
    if (!modal || !form || !loading || !content || !protocolsHost) return;

    const escapeHtml = (value) => String(value ?? '').replace(/[&<>'"]/g, (character) => ({
        '&': '&amp;', '<': '&lt;', '>': '&gt;', "'": '&#39;', '"': '&quot;'
    })[character]);
    const selected = (actual, expected) => actual === expected ? ' selected' : '';
    const setValue = (name, value) => {
        const field = form.querySelector(`[name="${name}"]`);
        if (field) field.value = value ?? '';
    };
    const setChecked = (name, value) => {
        const field = form.querySelector(`[name="${name}"]`);
        if (field) field.checked = Boolean(value);
    };

    const protocolTemplate = (protocol, index, drugId) => `
        <article class="saved-protocol-card">
            <header>
                <span class="protocol-number">${index + 1}</span>
                <div class="protocol-badge-report">
                    <span class="protocol-icd-badge"><i>ICD‑10</i><strong>${escapeHtml(protocol.icd10Code)}</strong></span>
                    <span class="protocol-thai-badge"><i>โรคภาษาไทย</i><strong>${escapeHtml(protocol.diseaseName)}</strong></span>
                </div>
                <span class="protocol-state">เชื่อมโยงแล้ว</span>
            </header>
            <input type="hidden" name="Protocols[${index}].Id" value="${protocol.id}">
            <input type="hidden" name="Protocols[${index}].ClinicDrugId" value="${drugId}">
            <div class="saved-protocol-grid relation-only-grid">
                <label>รหัส ICD‑10<input class="form-control" name="Protocols[${index}].Icd10Code" value="${escapeHtml(protocol.icd10Code)}" required></label>
                <label>ประเภทการวินิจฉัย<select class="form-select" name="Protocols[${index}].DiagnosisType"><option value="Primary"${selected(protocol.diagnosisType, 'Primary')}>วินิจฉัยหลัก</option><option value="Differential"${selected(protocol.diagnosisType, 'Differential')}>วินิจฉัยแยกโรค</option><option value="Symptom"${selected(protocol.diagnosisType, 'Symptom')}>อาการเบื้องต้น</option></select></label>
                <label>ลำดับการแสดงผล<input type="number" class="form-control" name="Protocols[${index}].DisplayOrder" value="${protocol.displayOrder ?? 0}"></label>
            </div>
        </article>`;

    modal.addEventListener('show.bs.modal', async (event) => {
        const id = event.relatedTarget?.dataset.id;
        if (!id) return;
        form.reset();
        loading.className = 'saved-edit-loading';
        loading.textContent = 'กำลังโหลดข้อมูลรายการยา...';
        loading.hidden = false;
        content.hidden = true;
        saveButton.disabled = true;
        protocolsHost.innerHTML = '';
        try {
            const response = await fetch(`/DrugCatalog/EditDrugDetails?id=${encodeURIComponent(id)}`, { headers: { Accept: 'application/json' } });
            if (!response.ok) throw new Error(`HTTP ${response.status}`);
            const data = await response.json();
            setValue('Id', data.id);
            setValue('GenericName', data.genericName);
            setValue('TradeName', data.tradeName);
            setValue('ManufacturerName', data.manufacturerName);
            setValue('Strength', data.strength);
            setValue('DosageForm', data.dosageForm);
            setValue('Unit', data.unit);
            setValue('TmtCode', data.tmtCode);
            setValue('RegistrationNumber', data.registrationNumber);
            setValue('Source', data.source);
            setValue('DoseAmount', data.doseAmount);
            setValue('FrequencyPerDay', data.frequencyPerDay);
            setValue('MealTiming', data.mealTiming);
            setValue('IntervalHours', data.intervalHours);
            setValue('UsageText', data.usageText);
            setValue('AdviceText', data.adviceText);
            setChecked('Morning', data.morning);
            setChecked('Noon', data.noon);
            setChecked('Evening', data.evening);
            setChecked('Bedtime', data.bedtime);
            document.getElementById('drugDetailsStatus').textContent = data.approvalStatus || 'Approved';
            const protocols = data.protocols || [];
            document.getElementById('drugDetailsProtocolCount').textContent = `${protocols.length} รายการ`;
            protocolsHost.innerHTML = protocols.length
                ? protocols.map((protocol, index) => protocolTemplate(protocol, index, data.id)).join('')
                : '<div class="saved-protocol-empty"><span>⚕</span><strong>ยังไม่มีวิธีใช้ที่เชื่อมกับ ICD‑10</strong><small>ปิดหน้าต่างนี้แล้วเลือก “ผูก ICD‑10” เพื่อเพิ่มข้อมูล</small></div>';
            loading.hidden = true;
            content.hidden = false;
            saveButton.disabled = false;
        } catch (error) {
            loading.className = 'saved-edit-loading error';
            loading.textContent = 'ไม่สามารถโหลดข้อมูลรายการยาได้ กรุณาปิดหน้าต่างแล้วลองใหม่';
        }
    });

    form.addEventListener('submit', () => {
        saveButton.disabled = true;
        saveButton.textContent = 'กำลังบันทึก...';
    });
    modal.addEventListener('hidden.bs.modal', () => {
        saveButton.disabled = false;
        saveButton.textContent = 'บันทึกการแก้ไข';
    });
})();
