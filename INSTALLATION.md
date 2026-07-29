# 📥 SmartClinic - คู่มือการติดตั้ง (Installation Guide)

**เวอร์ชัน**: 1.0.0  
**วันที่อัปเดต**: 29 มกราคม 2026  
**ภาษา**: ไทย

---

## ✅ Checklist ก่อนติดตั้ง

- [ ] Windows Server 2019 / 2022 หรือ Windows 10/11 Pro (64-bit)
- [ ] .NET 10.0 SDK/Runtime ติดตั้งแล้ว
- [ ] SQL Server 2019 / 2022 และ SSMS
- [ ] Administrator access เพื่อติดตั้งบริการ
- [ ] Smart card reader ที่รองรับ PC/SC (สำหรับบ้านอ่านบัตร)
- [ ] Driver smart card reader ติดตั้งแล้ว

---

## 🖥️ ระบบตัวอักษรขั้นต่ำ

| Component | ข้อกำหนด |
|-----------|---------|
| OS | Windows Server 2019 / Windows 10 Pro (64-bit) |
| Processor | Intel Core i5 / AMD Ryzen 5 หรือสูงกว่า |
| RAM | 2GB (4GB recommended) |
| Storage | 50GB+ (สำหรับ DB + files) |
| Network | LAN/Internet connection |
| Smart Card Reader | PC/SC compatible (optional) |

---

## 📋 ส่วนที่ 1: ติดตั้ง .NET 10.0

### 1.1 ดาวน์โหลด .NET 10.0
1. เปิด https://dotnet.microsoft.com/download/dotnet/10.0
2. คลิก **Download .NET 10.0 Runtime** (สำหรับ server) หรือ **SDK** (สำหรับ development)
3. เลือก **Windows x64**
4. บันทึก installer ไป Desktop

### 1.2 รันตัวติดตั้ง
```bash
# คลิกดับเบิลแคลิก installer ที่ดาวน์โหลด (.msi file)
# หรือจาก PowerShell:
# .\dotnet-runtime-10.0.x-win-x64.exe

# ตรวจสอบการติดตั้ง:
dotnet --version
# ผลลัพธ์ที่คาดหวัง: 10.0.x
```

---

## 📊 ส่วนที่ 2: สร้างฐานข้อมูล SQL Server

### 2.1 สร้าง Database
```sql
-- เปิด SQL Server Management Studio (SSMS)
-- คลิก New Query

CREATE DATABASE SmartClinic;
USE SmartClinic;
GO
```

### 2.2 ตั้งค่า Connection String
บันทึก connection string ของคุณ:
```
Server=YOUR_SERVER_NAME;Database=SmartClinic;Integrated Security=true;TrustServerCertificate=true;
```

ตัวอย่าง:
- `Server=localhost` - สำหรับเซิร์ฟเวอร์บนเครื่องเดียวกัน
- `Server=CLINIC-PC\SQLEXPRESS` - สำหรับ SQL Server Express
- `Server=192.168.1.100` - สำหรับเซิร์ฟเวอร์ Remote

---

## 🌐 ส่วนที่ 3: ติดตั้ง SmartClinic Web Application

### 3.1 ดาวน์โหลด Source Code
```bash
# Clone repository
git clone https://github.com/yourusername/SmartClinic.git
cd SmartClinic

# หรือ extract zip file ที่ดาวน์โหลด
```

### 3.2 แก้ไข Configuration
เปิดไฟล์ `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Database=SmartClinic;Integrated Security=true;TrustServerCertificate=true;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Information"
    }
  },
  "AllowedHosts": "*"
}
```

**แก้ไข Connection String ให้ตรงกับการตั้งค่าของคุณ**

### 3.3 สตาร์ท Web Application

#### Option A: จาก Visual Studio
```bash
# เปิด SmartClinic.sln
# กดปุ่ม Run (F5)
# แอปพลิเคชันจะเปิดที่ https://localhost:5247
```

#### Option B: จาก PowerShell / Command Prompt
```bash
cd c:\path\to\SmartClinic
dotnet run

# หรือรัน Release build:
.\bin\Release\net10.0\SmartClinic.Web.exe
```

#### Option C: Publish as Standalone
```bash
# สร้าง self-contained executable
dotnet publish -c Release -r win-x64 --self-contained

# Run:
.\bin\Release\net10.0\win-x64\publish\SmartClinic.Web.exe
```

### 3.4 ยืนยันการติดตั้ง Web App
- [ ] ได้รับข้อความ "Application started. Press Ctrl+C to shut down"
- [ ] เปิด Browser ไปที่ https://localhost:5247
- [ ] เห็นหน้า Login
- [ ] Database migrated successfully

---

## 🎫 ส่วนที่ 4: ติดตั้ง Smart Card Reader Bridge

### 4.1 ติดตั้ง Smart Card Reader Driver

**สำหรับ ACS Reader**:
```bash
# ดาวน์โหลดจาก: https://www.acs.com.hk/en/download/
# รันตัวติดตั้ง driver
# เสียบ card reader เข้า USB
```

**สำหรับ Gemalto Reader**:
```bash
# ดาวน์โหลดจาก: https://www.gemalto.com/
# ติดตั้ง middleware
```

### 4.2 ทดสอบ Smart Card Reader
```powershell
# เปิด PowerShell as Administrator
# ตรวจสอบให้เห็น card reader:
Get-PnpDevice | Where-Object {$_.name -like "*card*"}

# ทดสอบการอ่านบัตร:
# Windows → Settings → Devices → Smart card readers
```

### 4.3 รันสคริปต์ติดตั้ง Bridge

**วิธี A: GUI (คลิกรัน)**
```bash
# ไปที่ folder: c:\Users\p_cha\OneDrive\GitHub\SmartCardReaderBridge\SmartClinic.CardReader.Bridge
# คลิกขวา (Right-click) → Run as administrator
# ค้นหา: install-bridge.bat
# คลิก: เรียกใช้ (Open)
```

**วิธี B: PowerShell (Admin)**
```powershell
# เปิด PowerShell as Administrator
cd "c:\Users\p_cha\OneDrive\GitHub\SmartCardReaderBridge\SmartClinic.CardReader.Bridge"
.\install-bridge.bat
```

**วิธี C: Command Prompt (Admin)**
```cmd
# เปิด Command Prompt as Administrator
cd c:\Users\p_cha\OneDrive\GitHub\SmartCardReaderBridge\SmartClinic.CardReader.Bridge
install-bridge.bat
```

### 4.4 ผลลัพธ์ที่คาดหวัง
```
[*] SmartClinic Card Reader Bridge Installer
[*] Checking administrator privileges... OK
[*] Building bridge in Release mode...
[*] Build succeeded
[*] Creating installation directory: C:\Program Files\SmartClinic\CardReader
[*] Copying executable and dependencies...
[*] Creating start-bridge.bat launcher...
[✓] Installation completed successfully!

Next steps:
  1. Start the bridge service:
     C:\Program Files\SmartClinic\CardReader\start-bridge.bat
  2. Open a terminal and run (or double-click the batch file)
  3. You should see: "WebSocket server on ws://localhost:9999/card"
  4. Smart card reading will now work in the web application!
```

### 4.5 สตาร์ท Bridge Service
```bash
# วิธี A: คลิก batch file
# C:\Program Files\SmartClinic\CardReader\start-bridge.bat

# วิธี B: PowerShell
& "C:\Program Files\SmartClinic\CardReader\SmartClinic.CardReader.Bridge.exe"

# ผลลัพธ์ที่คาดหวัง:
# WebSocket server listening on ws://localhost:9999/card
# Waiting for connections...
```

---

## 🔑 ส่วนที่ 5: เข้าสู่ระบบครั้งแรก

### 5.1 SuperAdmin Login
1. เปิด https://localhost:5247
2. กรอก:
   - **Username**: superadmin@smartclinic.local
   - **Password**: 0999999999 (เบอร์โทรเบื้องต้น)
3. ระบบบังคับให้เปลี่ยนรหัสผ่าน
4. ตั้งรหัสผ่านใหม่ (Strong password)
5. คลิก **เปลี่ยนรหัสผ่าน**

### 5.2 ตรวจสอบการติดตั้ง
- [ ] Dashboard แสดงผลสำเร็จ
- [ ] เมนูภาษาไทยมองเห็นชัด
- [ ] ธีมสามารถเปลี่ยนได้
- [ ] Link NHSO Master Data accessible

---

## 🏥 ส่วนที่ 6: การตั้งค่า NHSO Master Data

### 6.1 เพิ่มคลินิก
1. ไปที่ **ข้อมูลหลัก สปสช.** (SuperAdmin only)
2. คลิก **เพิ่มคลินิกใหม่**
3. กรอก:
   - **รหัสคลินิก**: CL0001 (9-10 ตัวอักษร)
   - **ชื่อคลินิก**: คลินิกสมเด็จ
   - **ที่อยู่**: 123 ถนนสุขุมวิท...
   - **เบอร์โทรสายด่วน**: 0812345678
   - **อีเมล**: info@clinic.com
   - **เปิดใช้**: Check ✓
4. คลิก **บันทึก**

### 6.2 นำเข้า CSV (Batch Import)
เตรียมไฟล์ CSV (`clinics.csv`):

```csv
ClinicCode,Name,Address,ContactPhone,ContactEmail,IsActive
CL0001,คลินิกสมเด็จ,123 ถนนสุขุมวิท,0812345678,info@clinic.com,true
CL0002,คลินิกธรรมชาติ,456 ถนนประชาชน,0898765432,contact@natural.com,true
CL0003,โรงพยาบาล ABC,789 ถนนราชดำเนิน,0899876543,admin@abc-hospital.com,true
```

จากนั้น:
1. ไปที่ **ข้อมูลหลัก สปสช.**
2. คลิก **นำเข้า CSV**
3. เลือกไฟล์ `clinics.csv`
4. คลิก **Upload**
5. ระบบจะแสดงจำนวนคลินิกที่นำเข้า

---

## 👥 ส่วนที่ 7: ตั้งค่าสำหรับคลินิก

### 7.1 ลงทะเบียนคลินิก (เจ้าของคลินิก)
1. เปิด https://localhost:5247 (อื่น window หรือ incognito)
2. ไปที่ **ลงทะเบียนคลินิก**
3. เลือกคลินิกจาก dropdown (ต้อง NHSO Master มี)
4. กรอก:
   - **ชื่อผู้ดูแลระบบ**: นาย สมชาย ใจดี
   - **เบอร์โทรศัพท์**: 0812345678
   - **อีเมล**: admin@clinic.com
   - **ที่อยู่**: (auto-fill จาก NHSO)
5. เลือก **ธีม**: Lux (default)
6. คลิก **ลงทะเบียน**

ผลลัพธ์:
```
✓ Clinic registered successfully!
Username: CL0001 (clinic code)
Password: 0812345678 (clinic phone number)
Note: Must change password on first login
```

### 7.2 AdminClinic Login & First Setup
1. Logout จากบัญชี SuperAdmin
2. Login ด้วยบัญชี AdminClinic:
   - **Username**: CL0001 (รหัสคลินิก)
   - **Password**: 0812345678 (เบอร์โทรคลินิก)
3. ระบบบังคับให้เปลี่ยนรหัสผ่าน
4. ตั้งรหัสผ่านใหม่

### 7.3 สร้างบัญชีบุคลากร (AdminClinic)
1. ไปที่ **ผู้ใช้คลินิก**
2. คลิก **เพิ่มผู้ใช้ใหม่**
3. กรอก:
   - **รหัสพนักงาน**: NURSE001
   - **ชื่อเต็ม**: นางสาว ฟ้อนต์ ใจดี
   - **เบอร์โทร**: 0898765432
   - **บทบาท**: Nurse
   - **คลินิก**: (auto-select clinic ของ admin)
4. คลิก **สร้างบัญชี**

ผลลัพธ์:
```
✓ User created successfully!
Username: NURSE001
Temporary Password: 0898765432 (phone number)
Note: User must change password on first login
```

---

## 🧪 ส่วนที่ 8: ทดสอบระบบ

### 8.1 ทดสอบ Patient Registration (Manual)
1. Login ด้วย Nurse/User
2. ไปที่ **ผู้ป่วย** → **ลงทะเบียนใหม่**
3. กรอกข้อมูลด้วยตนเอง:
   - ID: 1234567890123
   - ชื่อ: นาย ทดสอบ ระบบ
   - ที่อยู่: 123 ซ.ทดสอบ
   - เบอร์โทร: 0812345678
   - วันเกิด: 01-05-2000
   - เพศ: ชาย
4. คลิก **บันทึก**
5. ยืนยัน: ผู้ป่วยปรากฏในรายการ

### 8.2 ทดสอบ Smart Card (หากติดตั้ง Bridge)
1. ไปที่ **ผู้ป่วย** → **ลงทะเบียนใหม่**
2. เสียบบัตรประชาชนเข้าตัวอ่าน
3. กรอก 13 หลัก ID
4. คลิก **อ่านข้อมูลบัตร** (Read Smart Card Button)
5. ยืนยัน:
   - [ ] ชื่อ-นามสกุลเติมเต็มอัตโนมัติ
   - [ ] ที่อยู่เติมเต็ม
   - [ ] Success modal ปรากฏ
6. คลิก **บันทึก**

### 8.3 ทดสอบ OPD Record
1. ไปที่ **เวชระเบียน OPD** → **เพิ่มใหม่**
2. เลือกผู้ป่วย
3. เลือกไฟล์ PDF (ตัวอย่าง OPD document)
4. กรอก วันที่ไปรักษา: วันนี้
5. คลิก **บันทึก**
6. ยืนยัน: PDF เก็บในฐานข้อมูลสำเร็จ

### 8.4 ทดสอบ Signature
1. ไปที่ **ลายเซ็น** → **อัปโหลด**
2. เลือกผู้ป่วย: ผู้ป่วยที่สร้างขึ้น
3. เลือกไฟล์รูปภาพลายเซ็น (.jpg, .png)
4. คลิก **อัปโหลด**
5. ยืนยัน: ลายเซ็นปรากฏในรายการ

### 8.5 ทดสอบ Report & Print
1. ไปที่ **รายงาน**
2. ระบบแสดง OPD records ทั้งหมด
3. คลิก **ดูรายละเอียด** (Details)
4. ยืนยัน: ลายเซ็นแสดงในรายงาน
5. คลิก **พิมพ์** → บันทึกเป็น PDF

---

## ⚠️ การแก้ไขปัญหาทั่วไป

### Bridge Connection Failed
```
❌ Error: "ไม่สามารถเชื่อมต่อบริดจ์ SmartCard Reader ได้"
```

**แนวทางแก้ไข**:
```powershell
# 1. ตรวจสอบ Bridge กำลังรัน:
Get-Process | Where-Object {$_.ProcessName -like "*CardReader*"}

# 2. ถ้าไม่พบ ให้สตาร์ท:
& "C:\Program Files\SmartClinic\CardReader\start-bridge.bat"

# 3. ตรวจสอบ Port 9999:
netstat -ano | findstr :9999
# ผลลัพธ์ที่คาดหวัง: LISTENING

# 4. ถ้า Firewall block:
# Settings → Firewall → Allow app through firewall
# → Find SmartClinic.CardReader.Bridge → Check ✓
```

### Database Connection Error
```
❌ Error: "Cannot open database requested"
```

**แนวทางแก้ไข**:
```bash
# 1. ตรวจสอบ SQL Server กำลังรัน:
# Services → SQL Server (MSSQLSERVER) → Running

# 2. ตรวจสอบ Connection String ใน appsettings.json

# 3. ทดสอบจาก SSMS:
# Open SSMS → Connect to Server
# Server name: localhost (หรือ server name ของคุณ)
# Database: SmartClinic (ต้องมีอยู่)

# 4. Re-run migration:
dotnet ef database update
```

### Smart Card Reader Not Found
```
❌ Error: "No smart card detected" หรือ "Device not found"
```

**แนวทางแก้ไข**:
```powershell
# 1. ตรวจสอบ Driver:
Get-PnpDevice | Where-Object {$_.name -like "*card*"}
# ถ้าไม่พบ → ดาวน์โหลด driver จากเว็บไซต์ผู้ผลิต

# 2. อัปเดต Driver:
# Device Manager → Smart Card Readers → Update Driver

# 3. ทดสอบบัตร:
# Windows Settings → Devices → Smart card readers → Manage smart cards
# → ใส่บัตร → ทดสอบการอ่าน

# 4. Restart Bridge Service:
Stop-Process -Name "SmartClinic.CardReader.Bridge" -Force
& "C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
```

### Application Won't Start
```
❌ Error: "The application failed to start" หรือ Port already in use
```

**แนวทางแก้ไข**:
```powershell
# 1. ตรวจสอบ .NET version:
dotnet --version

# 2. ถ้า .NET 10.0 ไม่มี:
# ดาวน์โหลด: https://dotnet.microsoft.com/download/dotnet/10.0

# 3. ถ้า Port 5247 ถูก occupy:
netstat -ano | findstr :5247
# Kill process: Stop-Process -Id <PID> -Force

# 4. ตรวจสอบ appsettings.json ถูกต้อง

# 5. เรียกใช้กับ verbose:
dotnet run --verbosity debug
```

---

## 🔐 Post-Installation Security Checklist

- [ ] Changed SuperAdmin password
- [ ] Set strong password (min 12 chars, mixed case, numbers, symbols)
- [ ] Disabled default accounts
- [ ] Configured firewall rules
- [ ] Enabled HTTPS (SSL certificate)
- [ ] Backed up database
- [ ] Configured database backups (scheduled)
- [ ] Set HTTPS only on production
- [ ] Limited network access (IP whitelist)
- [ ] Enabled audit logging
- [ ] Configured regular password change policy
- [ ] Removed temporary user accounts

---

## 📞 Support & Resources

- **Documentation**: README.md (ในโปรเจ็ก SmartClinic)
- **Issues/Bug Report**: https://github.com/yourusername/SmartClinic/issues
- **Email Support**: support@smartclinic.local

---

## ✨ Congratulations!

คุณได้ติดตั้ง SmartClinic สำเร็จแล้ว! 🎉

**ขั้นตอนถัดไป**:
1. เรียนรู้การใช้งาน (ดู README.md)
2. สำรองฐานข้อมูล
3. ตั้งค่า Clinic Master Data
4. สร้างบัญชีเจ้าหน้าที่
5. ฝึกอบรมผู้ใช้งาน

---

**ติดตั้งจำเน่อ**: January 2026  
**เวอร์ชัน**: 1.0.0  
**สนับสนุน**: ติดต่อ support@smartclinic.local
