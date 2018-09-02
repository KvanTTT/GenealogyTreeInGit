using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    /// <summary>
    /// This represents a line from the GEDCOM file AND all its related sublines in a structured hierarchy.
    /// </summary>
    public class GedcomChunk : GedcomLine
    {
        public Dictionary<string, List<GedcomChunk>> Subchunks { get; }

        public GedcomChunk(GedcomLine gedcomLine)
            : base(gedcomLine.Level, gedcomLine.Id, gedcomLine.Type, gedcomLine.Data, gedcomLine.Reference)
        {
            Subchunks = new Dictionary<string, List<GedcomChunk>>();
        }

        public override string ToString()
        {
            return base.ToString() + $" Subchunks: {Subchunks.Count}";
        }
    }
}