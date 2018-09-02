namespace GenealogyTreeInGit.Gedcom
{
    public abstract class GedcomRelation
    {
        public string FamilyId { get; }

        public string FromId { get; }

        public string ToId { get; }

        public GedcomRelation(string familtyId, string fromId, string toId)
        {
            FromId = fromId;
            ToId = toId;
        }

        public override string ToString()
        {
            return $"{FromId} - {ToId}";
        }
    }
}
