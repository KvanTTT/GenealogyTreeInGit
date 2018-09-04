using System;

namespace GenealogyTreeInGit.Git
{
    public class GitPersonEvent : IComparable<GitPersonEvent>
    {
        public GitPerson Person { get; }

        public EventType Type { get; }

        public DateTime Date { get; set; }

        public GitDateType DateType { get; set; }

        public string Description { get; }

        public GitPersonEvent(GitPerson gitPerson, EventType type, DateTime date, string description, GitDateType dateType = GitDateType.Exact)
        {
            Person = gitPerson;
            Type = type;
            Date = date;
            Description = description;
            DateType = dateType;
        }

        public int CompareTo(GitPersonEvent other)
        {
            if (other == null)
                return 1;

            return Date.CompareTo(other.Date);
        }

        public override string ToString()
        {
            return Description;
        }
    }
}
