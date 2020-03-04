# Ranorex ReportPortal Logger

## Build:
 * Checkout
 * Open in VS2019
 * Setup references/dependencies
 * Build dll
 * Use dll in your project
 
### Deps/References
Nuget: ReportPortal.Client; ReportPortal.Shared
Local: Ranorex.Core (ranorex Install Dir\bin)

## Usage:
 * Include dll as project reference;
 * In Program.cs in ranorex Project, add before TestSuiteRunner.Run();
```
RanorexReportPortalLogger rpLogger = new RanorexReportPortalLogger();
Report.AttachLogger(rpLogger);
```

Settings:
 * System.Uri - rp_endpoint
 * string - rp_uuid
 * string - rp_launch
 * string - rp_project
 
