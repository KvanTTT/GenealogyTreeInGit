namespace GenealogyTreeInGit.Gedcom.Relations
{
    public class SiblingRelation : GedcomRelation
    {
        public SiblingRelation(string familyId, string fromId, string toId)
            : base(familyId, fromId, toId)
        {
        }

        public override string ToString()
        {
            return "Sibling: " + base.ToString();
        }
    }
}
