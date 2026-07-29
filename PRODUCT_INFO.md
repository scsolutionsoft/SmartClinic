# 📋 SmartClinic v1.0.0 - ข้อมูลผลิตภัณฑ์และเอกสาร

**Project**: SmartClinic - ระบบบริหารจัดการคลินิกแบบอัจฉริยะ  
**Version**: 1.0.0  
**Release Date**: January 29, 2026  
**Language**: Thai (ไทย)

---

## 🎯 ภาพรวมผลิตภัณฑ์

SmartClinic เป็นแอปพลิเคชันบนเว็บแบบครบวงจร สำหรับการจัดการคลินิก บันทึกผู้ป่วย เวชระเบียน OPD และลายเซ็นดิจิทัล พร้อมการอ่านบัตรประชาชนอัจฉริยะผ่าน PC/SC smart card reader ระบบมีความปลอดภัยสูง พร้อมบทบาท (Roles) ที่ละเอียด เพื่อให้ผู้บริหารคลินิก สามารถจัดการข้อมูลหลัก สปสช. (NHSO Master Data) ด้วยตนเอง

---

## ✨ ฟีเจอร์หลัก

### 1. ระบบการรักษาความปลอดภัย
- ✅ ASP.NET Identity (Authentication & Authorization)
- ✅ บทบาท 4 ระดับ: SuperAdmin, AdminClinic, Nurse, User
- ✅ First-login password change enforcement
- ✅ Role-based access control (RBAC)

### 2. การจัดการคลินิก
- ✅ ลงทะเบียนคลินิก (ต้องเลือกจาก NHSO Master Data)
- ✅ SuperAdmin self-service NHSO Master CRUD
- ✅ Clinic code validation (9-10 ตัวอักษร)
- ✅ CSV import สำหรับ batch clinic registration

### 3. การบริหารผู้ป่วย
- ✅ ลงทะเบียนผู้ป่วย (Manual + Smart Card)
- ✅ บัตรประชาชน 13 หลัก
- ✅ ข้อมูลส่วนตัว (ชื่อ, ที่อยู่, เบอร์โทร, วันเกิด, เพศ)
- ✅ อัปโหลดรูปผู้ป่วย

### 4. เวชระเบียน OPD
- ✅ บันทึก OPD (อัปโหลด PDF)
- ✅ ประวัติการรักษา (History)
- ✅ Preview OPD PDF ในเบราว์เซอร์
- ✅ Storage ในฐานข้อมูล (VARBINARY)

### 5. ลายเซ็นดิจิทัล
- ✅ อัปโหลดเดี่ยว (Single)
- ✅ อัปโหลด Batch (13-digit filename validation)
- ✅ ประวัติลายเซ็นต่อผู้ป่วย
- ✅ Display ลายเซ็นในรายงาน

### 6. รายงาน & พิมพ์
- ✅ OPD Record Report (printable)
- ✅ Signature display ในรายงาน
- ✅ Date range filtering
- ✅ PDF export via browser print
- ✅ Statistics (signed/unsigned records)

### 7. Smart Card Integration
- ✅ PC/SC smart card reader support
- ✅ WebSocket bridge communication
- ✅ Auto-fill patient form จากบัตรประชาชน
- ✅ Error handling & fallback
- ✅ Mock API สำหรับ testing

### 8. User Experience
- ✅ ภาษาไทย (Full Thai UI)
- ✅ Responsive design (Bootstrap 5)
- ✅ Multiple themes (Bootswatch: Lux, Flatly, Minty, Journal, Materia, Morph)
- ✅ Status modals & notifications
- ✅ PDF preview modal

### 9. Data Governance
- ✅ SQL Server database encryption (recommended)
- ✅ Referential integrity (Foreign Keys)
- ✅ Auto-migration on startup
- ✅ Database backup support
- ✅ Audit logging (optional)

### 10. Clinic Staff Management
- ✅ Create clinic users (Nurse, User roles)
- ✅ Assign to clinic
- ✅ First-login password change
- ✅ Phone number as temporary password

---

## 📦 เอกสารที่รวมอยู่

### เอกสารหลัก
1. **README.md** - คู่มือการใช้งานที่สมบูรณ์ (ไทย)
2. **INSTALLATION.md** - ขั้นตอนการติดตั้งรายละเอียด (ไทย)
3. **QUICKSTART.md** - เริ่มต้นใช้งานใน 5 นาที (ไทย)
4. **DEPLOYMENT.md** - Deployment & download guide (ไทย/English)
5. **PRODUCT_INFO.md** - ไฟล์นี้

### Configuration Files
- `appsettings.json` - Application configuration
- `appsettings.Production.json` - Production settings
- `.env.example` - Environment variables template

### Scripts
- `install-bridge-final.bat` - Smart Card Bridge installer (Windows)
- `start-bridge.bat` - Bridge service launcher

### Source Code
- `Program.cs` - Application entry point
- `Controllers/` - Business logic
- `Models/` - EF Core entities
- `Views/` - Razor templates (Thai UI)
- `wwwroot/` - Static assets (CSS, JS, images)

---

## 🛠️ Technical Stack

### Backend
- **Framework**: ASP.NET Core 10.0 (MVC)
- **Language**: C# 13
- **ORM**: Entity Framework Core 10.0.0
- **Database**: SQL Server 2019+
- **Authentication**: ASP.NET Identity
- **APIs**: RESTful + WebSocket (PC/SC Bridge)

### Frontend
- **UI Framework**: Bootstrap 5
- **Themes**: Bootswatch (CDN-based)
- **JavaScript**: Vanilla JS + jQuery
- **PDF Viewer**: Browser native PDF viewer
- **Modal Framework**: Bootstrap Modals

### Smart Card Integration
- **PC/SC Library**: PCSC 7.0.1 + PCSC.Iso7816 7.0.1
- **Communication**: WebSocket (System.Net.WebSockets)
- **Bridge**: Separate Console app (net10.0)
- **Port**: 9999 (WebSocket)

### Database
- **DBMS**: SQL Server 2019 / 2022
- **Connection**: ADO.NET + EF Core
- **Schema**: 10+ tables (Clinics, Patients, TreatmentRecords, SignImgs, NhssoClinicMasters, Identity tables)
- **Storage**: VARBINARY (PDF, Images)

### Deployment
- **Runtime**: .NET 10.0 (64-bit)
- **OS**: Windows Server 2019/2022, Windows 10/11 Pro
- **Web Server**: Kestrel (built-in) or IIS
- **SSL/TLS**: HTTPS support (self-signed or CA-issued)

---

## 📋 System Requirements

### Minimum
- Windows Server 2019 / Windows 10 Pro (64-bit)
- Intel Core i5 / AMD Ryzen 5
- 2GB RAM
- 50GB+ storage
- .NET 10.0 Runtime
- SQL Server 2019

### Recommended
- Windows Server 2022
- Intel Xeon / AMD Ryzen 7
- 4GB+ RAM
- 100GB+ SSD storage
- .NET 10.0 SDK (for development)
- SQL Server 2022
- Smart Card Reader (PC/SC compatible)

### Network
- LAN/Internet connectivity
- HTTPS support (port 443 recommended)
- WebSocket support (port 9999 for bridge)

---

## 🚀 Quick Start Commands

### Install & Run (Local Development)
```bash
# 1. Prerequisites
dotnet --version              # Check .NET 10.0
sqlcmd -S localhost           # Check SQL Server

# 2. Configure
# Edit appsettings.json → Update connection string

# 3. Run
cd SmartClinic
dotnet run                    # Starts on https://localhost:5247

# 4. Login
# Username: superadmin@smartclinic.local
# Password: 0999999999 (change on first login)
```

### Install Smart Card Bridge
```bash
cd SmartCardReaderBridge\SmartClinic.CardReader.Bridge
.\install-bridge-final.bat    # Right-click → Run as administrator

# Start bridge
"C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
```

### Build Release
```bash
cd SmartClinic
dotnet build -c Release       # Produces bin\Release\net10.0\
```

---

## 📊 Architecture Overview

```
┌─────────────────────────────────────────────────────────────┐
│                    Web Browser (Client)                     │
│  ┌────────────────────────────────────────────────────────┐ │
│  │  SmartClinic Web UI (https://localhost:5247)           │ │
│  │  - Patient Registration                                │ │
│  │  - OPD Records                                         │ │
│  │  - Signatures                                          │ │
│  │  - Reports & Print                                    │ │
│  └────────────────────────────────────────────────────────┘ │
│                           ↓ HTTP/HTTPS                       │
└─────────────────────────────────────────────────────────────┘
                            ↓
         ┌──────────────────────────────────────┐
         │  ASP.NET Core 10.0 MVC Web App       │
         ├──────────────────────────────────────┤
         │  - Controllers (Business Logic)      │
         │  - Views (Razor Templates - Thai UI) │
         │  - Entity Framework Core             │
         │  - Identity & Authentication         │
         │  - WebSocket Server (Smart Card API) │
         └──────────────────────────────────────┘
              ↓                          ↓
        SQL Server              Smart Card Bridge
      (Patient Data)        (WebSocket ws://localhost:9999)
                                        ↓
                            ┌──────────────────────┐
                            │  PC/SC Smart Reader  │
                            │  - PCSC API          │
                            │  - Card Data Read    │
                            │  - APDU Commands     │
                            └──────────────────────┘
                                        ↓
                              Physical Smart Card
```

---

## 🔐 Security Features

### Authentication & Authorization
- ASP.NET Identity (passwords hashed with PBKDF2)
- Role-based access control (RBAC)
- First-login password change enforcement
- Session management

### Data Protection
- SQL Server encryption (TDE recommended)
- HTTPS/TLS for data in transit
- Password policies
- Audit logging support

### Smart Card
- PC/SC protocol (Windows-native)
- Citizen ID validation (13 digits)
- Error handling & fallback
- No card data storage (read-only)

---

## 📈 Database Schema (Key Tables)

| Table | Purpose | Key Columns |
|-------|---------|-------------|
| Clinics | Registered clinics | ClinicCode, Name, Address, AdminId |
| Patients | Patient records | Id, CitizenId, FullName, Address, Photo |
| TreatmentRecords | OPD records | Id, PatientId, OPDPdfData, VisitDate |
| SignImgs | Signature images | Id, PatientId, SignatureData, UploadDate |
| NhssoClinicMasters | NHSO master data | ClinicCode, Name, Address, IsActive |
| AspNetUsers | System users | Id, UserName, Email, ClinicCode, MustChangePassword |
| AspNetRoles | Role definitions | Id, Name (SuperAdmin, AdminClinic, Nurse, User) |
| AspNetUserRoles | User-role mappings | UserId, RoleId |

---

## 🎯 User Roles & Permissions

| Role | Features | Restrictions |
|------|----------|--------------|
| **SuperAdmin** | Manage NHSO Master, All system config | Cannot delete clinic if in use |
| **AdminClinic** | Manage own clinic staff, View all records | Limited to own clinic data |
| **Nurse** | Create patient records, Upload OPD/signatures | Cannot delete or modify others' records |
| **User** | View patient info, Limited OPD upload | Read-only for most functions |

---

## 📞 Support & Contact

- **Email**: support@smartclinic.local
- **GitHub**: https://github.com/yourusername/SmartClinic
- **Documentation**: README.md, INSTALLATION.md
- **Issues**: GitHub Issues tracker
- **Version**: 1.0.0
- **License**: [Specify your license]

---

## 🔄 Version History

### v1.0.0 (January 29, 2026)
**Initial Release** - Complete SmartClinic system
- ✅ Web application (ASP.NET Core 10.0 MVC)
- ✅ Patient management + smart card integration
- ✅ OPD records & digital signatures
- ✅ SuperAdmin clinic master management
- ✅ Full Thai UI translation
- ✅ Smart Card Reader Bridge (PC/SC + WebSocket)
- ✅ Multi-role access control
- ✅ Reports & printing with signatures
- ✅ Database auto-migration
- ✅ Theme switching (Bootswatch)

---

## 📝 License & Copyright

Copyright © 2026 SmartClinic. All rights reserved.

---

## ✅ Deployment Checklist

Before deploying to production:

- [ ] .NET 10.0 runtime installed
- [ ] SQL Server 2019+ configured
- [ ] HTTPS certificate acquired
- [ ] Firewall rules configured
- [ ] Database backup plan established
- [ ] Admin password changed
- [ ] NHSO clinic master data loaded
- [ ] Test user accounts created
- [ ] Smart card reader tested (if applicable)
- [ ] Documentation shared with staff
- [ ] Training completed

---

## 🎓 Getting Help

1. **Quick Issues**: See QUICKSTART.md
2. **Installation Help**: See INSTALLATION.md
3. **Usage Guide**: See README.md
4. **Deployment**: See DEPLOYMENT.md
5. **Bugs/Issues**: GitHub Issues
6. **Email Support**: support@smartclinic.local

---

**Final Note**: SmartClinic v1.0.0 is production-ready and fully tested. For questions or custom requirements, please contact the development team.

---

**Document Version**: 1.0  
**Last Updated**: January 29, 2026  
**Status**: ✅ Ready for Production
