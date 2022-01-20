// ReSharper disable ClassNeverInstantiated.Global
namespace TeamCity.CSharpInteractive;

internal class TeamCitySettings : ITeamCitySettings
{
    private const string VersionVariableName = "TEAMCITY_VERSION";
    private const string ProjectNameVariableName = "TEAMCITY_PROJECT_NAME";
    internal const string FlowIdEnvironmentVariableName = "TEAMCITY_PROCESS_FLOW_ID";
    private const string ServiceMessagesPathEnvironmentVariableName = "TEAMCITY_SERVICE_MESSAGES_PATH";
    private const string DefaultFlowId = "ROOT";
    private readonly IHostEnvironment _hostEnvironment;
    private readonly Lazy<bool> _isUnderTeamCity;
    private readonly Lazy<string> _flowId;
    private readonly Lazy<string> _serviceMessagesPath;

    public TeamCitySettings(
        IHostEnvironment hostEnvironment,
        IEnvironment environment)
    {
        _hostEnvironment = hostEnvironment;
        _isUnderTeamCity = new Lazy<bool>(() => 
            !string.IsNullOrWhiteSpace(_hostEnvironment.GetEnvironmentVariable(ProjectNameVariableName))
            || !string.IsNullOrWhiteSpace(_hostEnvironment.GetEnvironmentVariable(VersionVariableName)));

        _flowId = new Lazy<string>(() =>
        {
            var flowId = _hostEnvironment.GetEnvironmentVariable(FlowIdEnvironmentVariableName);
            return string.IsNullOrWhiteSpace(flowId) ? DefaultFlowId : flowId;
        });
            
        _serviceMessagesPath = new Lazy<string>(() =>
        {
            var serviceMessagesPath = _hostEnvironment.GetEnvironmentVariable(ServiceMessagesPathEnvironmentVariableName);
            return string.IsNullOrWhiteSpace(serviceMessagesPath) ? environment.GetPath(SpecialFolder.Temp) : serviceMessagesPath;
        });
    }

    public bool IsUnderTeamCity => _isUnderTeamCity.Value;
        
    public string Version => (_hostEnvironment.GetEnvironmentVariable(VersionVariableName) ?? string.Empty).Trim();

    public string FlowId => _flowId.Value;

    public string ServiceMessagesPath => _serviceMessagesPath.Value;
}