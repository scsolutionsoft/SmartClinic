SmartClinic Card Reader Bridge v1.0.4

This package provides the SmartClinic bridge installer and launcher files.
Version 1.0.4 adds Chromium Local Network Access support and restricts
smart-card access to approved SmartClinic web origins.

Files:
- install-bridge-final.bat : Installer script for Windows
- start-bridge.bat         : Launcher script for the bridge service

After installation, the bridge listens on ws://localhost:9999/card
and exposes its health check at http://localhost:9999/status.

Microsoft Edge / Google Chrome:
1. The SmartClinic website must use a valid trusted HTTPS certificate.
2. When prompted, allow Local network access for the SmartClinic website.
3. If access was previously blocked, reset the site's permission and try again.

Allowed origins:
- https://paweerapatclinic.online
- http://localhost and https://localhost (development)

Administrators can replace the allowlist before starting the bridge:
  set SMARTCLINIC_ALLOWED_ORIGINS=https://clinic.example.com,https://backup.example.com
