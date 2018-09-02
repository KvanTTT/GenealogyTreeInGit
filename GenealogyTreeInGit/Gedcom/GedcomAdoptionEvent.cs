namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomAdoptionEvent : GedcomEvent
    {
        public GedcomAdoptionEvent()
            : base(EventType.Adoption)
        {
        }

        public string AdoptionType { get; set; }

        public string AdoptingParents { get; set; }
    }
}
