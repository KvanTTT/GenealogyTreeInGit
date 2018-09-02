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

        public Uri Source { get; set; }

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
            AppendFinalization(result);

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
                if (currenBranchName != ev.Person.Id)
                {
                    currenBranchName = ev.Person.Id;
                    string orphan = existedBranches.Contains(currenBranchName) ? "" : "--orphan";
                    builder.AppendLine(Utils.JoinNotEmpty("git checkout", orphan, currenBranchName));
                    existedBranches.Add(currenBranchName);
                }

                if (ev is GitExtendedPersonEvent gitExtendedPersonEvent)
                {
                    foreach (GitPerson parent in gitExtendedPersonEvent.Parents)
                    {
                        builder.AppendLine($"git merge {parent.Id} --allow-unrelated-histories --no-commit");
                    }
                }

                DateTime dateTime = ev.Date > MinGitDateTime ? ev.Date : MinGitDateTime;
                string fullName = Utils.JoinNotEmpty(ev.Person.FirstName, ev.Person.LastName);
                string message = Escape(fullName + ": " + ev.ToString());
                string date = dateTime.ToString(CultureInfo.InvariantCulture);
                string author = Escape(Utils.JoinNotEmpty(fullName, $"<{ev.Person.EMail}>"));

                builder.AppendLine($@"git commit -m ""{message}"" --date ""{date}"" --author ""{author}"" --allow-empty");
            }
        }

        private void AppendFinalization(StringBuilder builder)
        {
            builder.AppendLine($"cd ..");
            if (Source != null)
            {
                builder.AppendLine();
            }
        }

        private static string Escape(string str)
        {
            return str.Replace("\\", "\\\\");
        }
    }
}
