// ReSharper disable ClassNeverInstantiated.Global
namespace TeamCity.CSharpInteractive
{
    using System.Collections;
    using System.Collections.Generic;
    using System.Diagnostics.CodeAnalysis;
    using System.Linq;

    [ExcludeFromCodeCoverage]
    internal class EnvironmentVariables : IEnvironmentVariables, ITraceSource
    {
        private readonly ILog<EnvironmentVariables> _log;

        public EnvironmentVariables(ILog<EnvironmentVariables> log) => _log = log;

        public string? GetEnvironmentVariable(string variable)
        {
            var value = System.Environment.GetEnvironmentVariable(variable);
            _log.Trace(() => new []{new Text($"Get environment variable {variable} = \"{value}\".")});
            return value;
        }

        public IEnumerable<Text> Trace
        {
            get
            {
                yield return new Text("Environment variables:");
                foreach (var entry in System.Environment.GetEnvironmentVariables().OfType<DictionaryEntry>().OrderBy(i => i.Key))
                {
                    yield return Text.Tab;
                    yield return new Text($"{entry.Key}={entry.Value}");
                }
            }
        }
    }
}