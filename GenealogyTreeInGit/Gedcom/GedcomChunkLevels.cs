using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    /// <summary>
    /// Keeps track of the current level hierarchy.
    /// Just for temporary usage during parsing.
    /// </summary>
    public class GedcomChunkLevels
    {
        private readonly Dictionary<int, GedcomChunk> _currentLevelChunks = new Dictionary<int, GedcomChunk>();

        public void Set(GedcomChunk gedcomChunk)
        {
            _currentLevelChunks[gedcomChunk.Level] = gedcomChunk;
        }

        public GedcomChunk GetParentChunk(GedcomChunk gedcomChunk)
        {
            return _currentLevelChunks[gedcomChunk.Level - 1];
        }
    }
}