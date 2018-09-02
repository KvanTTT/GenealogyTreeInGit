namespace GenealogyTreeInGit.Gedcom.Relations
{
    public class ChildRelation : GedcomRelation
    {
        public string Pedigree { get; set; }

        public string Validity { get; set; }

        public string Adoption { get; set; }

        public ChildRelation(string familyId, string fromId, string toId)
            : base(familyId, fromId, toId)
        {
        }

        public override string ToString()
        {
            return "Child: " + base.ToString();
        }
    }
}
