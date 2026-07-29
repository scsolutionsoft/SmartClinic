# ⚡ SmartClinic - Quick Start Guide

**สำหรับการเริ่มต้นใช้งานอย่างรวดเร็ว**

---

## 🚀 เริ่มต้นใน 5 นาที

### ขั้นตอนที่ 1: สตาร์ท Web Application
```bash
cd c:\Users\p_cha\OneDrive\GitHub\SmartClinic
dotnet run
# หรือ: .\bin\Release\net10.0\SmartClinic.Web.exe
```

เมื่อเห็น `Application started on http://localhost:5247` → ขั้นตอนถัดไป

### ขั้นตอนที่ 2: สตาร์ท Smart Card Bridge (ถ้ามี)
```bash
"C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
# หรือ:
& "C:\Program Files\SmartClinic\CardReader\SmartClinic.CardReader.Bridge.exe"
```

เมื่อเห็น `WebSocket server listening on ws://localhost:9999/card` → OK

### ขั้นตอนที่ 3: เปิด Web Application
- ไปที่ https://localhost:5247
- ชื่อผู้ใช้: **superadmin@smartclinic.local**
- รหัสผ่าน: **0999999999**
- คลิก **เข้าสู่ระบบ**

### ขั้นตอนที่ 4: เปลี่ยนรหัสผ่าน SuperAdmin
- ระบบบังคับให้เปลี่ยน
- ตั้งรหัสผ่านใหม่ (8 ตัวอักษรขึ้นไป)
- คลิก **เปลี่ยนรหัสผ่าน**

✅ ระบบพร้อมใช้งาน!

---

## 👥 สำหรับ SuperAdmin (ผู้ดูแลระบบ)

### เพิ่มคลินิกใหม่
```
1. ไปที่ ข้อมูลหลัก สปสช.
2. คลิก + เพิ่มคลินิกใหม่
3. กรอก: รหัสคลินิก (เช่น CL0001), ชื่อ, ที่อยู่
4. คลิก บันทึก
```

### นำเข้า CSV
```
1. ปรับเอกสาร CSV:
   ClinicCode,Name,Address,ContactPhone,ContactEmail,IsActive
   CL0001,คลินิก ABC,123 ถ.,0812345678,info@abc.com,true

2. ไปที่ ข้อมูลหลัก สปสช.
3. คลิก นำเข้า CSV
4. เลือกไฟล์ → Upload
```

---

## 🏥 สำหรับ Clinic Admin

### ลงทะเบียนคลินิก
```
1. ไปที่ ลงทะเบียนคลินิก (ใช้ browser/window ใหม่)
2. เลือกคลินิกจาก dropdown
3. กรอก: ชื่อผู้ดูแล, เบอร์โทร, อีเมล
4. คลิก ลงทะเบียน
```

ระบบจะสร้างบัญชี AdminClinic:
- Username: `<clinic_code>` (เช่น CL0001)
- Password: `<clinic_phone>` (เบอร์โทรคลินิก)

### สร้างบัญชี Staff
```
1. ไปที่ ผู้ใช้คลินิก
2. คลิก เพิ่มผู้ใช้ใหม่
3. กรอก: รหัส, ชื่อ, เบอร์โทร
4. เลือก: บทบาท (Nurse/User)
5. คลิก สร้างบัญชี
```

---

## 👨‍⚕️ สำหรับ Nurse/User (เจ้าหน้าที่)

### ลงทะเบียนผู้ป่วย
```
วิธี A - อ่านบัตรประชาชน (ถ้าติดตั้ง Bridge):
1. ไปที่ ผู้ป่วย → ลงทะเบียนใหม่
2. เสียบบัตร → กรอก 13 หลัก ID
3. คลิก อ่านข้อมูลบัตร
4. ฟอร์มจะเติมอัตโนมัติ
5. คลิก บันทึก

วิธี B - ป้อนด้วยตนเอง:
1. ไปที่ ผู้ป่วย → ลงทะเบียนใหม่
2. กรอก: ID 13 หลัก, ชื่อ, ที่อยู่, เบอร์โทร, วันเกิด, เพศ
3. คลิก บันทึก
```

### บันทึก OPD
```
1. ไปที่ เวชระเบียน OPD → เพิ่มใหม่
2. เลือก: ผู้ป่วย
3. เลือก: ไฟล์ OPD (PDF)
4. กรอก: วันที่ไปรักษา
5. คลิก บันทึก
```

### บันทึกลายเซ็น
```
เดี่ยว:
1. ไปที่ ลายเซ็น → อัปโหลด
2. เลือก: ผู้ป่วย, ไฟล์รูป
3. คลิก อัปโหลด

Batch:
1. เตรียมไฟล์: 1234567890123.jpg, 9876543210123.jpg, ...
2. ไปที่ ลายเซ็น → อัปโหลด Batch
3. คลิก อัปโหลด
```

### ดูรายงาน
```
1. ไปที่ รายงาน
2. ระบบแสดง OPD ทั้งหมด
3. คลิก ดูรายละเอียด
4. ลายเซ็นแสดงในรายงาน
5. คลิก พิมพ์ → บันทึก PDF
```

---

## 🎨 การเปลี่ยนธีม
```
1. บนแดชบอร์ด เห็น "ตัวอย่างธีม"
2. เลือก: Lux, Flatly, Minty, Journal, Materia, Morph
3. คลิก ใช้ธีม
4. หน้าเว็บจะเปลี่ยนสีใหม่
```

---

## 🔑 การเปลี่ยนรหัสผ่าน
```
1. คลิก (ชื่อบัญชี) ด้านบนขวา
2. เลือก เปลี่ยนรหัสผ่าน
3. กรอก: รหัสปัจจุบัน, รหัสใหม่ (2 ครั้ง)
4. คลิก เปลี่ยน
```

---

## 🆘 ช่วยเหลือด่วน

### Smart Card ไม่อ่านได้
```
✓ ตรวจสอบ Bridge กำลังรัน: 
  Get-Process | Where-Object {$_.ProcessName -like "*CardReader*"}

✓ ตรวจสอบ Port 9999:
  netstat -ano | findstr :9999

✓ Restart Bridge:
  C:\Program Files\SmartClinic\CardReader\start-bridge.bat

✓ ตรวจสอบบัตร: 
  - เสียบใหม่
  - ทำความสะอาด
  - ลองบัตรอื่น
```

### เข้าสู่ระบบไม่ได้
```
✓ ตรวจสอบ Caps Lock เปิดอยู่ไหม
✓ ตรวจสอบ Username ถูกต้อง
✓ ตรวจสอบ Database connection ใน appsettings.json
✓ Restart application:
  dotnet run
```

### Web ไม่เปิด
```
✓ Restart app:
  Ctrl+C ใน terminal → dotnet run

✓ ตรวจสอบ Port 5247:
  netstat -ano | findstr :5247
  
✓ ปิด Firewall / VPN ชั่วคราว

✓ ลองใช้ http:// แทน https://
  http://localhost:5247
```

---

## 📝 ข้อมูลอ้างอิงด่วน

| ความต้องการ | รายละเอียด |
|-----------|-----------|
| Web URL | https://localhost:5247 |
| WebSocket | ws://localhost:9999/card |
| Database | SmartClinic (SQL Server) |
| SuperAdmin | superadmin@smartclinic.local / 0999999999 |
| Default Port | 5247 (Web), 9999 (Bridge) |
| Documentation | README.md, INSTALLATION.md |

---

## 📞 ติดต่อ Support

- 📧 Email: support@smartclinic.local
- 📖 Documentation: README.md
- 🐛 Bug Report: GitHub Issues
- 💻 Source Code: GitHub Repository

---

**ยินดีต้อนรับเข้าสู่ SmartClinic!** 🎉

หากมีข้อสงสัย โปรดอ่านเอกสารฉบับเต็ม (README.md, INSTALLATION.md) หรือติดต่อฝ่าย Support
