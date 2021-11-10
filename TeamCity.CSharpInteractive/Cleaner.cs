// ReSharper disable ClassNeverInstantiated.Global
namespace TeamCity.CSharpInteractive
{
    using System;

    internal class Cleaner : ICleaner
    {
        private readonly ILog<Cleaner> _log;
        private readonly IFileSystem _fileSystem;

        public Cleaner(ILog<Cleaner> log, IFileSystem fileSystem)
        {
            _log = log;
            _fileSystem = fileSystem;
        }

        public IDisposable Track(string path)
        {
            _log.Trace($"Start tracking \"{path}\".");
            return Disposable.Create(() =>
            {
                _log.Trace($"Delete \"{path}\".");
                _fileSystem.DeleteDirectory(path, true);
            });
        }
    }
}