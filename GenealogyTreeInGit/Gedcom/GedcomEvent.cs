using System;
using System.Collections.Generic;

namespace GenealogyTreeInGit.Gedcom
{
    public class GedcomEvent : IComparable<GedcomEvent>
    {
        public static Dictionary<string, EventType> EventTypesMap = new Dictionary<string, EventType>()
        {
            ["BAPM"] = EventType.Baptized,
            ["BIRT"] = EventType.Birth,
            ["BURI"] = EventType.Buried,
            ["CHR"] = EventType.Baptized,
            ["DEAT"] = EventType.Death,
            ["EMIG"] = EventType.Emigrated,
            ["GRAD"] = EventType.Graduation,
            ["IMMI"] = EventType.Immigrated,
            ["NATU"] = EventType.BecomingCitizen,
            ["RESI"] = EventType.Residence,

            ["DIV"] = EventType.Divorce,
            ["MARR"] = EventType.Marriage,

            ["ADOP"] = EventType.Adoption,
        };

        public EventType Type { get; }

        public GedcomDate Date { get; set; }

        public string Place { get; set; }

        public string Latitude { get; set; }

        public string Longitude { get; set; }

        public string Note { get; set; }

        public GedcomEvent(EventType eventType)
        {
            Type = eventType;
        }

        public int CompareTo(GedcomEvent other)
        {
            if (other == null)
                return 1;

            if (Date == null && other.Date == null)
                return 0;

            if (Date != null && other.Date == null)
                return 1;

            if (Date == null && other.Date != null)
                return -1;

            return Date.DefaultDate.CompareTo(other.Date.DefaultDate);
        }

        public override string ToString()
        {
            return Utils.JoinNotEmpty(Type.ToString(), Date?.ToString(), Place, Latitude, Longitude, Note);
        }
    }
}