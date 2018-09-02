
using GenealogyTreeInGit.Gedcom;
using GenealogyTreeInGit.Git;
using System.IO;

namespace GenealogyTreeInGit
{
    class Program
    {
        static void Main(string[] args)
        {
            var gedcomParser = new GedcomParser();
            GedcomParseResult parseResult = gedcomParser.Parse(args[0]);

            var converter = new GedcomToGitFamilyConverter();
            Family family = converter.Convert(parseResult);

            var commandGenerator = new GitCommandGenerator(family);
            commandGenerator.OnlyBirthEvents = false;
            string commands = commandGenerator.Generate();

            string filePath = Path.GetDirectoryName(args[0]);
            string fileName = Path.GetFileNameWithoutExtension(args[0]);
            File.WriteAllText(Path.Combine(filePath, fileName + ".cmd"), commands);
        }
    }
}
