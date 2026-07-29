# 📥 SmartClinic v1.0.0 - Download & Files Guide

**สำหรับการดาวน์โหลดไฟล์ที่จำเป็นสำหรับติดตั้ง SmartClinic**

---

## 📦 ไฟล์ที่พร้อมสำหรับดาวน์โหลด

### 1. Web Application Package
**ไฟล์**: `SmartClinic-v1.0.0-WebApp.zip`

**ขนาด**: ~150 MB (ประมาณ)

**ประกอบด้วย**:
```
SmartClinic-v1.0.0-WebApp/
├── bin/Release/net10.0/
│   ├── SmartClinic.Web.exe            ← Main executable
│   ├── SmartClinic.Web.dll
│   ├── appsettings.json               ← Configuration
│   ├── appsettings.Production.json
│   ├── wwwroot/                       ← Static files (CSS, JS)
│   └── (All dependencies & DLLs)
├── appsettings.json                   ← Config template
├── README.md                          ← Usage guide (Thai)
├── INSTALLATION.md                    ← Installation guide (Thai)
├── QUICKSTART.md                      ← Quick start (Thai)
├── DEPLOYMENT.md                      ← Deployment guide
├── PRODUCT_INFO.md                    ← Product info
└── LICENSE                            ← License file
```

**วิธีใช้**:
```powershell
# Extract
Expand-Archive "SmartClinic-v1.0.0-WebApp.zip" -DestinationPath "c:\SmartClinic"

# Edit config
notepad c:\SmartClinic\appsettings.json
# Update connection string

# Run
cd c:\SmartClinic
.\bin\Release\net10.0\SmartClinic.Web.exe
```

---

### 2. Smart Card Reader Bridge Package
**ไฟล์**: `SmartClinic-CardReader-Bridge-v1.0.0.zip`

**ขนาด**: ~50 MB (ประมาณ)

**ประกอบด้วย**:
```
SmartCardReaderBridge-v1.0.0/
├── SmartClinic.CardReader.Bridge/
│   ├── bin/Release/net10.0/
│   │   ├── SmartClinic.CardReader.Bridge.exe  ← Bridge executable
│   │   └── (PCSC dependencies)
│   ├── install-bridge-final.bat               ← Installer script
│   ├── start-bridge.bat                       ← Launcher script
│   └── README.md                              ← Bridge documentation
└── INSTALLATION.md                            ← Setup instructions
```

**วิธีใช้**:
```bash
# Extract
Expand-Archive "SmartClinic-CardReader-Bridge-v1.0.0.zip" `
  -DestinationPath "c:\SmartCardBridge"

# Install (Right-click → Run as administrator)
c:\SmartCardBridge\SmartClinic.CardReader.Bridge\install-bridge-final.bat

# Run
"C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
```

---

### 3. Complete Package (Web + Bridge)
**ไฟล์**: `SmartClinic-v1.0.0-Complete.zip`

**ขนาด**: ~200 MB (ประมาณ)

**ประกอบด้วย**:
- Web application (release build)
- Smart Card Bridge (release build)
- All documentation files
- Configuration templates
- Installation scripts

---

### 4. Source Code (Development)
**ไฟล์**: `SmartClinic-v1.0.0-Source.zip`

**ขนาด**: ~20 MB (ประมาณ)

**ประกอบด้วย**:
- Complete C# source code
- SmartClinic.Web project
- SmartClinic.CardReader.Bridge project
- .csproj files
- All configuration files
- Documentation

**ต้องการ**:
- Visual Studio 2022 หรือ VS Code
- .NET 10.0 SDK
- SQL Server + Database tools

---

## 🔗 Download Links

| Package | Link | Size | Type |
|---------|------|------|------|
| Web App | https://github.com/yourusername/SmartClinic/releases/download/v1.0.0/SmartClinic-v1.0.0-WebApp.zip | 150 MB | Binary |
| Bridge | https://github.com/yourusername/SmartClinic/releases/download/v1.0.0/SmartClinic-v1.0.0-CardReader-Bridge.zip | 50 MB | Binary |
| Complete | https://github.com/yourusername/SmartClinic/releases/download/v1.0.0/SmartClinic-v1.0.0-Complete.zip | 200 MB | Binary |
| Source | https://github.com/yourusername/SmartClinic/releases/download/v1.0.0/SmartClinic-v1.0.0-Source.zip | 20 MB | Source |

---

## 📋 Installation Files Reference

### For IT Admin (Server Setup)

**ขั้นตอนที่ 1 - ดาวน์โหลด**:
1. ดาวน์โหลด `SmartClinic-v1.0.0-Complete.zip`
2. ตรวจสอบ checksum: `SmartClinic-v1.0.0-Complete.zip.sha256`

**ขั้นตอนที่ 2 - ติดตั้ง**:
```powershell
# 1. Extract ทั้ง 2 packages
Expand-Archive "SmartClinic-v1.0.0-WebApp.zip" -DestinationPath "c:\SmartClinic"
Expand-Archive "SmartClinic-v1.0.0-CardReader-Bridge.zip" -DestinationPath "c:\SmartCardBridge"

# 2. Configure Web App
cd c:\SmartClinic
# Edit appsettings.json with connection string

# 3. Install Bridge (Admin)
cd c:\SmartCardBridge\SmartClinic.CardReader.Bridge
.\install-bridge-final.bat

# 4. Start services
# Terminal 1 (Web App):
cd c:\SmartClinic
.\bin\Release\net10.0\SmartClinic.Web.exe

# Terminal 2 (Bridge):
"C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
```

---

### For Developers (Source Code)

**ขั้นตอนที่ 1 - ตั้งค่า Development Environment**:
```bash
# Check prerequisites
dotnet --version              # Requires: 10.0
sqlcmd -S localhost           # Requires: SQL Server

# Visual Studio 2022
# - Install: ASP.NET and web development workload
# - Install: .NET 10.0 development tools
```

**ขั้นตอนที่ 2 - ดาวน์โหลด Source**:
```bash
# Option A: GitHub Release
Expand-Archive "SmartClinic-v1.0.0-Source.zip"

# Option B: Git Clone
git clone https://github.com/yourusername/SmartClinic.git
cd SmartClinic
git checkout v1.0.0
```

**ขั้นตอนที่ 3 - Build & Run**:
```bash
cd SmartClinic

# Restore packages
dotnet restore

# Build
dotnet build

# Run (Debug)
dotnet run

# Or in Visual Studio 2022:
# File → Open Project/Solution → SmartClinic.sln
# Press F5 to start debugging
```

---

## ✅ Pre-Download Checklist

### สำหรับ Server Installation
- [ ] Internet connection (download ~200 MB)
- [ ] Administrator access
- [ ] 500 MB free disk space
- [ ] 7-Zip หรือ WinRAR สำหรับ extract
- [ ] Text editor (Notepad++ หรือ VS Code)
- [ ] SQL Server Management Studio (SSMS)

### สำหรับ Smart Card Setup
- [ ] Smart card reader (USB หรือ Integrated)
- [ ] Windows Smart Card Reader driver
- [ ] Administrator access
- [ ] 200 MB free disk space

### สำหรับ Development
- [ ] Visual Studio 2022 (Community free)
- [ ] .NET 10.0 SDK
- [ ] SQL Server + SSMS
- [ ] Git (optional)
- [ ] 2GB free disk space

---

## 🔒 Checksum Verification (Security)

**สำหรับตรวจสอบความปลอดภัยของไฟล์ที่ดาวน์โหลด**:

### Get Checksum
```powershell
# Windows:
(Get-FileHash "SmartClinic-v1.0.0-WebApp.zip" -Algorithm SHA256).Hash

# Linux/Mac:
shasum -a 256 SmartClinic-v1.0.0-WebApp.zip
```

### Verify
```powershell
# Expected SHA256:
# SmartClinic-v1.0.0-WebApp.zip
# 0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0

# Verify Windows:
(Get-FileHash "SmartClinic-v1.0.0-WebApp.zip" -Algorithm SHA256).Hash -eq "0a1b2c3d4e5f6a7b8c9d0e1f2a3b4c5d6e7f8a9b0c1d2e3f4a5b6c7d8e9f0"
```

---

## 📊 File Structure After Download

### After Extracting Web App
```
c:\SmartClinic\
├── bin/
│   └── Release/
│       └── net10.0/
│           ├── SmartClinic.Web.exe      ← Run this
│           ├── SmartClinic.Web.dll
│           ├── appsettings.json
│           └── wwwroot/
├── appsettings.json                     ← Edit this
├── appsettings.Production.json
├── README.md
├── INSTALLATION.md
└── QUICKSTART.md
```

### After Extracting Bridge
```
c:\SmartCardBridge\SmartClinic.CardReader.Bridge\
├── bin/
│   └── Release/
│       └── net10.0/
│           ├── SmartClinic.CardReader.Bridge.exe
│           └── (dependencies)
├── install-bridge-final.bat             ← Run this first
├── start-bridge.bat                     ← Then run this
└── README.md
```

---

## 🚀 Quick Start After Download

### 1. Web App (5 minutes)
```bash
# Extract
Expand-Archive SmartClinic-v1.0.0-WebApp.zip -DestinationPath c:\SmartClinic

# Configure
cd c:\SmartClinic
notepad appsettings.json
# Update: "Server=<YOUR_SERVER>; Database=SmartClinic"

# Run
.\bin\Release\net10.0\SmartClinic.Web.exe

# Open browser
# https://localhost:5247
# Login: superadmin@smartclinic.local / 0999999999
```

### 2. Bridge (3 minutes)
```bash
# Extract
Expand-Archive SmartClinic-v1.0.0-CardReader-Bridge.zip -DestinationPath c:\SmartCardBridge

# Install (Run as Administrator)
cd c:\SmartCardBridge\SmartClinic.CardReader.Bridge
.\install-bridge-final.bat

# Run
C:\Program Files\SmartClinic\CardReader\start-bridge.bat

# Expected output:
# "WebSocket server listening on ws://localhost:9999/card"
```

---

## 📞 Download Support

**ถ้าดาวน์โหลดช้า**:
- ลองใช้ VPN/Proxy
- ลองดาวน์โหลดในเวลาอื่น ๆ
- ตรวจสอบความเร็ว Internet

**ถ้าไฟล์เสียหาย**:
- ลบไฟล์ที่ดาวน์โหลดแล้ว
- ลองดาวน์โหลดใหม่
- ตรวจสอบ antivirus ไม่ block
- ติดต่อ support@smartclinic.local

**ถ้า Extract ล้มเหลว**:
- ติดตั้ง 7-Zip หรือ WinRAR ที่ล่าสุด
- ลองใช้ `Expand-Archive` ใน PowerShell
- ตรวจสอบ disk space พอ

---

## 🔗 Resources

- **Download Center**: https://github.com/yourusername/SmartClinic/releases
- **Documentation**: README.md (ในแต่ละ package)
- **Installation Guide**: INSTALLATION.md
- **Quick Start**: QUICKSTART.md
- **Support**: support@smartclinic.local

---

## ⏱️ Download Time Estimates

| Package | Size | Speed | Time |
|---------|------|-------|------|
| Web App | 150 MB | 10 Mbps | 2 min |
| Bridge | 50 MB | 10 Mbps | 40 sec |
| Complete | 200 MB | 10 Mbps | 2.5 min |
| Source | 20 MB | 10 Mbps | 16 sec |

---

**Version**: 1.0.0  
**Last Updated**: January 29, 2026  
**Status**: ✅ Ready for Download

สำหรับคำถามเพิ่มเติม โปรดติดต่อ: support@smartclinic.local
