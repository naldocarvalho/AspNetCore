// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using Xunit;
using Xunit.Abstractions;
using Xunit.Sdk;

namespace Microsoft.AspNetCore.E2ETesting
{
    [CaptureSeleniumLogs]
    public class BrowserTestBase : IClassFixture<BrowserFixture>, IAsyncLifetime
    {
        private static readonly AsyncLocal<IWebDriver> _asyncBrowser = new AsyncLocal<IWebDriver>();
        private static readonly AsyncLocal<ILogs> _logs = new AsyncLocal<ILogs>();
        private static readonly AsyncLocal<ITestOutputHelper> _output = new AsyncLocal<ITestOutputHelper>();

        public BrowserTestBase(BrowserFixture browserFixture, ITestOutputHelper output)
        {
            BrowserFixture = browserFixture;
            _output.Value = output;
        }

        public IWebDriver Browser { get; set; }

        public static IWebDriver BrowserAccessor => _asyncBrowser.Value;

        public static ILogs Logs => _logs.Value;

        public static ITestOutputHelper Output => _output.Value;

        public BrowserFixture BrowserFixture { get; }

        public Task DisposeAsync()
        {
            return Task.CompletedTask;
        }

        public virtual async Task InitializeAsync()
        {
            var (browser, logs) = await BrowserFixture.GetOrCreateBrowserAsync(Output);
            _asyncBrowser.Value = browser;
            _logs.Value = logs;

            Browser = browser;

            InitializeAsyncCore();
        }

        protected virtual void InitializeAsyncCore()
        {
            // Clear logs - we check these during tests in some cases.
            // Make sure each test starts clean.
            ((IJavaScriptExecutor)Browser).ExecuteScript("console.clear()");
        }

        protected IWebElement WaitUntilExists(By findBy, int timeoutSeconds = 10, bool throwOnError = false)
        {
            List<LogEntry> errors = null;
            IWebElement result = null;
            new WebDriverWait(Browser, TimeSpan.FromSeconds(timeoutSeconds)).Until(driver =>
            {
                if (throwOnError && Browser.Manage().Logs.AvailableLogTypes.Contains(LogType.Browser))
                {
                    // Fail-fast if any errors were logged to the console.
                    var log = Browser.Manage().Logs.GetLog(LogType.Browser);
                    if (log.Any(entry => entry.Level == LogLevel.Severe))
                    {
                        errors = log.Where(entry => entry.Level == LogLevel.Severe).ToList();
                        return true;
                    }
                }

                return (result = driver.FindElement(findBy)) != null;
            });

            if (errors != null)
            {
                var message =
                    $"Encountered errors while looking for '{findBy}'." + Environment.NewLine +
                    string.Join(Environment.NewLine, errors);
                throw new XunitException(message);
            }

            return result;
        }
    }
}
