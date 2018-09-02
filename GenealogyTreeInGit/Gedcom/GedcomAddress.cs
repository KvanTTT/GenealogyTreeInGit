using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomAddress
    {
        public string Country { get; set; }

        public string State { get; set; }

        public string City { get; set; }

        public string Street { get; set; }

        public string ZipCode { get; set; }

        public List<string> Phone { get; set; } = new List<string>();

        public List<string> Fax { get; set; } = new List<string>();

        public List<string> Email { get; set; } = new List<string>();

        public List<string> Web { get; set; } = new List<string>();

        public override string ToString()
        {
            return Utils.JoinNotEmpty(Country, City, Street);
        }
    }
}
