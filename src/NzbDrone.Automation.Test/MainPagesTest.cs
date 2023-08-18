using System.Reflection;
using FluentAssertions;
using NUnit.Framework;
using NzbDrone.Automation.Test.PageModel;
using OpenQA.Selenium;

namespace NzbDrone.Automation.Test
{
    [TestFixture]
    public class MainPagesTest : AutomationTest
    {
        private PageBase _page;

        [SetUp]
        public void Setup()
        {
            _page = new PageBase(driver);
        }

        [Test]
        public void indexer_page()
        {
            _page.MovieNavIcon.Click();
            _page.WaitForNoSpinner();

            var imageName = MethodBase.GetCurrentMethod().Name;
            TakeScreenshot(imageName);

            _page.Find(By.CssSelector("div[class*='IndexerIndex']")).Should().NotBeNull();
        }

        [Test]
        public void system_page()
        {
            _page.SystemNavIcon.Click();
            _page.WaitForNoSpinner();

            var imageName = MethodBase.GetCurrentMethod().Name;
            TakeScreenshot(imageName);

            _page.Find(By.CssSelector("div[class*='Health']")).Should().NotBeNull();
        }
    }
}
