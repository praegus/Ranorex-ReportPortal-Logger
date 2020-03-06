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

using ReportPortal.Shared.Reporter;
using System.Collections.Generic;

namespace RanorexReportPortalLogging
{
    class TestRunContext
    {
        private readonly Dictionary<string, ITestReporter> tests = new Dictionary<string, ITestReporter>();
        private readonly Dictionary<string, ITestReporter> suites = new Dictionary<string, ITestReporter>();

        public void AddTest(string testName, ITestReporter reporter)
        {
            tests.Add(testName, reporter);
        }

        public ITestReporter GetTestReporter(string name)
        {
            if (null != name && tests.ContainsKey(name))
            {
                return tests[name];
            }
            return null;
        }

        public void RemoveTestReporter(string name)
        {
            tests.Remove(name);
        }

        public ITestReporter GetSuiteReporter(string name)
        {
            if (null != name && suites.ContainsKey(name))
            {
                return suites[name];
            }
            return null;
        }

        public void AddSuite(string suiteName, ITestReporter reporter)
        {
            suites.Add(suiteName, reporter);
        }

        public void RemoveSuiteReporter(string name)
        {
            suites.Remove(name);
        }

        public List<ITestReporter> GetTests()
        {
            return new List<ITestReporter>(tests.Values);
        }

        public List<ITestReporter> GetSuites()
        {
            return new List<ITestReporter>(suites.Values);
        }

    }
}
