// ReSharper disable ClassNeverInstantiated.Global
namespace TeamCity.CSharpInteractive
{
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using Contracts;
    using Microsoft.CodeAnalysis.CSharp.Scripting;
    using Microsoft.CodeAnalysis.Scripting;

    internal class CSharpScriptRunner : ICSharpScriptRunner
    {
        private readonly ILog<CSharpScriptRunner> _log;
        private readonly IPresenter<ScriptState<object>> _scriptStatePresenter;
        private readonly IPresenter<CompilationDiagnostics> _diagnosticsPresenter;
        private readonly IReadOnlyCollection<IScriptOptionsFactory> _scriptOptionsFactories;
        private readonly IHost _host;
        private ScriptState<object>? _scriptState;
        
        public CSharpScriptRunner(
            ILog<CSharpScriptRunner> log,
            IPresenter<ScriptState<object>> scriptStatePresenter,
            IPresenter<CompilationDiagnostics> diagnosticsPresenter,
            IReadOnlyCollection<IScriptOptionsFactory> scriptOptionsFactories,
            IHost host)
        {
            _log = log;
            _scriptStatePresenter = scriptStatePresenter;
            _diagnosticsPresenter = diagnosticsPresenter;
            _scriptOptionsFactories = scriptOptionsFactories;
            _host = host;
        }

        public bool Run(ICommand sourceCommand, string script)
        {
            var success = true;
            try
            {
                var options = _scriptOptionsFactories.Aggregate(ScriptOptions.Default, (current, scriptOptionsFactory) => scriptOptionsFactory.Create(current));
                var stopwatch = new Stopwatch();
                stopwatch.Start();

                _scriptState =
                    (_scriptState ?? CSharpScript.RunAsync(string.Empty, options, _host, typeof(IHost)).Result)
                    .ContinueWithAsync(
                        script,
                        options,
                        exception =>
                        {
                            success = false;
                            _log.Error(ErrorId.Exception, new[] {new Text(exception.ToString())});
                            return true;
                        })
                    .Result;

                stopwatch.Stop();
                _log.Trace(() => new []{new Text($"Time Elapsed {stopwatch.Elapsed:g}")});
                _diagnosticsPresenter.Show(new CompilationDiagnostics(sourceCommand, _scriptState.Script.GetCompilation().GetDiagnostics().ToList().AsReadOnly()));
            }
            catch (CompilationErrorException e)
            {
                _diagnosticsPresenter.Show(new CompilationDiagnostics(sourceCommand, e.Diagnostics.ToList().AsReadOnly()));
                success = false;
            }
            finally
            {
                if (_scriptState != null)
                {
                    _scriptStatePresenter.Show(_scriptState);
                }
            }

            return success;
        }

        public void Reset()
        {
            _log.Trace(() => new []{new Text("Reset state.")});
            _scriptState = default;
        }
    }
}