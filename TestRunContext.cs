using ReportPortal.Shared.Reporter;
using System.Collections.Generic;

namespace RanorexReportPortalLogger
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
            else
            {
                return null;
            }
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
            else
            {
                return null;
            }
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
