namespace TeamCity.CSharpInteractive.Tests;

public class ProgramTests
{
    private readonly Mock<ILog<Program>> _log;
    private readonly Mock<IInfo> _info;
    private readonly Mock<ISettingsManager> _settingsManager;
    private readonly Mock<ISettings> _settings;
    private readonly Mock<IExitTracker> _exitTracker;
    private readonly Mock<IDisposable> _trackToken;
    private readonly Mock<IRunner> _runner;
    private readonly Mock<IActive> _active;
    private readonly Mock<IDisposable> _activationToken;
    private readonly Mock<IStatistics> _statistics;

    public ProgramTests()
    {
        _log = new Mock<ILog<Program>>();
        _info = new Mock<IInfo>();
        _settingsManager = new Mock<ISettingsManager>();
        _settings = new Mock<ISettings>();
        _trackToken = new Mock<IDisposable>();
        _exitTracker = new Mock<IExitTracker>();
        _exitTracker.Setup(i => i.Track()).Returns(_trackToken.Object);
        _runner = new Mock<IRunner>();
        _runner.Setup(i => i.Run()).Returns(ExitCode.Success);
        _activationToken = new Mock<IDisposable>();
        _active = new Mock<IActive>();
        _active.Setup(i => i.Activate()).Returns(_activationToken.Object);
        _statistics = new Mock<IStatistics>();
        _statistics.SetupGet(i => i.Errors).Returns(Array.Empty<string>());
    }

    [Fact]
    public void ShouldRun()
    {
        // Given
        var program = CreateInstance();

        // When
        var actualResult = program.Run();

        // Then
        _settingsManager.Verify(i => i.Load());
        _info.Verify(i => i.ShowHeader());
        actualResult.ShouldBe(ExitCode.Success);
        _trackToken.Verify(i => i.Dispose());
        _info.Verify(i => i.ShowFooter());
        _active.Verify(i => i.Activate());
        _activationToken.Verify(i => i.Dispose());
    }
        
    [Fact]
    public void ShouldRunLogUnhandledException()
    {
        // Given
        var program = CreateInstance();

        // When
        _runner.Setup(i => i.Run()).Throws<Exception>();
        var actualResult = program.Run();

        // Then
        actualResult.ShouldBe(ExitCode.Fail);
        _trackToken.Verify(i => i.Dispose());
        _info.Verify(i => i.ShowFooter());
        _activationToken.Verify(i => i.Dispose());
        _log.Verify(i => i.Error(ErrorId.Unhandled, It.IsAny<Text[]>()));
    }
        
    [Fact]
    public void ShouldShowVersion()
    {
        // Given
        var program = CreateInstance();

        // When
        _settings.SetupGet(i => i.ShowVersionAndExit).Returns(true);
        var actualResult = program.Run();

        // Then
        _settingsManager.Verify(i => i.Load());
        _info.Verify(i => i.ShowVersion());
        actualResult.ShouldBe(ExitCode.Success);
    }
        
    [Fact]
    public void ShouldShowHelp()
    {
        // Given
        var program = CreateInstance();

        // When
        _settings.SetupGet(i => i.ShowHelpAndExit).Returns(true);
        var actualResult = program.Run();

        // Then
        _settingsManager.Verify(i => i.Load());
        _info.Verify(i => i.ShowHeader());
        _info.Verify(i => i.ShowHelp());
        actualResult.ShouldBe(ExitCode.Success);
    }
        
    [Fact]
    public void ShouldFailedWhenHasErrors()
    {
        // Given
        var program = CreateInstance();

        // When
        _statistics.SetupGet(i => i.Errors).Returns(new []{"some error"});
        var actualResult = program.Run();

        // Then
        actualResult.ShouldBe(ExitCode.Fail);
    }

    private Program CreateInstance() =>
        new(
            _log.Object,
            new []{_active.Object},
            _info.Object,
            _settingsManager.Object,
            _settings.Object,
            _exitTracker.Object,
            () => _runner.Object,
            _statistics.Object);
}