using GenealogyTreeInGit.Gedcom;
using GenealogyTreeInGit.Gedcom.Relations;
using GenealogyTreeInGit.Git;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GenealogyTreeInGit
{
    public class GedcomToGitFamilyConverter
    {
        public Family Convert(GedcomParseResult parseResult)
        {
            var persons = new Dictionary<string, GitPerson>();

            foreach (KeyValuePair<string, GedcomPerson> gedcomPerson in parseResult.Persons)
            {
                GitPerson gitPerson = Convert(gedcomPerson.Value);
                persons.Add(gitPerson.Id, gitPerson);
            }

            FillRelations(parseResult, persons, out List<GitPersonEvent> notPersonEvents);

            var family = new Family(parseResult.Title, persons, notPersonEvents);
            return family;
        }

        private static void FillRelations(GedcomParseResult parseResult, Dictionary<string, GitPerson> persons,
            out List<GitPersonEvent> notPersonEvents)
        {
            notPersonEvents = new List<GitPersonEvent>();

            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (persons.TryGetValue(relation.ToId, out GitPerson toPerson) &&
                    persons.TryGetValue(relation.FromId, out GitPerson fromPerson))
                {
                    if (relation is ChildRelation childRelation)
                    {
                        GitPerson parent = toPerson;
                        GitPerson child = fromPerson;

                        DateTime prevDate = parent.Events.FirstOrDefault(p => IsParentBeforeTheChildBirthEvent(p.Type))?.Date ?? DateTime.MinValue;

                        for (int i = 0; i < child.Events.Count; i++)
                        {
                            GitPersonEvent childEvent = child.Events[i];

                            if (childEvent.Date <= prevDate && childEvent.DateType != GitDateType.Exact)
                            {
                                childEvent.Date = prevDate.AddTicks(1);
                                childEvent.DateType = GitDateType.After;
                                FixChildrenDates(childEvent);
                            }
                            prevDate = childEvent.Date;

                            if (childEvent.Type == EventType.Birth)
                            {
                                childEvent.Parents.Add(parent);
                                child.Events[i] = childEvent;
                                InsertChild(parseResult.Persons[parent.Id], parent, child, childEvent);
                            }
                        }
                    }
                    else if (relation is SpouseRelation spouseRelation)
                    {
                    }
                    else if (relation is SiblingRelation siblingRelation)
                    {
                        DateTime date1 = fromPerson.Events.FirstOrDefault()?.Date ?? DateTime.MinValue;
                        DateTime date2 = toPerson.Events.FirstOrDefault()?.Date ?? DateTime.MinValue;
                        DateTime minDate = date1 > date2 ? date1 : date2;
                        minDate = minDate.AddTicks(1);

                        string description = GenerateDescription(null, EventType.Sibling, minDate, GitDateType.After, fromPerson, toPerson);
                        var ev = new GitPersonEvent(null, EventType.Sibling, minDate, description, GitDateType.After)
                        {
                            FirstName = GenerateName(fromPerson, toPerson),
                            Parents = new List<GitPerson>()
                            {
                                toPerson,
                                fromPerson
                            }
                        };
                        notPersonEvents.Add(ev);
                    }
                }
            }
        }

        private static void InsertChild(GedcomPerson gedcomParent, GitPerson gitParent, GitPerson gitChild, GitPersonEvent gitChildBirthEvent)
        {
            if (gitParent.Events.Count == 0)
            {
                gitParent.Events.Add(CreateBirthEvent(gedcomParent, gitParent, null, DateTime.MinValue, GitDateType.After));
            }

            bool childAdded = false;

            for (int i = gitParent.Events.Count - 1; i >= 0; i--)
            {
                if (gitParent.Events[i].Date < gitChildBirthEvent.Date)
                {
                    gitParent.Events[i].Children.Add(gitChild);
                    childAdded = true;
                    break;
                }
            }

            if (!childAdded)
            {
                gitParent.Events[0].Parents.Add(gitChild);
            }
        }

        private static void FixChildrenDates(GitPersonEvent ev)
        {
            foreach (GitPerson child in ev.Children)
            {
                DateTime curDate = ev.Date;

                foreach (GitPersonEvent childEvent in child.Events)
                {
                    if (childEvent.DateType == GitDateType.Exact)
                    {
                        break;
                    }
                    else if (childEvent.Date <= curDate)
                    {
                        curDate = curDate.AddTicks(1);
                        childEvent.Date = curDate;
                        childEvent.DateType = GitDateType.After;
                        FixChildrenDates(childEvent);
                    }
                }
            }
        }

        private static GitPerson Convert(GedcomPerson gedcomPerson)
        {
            var result = new GitPerson(gedcomPerson.Id)
            {
                FirstName = gedcomPerson.FirstName,
                LastName = gedcomPerson.LastName,
            };

            DateTime curDate = DateTime.MinValue;

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
                    string description = GenerateDescription(ev, ev.Type, curDate, dateType, result);
                    gitPersonEvent = new GitPersonEvent(result, ev.Type, curDate, description, dateType);
                }

                events.Add(gitPersonEvent);
            }

            if (!events.Exists(ev => ev.Type == EventType.Birth))
            {
                GitPersonEvent birthEvent = CreateBirthEvent(gedcomPerson, result, null, DateTime.MinValue, GitDateType.After);
                events.Insert(0, birthEvent);
            }

            result.Events = events.OrderBy(ev => ev.Date).ToList();

            return result;
        }

        private static GitPersonEvent CreateBirthEvent(GedcomPerson gedcomPerson, GitPerson gitPerson, GedcomEvent ev, DateTime date, GitDateType dateType)
        {
            string description =
                GenerateDescription(ev, EventType.Birth, date, dateType, gitPerson) +
                " " + Utils.JoinNotEmpty(gedcomPerson.Gender, gedcomPerson.Education,
                gedcomPerson.Religion, gedcomPerson.Note, gedcomPerson.Changed,
                gedcomPerson.Occupation, gedcomPerson.Health, gedcomPerson.Title);

            return new GitPersonEvent(gitPerson, EventType.Birth, date, description, dateType);
        }

        private static string GenerateDescription(GedcomEvent ev, EventType eventType, DateTime date, GitDateType dateType, params GitPerson[] gitPersons)
        {
            string dateStr = dateType == GitDateType.Exact ? "at " + date.ToShortDateString() : "";
            string personsString = GenerateName(gitPersons);
            return Utils.JoinNotEmpty(personsString, ":", ev?.Type.ToString() ?? eventType.ToString(), dateStr, ev?.Place, ev?.Latitude, ev?.Longitude, ev?.Note);
        }

        private static string GenerateName(params GitPerson[] gitPersons)
        {
            var personsString = new StringBuilder();

            foreach (GitPerson gitPerson in gitPersons)
            {
                personsString.Append(Utils.JoinNotEmpty(gitPerson.FirstName, gitPerson.LastName, "& "));
            }
            if (gitPersons.Length > 0)
            {
                personsString.Remove(personsString.Length - 2, 2);
            }

            return personsString.ToString().Trim();
        }

        private static bool IsParentBeforeTheChildBirthEvent(EventType type)
        {
            return type == EventType.Birth || type == EventType.Baptized;
        }
    }
}
