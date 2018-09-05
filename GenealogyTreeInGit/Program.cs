
using GenealogyTreeInGit.Gedcom;
using GenealogyTreeInGit.Git;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace GenealogyTreeInGit
{
    class Program
    {
        static void Main(string[] args)
        {
            var logger = new ConsoleLogger();

            if (args.Length == 0)
            {
                logger.LogError("Path to .ged file should be specified");
                return;
            }

            if (!File.Exists(args[0]))
            {
                logger.LogError($"File {args[0]} does not exist");
                return;
            }

            var gedcomParser = new GedcomParser()
            {
                Logger = logger
            };
            GedcomParseResult parseResult = gedcomParser.Parse(args[0]);

            var converter = new GedcomToGitFamilyConverter();
            Family family = converter.Convert(parseResult);

            bool runScript = false;
            var commandGenerator = new GitCommandGenerator(family);

            for (int i = 1; i < args.Length; i++)
            {
                string arg = args[i];
                if (arg == "-r" || arg == "--run")
                {
                    runScript = true;
                }
                else if (arg == "--only-birth-events")
                {
                    commandGenerator.OnlyBirthEvents = true;
                }
                else if (arg == "--ignore-not-person-events")
                {
                    commandGenerator.IgnoreNotPersonEvents = true;
                }
                else if (arg == "--ignore-events-without-date")
                {
                    commandGenerator.IgnoreEventsWithoutDate = true;
                }
                else if (arg.StartsWith("http") || arg.StartsWith("git"))
                {
                    commandGenerator.Source = arg; 
                }
            }

            string commands = commandGenerator.Generate();
            string filePath = Path.GetDirectoryName(args[0]);
            string fileName = Path.GetFileNameWithoutExtension(args[0]);

            string ext = RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ? ".cmd" : ".sh";
            string scriptFileName = Path.Combine(filePath, fileName + ext);

            File.WriteAllText(scriptFileName, commands);

            if (runScript)
            {
                string dirName = Path.Combine(filePath, fileName);
                if (Directory.Exists(dirName))
                {
                    logger.LogError($"Directory {dirName} exists. Remove it before running script");
                    return;
                }

                var runner = new GitScriptRunner
                {
                    Logger = logger
                };
                runner.Run(filePath, scriptFileName);
            }
        }
    }
}
