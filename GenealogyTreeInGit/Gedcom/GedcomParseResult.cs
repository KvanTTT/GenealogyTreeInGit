using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomParseResult
    {
        public string Title { get; set; }

        public Dictionary<string, GedcomPerson> Persons { get; } = new Dictionary<string, GedcomPerson>();

        public List<GedcomRelation> Relations { get; } = new List<GedcomRelation>();
    }
}
