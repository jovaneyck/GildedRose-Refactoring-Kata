using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using GildedRoseKata;
using VerifyTests;
using VerifyXunit;
using Xunit;

namespace GildedRoseTests;

public class ApprovalTest
{
    public ApprovalTest()
    {
        VerifierSettings.OnVerifyMismatch(async (pair, msg) =>
        {
            var received = await File.ReadAllTextAsync(pair.ReceivedPath);
            var verified = await File.ReadAllTextAsync(pair.VerifiedPath);
            received.ReplaceLineEndings().Should().Be(verified.ReplaceLineEndings());
        });
    }
    
    [Fact]
    public Task ThirtyDays()
    {
        var fakeOutput = new StringBuilder();
        Console.SetOut(new StringWriter(fakeOutput));
        Console.SetIn(new StringReader($"a{Environment.NewLine}"));

        Program.Main(["30"]);
        var output = fakeOutput.ToString();
        
        return Verifier.Verify(output);
    }
}