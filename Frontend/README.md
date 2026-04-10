# PubMed Validation Dashboard - Frontend

A modern Angular 17+ application for managing and displaying PubMed bibliographic validation results with real-time filtering, sorting, and job management capabilities.

## 🚀 Quick Start

### Prerequisites
- Node.js 18+ and npm 9+
- Backend API running on `http://localhost:5000`
- Angular CLI 17+

### Setup

1. **Install Dependencies**
   ```bash
   npm install
   ```

2. **Start Development Server**
   ```bash
   npm start
   ```
   The application will automatically open at `http://localhost:4200`

3. **Build for Production**
   ```bash
   npm run build:prod
   ```

## 📋 Architecture Overview

### Project Structure
```
src/
├── app/
│   ├── app.component.ts          # Root component
│   ├── app.config.ts             # Angular configuration & providers
│   ├── core/                      # Core singleton services
│   │   ├── interceptors/         # HTTP interceptors
│   │   │   └── error.interceptor.ts
│   │   └── services/             # Business logic services
│   │       ├── api.service.ts         # Generic HTTP wrapper
│   │       ├── validation.service.ts  # Job polling & management
│   │       └── record.service.ts      # Data filtering & sorting
│   ├── shared/
│   │   └── models/               # TypeScript interfaces & types
│   │       ├── batch-job.ts
│   │       └── validated-record.ts
│   └── features/
│       └── records/              # Feature module
│           ├── components/
│           │   ├── error-banner.component.ts
│           │   ├── validation-job.component.ts
│           │   └── records-list.component.ts
│           └── styles/
├── environments/                 # Environment configuration
├── styles/
│   └── global.scss              # Global styles
└── main.ts                      # Bootstrap entry point
```

### Component Tree
```
AppComponent
└── RecordsListComponent (main dashboard)
    ├── ErrorBannerComponent (error display)
    └── ValidationJobComponent (sidebar - job management)
```

## 🔌 API Integration

### Endpoints Consumed
The application communicates with the backend ASP.NET Core API:

| Method | Endpoint | Purpose |
|--------|----------|---------|
| POST | `/api/validation/validate` | Start new validation job |
| GET | `/api/validation/job/{jobId}` | Get job status |
| GET | `/api/validation/latest` | Get latest job |
| GET | `/api/records` | Fetch all validated records |
| GET | `/api/records/{index}` | Get single record by index |

### Configuration
API URL is configured in `src/environments/environment.ts`:
- **Development:** `http://localhost:5000/api`
- **Production:** Update in `src/environments/environment.prod.ts`

## ✨ Features

### 1. Data Table Display
- **Columns:** InputPMID, MatchedPMID, Confidence Score, Match Type, Title, Actions
- **Pagination:** 10 records per page with navigation controls
- **Sorting:** Click column headers to sort (▲ ascending, ▼ descending)
- **Expandable Rows:** Click "View" to see detailed record information

### 2. Match Type Flagging
Records are color-coded by match type:
- 🟢 **EXACT_MATCH** (Green #2e7d32) - Perfect match found
- 🟡 **MINOR_CHANGE** (Yellow #f57f17) - Minor discrepancies detected
- 🔴 **MAJOR_DISCREPANCY** (Red #c62828) - Significant differences
- ⚫ **NO_MATCH** (Gray #616161) - No matching record found

### 3. Filtering & Search
- **Match Type Filter:** Dropdown to filter by match extent
- **Confidence Range:** Slider to filter by confidence score (0.0 - 1.0)
- **Search:** Full-text search by PMID or title
- **Clear Filters:** Reset all filters with one click

### 4. Job Management (Sidebar)
- **Start Validation:** Upload Excel file and trigger new validation job
- **Job Status:** Real-time job status display with progress bar
- **Auto-Refresh:** Automatically updates when job completes
- **Polling:** 2-second interval checks for job status updates
- **Metrics:** View job duration, success/failure counts

### 5. Detail Expansion
Click "View" on any record to expand and see:
- PMID Information (Input, Matched, Mismatch Warning)
- Confidence & Match Type Summary
- Full Title
- Logical Discrepancies
- Metadata Discrepancies
- Detailed Summary

## 💾 State Management

### Services
- **ApiService:** Generic HTTP wrapper with error handling
- **ValidationService:** Job lifecycle management with RxJS polling
- **RecordService:** Data filtering, sorting, pagination logic
- **ErrorInterceptor:** Global error handling and user notifications

### Observables
State is managed via RxJS Observables with proper memory management:
- `ValidationService.currentJob$` - Current job state
- `RecordService.records$` - All loaded records
- `RecordService.filteredRecords$` - Filtered/sorted records
- `RecordService.isLoading$` - Loading indicator
- Auto-unsubscribe via `takeUntil` pattern

## 🎨 Styling

### Design System
- **Primary Color:** #2196f3 (Blue)
- **Success Color:** #4caf50 (Green)
- **Warning Color:** #ff9800 (Orange)
- **Error Color:** #f44336 (Red)

### Responsive Breakpoints
- **Desktop:** 1200px+ (2-column layout)
- **Tablet:** 1024px - 1199px (sidebar collapses)
- **Mobile:** 768px - 1023px (single column)
- **Small Mobile:** < 768px (optimized layout)

### Features
- Material Design principles (no Material library dependency)
- Smooth animations (0.2-0.3s ease transitions)
- Gradient progress bars (green → orange → red)
- Hover effects on interactive elements
- Professional color-coded badges

## 🛠️ Development

### Available Scripts
```bash
# Start dev server with auto-reload
npm start

# Build for production
npm run build:prod

# Watch mode (rebuild on file changes)
npm run watch

# Run tests
npm test

# Run linter
npm lint
```

### Environment Variables
Create a `.env` file in the root (optional, for secrets):
```
BACKEND_API_URL=http://localhost:5000/api
```

### CORS Configuration
If encountering CORS errors:

**Backend (Program.cs)** must have:
```csharp
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

### Dev Server Proxy
Alternatively, use Angular dev server proxy in `angular.json` (already configured):
```json
"serve": {
  "builder": "@angular-devkit/build-angular:dev-server",
  "options": {
    "proxyConfig": "proxy.conf.json"
  }
}
```

## 📊 Data Models

### ValidatedRecord
```typescript
{
  InputPMID: string;
  MatchedPMID: string;
  ConfidenceScore: number;     // 0.0 - 1.0
  Match_Extent: MatchExtent;   // EXACT_MATCH | MINOR_CHANGE | MAJOR_DISCREPANCY | NO_MATCH
  Discrepancies_Logical: string;
  Discrepancies_Metadata: string;
  Summary: string;
  OriginalTitle: string;
}
```

### BatchJob
```typescript
{
  JobId: string;
  Status: JobStatus;            // PENDING | RUNNING | COMPLETED | FAILED
  CreatedAt: Date;
  StartedAt?: Date;
  CompletedAt?: Date;
  DurationSeconds?: number;
  TotalRecords: number;
  SuccessfulRecords: number;
  FailedRecords: number;
  ErrorMessage?: string;
}
```

## 🐛 Troubleshooting

### Issue: "Backend API not accessible"
- Verify backend is running on `http://localhost:5000`
- Check CORS headers in backend response
- Ensure firewall isn't blocking localhost:5000

### Issue: "Records not loading"
- Open browser DevTools (F12) → Network tab
- Check API response from `GET /api/records`
- Verify data format matches ValidatedRecord interface

### Issue: "Styling looks broken"
- Clear browser cache (Ctrl+Shift+Delete)
- Run `npm install` again to ensure dependencies
- Try different browser (Chrome, Firefox)

### Issue: "Pagination not working"
- Check if records count is > 10
- Verify RecordService.paginatedRecords computed property
- Check console for TypeScript errors

## 📦 Dependencies

### Core
- `@angular/animations` - Animation library
- `@angular/common` - Common directives (ngIf, ngFor, etc.)
- `@angular/forms` - Reactive forms
- `@angular/platform-browser` - DOM operations
- `@angular/router` - Routing
- `rxjs` - Reactive programming

### Dev Tools
- `@angular/cli` - Angular development CLI
- `@angular/compiler-cli` - Angular compiler
- `typescript` - TypeScript compiler
- `karma` - Test runner
- `jasmine` - Testing framework

## 🚢 Deployment

### Build for Production
```bash
npm run build:prod
```

Output files will be in `dist/pubmed-validation-frontend/`

### Deploy to Web Server
1. Copy contents of `dist/pubmed-validation-frontend/` to web server
2. Configure web server to serve `index.html` for all routes (SPA routing)
3. Update `environment.prod.ts` with production API URL
4. Rebuild with production environment

### Docker (Optional)
Create `Dockerfile`:
```dockerfile
FROM node:18 as builder
WORKDIR /app
COPY package*.json ./
RUN npm install
COPY . .
RUN npm run build:prod

FROM nginx:alpine
COPY --from=builder /app/dist/pubmed-validation-frontend /usr/share/nginx/html
COPY nginx.conf /etc/nginx/nginx.conf
EXPOSE 80
CMD ["nginx", "-g", "daemon off;"]
```

Build and run:
```bash
docker build -t pubmed-validation:latest .
docker run -p 80:80 pubmed-validation:latest
```

## 📝 Future Enhancements

1. **WebSocket Integration** - Replace polling with real-time job updates via WebSocket
2. **Batch Operations** - Select multiple records and apply actions
3. **Export Functionality** - Export filtered results to CSV/Excel
4. **Advanced Search** - Full-text search with operators (AND, OR, NOT)
5. **Analytics Dashboard** - Charts showing validation statistics
6. **User Authentication** - Login/logout with role-based access
7. **Backend Pagination** - Server-side pagination for 10k+ records
8. **Dark Mode** - Theme toggle for dark/light mode

## 📞 Support

For issues or questions:
1. Check Backend API is accessible
2. Verify network requests in DevTools
3. Check browser console for errors
4. Review component logs with `ng serve --poll 2000` for file change detection

## 📄 License

This project is part of the PubMed Validation System.
