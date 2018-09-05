using System;
using System.Runtime.InteropServices;

namespace GenealogyTreeInGit
{
    public class GitScriptRunner
    {
        public ILogger Logger { get; set; }

        public void Run(string filePath, string scriptFileName)
        {
            string toolName, arguments;

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                toolName = "cmd";
                arguments = $@"/c ""{scriptFileName}""";
            }
            else
            {
                toolName = "sh";
                arguments = '\"' + scriptFileName + '\"';
            }

            var processor = new Processor(toolName, scriptFileName)
            {
                WorkingDirectory = filePath
            };

            processor.ErrorDataReceived += (object sender, string e) =>
            {
                Logger?.LogError(e);
            };

            processor.OutputDataReceived += (object sender, string e) =>
            {
                Logger?.LogInfo(e);
            };

            processor.Start();
        }
    }
}
