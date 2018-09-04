using GenealogyTreeInGit.Gedcom;
using GenealogyTreeInGit.Gedcom.Relations;
using GenealogyTreeInGit.Git;
using System;
using System.Collections.Generic;
using System.Linq;

namespace GenealogyTreeInGit
{
    public class GedcomToGitFamilyConverter
    {
        public Family Convert(GedcomParseResult parseResult)
        {
            var persons = new Dictionary<string, GitPerson>();

            Dictionary<string, DateTime> minDates = GetMinDatesForPersons(parseResult);

            foreach (KeyValuePair<string, GedcomPerson> gedcomPerson in parseResult.Persons)
            {
                GitPerson gitPerson = Convert(gedcomPerson.Value,
                    minDates.TryGetValue(gedcomPerson.Key, out DateTime minDate) ? minDate : DateTime.MinValue);
                persons.Add(gitPerson.Id, gitPerson);
            }

            FillParents(parseResult, persons);

            var family = new Family(parseResult.Title, persons, new List<GitPersonEvent>());
            return family;
        }

        private static Dictionary<string, DateTime> GetMinDatesForPersons(GedcomParseResult parseResult)
        {
            Dictionary<string, DateTime> minDates = new Dictionary<string, DateTime>();

            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (relation is ChildRelation childRelation)
                {
                    if (parseResult.Persons.TryGetValue(childRelation.ToId, out GedcomPerson parent) &&
                        parseResult.Persons.TryGetValue(childRelation.FromId, out GedcomPerson child))
                    {
                        GedcomEvent gedcomEvent = parent.Events.FirstOrDefault(ev => IsParentBeforeTheChildBirthEvent(ev.Type));
                        if (gedcomEvent?.Date != null &&
                            (minDates.TryGetValue(child.Id, out DateTime minDate) ? minDate > (DateTime)gedcomEvent.Date : true))
                        {
                            minDates[child.Id] = gedcomEvent.Date.DefaultDate.AddTicks(1);
                        }
                    }
                }
            }

            return minDates;
        }

        private static void FillParents(GedcomParseResult parseResult, Dictionary<string, GitPerson> persons)
        {
            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (relation is ChildRelation childRelation)
                {
                    if (persons.TryGetValue(childRelation.ToId, out GitPerson parent) &&
                        persons.TryGetValue(childRelation.FromId, out GitPerson child))
                    {
                        bool birthEventExists = false;

                        for (int i = 0; i < child.Events.Count; i++)
                        {
                            if (child.Events[i].Type == EventType.Birth)
                            {
                                birthEventExists = true;

                                var childEvent = child.Events[i] is GitExtendedPersonEvent gitExtendedPersonEvent
                                    ? gitExtendedPersonEvent
                                    : new GitExtendedPersonEvent(child.Events[i]);

                                DateTime parentBirthDate = parent.Events.FirstOrDefault(p => IsParentBeforeTheChildBirthEvent(p.Type))?.Date ?? default(DateTime);

                                if (childEvent.Date < parentBirthDate)
                                {
                                    childEvent.Date = parentBirthDate;
                                    childEvent.DateType = GitDateType.After;
                                }

                                childEvent.Parents.Add(parent);
                                child.Events[i] = childEvent;
                            }
                        }

                        if (!birthEventExists)
                        {
                            DateTime parentBirthDate = parent.Events.FirstOrDefault(p => IsParentBeforeTheChildBirthEvent(p.Type))?.Date ?? default(DateTime);
                            GitExtendedPersonEvent birthEvent = CreateBirthEvent(parseResult.Persons[child.Id], child, null, parentBirthDate, GitDateType.After);
                            birthEvent.Parents.Add(parent);
                            child.Events.Add(birthEvent);
                        }
                    }
                }
            }
        }

        private static GitPerson Convert(GedcomPerson gedcomPerson, DateTime minDate)
        {
            var result = new GitPerson(gedcomPerson.Id)
            {
                FirstName = gedcomPerson.FirstName,
                LastName = gedcomPerson.LastName,
            };

            DateTime curDate = minDate;

            var events = new List<GitPersonEvent>();

            foreach (GedcomEvent ev in gedcomPerson.Events)
            {
                GitDateType dateType;

                if (ev.Date?.IsDefined == true)
                {
                    curDate = (DateTime)ev.Date;
                    dateType = GitDateType.Exact;
                }
                else
                {
                    curDate = curDate.AddTicks(1);
                    dateType = GitDateType.After;
                }

                GitPersonEvent gitPersonEvent;

                if (ev.Type == EventType.Birth)
                {
                    gitPersonEvent = CreateBirthEvent(gedcomPerson, result, ev, curDate, dateType);
                }
                else
                {
                    string description = GenerateDescription(result, ev, curDate, dateType);
                    gitPersonEvent = new GitPersonEvent(result, ev.Type, curDate, description, dateType);
                }

                events.Add(gitPersonEvent);
            }

            result.Events = events.OrderBy(ev => ev.Date).ToList();

            return result;
        }

        private static GitExtendedPersonEvent CreateBirthEvent(GedcomPerson gedcomPerson, GitPerson gitPerson, GedcomEvent ev, DateTime date, GitDateType dateType)
        {
            string description =
                GenerateDescription(gitPerson, ev, date, dateType) +
                " " + Utils.JoinNotEmpty(gedcomPerson.Gender, gedcomPerson.Education,
                gedcomPerson.Religion, gedcomPerson.Note, gedcomPerson.Changed,
                gedcomPerson.Occupation, gedcomPerson.Health, gedcomPerson.Title);

            return new GitExtendedPersonEvent(gitPerson, EventType.Birth, date, description, dateType);
        }

        private static string GenerateDescription(GitPerson gitPerson, GedcomEvent ev, DateTime date, GitDateType dateType)
        {
            string dateStr = dateType == GitDateType.Exact ? "at " + date.ToShortDateString() : "";
            return Utils.JoinNotEmpty(gitPerson.FirstName, gitPerson.LastName, ":", ev?.Type.ToString(), dateStr, ev?.Place, ev?.Latitude, ev?.Longitude, ev?.Note);
        }

        private static bool IsParentBeforeTheChildBirthEvent(EventType type)
        {
            return type == EventType.Birth || type == EventType.Baptized;
        }
    }
}
