using System;
using System.IO;
using System.Linq;
using Orchard.Host;
using Orchard.Parameters;

namespace Orchard.HostContext
{
    public class CommandHostContextProvider : ICommandHostContextProvider
    {
        private readonly string[] _args;
        private TextWriter _output;
        private TextReader _input;

        public CommandHostContextProvider(string[] args)
        {
            _input = Console.In;
            _output = Console.Out;
            _args = args;
        }

        public CommandHostContext CreateContext()
        {
            var context = new CommandHostContext { RetryResult = CommandReturnCodes.Retry };
            Initialize(context);
            return context;
        }

        public void Shutdown(CommandHostContext context)
        {
            try
            {
                if (context.CommandHost != null)
                {
                    LogInfo(context, "Shutting down Orchard session...");
                    context.CommandHost.StopSession(_input, _output);
                }
            }
            catch (AppDomainUnloadedException)
            {
                LogInfo(context, "   (AppDomain already unloaded)");
            }
        }

        private void Initialize(CommandHostContext context)
        {
            context.Arguments = new OrchardParametersParser().Parse(new CommandParametersParser().Parse(_args));
            context.Logger = new Logger(context.Arguments.Verbose, _output);

            // Perform some argument validation and display usage if something is incorrect
            context.DisplayUsageHelp = context.Arguments.Switches.ContainsKey("?");
            if (context.DisplayUsageHelp)
                return;

            context.DisplayUsageHelp = context.Arguments.Arguments.Any() && context.Arguments.ResponseFiles.Any();
            if (context.DisplayUsageHelp)
            {
                _output.WriteLine("Incorrect syntax: Response files cannot be used in conjunction with commands");
                return;
            }

            if (string.IsNullOrEmpty(context.Arguments.VirtualPath))
                context.Arguments.VirtualPath = "/";
            LogInfo(context, "Virtual path: \"{0}\"", context.Arguments.VirtualPath);

            if (string.IsNullOrEmpty(context.Arguments.WorkingDirectory))
                context.Arguments.WorkingDirectory = Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location);

            LogInfo(context, "Working directory: \"{0}\"", context.Arguments.WorkingDirectory);

            LogInfo(context, "Detecting orchard installation root directory...");
            context.OrchardDirectory = GetOrchardDirectory(context.Arguments.WorkingDirectory);
            LogInfo(context, "Orchard root directory: \"{0}\"", context.OrchardDirectory.FullName);

            LogInfo(context, "Loading Orchard for command agent...");
            context.CommandHost = new CommandHost(context.OrchardDirectory.FullName);

            LogInfo(context, "Starting Orchard session");
            context.StartSessionResult = context.CommandHost.StartSession(_input, _output);
        }

        private void LogInfo(CommandHostContext context, string format, params object[] args)
        {
            if (context.Logger != null)
                context.Logger.LogInfo(format, args);
        }

        private DirectoryInfo GetOrchardDirectory(string directory)
        {
            for (var directoryInfo = new DirectoryInfo(directory); directoryInfo != null; directoryInfo = directoryInfo.Parent)
            {
                if (!directoryInfo.Exists)
                {
                    throw new ApplicationException(string.Format("Directory \"{0}\" does not exist", directoryInfo.FullName));
                }

                // We look for 
                // 1) .\web.config (or the Orchard.Web project)
                // 2) .\bin\Orchard.Framework.dll
                var webConfigFileInfo = new FileInfo(Path.Combine(directoryInfo.FullName, "web.config"));
                if (!webConfigFileInfo.Exists && !File.Exists(Path.Combine(directoryInfo.FullName, "Orchard.Web.csproj")))
                    continue;

                var binDirectoryInfo = new DirectoryInfo(Path.Combine(directoryInfo.FullName, "bin"));
                if (!binDirectoryInfo.Exists)
                    continue;

                var orchardFrameworkFileInfo = new FileInfo(Path.Combine(binDirectoryInfo.FullName, "Orchard.Framework.dll"));
                if (!orchardFrameworkFileInfo.Exists)
                    continue;

                return directoryInfo;
            }

            throw new ApplicationException(
                string.Format("Directory \"{0}\" doesn't seem to contain an Orchard installation", new DirectoryInfo(directory).FullName));
        }
    }
}
