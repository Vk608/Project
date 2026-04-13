# 🚀 Full-Stack Repository Setup Guide

Complete step-by-step instructions to set up and run the PubMed Validation Full-Stack application on a new device.

---

## 📋 Prerequisites

Before starting, ensure you have the following installed on your machine:

### For Backend (.NET)
- **.NET 10.0 SDK** ([Download here](https://dotnet.microsoft.com/download))
  - Check version: `dotnet --version`

### For Frontend (Angular)
- **Node.js 18+** (includes npm) ([Download here](https://nodejs.org/))
  - Check version: `node --version` and `npm --version`
- **Git** ([Download here](https://git-scm.com/)) - to clone the repository

### System Requirements
- **Disk space**: ~1-2 GB (dependencies + build artifacts)
- **RAM**: 4GB minimum (8GB recommended)
- **Ports needed**: 5000 (Backend), 4200 (Frontend)

---

## 📥 Step 1: Clone the Repository

Open a terminal/command prompt and run:

```bash
git clone <your-github-repo-url>
cd FullStack
```

Or if you already have the code, navigate to the FullStack folder:

```bash
cd FullStack
```

---

## 🔧 Step 2: Setup Backend (.NET)

### 2.1 Navigate to Backend Folder
```bash
cd Backend
```

### 2.2 Restore NuGet Dependencies
```bash
dotnet restore
```

This installs all required NuGet packages (EPPlus, Swashbuckle, etc.).

**Expected output**: Should show "Restore completed in X seconds"

### 2.3 Build the Backend
```bash
dotnet build
```

**Expected output**: "Build succeeded" message

### 2.4 Verify Build Structure
Check that build artifacts were created:
```bash
# Windows
dir bin\Debug\net10.0

# Mac/Linux
ls bin/Debug/net10.0
```

Expected files:
- `Backend.dll`
- `Backend.runtimeconfig.json`
- `appsettings.json`
- `config.json`

### 2.5 Key Configuration Files

Ensure these files exist in the Backend folder:
- **appsettings.json** - Logging configuration (included)
- **config.json** - Application pipeline configuration (included)

---

## 🌐 Step 3: Setup Frontend (Angular)

### 3.1 Navigate to Frontend Folder
```bash
cd ../Frontend
```

### 3.2 Install Dependencies
```bash
npm install
```

**Duration**: 2-5 minutes (depends on internet speed)

**Expected output**: Shows number of packages installed

### 3.3 Verify Installation
```bash
npm list @angular/cli
```

Should show Angular CLI version 17.x.x installed.

---

## ▶️ Step 4: Run the Application

### Option A: Run Both Backend and Frontend (Recommended)

**Terminal 1 - Backend:**
```bash
cd Backend
dotnet run
```

Expected output:
```
=== FAB Batch Validator Web API ===
[Main] Loading configuration...
[Main] Configuration loaded successfully.
...
info: Microsoft.Hosting.Lifetime[14]
      Now listening on: http://localhost:5000
      Now listening on: https://localhost:5001
```

**Terminal 2 - Frontend:**
```bash
cd Frontend
npm start
```

Expected output:
```
✔ Compiled successfully.
✔ Built successfully.

Application bundle generated successfully.
```

This will automatically open `http://localhost:4200` in your default browser.

### Option B: Run Backend Only
```bash
cd Backend
dotnet run
```

Access API at: `http://localhost:5000/api/records`

### Option C: Run Frontend Only (requires running Backend separately)
```bash
cd Frontend
npm start
```

---

## ✅ Step 5: Verify Everything Works

### 5.1 Test Backend API
Open a new terminal and run:

```bash
# Test 1: Check if backend is responding
curl http://localhost:5000/api/records

# Expected: Returns JSON array of records or empty array []
```

Or in browser: http://localhost:5000/swagger/index.html (Swagger UI)

### 5.2 Test Frontend
1. Open browser and go to: http://localhost:4200
2. You should see: **"PubMed Validation Dashboard"**
3. Verify records appear in the table
4. Test filtering and sorting controls

### 5.3 Complete Validation Test
```bash
# Start a validation job
curl -X POST http://localhost:5000/api/validation/validate

# Expected response (202 Accepted):
# {
#   "jobId": "550e8400-e29b-41d4-a716-446655440000",
#   "status": "Pending",
#   "message": "Validation job queued"
# }
```

---

## 🛠️ Available Commands

### Backend Commands
```bash
cd Backend

# Run development server
dotnet run

# Build project
dotnet build

# Build for production
dotnet build -c Release

# Run tests (if available)
dotnet test

# Clean build artifacts
dotnet clean
```

### Frontend Commands
```bash
cd Frontend

# Start dev server (opens browser)
npm start

# Build for production
npm run build:prod

# Build and watch for changes
npm run watch

# Run unit tests
npm test

# Run linting
npm lint
```

---

## 🔍 Troubleshooting

### ❌ Backend Issues

#### "dotnet: Command not found"
**Solution**: Install .NET 10.0 SDK from https://dotnet.microsoft.com/download

#### "Port 5000 already in use"
```bash
# Windows - Kill process using port 5000
netstat -ano | findstr :5000
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:5000 | xargs kill -9
```

#### "Error: Configuration file config.json not found"
**Solution**: Ensure `config.json` exists in Backend folder. If missing, check git status:
```bash
git status
git pull
```

#### "Build failed: Could not find required packages"
**Solution**: Clear NuGet cache and restore again
```bash
dotnet nuget locals all --clear
dotnet restore
```

---

### ❌ Frontend Issues

#### "npm: Command not found"
**Solution**: Install Node.js from https://nodejs.org/

#### "ng: Command not found"
**Solution**: Install Angular CLI globally or use npm
```bash
npm install -g @angular/cli@17
# Or use npx
npx ng serve
```

#### "Port 4200 already in use"
```bash
# Use different port
ng serve --port 4201

# Or kill existing process
# Windows
netstat -ano | findstr :4200
taskkill /PID <PID> /F

# Mac/Linux
lsof -ti:4200 | xargs kill -9
```

#### "npm install fails"
**Solution**: Try these steps
```bash
# Clear npm cache
npm cache clean --force

# Delete node_modules and lock file
rm -rf node_modules package-lock.json

# Reinstall
npm install
```

#### "CORS errors in browser console"
**Cause**: Backend doesn't have CORS enabled
**Solution**: Verify CORS is configured in Backend Program.cs (should already be there)

---

### ❌ General Issues

#### "Application runs but shows blank page"
1. Check browser console for errors (F12)
2. Verify backend is running: http://localhost:5000/api/records
3. Check network tab in DevTools for failed requests

#### "Validation job stays in 'Pending' status"
1. Check backend logs for errors
2. Verify `jobs-metadata.json` exists in Backend folder
3. Try restarting backend: Stop (Ctrl+C) and run `dotnet run` again

#### "Cannot connect to http://localhost:5000"
1. Backend might not be running - check Terminal 1
2. Backend might have crashed - check error messages
3. Try accessing Swagger UI: http://localhost:5000/swagger/index.html

---

## 📁 Project Structure

```
FullStack/
├── Backend/                  # .NET 10.0 Web API
│   ├── Controllers/          # API endpoints
│   ├── Services/             # Business logic (validation, job management)
│   ├── Models/               # Data models
│   ├── Configuration/        # Config loader
│   ├── Excel/                # Excel file handling
│   ├── Program.cs            # Entry point
│   ├── Backend.csproj        # Project file
│   ├── appsettings.json      # Logging config
│   └── config.json           # Application configuration
│
├── Frontend/                 # Angular 17 Application
│   ├── src/
│   │   ├── app/              # Angular components
│   │   ├── index.html        # Main HTML
│   │   ├── main.ts           # Angular bootstrap
│   │   └── styles/           # Global styles
│   ├── package.json          # npm dependencies
│   ├── angular.json          # Angular config
│   └── proxy.conf.json       # Development proxy
│
└── .gitignore                # Git ignore rules
```

---

## 🎯 Quick Reference

| Component | Port | URL |
|-----------|------|-----|
| **Backend API** | 5000 | http://localhost:5000 |
| **Backend Swagger** | 5000 | http://localhost:5000/swagger/index.html |
| **Frontend** | 4200 | http://localhost:4200 |

---

## 🔐 Important Notes

1. **Data Files**: The application creates/modifies:
   - `results.json` - Validation results
   - `jobs-metadata.json` - Job tracking data
   - These are auto-generated; don't delete them

2. **Configuration**: 
   - Modify `Backend/config.json` for pipeline settings
   - Modify `Backend/appsettings.json` for logging levels

3. **Development vs Production**:
   - Development: `dotnet run` and `npm start` (hot reload enabled)
   - Production: Use `dotnet publish` and `npm run build:prod`

4. **Git**: The `.gitignore` file is configured to exclude:
   - Node modules and build artifacts
   - .NET bin/obj folders
   - Environment-specific files
   - IDE settings

---

## 📚 Additional Resources

- [.NET 10.0 Documentation](https://learn.microsoft.com/en-us/dotnet/)
- [Angular 17 Documentation](https://angular.io/docs)
- [API_REFERENCE.md](./Backend/API_REFERENCE.md) - Detailed API endpoints
- [ASYNC_ARCHITECTURE.md](./Backend/ASYNC_ARCHITECTURE.md) - Architecture overview
- [FRONTEND_IMPLEMENTATION_SUMMARY.md](./Frontend/FRONTEND_IMPLEMENTATION_SUMMARY.md) - Frontend details

---

## 🆘 Still Having Issues?

1. **Check the logs**: Look at terminal output for error messages
2. **Verify prerequisites**: Run `dotnet --version` and `node --version`
3. **Try a clean build**:
   ```bash
   # Backend
   cd Backend
   dotnet clean
   dotnet build
   
   # Frontend
   cd ../Frontend
   rm -rf node_modules
   npm install
   ```
4. **Restart everything**: Close all terminals and the browser, then start from scratch

Good luck! 🎉
