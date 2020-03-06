# Ranorex ReportPortal Logger

Logs any Ranorex log data to RPusing the IReportlogger interface.
Currently supports text logs and image attachments.


## Building the thing:
 * Checkout
 * Open in VS
 * Setup references/dependencies
 * Build solution dll
 
### Dependencies/References
 * Nuget: ReportPortal.Client; ReportPortal.Shared
 * Local: Ranorex.Core (from Ranorex Install Dir\bin)


## Usage:
 * Reference dll in your Ranorex project
 * In Program.cs in ranorex Project, add before the call to TestSuiteRunner.Run();
 
```           
Environment.SetEnvironmentVariable("rp.endpoint", "https://your-rp-endpoint");
Environment.SetEnvironmentVariable("rp.uuid", "your-uuid-for-report-portal");
Environment.SetEnvironmentVariable("rp.project", "Project name");
Environment.SetEnvironmentVariable("rp.launch", "Launch name");
                
RanorexReportPortalLogger rpLogger = new RanorexReportPortalLogger();
Report.AttachLogger(rpLogger);
```

### Happy Testing!
