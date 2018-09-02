using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomPerson
    {
        public string Id { get; set; }

        public string Uid { get; set; }

        public string IdNumber { get; set; }

        public string FirstName { get; set; }

        public string LastName { get; set; }

        public string Gender { get; set; }

        public List<GedcomEvent> Events { get; set; } = new List<GedcomEvent>();

        public string Education { get; set; }

        public string Religion { get; set; }

        public string Note { get; set; }

        public string Changed { get; set; }

        public string Occupation { get; set; }

        public string Health { get; set; }

        public string Title { get; set; }

        public GedcomAddress Address { get; set; }

        public GedcomPerson(string id)
        {
            Id = id;
        }

        public override string ToString()
        {
            return Utils.JoinNotEmpty(Id, FirstName, LastName);
        }
    }
}
