/*
 * Copyright 2020 Praegus Solutions (https://www.praegus.nl)
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using Ranorex;
using Ranorex.Core;
using Ranorex.Core.Testing;
using ReportPortal.Client;
using ReportPortal.Client.Models;
using ReportPortal.Client.Requests;
using ReportPortal.Shared.Reporter;
using DateTime = System.DateTime;

namespace RanorexReportPortalLogging
{
    public class RanorexReportPortalLogger : IReportLogger
    {
        private readonly TestRunContext _context = new TestRunContext();

        private readonly Service _rpService;

        private ITestReporter _currentReporter;
        private string _currentTestState;
        private LaunchReporter _launchReporter;

        public RanorexReportPortalLogger()
        {
            //check config
            CheckEnvVar("rp.uuid");
            CheckEnvVar("rp.endpoint");
            CheckEnvVar("rp.project");
            CheckEnvVar("rp.launch");

            var rpUuid = Environment.GetEnvironmentVariable("rp.uuid");
            var rpEndpoint = Environment.GetEnvironmentVariable("rp.endpoint");
            var rpProject = Environment.GetEnvironmentVariable("rp.project");

            _rpService = new Service(new Uri(rpEndpoint), rpProject, rpUuid);
        }

        public bool PreFilterMessages => false;

        public void Start()
        {
            _launchReporter = new LaunchReporter(_rpService, null, null);
            _launchReporter.Start(new StartLaunchRequest
            {
                StartTime = DateTime.UtcNow,
                Name = Environment.GetEnvironmentVariable("rp.launch")
            });
        }

        public void End()
        {
            // End all tests & suites
            foreach (var test in _context.GetTests()) FinishTestItem(test);
            foreach (var suite in _context.GetSuites())
                suite.Finish(new FinishTestItemRequest {EndTime = DateTime.UtcNow});
            //Finish launch
            _launchReporter.Finish(new FinishLaunchRequest {EndTime = DateTime.UtcNow});
            _launchReporter.Sync();
        }

        public void LogData(ReportLevel level, string category, string message, object data,
            IDictionary<string, string> metaInfos)
        {
            //Currently only screenshot attahments are supported. Can ranorex attach anything else?
            byte[] dataBytes = null;
            if (data is Bitmap)
            {
                dataBytes = ByteArrayForImage((Bitmap) data);
            }
           

            ReportToReportPortal(new RanorexRpLogItem
            {
                Level = level,
                Category = category,
                Message = message,
                AttachData = dataBytes,
                MetaInfo = metaInfos
            });
        }

        public void LogText(ReportLevel level, string category, string message, bool escape,
            IDictionary<string, string> metaInfos)
        {
            ReportToReportPortal(new RanorexRpLogItem
            {
                Level = level,
                Category = category,
                Message = message,
                AttachData = null,
                MetaInfo = metaInfos
            });
        }

        private void ReportToReportPortal(RanorexRpLogItem logItem)
        {
            var level = DetermineLogLevel(logItem.Level.Name);

            SetOrCreateReporter(TestSuite.Current);
            //report message
            _currentReporter.Log(CreateLogItemRequest(logItem, level));

            //report meta-info if present
            if (logItem.MetaInfo.Keys.Count > 0)
            {
                _currentReporter.Log(CreateMetaDataLogItemRequest(logItem, LogLevel.Debug));
            }

            UpdateCurrentTestState(logItem.Level.Name);
        }


        private void SetOrCreateReporter(ITestSuite currentContext)
        {
            if (_context.GetSuiteReporter(currentContext.Name) == null)
                _context.AddSuite(currentContext.Name, _launchReporter.StartChildTestReporter(new StartTestItemRequest
                {
                    StartTime = DateTime.UtcNow,
                    Type = TestItemType.Suite,
                    Name = currentContext.Name
                }));
            _currentReporter = _context.GetSuiteReporter(currentContext.Name);

            if (currentContext.CurrentTestContainer != null)
            {
                var currentTest = currentContext.CurrentTestContainer;

                if (_context.GetTestReporter(currentTest.Name) == null)
                {
                    //new test. finish previous test(s) as ranorex has only single run
                    foreach (var test in _context.GetTests()) FinishTestItem(test);
                    _currentTestState = null;
                    _context.AddTest(currentTest.Name, _currentReporter.StartChildTestReporter(new StartTestItemRequest
                    {
                        StartTime = DateTime.UtcNow,
                        Type = TestItemType.Test,
                        Name = currentTest.Name
                    }));
                }

                _currentReporter = _context.GetTestReporter(currentTest.Name);
            }
        }

        private void FinishTestItem(ITestReporter test)
        {
            var status = Status.Passed;
            if (_currentTestState == "failed" || _currentTestState == "error") status = Status.Failed;

            try
            {
                test.Finish(new FinishTestItemRequest
                {
                    EndTime = DateTime.UtcNow,
                    Status = status
                });
            }
            catch (InsufficientExecutionStackException)
            {
                //skip if we've already requested a finish for this item
            }
        }

        private void UpdateCurrentTestState(string name)
        {
            switch (name)
            {
                case "Failure":
                    _currentTestState = "failed";
                    break;
                case "Error":
                    _currentTestState = "error";
                    break;
                default:
                    _currentTestState = "passed";
                    break;
            }
        }

        private static LogLevel DetermineLogLevel(string levelStr)
        {
            LogLevel level;
            var logLevel = char.ToUpper(levelStr[0]) + levelStr.Substring(1);
            if (Enum.IsDefined(typeof(LogLevel), logLevel))
                level = (LogLevel)Enum.Parse(typeof(LogLevel), logLevel);
            else if (logLevel.Equals("Failure"))
                level = LogLevel.Error;
            else if (logLevel.Equals("Warn"))
                level = LogLevel.Warning;
            else
                level = LogLevel.Info;
            return level;
        }

        private AddLogItemRequest CreateLogItemRequest(RanorexRpLogItem logItem, LogLevel level)
        {
            var rq = new AddLogItemRequest();
            rq.Time = DateTime.UtcNow;
            rq.Level = level;
            rq.Text = logItem.Category + " - " + logItem.Message;

            if (logItem.AttachData != null) rq.Attach = new Attach("Attachment", "image/jpeg", logItem.AttachData);
            return rq;
        }

        private AddLogItemRequest CreateMetaDataLogItemRequest(RanorexRpLogItem logItem, LogLevel level)
        {

            var rq = new AddLogItemRequest();
            rq.Time = DateTime.UtcNow;
            rq.Level = level;
            rq.Text = ConstructMetaDataLogMessage(logItem);
       
            return rq;
        }

        private string ConstructMetaDataLogMessage(RanorexRpLogItem logItem)
        {
            var meta = new StringBuilder();
            meta.Append("\r\n\r\n")
                .Append("Meta Info:\r\n");

            foreach (var key in logItem.MetaInfo.Keys)
                meta.Append("\t").Append(key).Append(" => ").Append(logItem.MetaInfo[key]).Append("\r\n");

            return meta.ToString();
        }

        private static byte[] ByteArrayForImage(Bitmap data)
        {
            var converter = new ImageConverter();
            return (byte[])converter.ConvertTo(data, typeof(byte[]));
        }


        private static void CheckEnvVar(string name)
        {
            if (Environment.GetEnvironmentVariable(name) == null) { throw new MissingEnvironmentVariableException(name); }
        }
    }
}
