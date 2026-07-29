window.smartClinicStatusModal = (function () {
	function getModalElement() {
		return document.getElementById('statusModal');
	}

	function setThemeClass(header, type) {
		header.className = 'modal-header text-white smartclinic-status-header';

		const typeMap = {
			success: 'bg-success',
			warning: 'bg-warning text-dark',
			danger: 'bg-danger',
			info: 'bg-info',
			primary: 'bg-primary'
		};

		const classes = (typeMap[type] ?? typeMap.info).split(' ').filter(Boolean);
		header.classList.add(...classes);
	}

	function renderList(listElement, items) {
		listElement.innerHTML = '';
		(items || []).forEach((item) => {
			const li = document.createElement('li');
			li.className = 'list-group-item d-flex justify-content-between align-items-start px-0';
			li.innerHTML = `<div class="me-3"><div class="fw-semibold">${item.title}</div><div class="small text-body-secondary">${item.detail}</div></div><span class="badge text-bg-light">${item.time}</span>`;
			listElement.appendChild(li);
		});
	}

	function show(options) {
		const modalElement = getModalElement();
		if (!modalElement) {
			return;
		}

		const header = document.getElementById('statusModalHeader');
		const subtitle = document.getElementById('statusModalSubtitle');
		const state = document.getElementById('statusModalState');
		const stateDescription = document.getElementById('statusModalStateDescription');
		const message = document.getElementById('statusModalMessage');
		const meta = document.getElementById('statusModalMeta');
		const list = document.getElementById('statusModalList');
		const action = document.getElementById('statusModalAction');

		setThemeClass(header, options?.type);
		subtitle.textContent = options?.subtitle || 'Status overview';
		state.textContent = options?.state || 'In progress';
		stateDescription.textContent = options?.stateDescription || 'กำลังดำเนินการ';
		message.textContent = options?.message || '-';
		meta.textContent = options?.meta || '-';
		renderList(list, options?.items || []);
		action.textContent = options?.actionText || 'Acknowledge';
		action.onclick = options?.onAction || function () {
			const instance = bootstrap.Modal.getInstance(modalElement);
			if (instance) {
				instance.hide();
			}
		};

		const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
		modal.show();
	}

	return {
		show: show
	};
})();

document.addEventListener('click', function (event) {
	const trigger = event.target.closest('[data-status-modal]');
	if (!trigger) {
		return;
	}

	const payload = trigger.getAttribute('data-status-modal');
	if (!payload) {
		return;
	}

	try {
		smartClinicStatusModal.show(JSON.parse(payload));
	} catch (error) {
		console.error('Invalid status modal payload', error);
	}
});

document.addEventListener('click', function (event) {
	const trigger = event.target.closest('[data-patient-fill]');
	if (!trigger) {
		return;
	}

	const payload = trigger.getAttribute('data-patient-fill');
	if (!payload) {
		return;
	}

	try {
		const data = JSON.parse(payload);
		const patientCitizenId = document.getElementById('patientCitizenId');
		const patientFullName = document.getElementById('patientFullName');
		const patientAddress = document.getElementById('patientAddress');
		const patientPhoneNumber = document.getElementById('patientPhoneNumber');
		const patientBirthDate = document.getElementById('patientBirthDate');
		const patientGender = document.getElementById('patientGender');

		if (patientCitizenId) patientCitizenId.value = data.citizenId || '';
		if (patientFullName) patientFullName.value = data.fullName || '';
		if (patientAddress) patientAddress.value = data.address || '';
		if (patientPhoneNumber) patientPhoneNumber.value = data.phoneNumber || '';
		if (patientBirthDate) patientBirthDate.value = data.birthDate || '';
		if (patientGender) patientGender.value = data.gender || 'ไม่ระบุ';

		smartClinicStatusModal.show({
			type: 'success',
			subtitle: 'Smart card bridge simulation',
			state: 'Card data loaded',
			stateDescription: 'เติมข้อมูลลงฟอร์มแล้ว',
			message: 'ระบบจำลองอ่านบัตรประชาชนได้และกรอกข้อมูลผู้ป่วยลงฟอร์มเรียบร้อย',
			meta: 'Use this pattern as the client bridge contract',
			actionText: 'Great',
			items: [
				{ title: 'Citizen ID', detail: data.citizenId || '-', time: 'Loaded' },
				{ title: 'Full name', detail: data.fullName || '-', time: 'Loaded' },
				{ title: 'Address', detail: data.address || '-', time: 'Loaded' }
			]
		});
	} catch (error) {
		console.error('Invalid patient fill payload', error);
	}
});

function getSmartClinicReadSession() {
	return window.smartClinicReadSession || (window.smartClinicReadSession = {
		webSocket: null,
		cancelled: false,
		fallbackController: null,
		inProgress: false,
		statusPollHandle: null
	});
}

function updateReaderConnectionStatus(payload) {
	const badge = document.getElementById('smartCardReaderStatus');
	const detail = document.getElementById('smartCardReaderDetail');
	const dot = document.getElementById('readerDot');

	const hasReader = payload && payload.hasReader !== false;
	const hasCardInserted = payload && payload.hasCardInserted === true;

	const setDot = function(state) {
		if (!dot) return;
		dot.className = 'reader-dot ' + state;
	};

	if (!badge) return;

	badge.className = '';
	if (!payload || payload.success === false) {
		setDot('error');
		badge.textContent = 'Bridge ไม่พร้อม';
		if (detail) detail.textContent = payload?.statusText || payload?.error || 'ไม่สามารถอ่านสถานะได้';
		return;
	}

	if (!hasReader) {
		setDot('disconnected');
		badge.textContent = 'ไม่พบเครื่องอ่าน';
		if (detail) detail.textContent = payload.statusText || '-';
		return;
	}

	if (hasCardInserted) {
		setDot('connected');
		badge.textContent = 'เสียบบัตรแล้ว';
	} else {
		setDot('disconnected');
		badge.textContent = 'ถอดบัตรอยู่';
	}

	const readers = Array.isArray(payload.readers) ? payload.readers : [];
	if (readers.length > 0) {
		const primary = readers[0];
		if (detail) detail.textContent = `${primary.readerName || '-'} | ${primary.stateText || '-'}`;
	} else {
		if (detail) detail.textContent = payload.statusText || '-';
	}
}

async function fetchReaderStatus() {
	try {
		const response = await fetch('/api/smartcard/reader-status');
		const payload = await response.json();
		updateReaderConnectionStatus(payload);
	} catch (error) {
		updateReaderConnectionStatus({
			success: false,
			statusText: 'ไม่สามารถดึงสถานะเครื่องอ่านได้',
			error: error.message
		});
	}
}

function startReaderStatusPolling() {
	const badge = document.getElementById('smartCardReaderStatus');
	if (!badge) {
		return;
	}

	const session = getSmartClinicReadSession();
	if (session.statusPollHandle) {
		return;
	}

	fetchReaderStatus();
	session.statusPollHandle = window.setInterval(fetchReaderStatus, 2500);
}

document.addEventListener('DOMContentLoaded', function () {
	startReaderStatusPolling();
});

function updateSmartCardPreview(data, statusText) {
	const setText = function (id, value) {
		const element = document.getElementById(id);
		if (element) {
			element.textContent = value && String(value).trim().length > 0 ? String(value) : '-';
		}
	};

	// New ID-card style panel (Index.cshtml redesign)
	setText('idCardCitizenId', data?.citizenId || '');
	setText('idCardThaiName', data?.thaiFullName || data?.fullName || '');
	setText('idCardEnglishName', data?.englishFullName || '');
	setText('idCardBirthDate', data?.birthDate || '');
	setText('idCardGender', data?.gender || '');
	setText('idCardIssueDate', data?.issueDate || '');
	setText('idCardExpiryDate', data?.expiryDate || '');
	setText('idCardIssuer', data?.issuer || '');
	setText('idCardAddress', data?.address || '');

	const sourceBadge = document.getElementById('cardPreviewSource');
	if (sourceBadge) {
		sourceBadge.textContent = data?.source ? data.source : 'ยังไม่อ่านข้อมูล';
	}
}

function getSmartCardPhotoPayload(data) {
	const rawPhoto = data?.photoBase64 || data?.photo || data?.imageBase64 || data?.photoData;
	if (!rawPhoto) {
		return null;
	}

	if (Array.isArray(rawPhoto)) {
		let binary = '';
		rawPhoto.forEach((value) => {
			binary += String.fromCharCode(Number(value) & 0xff);
		});
		const base64 = btoa(binary);
		return {
			base64: base64,
			src: `data:image/jpeg;base64,${base64}`
		};
	}

	const photoText = String(rawPhoto).trim();
	if (photoText.length <= 100) {
		return null;
	}

	const dataUrlMatch = photoText.match(/^data:image\/[a-z0-9.+-]+;base64,(.+)$/i);
	if (dataUrlMatch) {
		return {
			base64: dataUrlMatch[1],
			src: photoText
		};
	}

	return {
		base64: photoText,
		src: `data:image/jpeg;base64,${photoText}`
	};
}

function setPatientPhotoFromPayload(photoPayload, sourceLabel) {
	const photoBase64Input = document.getElementById('patientPhotoBase64Input');
	const patientPhotoPreview = document.getElementById('patientPhotoPreview');
	const photoPlaceholder = document.getElementById('photoPlaceholderIcon');
	const photoSourceLabel = document.getElementById('photoSourceLabel');
	const idCardThumb = document.getElementById('idCardPhotoThumb');
	const idCardPlaceholder = document.getElementById('idCardPhotoPlaceholder');

	if (!photoPayload) {
		if (photoBase64Input) photoBase64Input.value = '';
		if (patientPhotoPreview) {
			patientPhotoPreview.removeAttribute('src');
			patientPhotoPreview.style.display = 'none';
		}
		if (photoPlaceholder) photoPlaceholder.style.display = '';
		if (photoSourceLabel) photoSourceLabel.textContent = '-';
		if (idCardThumb) {
			idCardThumb.removeAttribute('src');
			idCardThumb.style.display = 'none';
		}
		if (idCardPlaceholder) idCardPlaceholder.style.display = '';
		return;
	}

	if (photoBase64Input) photoBase64Input.value = photoPayload.base64;
	if (patientPhotoPreview) {
		patientPhotoPreview.src = photoPayload.src;
		patientPhotoPreview.style.display = 'block';
	}
	if (photoPlaceholder) photoPlaceholder.style.display = 'none';
	if (photoSourceLabel) photoSourceLabel.textContent = sourceLabel || 'บัตรประชาชน';
	if (idCardThumb) {
		idCardThumb.src = photoPayload.src;
		idCardThumb.style.display = 'block';
	}
	if (idCardPlaceholder) idCardPlaceholder.style.display = 'none';
}

function setEditPhotoFromPayload(photoPayload) {
	const photoBase64Input = document.getElementById('editPhotoBase64Input');
	const photoFile = document.getElementById('editPhotoFile');
	const preview = document.getElementById('editPhotoPreview');
	const placeholder = document.getElementById('editPhotoPlaceholder');
	const sourceLabel = document.getElementById('editPhotoSourceLabel');

	if (!photoPayload) {
		if (photoBase64Input) photoBase64Input.value = '';
		return;
	}

	if (photoBase64Input) photoBase64Input.value = photoPayload.base64;
	if (photoFile) photoFile.value = '';
	if (preview) {
		preview.src = photoPayload.src;
		preview.style.display = 'block';
	}
	if (placeholder) placeholder.style.display = 'none';
	if (sourceLabel) sourceLabel.textContent = 'รูปจากบัตรประชาชน';
}

function fillPatientFormFromPdf(data) {
	const fieldMap = {
		patientCitizenId: data?.citizenId || '',
		patientFullName: data?.fullName || '',
		patientAddress: data?.address || '',
		patientPhoneNumber: data?.phoneNumber || '',
		patientBirthDate: data?.birthDate || ''
	};

	Object.entries(fieldMap).forEach(([id, value]) => {
		const element = document.getElementById(id);
		if (element) {
			element.value = value;
		}
	});

	const patientGender = document.getElementById('patientGender');
	if (patientGender) {
		patientGender.value = data?.gender || 'ไม่ระบุ';
	}

	const photoFile = document.getElementById('patientPhotoFile');
	if (photoFile) {
		photoFile.value = '';
	}
	setPatientPhotoFromPayload(null);

	const cardPreviewPanel = document.getElementById('cardPreviewPanel');
	if (cardPreviewPanel) cardPreviewPanel.style.display = 'none';
}

document.addEventListener('click', async function (event) {
	const trigger = event.target.closest('[data-pdf-import]');
	if (!trigger) {
		return;
	}

	const pdfInput = document.getElementById('patientPdfFile');
	const token = document.querySelector('input[name="__RequestVerificationToken"]');
	const file = pdfInput?.files?.[0];
	if (!file) {
		smartClinicStatusModal.show({
			type: 'warning',
			subtitle: 'PDF import',
			state: 'No file',
			stateDescription: 'ยังไม่ได้เลือกไฟล์',
			message: 'กรุณาเลือกไฟล์ PDF เวชระเบียนก่อนอ่านข้อมูล',
			meta: 'รองรับไฟล์ PDF เท่านั้น',
			actionText: 'ปิด'
		});
		return;
	}

	const formData = new FormData();
	formData.append('medicalRecordPdf', file);

	trigger.disabled = true;
	try {
		const response = await fetch('/Patients/ImportPdf', {
			method: 'POST',
			headers: token ? { RequestVerificationToken: token.value } : {},
			body: formData
		});
		const data = await response.json();

		if (!response.ok || !data.success) {
			throw new Error(data.error || `HTTP ${response.status}`);
		}

		fillPatientFormFromPdf(data);
		smartClinicStatusModal.show({
			type: 'success',
			subtitle: 'PDF import',
			state: 'Completed',
			stateDescription: 'อ่านข้อมูลเวชระเบียนสำเร็จ',
			message: 'ระบบเติมข้อมูลผู้ป่วยจาก PDF แล้ว โดยเว้นข้อมูลรูปภาพไว้',
			meta: `Source: ${data.source || 'medical-record-pdf'}`,
			actionText: 'ตกลง',
			items: [
				{ title: 'Citizen ID', detail: data.citizenId || '-', time: 'Loaded' },
				{ title: 'ชื่อ-นามสกุล', detail: data.fullName || '-', time: 'Loaded' },
				{ title: 'วันเกิด', detail: data.birthDate || '-', time: 'Loaded' },
				{ title: 'เบอร์โทร', detail: data.phoneNumber || '-', time: 'Loaded' }
			]
		});
	} catch (error) {
		smartClinicStatusModal.show({
			type: 'danger',
			subtitle: 'PDF import',
			state: 'Failed',
			stateDescription: 'อ่าน PDF ไม่สำเร็จ',
			message: error.message,
			meta: 'ตรวจสอบว่าเป็นไฟล์เวชระเบียนรูปแบบที่ระบบรองรับ',
			actionText: 'ปิด'
		});
	} finally {
		trigger.disabled = false;
	}
});

document.addEventListener('click', async function (event) {
	const trigger = event.target.closest('[data-smartcard-edit-photo-read]');
	if (!trigger) {
		return;
	}

	const session = getSmartClinicReadSession();
	if (session.inProgress) {
		smartClinicStatusModal.show({
			type: 'warning',
			subtitle: 'Smart card photo',
			state: 'Busy',
			stateDescription: 'กำลังเชื่อมต่ออยู่',
			message: 'ระบบกำลังอ่านข้อมูลอยู่แล้ว กรุณารอสักครู่',
			meta: 'มีคำขออ่านบัตรที่กำลังทำงานอยู่',
			actionText: 'ปิด'
		});
		return;
	}

	const citizenId = trigger.getAttribute('data-citizen-id') || '';
	session.inProgress = true;
	session.cancelled = false;

	smartClinicStatusModal.show({
		type: 'info',
		subtitle: 'Smart card photo',
		state: 'Reading',
		stateDescription: 'กำลังอ่านรูปจากบัตรประชาชน',
		message: 'ระบบจะอัปเดตเฉพาะรูปภาพจากบัตรประชาชน ไม่เปลี่ยนข้อมูลผู้ป่วยช่องอื่น',
		meta: citizenId ? `Citizen ID: ${citizenId}` : 'อ่านรูปจากบัตรโดยตรง',
		actionText: 'ปิด'
	});

	try {
		const ws = new WebSocket('ws://localhost:9999/card');
		session.webSocket = ws;

		const finalize = function () {
			session.inProgress = false;
			session.webSocket = null;
			session.fallbackController = null;
		};

		ws.onopen = function () {
			fetchReaderStatus();
			ws.send(JSON.stringify({ citizenId: /^\d{13}$/.test(citizenId) ? citizenId : null }));
		};

		ws.onmessage = function (wsEvent) {
			try {
				const data = JSON.parse(wsEvent.data);
				const photoPayload = data.success ? getSmartCardPhotoPayload(data) : null;
				if (!photoPayload) {
					throw new Error(data.error || 'ไม่พบข้อมูลรูปภาพจากบัตรประชาชน');
				}

				setEditPhotoFromPayload(photoPayload);
				smartClinicStatusModal.show({
					type: 'success',
					subtitle: 'Smart card photo',
					state: 'Completed',
					stateDescription: 'อ่านรูปจากบัตรสำเร็จ',
					message: 'ระบบเตรียมอัปเดตเฉพาะรูปภาพจากบัตรแล้ว กดบันทึกการแก้ไขเพื่อจัดเก็บ',
					meta: `Source: ${data.source || 'smartcard-reader'}`,
					actionText: 'ตกลง'
				});
			} catch (error) {
				smartClinicStatusModal.show({
					type: 'warning',
					subtitle: 'Smart card photo',
					state: 'No photo',
					stateDescription: 'ยังไม่ได้รูปจากบัตร',
					message: error.message,
					meta: 'ตรวจสอบว่าเสียบบัตรแล้วและ Bridge พร้อมใช้งาน',
					actionText: 'ปิด'
				});
			} finally {
				ws.close();
				fetchReaderStatus();
				finalize();
			}
		};

		ws.onerror = function () {
			smartClinicStatusModal.show({
				type: 'danger',
				subtitle: 'Smart card photo',
				state: 'Connection failed',
				stateDescription: 'เชื่อมต่อ Bridge ไม่สำเร็จ',
				message: 'ไม่สามารถเชื่อมต่อ ws://localhost:9999/card ได้',
				meta: 'ตรวจสอบว่าเปิด SmartClinic Card Reader Bridge แล้ว',
				actionText: 'ปิด'
			});
			finalize();
		};
	} catch (error) {
		session.inProgress = false;
		smartClinicStatusModal.show({
			type: 'danger',
			subtitle: 'Smart card photo',
			state: 'Failed',
			stateDescription: 'อ่านรูปจากบัตรไม่สำเร็จ',
			message: error.message,
			meta: 'ตรวจสอบ Bridge และเครื่องอ่านบัตร',
			actionText: 'ปิด'
		});
	}
});

document.addEventListener('click', async function (event) {
	const trigger = event.target.closest('[data-smartcard-photo-read]');
	if (!trigger) {
		return;
	}

	const session = getSmartClinicReadSession();
	if (session.inProgress) {
		smartClinicStatusModal.show({
			type: 'warning',
			subtitle: 'Smart card photo',
			state: 'Busy',
			stateDescription: 'กำลังเชื่อมต่ออยู่',
			message: 'ระบบกำลังอ่านข้อมูลอยู่แล้ว กรุณารอสักครู่',
			meta: 'มีคำขออ่านบัตรที่กำลังทำงานอยู่',
			actionText: 'ปิด'
		});
		return;
	}

	const patientCitizenId = document.getElementById('patientCitizenId');
	const enteredCitizenId = patientCitizenId ? patientCitizenId.value.trim() : '';
	const hasValidCitizenId = /^\d{13}$/.test(enteredCitizenId);

	session.inProgress = true;
	session.cancelled = false;

	smartClinicStatusModal.show({
		type: 'info',
		subtitle: 'Smart card photo',
		state: 'Reading',
		stateDescription: 'กำลังอ่านรูปจากบัตรประชาชน',
		message: 'ระบบจะนำมาใช้เฉพาะรูปภาพจากบัตร และไม่เปลี่ยนข้อมูลผู้ป่วยที่อ่านจาก PDF',
		meta: hasValidCitizenId ? `Citizen ID: ${enteredCitizenId}` : 'อ่านรูปจากบัตรโดยตรง',
		actionText: 'ปิด'
	});

	try {
		const ws = new WebSocket('ws://localhost:9999/card');
		session.webSocket = ws;

		const finalize = function () {
			session.inProgress = false;
			session.webSocket = null;
			session.fallbackController = null;
		};

		ws.onopen = function () {
			fetchReaderStatus();
			ws.send(JSON.stringify({ citizenId: hasValidCitizenId ? enteredCitizenId : null }));
		};

		ws.onmessage = function (wsEvent) {
			try {
				const data = JSON.parse(wsEvent.data);
				const photoPayload = data.success ? getSmartCardPhotoPayload(data) : null;
				if (!photoPayload) {
					throw new Error(data.error || 'ไม่พบข้อมูลรูปภาพจากบัตรประชาชน');
				}

				setPatientPhotoFromPayload(photoPayload, 'บัตรประชาชน');
				smartClinicStatusModal.show({
					type: 'success',
					subtitle: 'Smart card photo',
					state: 'Completed',
					stateDescription: 'อ่านรูปจากบัตรสำเร็จ',
					message: 'ระบบอัปโหลดเฉพาะรูปภาพจากบัตรเข้าสู่ฟอร์มแล้ว',
					meta: `Source: ${data.source || 'smartcard-reader'}`,
					actionText: 'ตกลง'
				});
			} catch (error) {
				smartClinicStatusModal.show({
					type: 'warning',
					subtitle: 'Smart card photo',
					state: 'No photo',
					stateDescription: 'ยังไม่ได้รูปจากบัตร',
					message: error.message,
					meta: 'ตรวจสอบว่าเสียบบัตรแล้วและ Bridge พร้อมใช้งาน',
					actionText: 'ปิด'
				});
			} finally {
				ws.close();
				fetchReaderStatus();
				finalize();
			}
		};

		ws.onerror = function () {
			smartClinicStatusModal.show({
				type: 'danger',
				subtitle: 'Smart card photo',
				state: 'Connection failed',
				stateDescription: 'เชื่อมต่อ Bridge ไม่สำเร็จ',
				message: 'ไม่สามารถเชื่อมต่อ ws://localhost:9999/card ได้',
				meta: 'ตรวจสอบว่าเปิด SmartClinic Card Reader Bridge แล้ว',
				actionText: 'ปิด'
			});
			finalize();
		};
	} catch (error) {
		session.inProgress = false;
		smartClinicStatusModal.show({
			type: 'danger',
			subtitle: 'Smart card photo',
			state: 'Failed',
			stateDescription: 'อ่านรูปจากบัตรไม่สำเร็จ',
			message: error.message,
			meta: 'ตรวจสอบ Bridge และเครื่องอ่านบัตร',
			actionText: 'ปิด'
		});
	}
});

async function restartSmartCardBridgeSession() {
	const session = getSmartClinicReadSession();
	session.cancelled = true;

	if (session.fallbackController) {
		session.fallbackController.abort();
		session.fallbackController = null;
	}

	if (session.webSocket && (session.webSocket.readyState === WebSocket.OPEN || session.webSocket.readyState === WebSocket.CONNECTING)) {
		session.webSocket.close(1000, 'User cancelled');
	}

	const response = await fetch('/api/smartcard/restart-bridge', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		}
	});
	const payload = await response.json();

	if (!response.ok || !payload.success) {
		throw new Error(payload.error || payload.message || `HTTP ${response.status}`);
	}

	session.inProgress = false;
	session.webSocket = null;
	session.cancelled = false;

	return payload;
}

document.addEventListener('click', async function (event) {
	const trigger = event.target.closest('[data-smartcard-restart]');
	if (!trigger) {
		return;
	}

	try {
		const payload = await restartSmartCardBridgeSession();
		updateSmartCardPreview(null, 'รีสตาร์ท Bridge แล้ว พร้อมอ่านบัตรใหม่');
		fetchReaderStatus();
		smartClinicStatusModal.show({
			type: 'success',
			subtitle: 'Smart card bridge',
			state: 'Restarted',
			stateDescription: 'ตัดการเชื่อมต่อและรีสตาร์ทสำเร็จ',
			message: payload.message || 'Bridge พร้อมใช้งานแล้ว',
			meta: `Port: ${payload.port || 9999}`,
			actionText: 'ตกลง'
		});
	} catch (restartError) {
		smartClinicStatusModal.show({
			type: 'danger',
			subtitle: 'Smart card bridge',
			state: 'Restart failed',
			stateDescription: 'รีสตาร์ทพอร์ตไม่สำเร็จ',
			message: `ไม่สามารถรีสตาร์ทพอร์ตได้: ${restartError.message}`,
			meta: 'ตรวจสอบสิทธิ์การรันแอปและการติดตั้ง Bridge ที่ C:/Program Files/SmartClinic/CardReader',
			actionText: 'ปิด'
		});
	}
});

document.addEventListener('click', async function (event) {
	const trigger = event.target.closest('[data-smartcard-read]');
	if (!trigger) {
		return;
	}

	const session = getSmartClinicReadSession();

	if (session.inProgress) {
		smartClinicStatusModal.show({
			type: 'warning',
			subtitle: 'Smart card read',
			state: 'Busy',
			stateDescription: 'กำลังเชื่อมต่ออยู่',
			message: 'ระบบกำลังอ่านข้อมูลอยู่แล้ว กรุณารอสักครู่หรือตัดการเชื่อมต่อก่อน',
			meta: 'มีคำขออ่านบัตรที่กำลังทำงานอยู่',
			actionText: 'ปิด'
		});
		return;
	}

	const patientCitizenId = document.getElementById('patientCitizenId');
	const enteredCitizenId = patientCitizenId ? patientCitizenId.value.trim() : '';
	const hasValidCitizenId = /^\d{13}$/.test(enteredCitizenId);

	const closeActiveConnection = function () {
		session.cancelled = true;
		if (session.fallbackController) {
			session.fallbackController.abort();
			session.fallbackController = null;
		}
		if (session.webSocket && session.webSocket.readyState === WebSocket.OPEN) {
			session.webSocket.close(1000, 'User cancelled');
		}
	};

	const restartBridgePort = async function () {
		closeActiveConnection();
		try {
			const payload = await restartSmartCardBridgeSession();
			fetchReaderStatus();

			smartClinicStatusModal.show({
				type: 'success',
				subtitle: 'Smart card bridge',
				state: 'Restarted',
				stateDescription: 'รีสตาร์ทพอร์ตสำเร็จ',
				message: payload.message || 'Bridge พร้อมใช้งานแล้ว',
				meta: `Port: ${payload.port || 9999}`,
				actionText: 'ตกลง'
			});
		} catch (restartError) {
			smartClinicStatusModal.show({
				type: 'danger',
				subtitle: 'Smart card bridge',
				state: 'Restart failed',
				stateDescription: 'รีสตาร์ทพอร์ตไม่สำเร็จ',
				message: `ไม่สามารถรีสตาร์ทพอร์ตได้: ${restartError.message}`,
				meta: 'ตรวจสอบสิทธิ์การรันแอปและการติดตั้ง Bridge ที่ C:/Program Files/SmartClinic/CardReader',
				actionText: 'ปิด'
			});
		}
	};

	const fillPatientForm = function (data) {
		const patientFullName = document.getElementById('patientFullName');
		const patientAddress = document.getElementById('patientAddress');
		const patientPhoneNumber = document.getElementById('patientPhoneNumber');
		const patientBirthDate = document.getElementById('patientBirthDate');
		const patientGender = document.getElementById('patientGender');

		if (patientCitizenId) patientCitizenId.value = data.citizenId || '';
		if (patientFullName) patientFullName.value = data.fullName || data.thaiFullName || data.englishFullName || '';
		if (patientAddress) patientAddress.value = data.address || '';
		if (patientPhoneNumber) patientPhoneNumber.value = data.phoneNumber || '';
		if (patientBirthDate) patientBirthDate.value = data.birthDate || '';
		if (patientGender) patientGender.value = data.gender || 'ไม่ระบุ';

		// Photo from card
		const photoPayload = getSmartCardPhotoPayload(data);
		const photoBase64Input = document.getElementById('patientPhotoBase64Input');
		const patientPhotoPreview = document.getElementById('patientPhotoPreview');
		const photoPlaceholder = document.getElementById('photoPlaceholderIcon');
		const photoSourceLabel = document.getElementById('photoSourceLabel');
		if (photoPayload) {
			if (photoBase64Input) photoBase64Input.value = photoPayload.base64;
			if (patientPhotoPreview) { patientPhotoPreview.src = photoPayload.src; patientPhotoPreview.style.display = 'block'; }
			if (photoPlaceholder) photoPlaceholder.style.display = 'none';
			if (photoSourceLabel) photoSourceLabel.textContent = 'บัตรประชาชน';
			// ID card thumbnail
			const idCardThumb = document.getElementById('idCardPhotoThumb');
			const idCardPlaceholder = document.getElementById('idCardPhotoPlaceholder');
			if (idCardThumb) { idCardThumb.src = photoPayload.src; idCardThumb.style.display = 'block'; }
			if (idCardPlaceholder) idCardPlaceholder.style.display = 'none';
		}

		// ID card preview panel fields
		const cardPreviewPanel = document.getElementById('cardPreviewPanel');
		if (cardPreviewPanel) cardPreviewPanel.style.display = '';
		const setText = function(id, val) { var el = document.getElementById(id); if (el) el.textContent = val || '-'; };
		setText('idCardThaiName', data.thaiFullName || data.fullName);
		setText('idCardEnglishName', data.englishFullName);
		setText('idCardCitizenId', data.citizenId);
		setText('idCardBirthDate', data.birthDate);
		setText('idCardGender', data.gender);
		setText('idCardIssueDate', data.issueDate);
		setText('idCardExpiryDate', data.expiryDate);
		setText('idCardAddress', data.address);
		setText('idCardIssuer', data.issuer);
		setText('cardPreviewSource', data.source);
	};

	const buildItems = function (data) {
		const fields = [
			{ title: 'Citizen ID', value: data.citizenId },
			{ title: 'ชื่อ-นามสกุล (ไทย)', value: data.thaiFullName || data.fullName },
			{ title: 'ชื่อ-นามสกุล (อังกฤษ)', value: data.englishFullName },
			{ title: 'วันเกิด', value: data.birthDate },
			{ title: 'เพศ', value: data.gender },
			{ title: 'ที่อยู่', value: data.address },
			{ title: 'วันออกบัตร', value: data.issueDate },
			{ title: 'วันหมดอายุ', value: data.expiryDate },
			{ title: 'หน่วยงานผู้ออกบัตร', value: data.issuer },
			{ title: 'Reader', value: data.readerName },
			{ title: 'Source', value: data.source }
		];

		return fields
			.filter((field) => field.value && String(field.value).trim().length > 0)
			.map((field) => ({ title: field.title, detail: String(field.value), time: 'Loaded' }));
	};

	const tryDatabaseFallback = async function (citizenId) {
		session.fallbackController = new AbortController();
		const response = await fetch(`/api/smartcard/read?citizenId=${encodeURIComponent(citizenId)}`, {
			signal: session.fallbackController.signal
		});
		const data = await response.json();
		if (!response.ok || !data.success) {
			throw new Error(data.message || data.error || `API returned ${response.status}`);
		}
		return data;
	};

	session.inProgress = true;
	session.cancelled = false;

	smartClinicStatusModal.show({
		type: 'info',
		subtitle: 'Smart card bridge',
		state: 'Connecting',
		stateDescription: 'กำลังเชื่อมต่อเครื่องอ่านบัตร',
		message: hasValidCitizenId
			? 'ระบบจะอ่านข้อมูลจากบัตรก่อน หากอ่านไม่ได้จะค้นหาจากฐานข้อมูลด้วยเลขบัตรที่กรอก'
			: 'ระบบจะอ่านข้อมูลจากบัตรประชาชนโดยตรงก่อน (ยังไม่บันทึกฐานข้อมูล) หากต้องการ fallback ให้กรอกเลขบัตร 13 หลัก',
		meta: hasValidCitizenId ? `Fallback citizenId: ${enteredCitizenId}` : 'โหมดอ่านจากบัตรโดยตรง',
		actionText: 'ตัดการเชื่อมต่อและรีสตาร์ทพอร์ต',
		onAction: restartBridgePort
	});

	try {
		const ws = new WebSocket('ws://localhost:9999/card');
		session.webSocket = ws;

		const finalize = function () {
			session.inProgress = false;
			session.webSocket = null;
			session.fallbackController = null;
		};

		ws.onopen = function () {
			if (session.cancelled) {
				ws.close();
				return;
			}
			fetchReaderStatus();
			ws.send(JSON.stringify({ citizenId: hasValidCitizenId ? enteredCitizenId : null }));
		};

		ws.onmessage = async function (wsEvent) {
			if (session.cancelled) {
				ws.close();
				finalize();
				return;
			}

			try {
				const data = JSON.parse(wsEvent.data);
				if (data.success) {
					fillPatientForm(data);
					updateSmartCardPreview(data, 'อ่านข้อมูลจากบัตรแล้ว กรุณาตรวจสอบก่อนกดบันทึก');
					smartClinicStatusModal.show({
						type: 'success',
						subtitle: 'Smart card read',
						state: 'Completed',
						stateDescription: 'อ่านข้อมูลบัตรสำเร็จ (ยังไม่บันทึกฐานข้อมูล)',
						message: 'ระบบแสดงข้อมูลจากบัตรในฟอร์มครบแล้ว กรุณาตรวจสอบก่อนกดบันทึกผู้ป่วย',
						meta: `Source: ${data.source || 'smartcard-reader'} | Bridge: ws://localhost:9999/card`,
						actionText: 'พร้อมบันทึก',
						items: buildItems(data)
					});
					ws.close();
					fetchReaderStatus();
					finalize();
					return;
				}

				if (hasValidCitizenId) {
					const fallbackData = await tryDatabaseFallback(enteredCitizenId);
					fillPatientForm(fallbackData);
					updateSmartCardPreview(fallbackData, 'ใช้ข้อมูลจากฐานข้อมูลแทนบัตร กรุณาตรวจสอบก่อนกดบันทึก');
					smartClinicStatusModal.show({
						type: 'success',
						subtitle: 'Smart card read (Database fallback)',
						state: 'Completed',
						stateDescription: 'อ่านข้อมูลจากฐานข้อมูลสำเร็จ (ยังไม่บันทึกฐานข้อมูล)',
						message: 'ไม่สามารถอ่านบัตรได้ แต่ระบบแสดงข้อมูลผู้ป่วยจากฐานข้อมูลในฟอร์มแล้ว',
						meta: `Source: ${fallbackData.source || 'database'} | API: /api/smartcard/read`,
						actionText: 'พร้อมบันทึก',
						items: buildItems(fallbackData)
					});
					ws.close();
					fetchReaderStatus();
					finalize();
					return;
				}

				smartClinicStatusModal.show({
					type: 'warning',
					subtitle: 'Smart card read',
					state: 'No data',
					stateDescription: 'ยังไม่พบข้อมูลจากบัตร',
					message: data.error || 'ไม่พบบัตรหรืออ่านบัตรไม่สำเร็จ',
					meta: 'ลองเสียบบัตรใหม่ หรือกรอกเลขบัตร 13 หลักเพื่อใช้ fallback จากฐานข้อมูล',
					actionText: 'ปิด'
				});
				updateSmartCardPreview(null, 'อ่านบัตรไม่สำเร็จ กรุณาลองใหม่');
				ws.close();
				fetchReaderStatus();
				finalize();
			} catch (readError) {
				console.error('Smart card processing error', readError);
				updateSmartCardPreview(null, `เกิดข้อผิดพลาด: ${readError.message}`);
				smartClinicStatusModal.show({
					type: 'danger',
					subtitle: 'Smart card read',
					state: 'Failed',
					stateDescription: 'อ่านข้อมูลไม่สำเร็จ',
					message: `เกิดข้อผิดพลาด: ${readError.message}`,
					meta: 'ใช้ปุ่ม "ตัดการเชื่อมต่อและรีสตาร์ทพอร์ต" แล้วลองอีกครั้ง',
					actionText: 'ปิด'
				});
				ws.close();
				fetchReaderStatus();
				finalize();
			}
		};

		ws.onerror = async function () {
			if (session.cancelled) {
				finalize();
				return;
			}

			try {
				if (!hasValidCitizenId) {
					throw new Error('เชื่อมต่อ Bridge ไม่สำเร็จ และยังไม่มีเลขบัตร 13 หลักสำหรับ fallback');
				}

				const fallbackData = await tryDatabaseFallback(enteredCitizenId);
				fillPatientForm(fallbackData);
				updateSmartCardPreview(fallbackData, 'Bridge ไม่พร้อม จึงใช้ข้อมูลจากฐานข้อมูลแทน');
				smartClinicStatusModal.show({
					type: 'success',
					subtitle: 'Smart card read (Database fallback)',
					state: 'Completed',
					stateDescription: 'Bridge ใช้งานไม่ได้ แต่อ่านจากฐานข้อมูลสำเร็จ',
					message: 'ระบบเติมข้อมูลลงฟอร์มแล้ว กรุณาตรวจสอบก่อนกดบันทึกผู้ป่วย',
					meta: `Source: ${fallbackData.source || 'database'} | API: /api/smartcard/read`,
					actionText: 'พร้อมบันทึก',
					items: buildItems(fallbackData)
				});
			} catch (fallbackError) {
				updateSmartCardPreview(null, `ไม่สามารถอ่านข้อมูลได้: ${fallbackError.message}`);
				smartClinicStatusModal.show({
					type: 'danger',
					subtitle: 'Smart card reader',
					state: 'Failed',
					stateDescription: 'ไม่สามารถอ่านข้อมูลได้',
					message: `ไม่สามารถอ่านข้อมูลได้: ${fallbackError.message}`,
					meta: 'กดปุ่ม "ตัดการเชื่อมต่อและรีสตาร์ทพอร์ต" แล้วลองใหม่ หรือกรอกเลขบัตร 13 หลัก',
					actionText: 'ปิด'
				});
			} finally {
				fetchReaderStatus();
				finalize();
			}
		};

		ws.onclose = function () {
			if (!session.inProgress) {
				return;
			}
			finalize();
		};
	} catch (error) {
		session.inProgress = false;
		session.webSocket = null;
		session.fallbackController = null;
		updateSmartCardPreview(null, `เริ่มการเชื่อมต่อไม่สำเร็จ: ${error.message}`);
		smartClinicStatusModal.show({
			type: 'danger',
			subtitle: 'Smart card read',
			state: 'Failed',
			stateDescription: 'ไม่สามารถเริ่มการเชื่อมต่อได้',
			message: `เกิดข้อผิดพลาดในการเชื่อมต่อ Smart Card Bridge: ${error.message}`,
			meta: 'ตรวจสอบว่า Bridge service กำลังทำงาน จากนั้นลองใหม่อีกครั้ง',
			actionText: 'ปิด'
		});
	}
});

document.addEventListener('click', function (event) {
	const trigger = event.target.closest('[data-preview-pdf-url]');
	if (!trigger) {
		return;
	}

	const url = trigger.getAttribute('data-preview-pdf-url');
	if (!url) {
		return;
	}

	const title = trigger.getAttribute('data-preview-pdf-title') || 'PDF Preview';
	const modalElement = document.getElementById('pdfPreviewModal');
	const iframe = document.getElementById('pdfPreviewFrame');
	const label = document.getElementById('pdfPreviewLabel');

	if (!modalElement || !iframe || !label) {
		return;
	}

	label.textContent = title;
	iframe.setAttribute('src', url);
	const modal = bootstrap.Modal.getOrCreateInstance(modalElement);
	modal.show();
});

document.addEventListener('hidden.bs.modal', function (event) {
	if (event.target && event.target.id === 'pdfPreviewModal') {
		const iframe = document.getElementById('pdfPreviewFrame');
		if (iframe) {
			iframe.setAttribute('src', 'about:blank');
		}
	}
});
