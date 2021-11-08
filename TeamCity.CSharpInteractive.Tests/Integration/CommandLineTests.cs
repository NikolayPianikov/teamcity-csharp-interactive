// ReSharper disable StringLiteralTypo
namespace TeamCity.CSharpInteractive.Tests.Integration
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Contracts;
    using Core;
    using Shouldly;
    using Xunit;
    using Composer = Composer;

    public class CommandLineTests
    {
        [Fact]
        public void ShouldRun()
        {
            // Given
            var events = new List<CommandLineOutput>();

            // When
            var exitCode = Composer.ResolveICommandLine().Run(DotNetScript.Create("WriteLine(\"Hello\");"), e => events.Add(e));

            // Then
            exitCode.HasValue.ShouldBeTrue();
            exitCode.ShouldBe(0);
            events.Any(i => i.IsError).ShouldBeFalse();
            events.Any(i => !i.IsError && i.Line == "Hello").ShouldBeTrue();
        }
        
        [Fact]
        public void ShouldRunWithEnvironmentVariable()
        {
            // Given
            var events = new List<CommandLineOutput>();

            // When
            var exitCode = Composer.ResolveICommandLine().Run(
                DotNetScript.Create("WriteLine(\"VAL=\" + System.Environment.GetEnvironmentVariable(\"ABC\"));").AddVars(("ABC", "123")),
                e => events.Add(e));

            // Then
            exitCode.HasValue.ShouldBeTrue();
            exitCode.ShouldBe(0);
            events.Any(i => i.IsError).ShouldBeFalse();
            events.Any(i => !i.IsError && i.Line == "VAL=123").ShouldBeTrue();
        }
        
        [Fact]
        public void ShouldRunUsingTimeout()
        {
            // Given
            var stopwatch = new Stopwatch();

            // When
            stopwatch.Start();
            var exitCode = Composer.ResolveICommandLine().Run(
                DotNetScript.Create("System.Threading.Thread.Sleep(TimeSpan.FromSeconds(15));"),
                _ => { },
                TimeSpan.FromMilliseconds(100));
            stopwatch.Stop();

            // Then
            exitCode.HasValue.ShouldBeFalse();
            stopwatch.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(10000);
            stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(100);
        }

        [Fact]
        public async Task ShouldRunAsync()
        {
            // Given
            var events = new List<CommandLineOutput>();

            // When
            var exitCode = await Composer.ResolveICommandLine().RunAsync(DotNetScript.Create("WriteLine(\"Hello\");"), e => events.Add(e));

            // Then
            exitCode.HasValue.ShouldBeTrue();
            exitCode.ShouldBe(0);
            events.Any(i => i.IsError).ShouldBeFalse();
            events.Any(i => !i.IsError && i.Line == "Hello").ShouldBeTrue();
        }
        
        [Fact]
        public void ShouldRunAsyncUsingCancellationToken()
        {
            // Given
            using var cancellationTokenSource = new CancellationTokenSource();
            var stopwatch = new Stopwatch();

            // When
            stopwatch.Start();
            Composer.ResolveICommandLine().RunAsync(
                DotNetScript.Create("System.Threading.Thread.Sleep(TimeSpan.FromSeconds(15));"),
                _ => { },
                cancellationTokenSource.Token);
            
            Thread.Sleep(100);
            cancellationTokenSource.Cancel();
            stopwatch.Stop();

            // Then
            stopwatch.ElapsedMilliseconds.ShouldBeLessThanOrEqualTo(10000);
            stopwatch.ElapsedMilliseconds.ShouldBeGreaterThanOrEqualTo(100);
        }
    }
}