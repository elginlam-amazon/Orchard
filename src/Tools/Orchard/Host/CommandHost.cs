using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Orchard.Parameters;
using Orchard.ResponseFiles;

namespace Orchard.Host
{

    /// <summary>
    /// Different return codes for a command execution.
    /// </summary>
    public enum CommandReturnCodes
    {
        Ok = 0,
        Fail = 5,
        Retry = 240
    }

    /// <summary>
    /// The CommandHost serves as an intermediate between the command line and the CommandHostAgent,
    /// which is known to the Orchard Framework and has the ability to execute commands.
    /// </summary>
    /// <remarks>
    /// It formerly ran inside an ASP.NET AppDomain created for the Orchard installation. AppDomains don't exist
    /// anymore on .NET: the Orchard assemblies are loaded from the bin folder of the installation into the
    /// command line process, and the agent runs in-process.
    /// </remarks>
    public class CommandHost
    {
        private readonly string _orchardDirectory;
        private readonly Assembly _frameworkAssembly;
        private object _agent;

        public CommandHost(string orchardDirectory)
        {
            _orchardDirectory = orchardDirectory;
            _frameworkAssembly = LoadFramework(orchardDirectory);
            InitializeHostingEnvironment();
        }

        private static Assembly LoadFramework(string orchardDirectory)
        {
            var binDirectory = Path.Combine(orchardDirectory, "bin");

            // the dependencies of the Orchard assemblies are resolved from the bin folder of the installation
            AssemblyLoadContext.Default.Resolving += (context, assemblyName) =>
            {
                var path = Path.Combine(binDirectory, assemblyName.Name + ".dll");
                return File.Exists(path) ? context.LoadFromAssemblyPath(path) : null;
            };

            var loaded = AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Orchard.Framework");
            return loaded ?? AssemblyLoadContext.Default.LoadFromAssemblyPath(Path.Combine(binDirectory, "Orchard.Framework.dll"));
        }

        private void InitializeHostingEnvironment()
        {
            // the application relative paths (~/App_Data, ~/Modules, ...) are resolved from the installation directory
            Directory.SetCurrentDirectory(_orchardDirectory);

            var hostingEnvironment = _frameworkAssembly.GetType("Orchard.Environment.HostingEnvironment", true);
            hostingEnvironment.GetMethod("Initialize").Invoke(null, new object[] { _orchardDirectory, (Action)(() => { }) });
        }

        public CommandReturnCodes StartSession(TextReader input, TextWriter output)
        {
            _agent = CreateAgent();
            return StartHost(_agent, input, output);
        }

        public void StopSession(TextReader input, TextWriter output)
        {
            if (_agent != null)
            {
                StopHost(_agent, input, output);
                _agent = null;
            }
        }

        public CommandReturnCodes RunCommand(TextReader input, TextWriter output, Logger logger, OrchardParameters args)
        {
            var agent = CreateAgent();
            CommandReturnCodes result = (CommandReturnCodes)(int)agent.GetType().GetMethod("RunSingleCommand").Invoke(agent, new object[] {
                input,
                output,
                args.Tenant,
                args.Arguments.ToArray(),
                ToDictionary(args.Switches)});

            return result;
        }

        public CommandReturnCodes RunCommandInSession(TextReader input, TextWriter output, Logger logger, OrchardParameters args)
        {
            CommandReturnCodes result = (CommandReturnCodes)(int)_agent.GetType().GetMethod("RunCommand").Invoke(_agent, new object[] {
                input,
                output,
                args.Tenant,
                args.Arguments.ToArray(),
                ToDictionary(args.Switches)});

            return result;
        }

        public CommandReturnCodes RunCommands(TextReader input, TextWriter output, Logger logger, IEnumerable<ResponseLine> responseLines)
        {
            var agent = CreateAgent();

            CommandReturnCodes result = StartHost(agent, input, output);
            if (result != CommandReturnCodes.Ok)
                return result;

            foreach (var line in responseLines)
            {
                logger.LogInfo("{0} ({1}): Running command: {2}", line.Filename, line.LineNumber, line.LineText);

                var args = new OrchardParametersParser().Parse(new CommandParametersParser().Parse(line.Args));

                result = (CommandReturnCodes)(int)agent.GetType().GetMethod("RunCommand").Invoke(agent, new object[] {
                    input,
                    output,
                    args.Tenant,
                    args.Arguments.ToArray(),
                    ToDictionary(args.Switches)});

                if (result != CommandReturnCodes.Ok)
                {
                    output.WriteLine("{0} ({1}): Command returned error ({2})", line.Filename, line.LineNumber, result);
                    return result;
                }
            }

            result = StopHost(agent, input, output);
            return result;
        }

        private static Dictionary<string, string> ToDictionary(IDictionary<string, string> switches)
        {
            return switches as Dictionary<string, string> ?? new Dictionary<string, string>(switches, StringComparer.OrdinalIgnoreCase);
        }

        private object CreateAgent()
        {
            return Activator.CreateInstance(_frameworkAssembly.GetType("Orchard.Commands.CommandHostAgent", true));
        }

        private CommandReturnCodes StopHost(object agent, TextReader input, TextWriter output)
        {
            return (CommandReturnCodes)(int)agent.GetType().GetMethod("StopHost").Invoke(agent, new object[] { input, output });
        }

        private CommandReturnCodes StartHost(object agent, TextReader input, TextWriter output)
        {
            return (CommandReturnCodes)(int)agent.GetType().GetMethod("StartHost").Invoke(agent, new object[] { input, output });
        }
    }
}
