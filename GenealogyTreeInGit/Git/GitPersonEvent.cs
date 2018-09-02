using System;

namespace GenealogyTreeInGit.Git
{
    public class GitPersonEvent : IComparable<GitPersonEvent>
    {
        public GitPerson Person { get; }

        public EventType Type { get; }

        public DateTime Date { get; }

        public string Description { get; }

        public GitPersonEvent(GitPerson gitPerson, EventType type, DateTime date, string description)
        {
            Person = gitPerson;
            Type = type;
            Date = date;
            Description = description;
        }

        public int CompareTo(GitPersonEvent other)
        {
            if (other == null)
                return 1;

            return Date.CompareTo(other.Date);
        }

        public override string ToString()
        {
            string dateStr = Date.IsDateUndefined() ? "" : Date.ToShortDateString();
            string dateDesr = Utils.JoinNotEmpty(dateStr, Description);
            if (!string.IsNullOrEmpty(dateDesr))
            {
                dateDesr = " at " + dateDesr;
            }
            return Type.ToString() + dateDesr;
        }
    }
}
