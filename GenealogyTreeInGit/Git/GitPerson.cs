using System;
using System.Collections.Generic;

namespace GenealogyTreeInGit.Git
{
    public class GitPerson
    {
        public string Id { get; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string EMail { get; set; }

        public List<GitPersonEvent> Events { get; set; } = new List<GitPersonEvent>();

        public GitPerson(string id)
        {
            Id = id ?? throw new ArgumentNullException(nameof(id));
        }

        public override string ToString()
        {
            return Utils.JoinNotEmpty(Id, FirstName, LastName, EMail);
        }
    }
}
