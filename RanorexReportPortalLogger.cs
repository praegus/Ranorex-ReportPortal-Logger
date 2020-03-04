using Ranorex;
using System;
using System.Collections.Generic;
using ReportPortal.Client;
using Ranorex.Core;
using ReportPortal.Client.Models;
using ReportPortal.Shared.Reporter;
using ReportPortal.Client.Requests;
using Ranorex.Core.Testing;


namespace RanorexReportPortalLogger
{
    class RanorexReportPortalLogger : IReportLogger
    {


        private readonly string rp_uuid;
        private readonly Uri rp_endpoint;
        private readonly Launch rp_launch = new Launch();
        private readonly string rp_project;

        private Service rpService;
        private LaunchReporter reporter;
        private ITestReporter currentReporter;

        private string currentTestState;


        private TestRunContext context = new TestRunContext();

        public bool PreFilterMessages => throw new NotImplementedException();

        public int Order => throw new NotImplementedException();

        public object FullDisplayName { get; private set; }

        public RanorexReportPortalLogger()
        {
            rp_uuid = Properties.RPSettings.Default.rp_uuid;
            rp_endpoint = Properties.RPSettings.Default.rp_endpoint;
            rp_launch.Name = Properties.RPSettings.Default.rp_launch;
            rp_project = Properties.RPSettings.Default.rp_project;

            rpService = new Service(rp_endpoint, rp_project, rp_uuid);

        }
        public void Start()
        {
            reporter = new LaunchReporter(rpService, null, null);
            reporter.Start(new StartLaunchRequest
            {
                StartTime = System.DateTime.UtcNow,
                Name = rp_launch.Name
            });
        }

        public void End()
        {
            // End all tests & suites
            foreach (ITestReporter test in context.GetTests())
            {
                FinishTestItem(test);
            }
            foreach (ITestReporter suite in context.GetSuites())
            {
                suite.Finish(new FinishTestItemRequest { EndTime = System.DateTime.UtcNow });
            }
            //Finish launch
            reporter.Finish(new FinishLaunchRequest { EndTime = System.DateTime.UtcNow });
            reporter.Sync();
        }

        public void LogData(ReportLevel level, string category, string message, object data, IDictionary<string, string> metaInfos)
        {
            Console.WriteLine(level.ToString() + " (data) - " + category + " - " + data.ToString());
        }

        public void LogText(ReportLevel level, string category, string message, bool escape, IDictionary<string, string> metaInfos)
        {

            ReportToReportPortal(new RanorexRPLogItem
            {
                level = level,
                category = category,
                message = message,
                data = null,
                metaInfo = metaInfos
            });
        }

        private void ReportToReportPortal(RanorexRPLogItem logItem)
        {
            LogLevel level;
            string logLevel = char.ToUpper(logItem.level.Name[0]) + logItem.level.Name.Substring(1);
            if (Enum.IsDefined(typeof(LogLevel), logLevel))
            {
                level = (LogLevel)Enum.Parse(typeof(LogLevel), logLevel);
            }
            else
            {
                level = LogLevel.Info;
            }

            SetOrCreateReporter(TestSuite.Current);

            currentReporter.Log(new AddLogItemRequest
            {
                Time = System.DateTime.UtcNow,
                Level = level,
                Text = logItem.category + " - " + logItem.message + " (" + logLevel + ")"
            });

            switch (logLevel)
            {
                case "Failure":
                    currentTestState = "failed";
                    break;
                case "Error":
                    currentTestState = "error";
                    break;
                default:
                    currentTestState = "passed";
                    break;
            }

        }

        private void SetOrCreateReporter(ITestSuite currentContext)
        {
            if (context.GetSuiteReporter(currentContext.Name) == null)
            {
                context.AddSuite(currentContext.Name, reporter.StartChildTestReporter(new StartTestItemRequest
                {
                    StartTime = System.DateTime.UtcNow,
                    Type = TestItemType.Suite,
                    Name = currentContext.Name,
                }));
            }
            currentReporter = context.GetSuiteReporter(currentContext.Name);

            if (currentContext.CurrentTestContainer != null)
            {
                var currentTest = currentContext.CurrentTestContainer;

                if (context.GetTestReporter(currentTest.Name) == null)
                {
                    //new test. finish previous test(s)
                    foreach (ITestReporter test in context.GetTests())
                    {
                        FinishTestItem(test);
                    }
                    currentTestState = null;
                    context.AddTest(currentTest.Name, currentReporter.StartChildTestReporter(new StartTestItemRequest
                    {
                        StartTime = System.DateTime.UtcNow,
                        Type = TestItemType.Test,
                        Name = currentTest.Name,
                    }));
                }

                currentReporter = context.GetTestReporter(currentTest.Name);


            }
        }

        private void FinishTestItem(ITestReporter test)
        {
            Status status = Status.Passed;
            if (currentTestState == "failed" || currentTestState == "error")
            {
                status = Status.Failed;
            }

            try
            {
                test.Finish(new FinishTestItemRequest
                {
                    EndTime = System.DateTime.UtcNow,
                    Status = status
                });
            }
            catch (System.InsufficientExecutionStackException)
            {
                //skip
            }

        }
    }

    class RanorexRPLogItem
    {
        public ReportLevel level;
        public string category;
        public string message;
        public object data = null;
        public IDictionary<string, string> metaInfo;

    }
}

