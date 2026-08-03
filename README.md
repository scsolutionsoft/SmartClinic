# SmartClinic - ระบบบริหารจัดการคลินิกแบบอัจฉริยะ

ระบบการจัดการคลินิกบนเว็บแบบครบวงจรที่รวมรวมการจัดการผู้ป่วย บันทึกการรักษา และลายเซ็นดิจิทัล พร้อมการอ่านบัตรประชาชนอัจฉริยะผ่าน PC/SC smart card reader

## 📋 ความต้องการของระบบ

### Server (Web Application)
- **Framework**: ASP.NET Core 10.0 (MVC)
- **Database**: Microsoft SQL Server 2019 หรือสูงกว่า
- **.NET Runtime**: .NET 10.0 SDK/Runtime
- **RAM**: 2GB ขึ้นไป
- **Storage**: 50GB สำหรับฐานข้อมูลและไฟล์เอกสาร

### Client (Smart Card Reader Bridge)
- **OS**: Windows 10/11 Professional หรือ Server
- **Smart Card Reader**: Gemalto, ACS, Lenovo ที่รองรับ PC/SC
- **Port**: 9999 (สำหรับ WebSocket communication)

### Web Browser
- Chrome, Edge, Safari, Firefox (version ล่าสุด)
- JavaScript enabled
- HTTPS support (recommended for production)

---

## 🚀 ขั้นตอนการติดตั้ง

### ขั้นตอนที่ 1: ติดตั้ง Web Application

#### 1.1 ติดตั้ง SQL Server Database
```bash
# สร้าง database ชื่อ SmartClinic
# Connection string: Server=localhost;Database=SmartClinic;Integrated Security=true;TrustServerCertificate=true;
```

#### 1.2 ตั้งค่า appsettings.json
แก้ไข `appsettings.json` ใน folder คลินิก:
```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=YOUR_SERVER;Database=SmartClinic;Integrated Security=true;TrustServerCertificate=true;"
  }
}
```

#### 1.3 สตาร์ท Web Application
```bash
cd c:\Users\p_cha\OneDrive\GitHub\SmartClinic
dotnet run
# หรือ
.\bin\Release\net10.0\SmartClinic.Web.exe
```

Web application จะรันบนที่อยู่ `https://localhost:5247` (HTTPS) หรือ `http://localhost:5247`

#### 1.4 เข้าสู่ระบบเครื่องแรก
- **ชื่อผู้ใช้**: superadmin@smartclinic.local
- **รหัสผ่าน**: 0999999999 (เบอร์โทรศัพท์ SuperAdmin)
- **ข้อกำหนด**: ต้องเปลี่ยนรหัสผ่านในการเข้าสู่ระบบครั้งแรก

---

### ขั้นตอนที่ 2: ติดตั้ง Smart Card Reader Bridge

#### 2.1 ติดตั้ง Windows Smart Card Reader Driver
- เสียบ smart card reader เข้ากับคอมพิวเตอร์
- ดาวน์โหลดและติดตั้ง driver จากผู้ผลิต (Gemalto, ACS, Lenovo ฯลฯ)
- ทดสอบการอ่านบัตรด้วย Windows Contact Smart Card Tool

#### 2.2 รันสคริปต์ติดตั้ง Bridge
```bash
# คัดลอก install-bridge.bat ไปยัง Desktop หรือ folder ที่ต้องการ
# คลิก右键 (Right-click) และเลือก "Run as administrator"

# หรือจากCommand Prompt (Admin):
cd c:\Users\p_cha\OneDrive\GitHub\SmartCardReaderBridge\SmartClinic.CardReader.Bridge
install-bridge.bat
```

#### 2.3 สตาร์ท Bridge Service
หลังการติดตั้งเสร็จ ให้รันไฟล์:
```bash
C:\Program Files\SmartClinic\CardReader\start-bridge.bat
```

หรือ PowerShell (Admin):
```powershell
& "C:\Program Files\SmartClinic\CardReader\SmartClinic.CardReader.Bridge.exe"
```

**ผลลัพธ์ที่คาดหวัง**:
```
WebSocket server listening on ws://localhost:9999/card
Waiting for connections...
```

#### 2.4 ทดสอบ Bridge Connection
เปิด Developer Console ในเว็บเบราว์เซอร์ (F12 → Console):
```javascript
const ws = new WebSocket('ws://localhost:9999/card');
ws.onopen = () => {
  console.log('✓ Bridge connected successfully');
  ws.send(JSON.stringify({ citizenId: '1234567890123' }));
};
ws.onmessage = (e) => {
  console.log('Card data received:', JSON.parse(e.data));
};
ws.onerror = () => {
  console.log('✗ Bridge connection failed - check if service is running');
};
```

---

## 📱 การใช้งานระบบ

### 1. การลงทะเบียนคลินิก
1. เข้า **ลงทะเบียนคลินิก** (Register Clinic)
2. เลือกคลินิกจาก **NHSO Master Data** dropdown
3. ระบบจะแสดงชื่อและที่อยู่โดยอัตโนมัติ
4. กรอกข้อมูลเพิ่มเติม (ผู้ดูแลระบบ, เบอร์โทร, อีเมล)
5. คลิก **ลงทะเบียน**
6. บัญชี AdminClinic จะถูกสร้างอัตโนมัติ

### 2. การลงทะเบียนผู้ป่วย
1. ไปที่ **ผู้ป่วย** → **ลงทะเบียนใหม่**
2. **วิธี A - อ่านบัตรประชาชนอัจฉริยะ** (ถ้าติดตั้ง Bridge):
   - เสียบบัตรเข้าในตัวอ่าน
   - คลิก **อ่านข้อมูลบัตร** (Read Smart Card)
   - ฟอร์มจะเติมข้อมูลโดยอัตโนมัติ
3. **วิธี B - ป้อนข้อมูลด้วยตนเอง**:
   - กรอก ID บัตรประชาชน 13 หลัก
   - ชื่อ-นามสกุล, ที่อยู่, เบอร์โทร
   - วันเกิด, เพศ
4. อัปโหลดภาพผู้ป่วย (ถ้า available)
5. คลิก **บันทึก**

### 3. บันทึกการรักษา OPD
1. ไปที่ **เวชระเบียน OPD** → **เพิ่มใหม่**
2. เลือกผู้ป่วยจาก dropdown
3. อัปโหลดไฟล์ OPD (PDF format)
4. ระบุวันที่ไปรักษา
5. คลิก **บันทึก**
6. ดูประวัติและ preview PDF ได้ที่ **รายงาน**

### 4. บันทึกลายเซ็น
#### อัปโหลดเดี่ยว:
1. ไปที่ **ลายเซ็น** → **อัปโหลด**
2. เลือก ผู้ป่วย, ไฟล์รูปภาพลายเซ็น
3. คลิก **อัปโหลด**

#### อัปโหลด Batch:
1. เตรียมไฟล์รูปภาพ ชื่อตามรูปแบบ: `1234567890123.jpg` (13 หลัก ID)
2. เลือก **อัปโหลด Batch**
3. ระบบจะ match ID อัตโนมัติ
4. ดำเนินการอัปโหลด

### 5. พิมพ์รายงาน
1. ไปที่ **รายงาน**
2. เลือก **ชิดซ้าย → ตัวเลือก** หรือ **ปรับคืน**
3. ระบบจะแสดงบันทึกเวชระเบียนพร้อมลายเซ็น
4. คลิก **พิมพ์** (Print) เพื่อบันทึกเป็น PDF

### 6. จัดการผู้ใช้ คลินิก
1. ไปที่ **ผู้ใช้คลินิก** (Clinic Users)
2. **เพิ่มบุคลากร**: กรอก ID, ชื่อ, เบอร์โทร
3. เลือก **บทบาท** (Role): Nurse หรือ User
4. ระบบจะสร้างบัญชีและส่งรหัสผ่านอัตโนมัติ

### 7. SuperAdmin - จัดการ NHSO Master Data
1. ไปที่ **ข้อมูลหลัก สปสช.** (NHSO Master)
2. **เพิ่มคลินิกใหม่**: กรอกรหัส, ชื่อ, ที่อยู่, เบอร์โทร, อีเมล
3. **แก้ไข/ลบ**: คลิกปุ่ม Edit/Delete ในตาราง
4. **นำเข้า CSV**: เตรียมไฟล์ CSV (ClinicCode, Name, Address, ContactPhone, ContactEmail, IsActive)
5. **เปิด/ปิด**: คลิกปุ่ม Toggle เพื่อเปิดปิดคลินิก

#### รูปแบบไฟล์ CSV:
```csv
ClinicCode,Name,Address,ContactPhone,ContactEmail,IsActive
CL0001,คลินิกสมเด็จ,123 ถนนสุขุมวิท,0812345678,info@clinic.com,true
CL0002,คลินิกธรรมชาติ,456 ถนนประชาชน,0898765432,contact@natural.com,true
```

---

## 🎨 การเปลี่ยนธีม

1. คลิก **ธีม** ในแดชบอร์ด
2. เลือกจากตัวเลือก: Lux (default), Flatly, Minty, Journal, Materia, Morph
3. คลิก **ใช้ธีม**
4. ตัวเลือกจะถูกบันทึกไว้ในเบราว์เซอร์

---

## 🔐 การรักษาความปลอดภัย

### SuperAdmin First Login
- ชื่อผู้ใช้: `superadmin@smartclinic.local`
- รหัสผ่านเบื้องต้น: `0999999999` (ต้องเปลี่ยนในการเข้าสู่ระบบครั้งแรก)

### AdminClinic First Login
- ชื่อผู้ใช้: `<clinic_code>` (เช่น CL0001)
- รหัสผ่านเบื้องต้น: เบอร์โทรคลินิก (ต้องเปลี่ยนในการเข้าสู่ระบบครั้งแรก)

### ข้อปฏิบัติที่ดี
- ✓ เปลี่ยนรหัสผ่าน SuperAdmin ทันที
- ✓ ใช้รหัสผ่านที่ซับซ้อน (ตัวอักษร, ตัวเลข, สัญลักษณ์)
- ✓ ไม่แชร์บัญชี SuperAdmin
- ✓ สำรองฐานข้อมูล (Database) อย่างสม่ำเสมอ
- ✓ ใช้ HTTPS ในสภาพแวดล้อม Production

---

## 🛠️ การแก้ไขปัญหา

### Bridge ไม่เชื่อมต่อ
```
ข้อผิดพลาด: "ไม่สามารถเชื่อมต่อบริดจ์ SmartCard Reader ได้"
```

**วิธีแก้ไข**:
1. ตรวจสอบว่า smart card reader เสียบเข้ากับคอมพิวเตอร์
2. ตรวจสอบว่า Bridge service กำลังรัน:
   ```powershell
   Get-Process | Select-String "SmartClinic.CardReader.Bridge"
   ```
3. หากไม่พบ ให้รัน:
   ```bash
   C:\Program Files\SmartClinic\CardReader\start-bridge.bat
   ```
4. ยืนยันว่า port 9999 เปิด (ไม่ block โดย firewall)

### ไม่สามารถเชื่อมต่อ Database
```
ข้อผิดพลาด: "Cannot open database requested in the login"
```

**วิธีแก้ไข**:
1. ตรวจสอบ Connection String ใน `appsettings.json`
2. ยืนยันว่า SQL Server กำลังรันและ SmartClinic database มีอยู่
3. รันการ migration อีกครั้ง:
   ```bash
   dotnet ef database update
   ```

### Smart Card ไม่อ่านได้
```
ข้อผิดพลาด: "No smart card detected" หรือ "Device not found"
```

**วิธีแก้ไข**:
1. ตรวจสอบ driver smart card reader:
   ```powershell
   Get-PnpDevice | Where-Object {$_.name -like "*card*"}
   ```
2. อัปเดต driver จากเว็บไซต์ผู้ผลิต
3. ทดสอบด้วย Windows Contact Smart Card Tool
4. เสียบบัตรใหม่ แล้วลองอีกครั้ง

### Application ไม่สตาร์ท
```
ข้อผิดพลาด: "The application failed to start"
```

**วิธีแก้ไข**:
1. ตรวจสอบว่า .NET 10.0 runtime ติดตั้งแล้ว:
   ```bash
   dotnet --version
   ```
2. หากไม่มี ให้ดาวน์โหลดจาก: https://dotnet.microsoft.com/download
3. ตรวจสอบไฟล์ `appsettings.json` ถูกต้อง
4. ดูไฟล์ log ใน folder `logs/` (หากมี)

---

## 📊 Architecture

### Web Application (ASP.NET Core MVC)
```
SmartClinic.Web/
├── Program.cs                 # Entry point, DI, migrations
├── Controllers/               # Business logic
│   ├── PatientsController
│   ├── MedicalRecordsController
│   ├── SignaturesController
│   ├── ClinicRegistrationController
│   ├── NhssoMastersController
│   ├── ClinicUsersController
│   └── ReportsController
├── Models/                    # EF Core entities + view models
│   ├── ApplicationUser
│   ├── Patient
│   ├── TreatmentRecord
│   ├── SignImg
│   ├── NhssoClinicMaster
│   └── ViewModels/
├── Views/                     # Razor templates
│   ├── Patients/
│   ├── MedicalRecords/
│   ├── Signatures/
│   ├── Clinics/
│   ├── NhssoMasters/
│   ├── ClinicUsers/
│   ├── Reports/
│   └── Shared/
├── Data/
│   └── ApplicationDbContext   # EF Core DbContext
├── wwwroot/                   # Static assets
│   ├── css/
│   ├── js/
│   └── lib/
└── appsettings.json          # Configuration
```

### Smart Card Reader Bridge (Console App)
```
SmartClinic.CardReader.Bridge/
├── Program.cs                 # WebSocket server
├── install-bridge.bat         # Windows installer script
├── start-bridge.bat           # Service launcher
└── bin/Release/net10.0/       # Compiled executable
```

### Database Schema
- **Clinics**: ข้อมูลคลินิก (ลงทะเบียนแล้ว)
- **Patients**: บัญชีผู้ป่วย (ชื่อ, ID, ที่อยู่, รูป)
- **TreatmentRecords**: เวชระเบียน OPD (PDF stored as VARBINARY)
- **SignImgs**: ลายเซ็นดิจิทัล (image stored as VARBINARY)
- **NhssoClinicMasters**: ข้อมูลหลัก สปสช. (NHSO master data)
- **AspNetUsers**: บัญชีผู้ใช้ (Identity users)
- **AspNetRoles**: บทบาท (Roles: SuperAdmin, AdminClinic, Nurse, User)

---

## 📚 API Reference

### Smart Card Reader Bridge
**Endpoint**: `ws://localhost:9999/card`

**Request**:
```json
{
  "citizenId": "1234567890123"
}
```

**Response (Success)**:
```json
{
  "success": true,
  "citizenId": "1234567890123",
  "fullName": "นาย สมชาย ใจดี",
  "address": "123 ถนนสุขุมวิท แขวงพระโขนง เขตคลองเตย กรุงเทพมหานคร 10110",
  "phoneNumber": "0812345678",
  "birthDate": "1990-05-15",
  "gender": "M",
  "source": "smartcard-reader"
}
```

**Response (Error)**:
```json
{
  "success": false,
  "error": "Invalid citizen ID format"
}
```

---

## 📝 Release Notes

### Version 1.0.0 (2026-01-29)
- ✅ Web application (ASP.NET Core 10.0 MVC)
- ✅ SQL Server database with auto-migration
- ✅ Patient registration with smart card integration
- ✅ OPD medical records (PDF upload & storage)
- ✅ Digital signatures (single/batch upload)
- ✅ SuperAdmin clinic master management
- ✅ Clinic staff management (Roles & Permissions)
- ✅ Reports with signature display & printing
- ✅ Smart Card Reader Bridge (PC/SC WebSocket)
- ✅ Multi-language support (Thai)
- ✅ Theme switching (Bootswatch)

---

## 🤝 Support

สำหรับการช่วยเหลือและรายงานข้อบกพร่อง โปรดติดต่อ:
- Email: support@smartclinic.local
- Documentation: https://github.com/yourusername/SmartClinic
- Issues: https://github.com/yourusername/SmartClinic/issues

---

## 📄 License

Copyright © 2026 SmartClinic. All rights reserved.

---

**Last Updated**: January 29, 2026
**Author**: Development Team
**Version**: 1.0.0
