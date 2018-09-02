namespace GenealogyTreeInGit.Gedcom.Relations
{
    public class SpouseRelation : GedcomRelation
    {
        public GedcomEvent Marriage { get; set; }

        public GedcomEvent Divorce { get; set; }

        public string Relation { get; set; }

        public string Note { get; set; }

        public SpouseRelation(string familyId, string fromId, string toId)
            : base(familyId, fromId, toId)
        {
        }

        public override string ToString()
        {
            return "Spouse: " + base.ToString();
        }
    }
}
