# Quick Start Guide - PubMed Validation Frontend

## ⚡ 5-Minute Setup

### Step 1: Install Dependencies (2 minutes)
```bash
cd Frontend
npm install
```

### Step 2: Verify Backend is Running (30 seconds)
Open terminal and check:
```bash
# Test backend API
curl http://localhost:5000/api/records

# Expected response: Array of records or 200 OK status
```

If backend is NOT running:
```bash
cd ../Backend
dotnet run
```

### Step 3: Start Frontend (1 minute)
```bash
npm start
```

This will:
- Compile Angular application
- Start dev server on http://localhost:4200
- Auto-open in your default browser
- Enable hot reload for file changes

### Step 4: Verify Data Loads (1 minute)
1. Check browser shows "PubMed Validation Dashboard"
2. Confirm records appear in table
3. Try filtering and sorting
4. Expand a row to see details

✅ **If all above works, you're done!**

---

## 🔧 Common Issues & Quick Fixes

### ❌ "Backend API not accessible"
```bash
# Solution: Ensure backend is running
cd Backend
dotnet run

# Check in browser: http://localhost:5000/api/records
# Should show JSON array
```

### ❌ "npm start fails with 'ng not found'"
```bash
# Solution: Install dependencies
npm install

# Or reinstall globally
npm install -g @angular/cli@17
```

### ❌ "Port 4200 already in use"
```bash
# Solution: Use different port
ng serve --port 4201

# Or kill existing process
# Windows:
netstat -ano | findstr :4200
taskkill /PID <PID> /F

# Mac/Linux:
lsof -ti:4200 | xargs kill -9
```

### ❌ "CORS error in browser console"
```bash
# Solution: Backend must have CORS enabled in Program.cs
# Add this to Backend/Program.cs:

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAngular", policy =>
    {
        policy.WithOrigins("http://localhost:4200")
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

app.UseCors("AllowAngular");
```

### ❌ "Records not loading but no error shown"
```bash
# Solution: Check network tab in DevTools
1. Open browser DevTools (F12)
2. Go to Network tab
3. Look for 'records' request
4. Check status code (should be 200)
5. Check response contains JSON array

# If 404: Backend doesn't have /api/records endpoint
# If 500: Backend error (check Backend console)
# If failed: Backend not running
```

### ❌ "Styling looks broken"
```bash
# Solution: Clear cache and reinstall
ctrl+shift+del  # Clear browser cache
npm install     # Reinstall node modules
npm start       # Restart dev server
```

---

## 📚 Available Commands

```bash
# Development
npm start           # Start dev server (recommended)
ng serve            # Alternative: start dev server
ng serve --poll 2000 # With file poll (useful on WSL/Docker)

# Building
npm run build       # Build development version
npm run build:prod  # Build production version (optimized)
ng build --prod     # Alternative: build production

# Testing
npm test            # Run unit tests
npm run lint        # Run code linter

# Utilities
ng generate component features/my-component  # Create component
ng generate service core/services/my-service # Create service
```

---

## 🎯 Features to Try

### 1. Filter by Match Type
1. Look for dropdown labeled "Match Type Filter"
2. Select **EXACT_MATCH** only
3. Table should show only records with green badge
4. Click to reset with "Clear Filters"

### 2. Search Records
1. Find search input "Search (PMID / Title)"
2. Type a PMID or partial title
3. Table filters in real-time
4. Clear search and table resets

### 3. Sort by Column
1. Click column header (e.g., "Confidence")
2. Records reorder with ▲ or ▼ indicator
3. Click again to reverse sort direction

### 4. Check Record Details
1. Click **"View"** button on any row
2. Row expands below table
3. Shows full record details including:
   - PMID mismatch warning (if any)
   - Confidence score with detailed tooltip
   - Match type explanation
   - Full title
   - Logical & Metadata discrepancies
   - Detailed summary

### 5. Start Validation Job
1. Look at **right sidebar** "Validation Job"
2. Click **"Start Validation"**
3. Progress bar appears and auto-updates every 2 seconds
4. Shows: Duration, Total Records, Successful, Failed
5. Auto-stops when job completes

---

## 🌐 Deployment

### For Production
```bash
# Build optimized version
npm run build:prod

# Output in: dist/pubmed-validation-frontend/
# Deploy these files to web server
```

### With Docker
```bash
# Build Docker image
docker build -t pubmed-validation:latest .

# Run container
docker run -p 80:80 pubmed-validation:latest
```

---

## 📞 Need Help?

### Check Logs
```bash
# Browser console (F12)
# Shows JavaScript errors and API calls

# Backend console
# Shows server errors and request logs

# Terminal running npm start
# Shows Angular compilation errors
```

### File Locations
- **Frontend code:** `src/app/`
- **Services:** `src/app/core/services/`
- **Components:** `src/app/features/`
- **Styles:** `src/styles/` and component `.scss` files
- **Configuration:** `src/environments/`

### Documentation
- **Full README:** [README.md](README.md)
- **API Integration:** [API_INTEGRATION_GUIDE.md](API_INTEGRATION_GUIDE.md)
- **Implementation Details:** [FRONTEND_IMPLEMENTATION_SUMMARY.md](FRONTEND_IMPLEMENTATION_SUMMARY.md)

---

## ✅ Success Checklist

- [ ] npm install completed without errors
- [ ] Backend running on localhost:5000
- [ ] npm start successful
- [ ] Browser opens to http://localhost:4200
- [ ] Records visible in table
- [ ] Filtering works
- [ ] Sorting works
- [ ] Detail expansion works
- [ ] No red errors in DevTools console

If all checked ✅, you're ready to go! 🚀

---

**Time Estimate:** 5 minutes for full setup
**System Requirements:** Node.js 18+, npm 9+, Backend running
**Browser Support:** Chrome, Firefox, Safari, Edge (latest versions)
