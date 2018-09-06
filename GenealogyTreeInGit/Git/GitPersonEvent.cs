using System;
using System.Collections.Generic;
using System.Linq;

namespace GenealogyTreeInGit.Git
{
    public class GitPersonEvent : IComparable<GitPersonEvent>
    {
        public GitPerson Person { get; }

        public EventType Type { get; }

        public DateTime Date { get; set; }

        public GitDateType DateType { get; set; }

        public string Description { get; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EMail { get; set; }

        public List<GitPerson> Parents { get; set; } = new List<GitPerson>();

        public List<GitPerson> Children { get; set; } = new List<GitPerson>();

        public string BranchName => Person?.Id ?? string.Join("-", Parents.Select(parent => parent.Id));

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
