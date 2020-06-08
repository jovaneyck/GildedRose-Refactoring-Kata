using Xunit;
using System;
using System.IO;
using System.Text;
using ApprovalTests;
using ApprovalTests.Reporters;
using ApprovalTests.Reporters.TestFrameworks;

namespace csharpcore
{
    [UseReporter(typeof(XUnit2Reporter))]
    public class ApprovalTest
    {
        [Fact]
        public void ThirtyDays()
        {
            var fakeoutput = new StringBuilder();
            Console.SetOut(new StringWriter(fakeoutput));
            Console.SetIn(new StringReader("a\n"));

            Program.Main(new string[] { });
            var output = fakeoutput.ToString();

            Approvals.Verify(output);
        }
    }
}
