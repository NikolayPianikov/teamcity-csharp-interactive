// ReSharper disable PartialTypeWithSinglePart
// ReSharper disable RedundantCast
// ReSharper disable UnusedMember.Local
namespace TeamCity.CSharpInteractive
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Diagnostics.CodeAnalysis;
    using System.Reflection;
    using System.Runtime.Versioning;
    using System.Threading;
    using Cmd;
    using JetBrains.TeamCity.ServiceMessages.Read;
    using JetBrains.TeamCity.ServiceMessages.Write;
    using JetBrains.TeamCity.ServiceMessages.Write.Special;
    using JetBrains.TeamCity.ServiceMessages.Write.Special.Impl.Updater;
    using Microsoft.Build.Framework;
    using Microsoft.CodeAnalysis;
    using Microsoft.CodeAnalysis.CSharp;
    using Microsoft.CodeAnalysis.Scripting;
    using Contracts;
    using Pure.DI;
    using static Pure.DI.Lifetime;

    [ExcludeFromCodeCoverage]
    internal static partial class Composer
    {
        private static void Setup()
        {
            // #trace=true
            // #verbosity=diagnostic
            // #out=C:\Projects\_temp\a
            DI.Setup()
                .Default(Singleton)
                .Bind<Program>().To<Program>()
                .Bind<Assembly>().To(_ => Assembly.GetEntryAssembly())
                .Bind<string>("TargetFrameworkMoniker").To(ctx => ctx.Resolve<Assembly?>()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName ?? string.Empty)
                .Bind<Process>().To(_ => Process.GetCurrentProcess())
                .Bind<string>("ModuleFile").To(ctx => ctx.Resolve<Process>().MainModule?.FileName ?? string.Empty)
                .Bind<CancellationTokenSource>().To(_ => new CancellationTokenSource())
                .Bind<CancellationToken>().As(Transient).To(ctx => ctx.Resolve<CancellationTokenSource>().Token)
                .Bind<IActive>(typeof(ExitManager)).To<ExitManager>()
                .Bind<IHostEnvironment>().To<HostEnvironment>()
                .Bind<IColorTheme>().To<ColorTheme>()
                .Bind<ITeamCityLineFormatter>().To<TeamCityLineFormatter>()
                .Bind<ITeamCitySpecific<TT>>().To<TeamCitySpecific<TT>>()
                .Bind<IStdOut>().Bind<IStdErr>().Tags("Default").To<ConsoleInOut>()
                .Bind<IStdOut>().Bind<IStdErr>().Tags("TeamCity").To<TeamCityInOut>()
                .Bind<IConsole>().To<Console>()
                .Bind<IStdOut>().To(ctx => ctx.Resolve<ITeamCitySpecific<IStdOut>>().Instance)
                .Bind<IStdErr>().To(ctx => ctx.Resolve<ITeamCitySpecific<IStdErr>>().Instance)
                .Bind<ILog<TT>>("Default").To<Log<TT>>()
                .Bind<ILog<TT>>("TeamCity").To<TeamCityLog<TT>>()
                .Bind<ILog<TT>>().To(ctx => ctx.Resolve<ITeamCitySpecific<ILog<TT>>>().Instance)
                .Bind<IFileSystem>().To<FileSystem>()
                .Bind<IEnvironment>().Bind<IScriptContext>().Bind<ITraceSource>(typeof(Environment)).To<Environment>()
                .Bind<ITeamCitySettings>().To<TeamCitySettings>()
                .Bind<IExitTracker>().To<ExitTracker>()
                .Bind<IDotnetEnvironment>().Bind<ITraceSource>(typeof(DotnetEnvironment)).To<DotnetEnvironment>()
                .Bind<IDockerEnvironment>().Bind<ITraceSource>(typeof(DockerEnvironment)).To<DockerEnvironment>()
                .Bind<INugetEnvironment>().Bind<ITraceSource>(typeof(NugetEnvironment)).To<NugetEnvironment>()
                .Bind<ISettings>().Bind<ISettingsManager>().Bind<ISettingSetter<VerbosityLevel>>().Bind<Settings>().To<Settings>()
                .Bind<ISettingDescription>().Tags(typeof(VerbosityLevel)).To<VerbosityLevelSettingDescription>()
                .Bind<ICommandLineParser>().To<CommandLineParser>()
                .Bind<IInfo>().To<Info>()
                .Bind<ICodeSource>().To<ConsoleSource>()
                .Bind<ICodeSource>("Host").To<HostIntegrationCodeSource>()
                .Bind<FileCodeSource>().To<FileCodeSource>()
                .Bind<IFileCodeSourceFactory>().To<FileCodeSourceFactory>()
                .Bind<IRunner>().Tags(InteractionMode.Interactive).To<InteractiveRunner>()
                .Bind<IRunner>().Tags(InteractionMode.Script).To<ScriptRunner>()
                .Bind<IRunner>().As(Transient).To(ctx => ctx.Resolve<ISettings>().InteractionMode == InteractionMode.Interactive ? ctx.Resolve<IRunner>(InteractionMode.Interactive) : ctx.Resolve<IRunner>(InteractionMode.Script))
                .Bind<ICommandSource>().To<CommandSource>()
                .Bind<IStringService>().To<StringService>()
                .Bind<IStatistics>().To<Statistics>()
                .Bind<IPresenter<IEnumerable<ITraceSource>>>().To<TracePresenter>()
                .Bind<IPresenter<IStatistics>>().To<StatisticsPresenter>()
                .Bind<IPresenter<CompilationDiagnostics>>().To<DiagnosticsPresenter>()
                .Bind<IPresenter<ScriptState<object>>>().To<ScriptStatePresenter>()
                .Bind<IBuildEngine>().To<BuildEngine>()
                .Bind<INugetRestoreService>().Bind<ISettingSetter<NuGetRestoreSetting>>().To<NugetRestoreService>()
                .Bind<NuGet.Common.ILogger>().To<NugetLogger>()
                .Bind<IUniqueNameGenerator>().To<UniqueNameGenerator>()
                .Bind<INugetAssetsReader>().To<NugetAssetsReader>()
                .Bind<ICleaner>().To<Cleaner>()
                .Bind<ICommandsRunner>().To<CommandsRunner>()
                .Bind<ICommandFactory<ICodeSource>>().To<CodeSourceCommandFactory>()
                .Bind<ICommandFactory<ScriptCommand>>().As(Transient).To<ScriptCommandFactory>()
                .Bind<ICSharpScriptRunner>().To<CSharpScriptRunner>()
                .Bind<ITargetFrameworkMonikerParser>().To<TargetFrameworkMonikerParser>()
                .Bind<IEnvironmentVariables>().Bind<ITraceSource>(typeof(EnvironmentVariables)).To<EnvironmentVariables>()
                .Bind<IActive>(typeof(Debugger)).To<Debugger>()
                .Bind<IWellknownValueResolver>().To<WellknownValueResolver>()
                .Bind<IBuildResult>("base").As(Transient).To<BuildResult>()
                .Bind<IBuildResult>().As(Transient).To<ReliableBuildResult>()
                .Bind<ITextToColorStrings>().To<TextToColorStrings>()
                .Bind<ITestDisplayNameToFullyQualifiedNameConverter>().To<TestDisplayNameToFullyQualifiedNameConverter>()
                .Bind<IFileExplorer>().To<FileExplorer>()
                .Bind<IProcessOutputWriter>().To<ProcessOutputWriter>()
                .Bind<IBuildMessageLogWriter>().To<BuildMessageLogWriter>()
                .Bind<MemoryPool<TT>>().To(_ => MemoryPool<TT>.Shared)
                .Bind<IMessageIndicesReader>().To<MessageIndicesReader>()
                .Bind<IMessagesReader>().To<MessagesReader>()
                .Bind<IPathResolverContext>().Bind<IVirtualContext>().To<PathResolverContext>()
                .Bind<IEncoding>().To<Utf8Encoding>()
                .Bind<IProcessMonitor>().As(Transient).To<ProcessMonitor>()
                .Bind<IBuildOutputConverter>().To<BuildOutputConverter>()
                .Bind<IBuildMessagesProcessor>("default").To<DefaultBuildMessagesProcessor>()
                .Bind<IBuildMessagesProcessor>("custom").To<CustomMessagesProcessor>()

                // Script options factory
                .Bind<ISettingGetter<LanguageVersion>>().Bind<ISettingSetter<LanguageVersion>>().To(_ => new Setting<LanguageVersion>(LanguageVersion.Default))
                .Bind<ISettingGetter<OptimizationLevel>>().Bind<ISettingSetter<OptimizationLevel>>().To(_ => new Setting<OptimizationLevel>(OptimizationLevel.Release))
                .Bind<ISettingGetter<WarningLevel>>().Bind<ISettingSetter<WarningLevel>>().To(_ => new Setting<WarningLevel>((WarningLevel)ScriptOptions.Default.WarningLevel))
                .Bind<ISettingGetter<CheckOverflow>>().Bind<ISettingSetter<CheckOverflow>>().To(_ => new Setting<CheckOverflow>(ScriptOptions.Default.CheckOverflow ? CheckOverflow.On : CheckOverflow.Off))
                .Bind<ISettingGetter<AllowUnsafe>>().Bind<ISettingSetter<AllowUnsafe>>().To(_ => new Setting<AllowUnsafe>(ScriptOptions.Default.AllowUnsafe ? AllowUnsafe.On : AllowUnsafe.Off))
                .Bind<IAssembliesProvider>().To<AssembliesProvider>()
                .Bind<IScriptOptionsFactory>().Bind<IActive>().Tags(typeof(AssembliesScriptOptionsProvider)).To<AssembliesScriptOptionsProvider>()
                .Bind<IScriptOptionsFactory>(typeof(ConfigurableScriptOptionsFactory)).To<ConfigurableScriptOptionsFactory>()
                .Bind<IScriptOptionsFactory>(typeof(ReferencesScriptOptionsFactory)).Bind<IReferenceRegistry>().To<ReferencesScriptOptionsFactory>()
                .Bind<ICommandFactory<string>>("REPL Set a C# language version parser").To<SettingCommandFactory<LanguageVersion>>()
                .Bind<ICommandRunner>("REPL Set a C# language version").To<SettingCommandRunner<LanguageVersion>>()
                .Bind<ISettingDescription>(typeof(LanguageVersion)).To<LanguageVersionSettingDescription>()
                .Bind<ICommandFactory<string>>("REPL Set an optimization level parser").To<SettingCommandFactory<OptimizationLevel>>()
                .Bind<ICommandRunner>("REPL Set an optimization level").To<SettingCommandRunner<OptimizationLevel>>()
                .Bind<ISettingDescription>(typeof(OptimizationLevel)).To<OptimizationLevelSettingDescription>()
                .Bind<ICommandFactory<string>>("REPL Set a warning level parser").To<SettingCommandFactory<WarningLevel>>()
                .Bind<ICommandRunner>("REPL Set a warning level").To<SettingCommandRunner<WarningLevel>>()
                .Bind<ISettingDescription>(typeof(WarningLevel)).To<WarningLevelSettingDescription>()
                .Bind<ICommandFactory<string>>("REPL Set an overflow check parser").To<SettingCommandFactory<CheckOverflow>>()
                .Bind<ICommandRunner>("REPL Set an overflow check").To<SettingCommandRunner<CheckOverflow>>()
                .Bind<ISettingDescription>(typeof(CheckOverflow)).To<CheckOverflowSettingDescription>()
                .Bind<ICommandFactory<string>>("REPL Set allow unsafe parser").To<SettingCommandFactory<AllowUnsafe>>()
                .Bind<ICommandRunner>("REPL Set allow unsafe").To<SettingCommandRunner<AllowUnsafe>>()
                .Bind<ISettingDescription>(typeof(AllowUnsafe)).To<AllowUnsafeSettingDescription>()
                .Bind<ICommandFactory<string>>("REPL Set NuGet restore setting parser").To<SettingCommandFactory<NuGetRestoreSetting>>()
                .Bind<ICommandRunner>("REPL Set NuGet restore setting").To<SettingCommandRunner<NuGetRestoreSetting>>()
                .Bind<ISettingDescription>(typeof(NuGetRestoreSetting)).To<NuGetRestoreSettingDescription>()
                .Bind<IScriptSubmissionAnalyzer>().To<ScriptSubmissionAnalyzer>()
                .Bind<ICommandRunner>("CSharp").To<CSharpScriptCommandRunner>()
                .Bind<ICommandFactory<string>>("REPL Help parser").To<HelpCommandFactory>()
                .Bind<ICommandRunner>("REPL Help runner").To<HelpCommandRunner>()
                .Bind<ICommandFactory<string>>("REPL Set verbosity level parser").To<SettingCommandFactory<VerbosityLevel>>()
                .Bind<ICommandRunner>("REPL Set verbosity level runner").To<SettingCommandRunner<VerbosityLevel>>()
                .Bind<ICommandFactory<string>>("REPL Add NuGet reference parser").To<AddNuGetReferenceCommandFactory>()
                .Bind<IFilePathResolver>().To<FilePathResolver>()
                .Bind<ICommandFactory<string>>("REPL Add assembly reference parser").To<AddAssemblyReferenceCommandFactory>()
                .Bind<ICommandRunner>("REPL Add package reference runner").To<AddNuGetReferenceCommandRunner>()
                .Bind<ICommandFactory<string>>("REPL Load script").To<LoadCommandFactory>();

            DI.Setup()
                .Bind<IStartInfoFactory>().To<StartInfoFactory>()
                .Bind<IProcessManager>().As(Transient).To<ProcessManager>()
                .Bind<IProperties>("Default").To<Properties>()
                .Bind<IProperties>("TeamCity").To<TeamCityProperties>()

                // Public
                .Bind<IHost>().To<HostService>()
                .Bind<IProperties>().To(ctx => ctx.Resolve<ITeamCitySpecific<IProperties>>().Instance)
                .Bind<NuGet.INuGet>().To<NuGetService>()
                .Bind<IProcessRunner>("base").To<ProcessRunner>()
                .Bind<IProcessRunner>("inBlock").To<ProcessInBlockRunner>()
                .Bind<IProcessRunner>().To<ProcessInFlowRunner>()
                .Bind<ICommandLine>().To<CommandLineService>()
                .Bind<Dotnet.IBuild>().To<BuildService>()
                .Bind<ITeamCity>().Bind<ITeamCityContext>().To<TeamCityService>()

                // TeamCity Service messages
                .Bind<ITeamCityWriter>().To<HierarchicalTeamCityWriter>()
                .Bind<ITeamCityServiceMessages>().To<TeamCityServiceMessages>()
                .Bind<IServiceMessageFormatter>().To<ServiceMessageFormatter>()
                .Bind<IFlowIdGenerator>().Bind<IFlowContext>().To<FlowIdGenerator>()
                .Bind<DateTime>().As(Transient).To(_ => DateTime.Now)
                .Bind<IServiceMessageUpdater>().To<TimestampUpdater>()
                .Bind<ITeamCityWriter>("Root").As(Transient).To(
                    ctx => ctx.Resolve<ITeamCityServiceMessages>().CreateWriter(
                        str => ctx.Resolve<IConsole>().WriteToOut((default, str + "\n"))))
                .Bind<IServiceMessageParser>().To<ServiceMessageParser>();
        }
    }
}