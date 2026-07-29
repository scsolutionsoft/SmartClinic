# 📦 SmartClinic - Deployment & Download Guide

**สำหรับการดาวน์โหลดและติดตั้งบนเครื่องอื่น ๆ**

---

## 📥 วิธีการดาวน์โหลด

### Option 1: GitHub Release (Recommended)
1. ไปที่ https://github.com/yourusername/SmartClinic/releases
2. ดาวน์โหลด Latest Release:
   - `SmartClinic-v1.0.0-WebApp.zip` (Web application)
   - `SmartClinic-CardReader-Bridge-v1.0.0.zip` (Smart card bridge)
3. Extract ไฟล์ไปยัง folder
4. ทำตามขั้นตอนการติดตั้ง

### Option 2: Clone from GitHub
```bash
git clone https://github.com/yourusername/SmartClinic.git
cd SmartClinic
```

### Option 3: Download Source Code Manually
1. ไปที่ GitHub repository
2. คลิก Code → Download ZIP
3. Extract ไฟล์

---

## 🔧 ไฟล์ที่จำเป็นสำหรับ Deployment

### สำหรับ Web Application
```
SmartClinic/
├── bin/Release/net10.0/
│   ├── SmartClinic.Web.exe        # Main executable
│   ├── SmartClinic.Web.dll        # Main assembly
│   ├── appsettings.json           # Configuration
│   ├── appsettings.Production.json # Production config
│   └── (dependencies)             # All required DLLs
├── appsettings.json               # Configuration file
├── README.md                       # Documentation
├── INSTALLATION.md                # Installation guide
└── QUICKSTART.md                  # Quick start guide
```

### สำหรับ Smart Card Bridge
```
SmartCardReaderBridge/SmartClinic.CardReader.Bridge/
├── bin/Release/net10.0/
│   ├── SmartClinic.CardReader.Bridge.exe  # Main executable
│   ├── (dependencies)                      # All required DLLs
├── install-bridge-final.bat               # Installation script
├── start-bridge.bat                       # Service launcher
└── README.md                              # Bridge documentation
```

---

## 🚀 ขั้นตอนการติดตั้งบนเครื่องอื่น ๆ

### ขั้นตอนที่ 1: ตรวจสอบ Prerequisites

```powershell
# 1. ตรวจสอบ .NET 10.0
dotnet --version
# ผลลัพธ์ที่คาดหวัง: 10.0.x

# ถ้าไม่มี ดาวน์โหลดจาก:
# https://dotnet.microsoft.com/download/dotnet/10.0

# 2. ตรวจสอบ SQL Server
# Services → SQL Server (MSSQLSERVER) → Status: Running

# 3. ตรวจสอบ PowerShell เวอร์ชัน
$PSVersionTable.PSVersion
# ผลลัพธ์ที่คาดหวัง: 5.0 หรือสูงกว่า
```

### ขั้นตอนที่ 2: ติดตั้ง Web Application

```bash
# 1. Extract SmartClinic-v1.0.0-WebApp.zip
Expand-Archive "SmartClinic-v1.0.0-WebApp.zip" -DestinationPath "c:\SmartClinic"

# 2. เข้า folder
cd c:\SmartClinic

# 3. แก้ไข appsettings.json
# - Server name: CLINIC-PC (หรือ server name ของคุณ)
# - Database: SmartClinic
# - Integrated Security: true

# 4. สตาร์ท application
.\bin\Release\net10.0\SmartClinic.Web.exe

# หรือจาก Visual Studio:
dotnet run
```

### ขั้นตอนที่ 3: ติดตั้ง Bridge (ถ้าต้องการ)

```powershell
# 1. Extract SmartClinic-CardReader-Bridge-v1.0.0.zip
Expand-Archive "SmartClinic-CardReader-Bridge-v1.0.0.zip" -DestinationPath "c:\SmartCardBridge"

# 2. Right-click install-bridge-final.bat → Run as administrator
# หรือ:
cd "c:\SmartCardBridge\SmartClinic.CardReader.Bridge"
.\install-bridge-final.bat

# 3. ให้สคริปต์ทำงาน (จะ build, copy files, create launcher)

# 4. สตาร์ท Bridge:
"C:\Program Files\SmartClinic\CardReader\start-bridge.bat"
```

---

## 📋 Deployment Checklist

### ก่อนติดตั้ง
- [ ] Windows Server 2019/2022 หรือ Windows 10/11 Pro
- [ ] .NET 10.0 runtime ติดตั้งแล้ว
- [ ] SQL Server 2019+ running
- [ ] Administrator access
- [ ] Internet connection (สำหรับ download)

### ระหว่างติดตั้ง
- [ ] Extract files ไปยัง folder ที่เหมาะสม
- [ ] แก้ไข appsettings.json ให้ถูกต้อง
- [ ] Database SmartClinic สร้างแล้ว
- [ ] Smart card reader driver ติดตั้ง (ถ้ามี)

### หลังติดตั้ง
- [ ] Web application เปิดที่ https://localhost:5247
- [ ] Bridge WebSocket listening บน ws://localhost:9999/card
- [ ] Database migration complete
- [ ] SuperAdmin login สำเร็จ
- [ ] ทดสอบ patient registration
- [ ] ทดสอบ smart card read (ถ้าติดตั้ง bridge)

---

## 🐳 Docker Deployment (Optional)

### สร้าง Docker Image

```dockerfile
# Dockerfile
FROM mcr.microsoft.com/dotnet/runtime:10.0 AS base
WORKDIR /app
EXPOSE 5247

FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /src
COPY ["SmartClinic.Web.csproj", "./"]
RUN dotnet restore "SmartClinic.Web.csproj"
COPY . .
RUN dotnet build "SmartClinic.Web.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "SmartClinic.Web.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "SmartClinic.Web.dll"]
```

### Build & Run Docker

```bash
# Build image
docker build -t smartclinic:1.0.0 .

# Run container
docker run -d \
  --name smartclinic \
  -p 5247:5247 \
  -e ConnectionStrings__DefaultConnection="Server=host.docker.internal;Database=SmartClinic;Integrated Security=true;" \
  smartclinic:1.0.0
```

---

## 🌐 Production Deployment

### HTTPS Setup (IIS)

```powershell
# 1. Install IIS
Enable-WindowsOptionalFeature -Online -FeatureName IIS-WebServer

# 2. Create Application in IIS
# IIS Manager → Add Application Pool → Add Website

# 3. Import SSL Certificate
# IIS Manager → Server Certificates → Import

# 4. Bind HTTPS
# Website → Binding → Add HTTPS binding with certificate
```

### Configure Application Pool

```powershell
# Set .NET version
Set-WebConfigurationProperty -PSPath "IIS:\AppPools\SmartClinic" `
  -Name "managedRuntimeVersion" -Value "v4.0"

# Set Pipeline Mode
Set-WebConfigurationProperty -PSPath "IIS:\AppPools\SmartClinic" `
  -Name "managedPipelineMode" -Value "Integrated"

# Start Application Pool
Start-WebAppPool -Name "SmartClinic"
```

### Environment Configuration

```json
// appsettings.Production.json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=clinic-db-server;Database=SmartClinic;User Id=clinicadmin;Password=SecurePassword123;"
  },
  "Logging": {
    "LogLevel": {
      "Default": "Warning"
    }
  },
  "AllowedHosts": "*.smartclinic.local",
  "Kestrel": {
    "Endpoints": {
      "Https": {
        "Url": "https://localhost:443",
        "Certificate": {
          "Path": "/etc/ssl/certs/smartclinic.pfx",
          "Password": "CertificatePassword"
        }
      }
    }
  }
}
```

---

## 📊 Load Balancing (Multiple Servers)

### Nginx Configuration

```nginx
upstream smartclinic_backend {
    server clinic-server-1:5247;
    server clinic-server-2:5247;
    server clinic-server-3:5247;
}

server {
    listen 443 ssl http2;
    server_name smartclinic.local;

    ssl_certificate /etc/nginx/ssl/smartclinic.crt;
    ssl_certificate_key /etc/nginx/ssl/smartclinic.key;

    location / {
        proxy_pass http://smartclinic_backend;
        proxy_set_header Host $host;
        proxy_set_header X-Real-IP $remote_addr;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }

    location /ws {
        proxy_pass ws://smartclinic_backend;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection "upgrade";
    }
}
```

---

## 🔒 Security Hardening

### Windows Firewall Rules

```powershell
# Allow web traffic (HTTP/HTTPS)
New-NetFirewallRule -DisplayName "SmartClinic HTTP" `
  -Direction Inbound -Protocol TCP -LocalPort 5247 -Action Allow

# Allow Bridge WebSocket
New-NetFirewallRule -DisplayName "SmartClinic Bridge" `
  -Direction Inbound -Protocol TCP -LocalPort 9999 -Action Allow

# Restrict to specific subnets (recommended)
New-NetFirewallRule -DisplayName "SmartClinic Internal" `
  -Direction Inbound -Protocol TCP -LocalPort 5247 `
  -RemoteAddress 192.168.1.0/24 -Action Allow
```

### Database Backup

```sql
-- Automatic daily backup
BACKUP DATABASE SmartClinic 
TO DISK = 'C:\Backups\SmartClinic_Daily.bak'
WITH INIT, COMPRESSION;

-- Scheduled via SQL Server Agent
-- Create Job: SmartClinic Daily Backup
-- Schedule: Every day at 2:00 AM
```

### Monitoring & Logging

```powershell
# Enable Application Event Logging
$EventLog = "SmartClinic"
New-EventLog -LogName $EventLog -Source "SmartClinic.Web" -ErrorAction SilentlyContinue

# Monitor Bridge process
Get-Process | Where-Object {$_.ProcessName -like "*CardReader*"} | 
  Select-Object Name, CPU, Memory, Handles

# Monitor Web application memory/CPU
Get-Process | Where-Object {$_.ProcessName -like "*SmartClinic*"} | 
  ForEach-Object { [pscustomobject]@{
    Name = $_.ProcessName
    CPU = $_.CPU
    Memory = "{0:N0}" -f ($_.WorkingSet/1MB) + " MB"
  }} | Format-Table
```

---

## 📚 Additional Resources

- **Microsoft Docs**: https://docs.microsoft.com/en-us/dotnet/
- **ASP.NET Core**: https://docs.microsoft.com/en-us/aspnet/core/
- **SQL Server**: https://docs.microsoft.com/en-us/sql/sql-server/
- **IIS**: https://docs.microsoft.com/en-us/iis/
- **Docker**: https://docs.docker.com/

---

## 🆘 Troubleshooting Deployment

### Application won't start
```
❌ Error: "Could not load type from assembly"
✓ Solution: Ensure .NET 10.0 is properly installed
✓ Command: dotnet --version
```

### Database connection fails
```
❌ Error: "Cannot open database requested"
✓ Solution: Verify connection string and SQL Server running
✓ Command: sqlcmd -S <ServerName> -Q "SELECT @@Version"
```

### Port already in use
```
❌ Error: "Address already in use"
✓ Solution: Find and kill process using port
✓ Command: netstat -ano | findstr :5247
✓ Kill: taskkill /PID <PID> /F
```

### Bridge won't connect
```
❌ Error: "WebSocket connection refused"
✓ Solution: Check firewall and bridge service
✓ Command: netstat -ano | findstr :9999
✓ Restart: C:\Program Files\SmartClinic\CardReader\start-bridge.bat
```

---

## 📞 Support & Documentation

- Documentation: README.md
- Installation Guide: INSTALLATION.md
- Quick Start: QUICKSTART.md
- GitHub Issues: https://github.com/yourusername/SmartClinic/issues
- Email: support@smartclinic.local

---

**Version**: 1.0.0  
**Last Updated**: January 2026  
**Author**: SmartClinic Development Team
