using System;
using System.Collections.Generic;

namespace GenealogyTreeInGit.Git
{
    public class GitExtendedPersonEvent : GitPersonEvent
    {
        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EMail { get; set; }

        public List<GitPerson> Parents { get; set; } = new List<GitPerson>();

        public GitExtendedPersonEvent(GitPerson gitPerson, EventType type, DateTime date, string description)
            : base(gitPerson, type, date, description)
        {
        }

        public GitExtendedPersonEvent(GitPersonEvent gitPersonEvent)
            : base(gitPersonEvent.Person, gitPersonEvent.Type, gitPersonEvent.Date, gitPersonEvent.Description)
        {
        }
    }
}
