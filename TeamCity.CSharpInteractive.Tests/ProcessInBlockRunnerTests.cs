namespace TeamCity.CSharpInteractive.Tests;

using System;
using System.Threading;
using System.Threading.Tasks;
using Cmd;
using Moq;
using Shouldly;
using Xunit;

public class ProcessInBlockRunnerTests
{
    private readonly Mock<ILog<ProcessInBlockRunner>> _log = new();
    private readonly Mock<IProcessRunner> _baseProcessRunner = new();
    private readonly Mock<IStartInfo> _startInfo = new();
    private readonly Mock<IProcessStateProvider> _stateProvider = new();
    private readonly Mock<IProcessMonitor> _monitor = new();
    private readonly Mock<IDisposable> _token = new();
    private static readonly ProcessResult ProcessResult = new(ProcessState.Success, 33);

    public ProcessInBlockRunnerTests()
    {
        _startInfo.SetupGet(i => i.ShortName).Returns("Abc");
        _log.Setup(i => i.Block(It.IsAny<Text[]>())).Returns(_token.Object);
    }

    [Fact]
    public void ShouldRunInBlock()
    {
        // Given
        _baseProcessRunner.Setup(i => i.Run(_startInfo.Object, Handler, _stateProvider.Object, _monitor.Object, TimeSpan.FromDays(1))).Returns(ProcessResult);
        var runner = CreateInstance();

        // When
        var result = runner.Run(_startInfo.Object, Handler, _stateProvider.Object, _monitor.Object, TimeSpan.FromDays(1));

        // Then
        _log.Verify(i => i.Block(It.Is<Text[]>(text => text.Length == 1 && text[0].Value == "Abc")));
        _token.Verify(i => i.Dispose());
        result.ShouldBe(ProcessResult);
    }
    
    [Fact]
    public async Task ShouldRunAsyncInBlock()
    {
        // Given
        using var tokenSource = new CancellationTokenSource();
        var token = tokenSource.Token;
        _baseProcessRunner.Setup(i => i.RunAsync(_startInfo.Object, Handler, _stateProvider.Object, _monitor.Object, token)).Returns(Task.FromResult(ProcessResult));
        var runner = CreateInstance();

        // When
        var result = await runner.RunAsync(_startInfo.Object, Handler, _stateProvider.Object, _monitor.Object, token);

        // Then
        _log.Verify(i => i.Block(It.Is<Text[]>(text => text.Length == 1 && text[0].Value == "Abc")));
        _token.Verify(i => i.Dispose());
        result.ShouldBe(ProcessResult);
    }

    private static void Handler(Output obj) { }

    private ProcessInBlockRunner CreateInstance() =>
        new(_log.Object, _baseProcessRunner.Object);
}