// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable ForCanBeConvertedToForeach
namespace TeamCity.CSharpInteractive
{
    using System;
    using System.Diagnostics;
    using System.Threading;
    using System.Threading.Tasks;
    using Cmd;

    internal class ProcessRunner: IProcessRunner
    {
        private readonly Func<IProcessManager> _processManagerFactory;
        private readonly CancellationTokenSource _cancellationTokenSource;

        public ProcessRunner(
            Func<IProcessManager> processManagerFactory,
            CancellationTokenSource cancellationTokenSource)
        {
            _processManagerFactory = processManagerFactory;
            _cancellationTokenSource = cancellationTokenSource;
        }

        public ProcessResult Run(ProcessRun processRun, TimeSpan timeout)
        {
            var (startInfo, monitor, handler, stateProvider) = processRun;
            using var processManager = _processManagerFactory();
            if (handler != default)
            {
                processManager.OnOutput += handler;
            }

            var stopwatch = new Stopwatch();
            stopwatch.Start();
            if (!processManager.Start(startInfo))
            {
                stopwatch.Stop();
                monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, ProcessState.Failed);
                return new ProcessResult(ProcessState.Failed);
            }

            monitor.Started(startInfo, processManager.Id);
            var finished = true;
            if (timeout == TimeSpan.Zero)
            {
                processManager.WaitForExit();
            }
            else
            {
                finished = processManager.WaitForExit(timeout);
            }

            if (finished)
            {
                stopwatch.Stop();
                var state = stateProvider?.GetState(processManager.ExitCode) ?? ProcessState.Unknown;
                monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, state, processManager.ExitCode);
                return new ProcessResult(state, processManager.ExitCode);
            }

            processManager.Kill();
            stopwatch.Stop();
            monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, ProcessState.Canceled);

            return new ProcessResult(ProcessState.Canceled);
        }

        public async Task<ProcessResult> RunAsync(ProcessRun processRun, CancellationToken cancellationToken)
        {
            var (startInfo, monitor, handler, stateProvider) = processRun;
            if (cancellationToken == default || cancellationToken == CancellationToken.None)
            {
                cancellationToken = _cancellationTokenSource.Token;
            }

            var processManager = _processManagerFactory();
            if (handler != default)
            {
                processManager.OnOutput += handler;
            }
            
            var completionSource = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
            // ReSharper disable once AccessToDisposedClosure
            processManager.OnExit += () => completionSource.TrySetResult(processManager.ExitCode);
            var stopwatch = new Stopwatch();
            if (!processManager.Start(startInfo))
            {
                stopwatch.Stop();
                monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, ProcessState.Failed);
                processManager.Dispose();
                return new ProcessResult(ProcessState.Failed);
            }
            
            monitor.Started(startInfo, processManager.Id);
            void Cancel()
            {
                if (processManager.Kill())
                {
                    completionSource.TrySetCanceled(cancellationToken);
                }
                
                processManager.Dispose();
                stopwatch.Stop();
                monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, ProcessState.Canceled);
            }

            await using (cancellationToken.Register(Cancel, false))
            {
                using (processManager)
                {
                    var exitCode = await completionSource.Task.ConfigureAwait(false);
                    stopwatch.Start();
                    var state = stateProvider?.GetState(exitCode) ?? ProcessState.Unknown;
                    monitor.Finished(startInfo, stopwatch.ElapsedMilliseconds, state, exitCode);
                    return new ProcessResult(state, exitCode);
                }
            }
        }
    }
}