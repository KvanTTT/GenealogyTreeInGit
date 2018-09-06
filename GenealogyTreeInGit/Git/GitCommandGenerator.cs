using System;
using System.Collections.Generic;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;

namespace GenealogyTreeInGit.Git
{
    class GitCommandGenerator
    {
        private static readonly DateTime MinGitDateTime = new DateTime(1970, 1, 1);

        public Family Family { get; }

        public string Source { get; set; }

        public bool OnlyBirthEvents { get; set; }

        public bool IgnoreEventsWithoutDate { get; set; }

        public bool IgnoreNotPersonEvents { get; set; }

        public GitCommandGenerator(Family family)
        {
            Family = family ?? throw new ArgumentNullException(nameof(family));
        }

        public string Generate()
        {
            var result = new StringBuilder();

            AppendInitialization(result);
            AppendPersons(result);

            return result.ToString();
        }

        private void AppendInitialization(StringBuilder builder)
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                builder.AppendLine("chcp 65001"); // Switch encoding to UTF8
            }
            builder.AppendLine($"mkdir {Family.Title}");
            builder.AppendLine($"cd {Family.Title}");
            builder.AppendLine("git init");
            builder.AppendLine();
        }

        private void AppendPersons(StringBuilder builder)
        {
            var events = new List<GitPersonEvent>();

            foreach (KeyValuePair<string, GitPerson> person in Family.Persons)
            {
                foreach (GitPersonEvent ev in person.Value.Events)
                {
                    if (!OnlyBirthEvents || ev.Type == EventType.Birth)
                    {
                        events.Add(ev);
                    }
                }
            }

            if (!IgnoreNotPersonEvents)
            {
                foreach (GitPersonEvent ev in Family.Events)
                {
                    if (!OnlyBirthEvents || ev.Type == EventType.Birth)
                    {
                        events.Add(ev);
                    }
                }
            }

            events.Sort();

            var existedBranches = new HashSet<string>();
            string currenBranchName = null;

            foreach (GitPersonEvent ev in events)
            {
                if (currenBranchName != ev.BranchName)
                {
                    currenBranchName = ev.BranchName;
                    string orphan = existedBranches.Contains(currenBranchName) ? "" : "--orphan";
                    builder.AppendLine(Utils.JoinNotEmpty("git checkout", orphan, currenBranchName));
                    existedBranches.Add(currenBranchName);
                }

                foreach (GitPerson parent in ev.Parents)
                {
                    builder.AppendLine($"git merge {parent.Id} --allow-unrelated-histories --no-commit");
                }

                DateTime dateTime = ev.Date > MinGitDateTime ? ev.Date : MinGitDateTime;
                string fullName = Utils.JoinNotEmpty(ev.FirstName ?? ev.Person?.FirstName, ev.LastName ?? ev.Person?.LastName);
                string message = Escape(ev.Description);
                string date = dateTime.ToString(CultureInfo.InvariantCulture);
                string author = Escape(Utils.JoinNotEmpty(fullName, $"<{ev.EMail ?? ev.Person?.EMail}>"));

                builder.AppendLine($@"git commit -m ""{message}"" --date ""{date}"" --author ""{author}"" --allow-empty");
            }

            builder.AppendLine();

            if (Source != null)
            {
                builder.AppendLine($"git remote add origin {Source}");
                builder.AppendLine("git push --all -u --force");
            }
        }

        private static string Escape(string str)
        {
            str = str.Replace("\\", "\\\\");
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                // TODO: try to fix line breaks and double quotes removing (does not work on Windows)
                str = str.Replace("\r\n", " ").Replace("\n", " ");
                str = str.Replace('"', '\'');
            }
            else
            {
                str = str.Replace("\"", "\\\"");
            }
            return str;
        }
    }
}
