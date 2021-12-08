using System;
using System.Collections;
using NUnit.Framework;
using NzbDrone.Core.Parser;
using NzbDrone.Core.Test.Framework;

namespace NzbDrone.Core.Test.ParserTests
{
    [TestFixture]
    public class DateTimeRoutinesFixture : CoreTest
    {
        public static IEnumerable DateTimeTestCases
        {
            get
            {
                yield return new TestCaseData(@"Member since:  	10-Feb-2008").Returns(new DateTime(2008, 2, 10, 0, 0, 0));
                yield return new TestCaseData(@"Last Update: 18:16 11 Feb '08 ").Returns(new DateTime(2008, 2, 11, 18, 16, 0));
                yield return new TestCaseData(@"date	Tue, Feb 10, 2008 at 11:06 AM").Returns(new DateTime(2008, 2, 10, 11, 06, 0));
                yield return new TestCaseData(@"see at 12/31/2007 14:16:32").Returns(new DateTime(2007, 12, 31, 14, 16, 32));
                yield return new TestCaseData(@"sack finish 14:16:32 November 15 2008, 1-144 app").Returns(new DateTime(2008, 11, 15, 14, 16, 32));
                yield return new TestCaseData(@"Genesis Message - Wed 04 Feb 08 - 19:40").Returns(new DateTime(2008, 2, 4, 19, 40, 0));
                yield return new TestCaseData(@"The day 07/31/07 14:16:32 is ").Returns(new DateTime(2007, 7, 31, 14, 16, 32));
                yield return new TestCaseData(@"Shipping is on us until December 24, 2008 within the U.S. ").Returns(new DateTime(2008, 12, 24, 0, 0, 0));
                yield return new TestCaseData(@" 2008 within the U.S. at 14:16:32").Returns(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, 14, 16, 32));
                yield return new TestCaseData(@"5th November, 1994, 8:15:30 pm").Returns(new DateTime(1994, 11, 5, 20, 15, 30));
                yield return new TestCaseData(@"7 boxes January 31 , 14:16:32.").Returns(new DateTime(DateTime.Now.Year, 1, 31, 14, 16, 32));
                yield return new TestCaseData(@"the blue sky of Sept  30th  2008 14:16:32").Returns(new DateTime(2008, 9, 30, 14, 16, 32));
                yield return new TestCaseData(@" e.g. 1997-07-16T19:20:30+01:00").Returns(new DateTime(1997, 7, 16, 19, 20, 30));
                yield return new TestCaseData(@"Apr 1st, 2008 14:16:32 tufa 6767").Returns(new DateTime(2008, 4, 1, 14, 16, 32));
                yield return new TestCaseData(@"wait for 07/31/07 14:16:32").Returns(new DateTime(2007, 7, 31, 14, 16, 32));
                yield return new TestCaseData(@"later 12.31.08 and before 1.01.09").Returns(new DateTime(2008, 12, 31, 0, 0, 0));
                yield return new TestCaseData(@"Expires: Sept  30th  2008 14:16:32").Returns(new DateTime(2008, 9, 30, 14, 16, 32));
                yield return new TestCaseData(@"Offer expires Apr 1st, 2007, 14:16:32").Returns(new DateTime(2007, 4, 1, 14, 16, 32));
                yield return new TestCaseData(@"Expires  14:16:32 January 31.").Returns(new DateTime(DateTime.Now.Year, 1, 31, 14, 16, 32));
                yield return new TestCaseData(@"Expires  14:16:32 January 31-st.").Returns(new DateTime(DateTime.Now.Year, 1, 31, 14, 16, 32));
                yield return new TestCaseData(@"Expires 23rd January 2010.").Returns(new DateTime(2010, 1, 23, 0, 0, 0));
                yield return new TestCaseData(@"Expires January 22nd, 2010.").Returns(new DateTime(2010, 1, 22, 0, 0, 0));
                yield return new TestCaseData(@"Expires DEC 22, 2010.").Returns(new DateTime(2010, 12, 22, 0, 0, 0));
                yield return new TestCaseData(@"Version: 1.0.0.692 6/1/2010 2:28:04 AM ").Returns(new DateTime(2010, 6, 1, 2, 28, 4));
                yield return new TestCaseData(@"Version: 1.0.0.692 04/21/11 12:30am ").Returns(new DateTime(2011, 4, 21, 00, 30, 00));
                yield return new TestCaseData(@"Version: 1.0.0.692 04/21/11 12:30pm ").Returns(new DateTime(2011, 4, 21, 12, 30, 00));
                yield return new TestCaseData(@"Version: Thu Aug 06 22:32:15 MDT 2009 ").Returns(new DateTime(2009, 8, 6, 22, 32, 15));
            }
        }

        [TestCaseSource("DateTimeTestCases")]
        public DateTime should_parse_date(string date)
        {
            DateTimeRoutines.TryParseDateOrTime(date, DateTimeRoutines.DateTimeFormat.USDate, out var parsedDateTime);

            return parsedDateTime.DateTime;
        }
    }
}
