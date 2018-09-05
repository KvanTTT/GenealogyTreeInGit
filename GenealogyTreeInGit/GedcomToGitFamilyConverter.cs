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

            foreach (KeyValuePair<string, GedcomPerson> gedcomPerson in parseResult.Persons)
            {
                GitPerson gitPerson = Convert(gedcomPerson.Value);
                persons.Add(gitPerson.Id, gitPerson);
            }

            FillParentsAndChildren(parseResult, persons);

            // TODO: also fill not person events
            var family = new Family(parseResult.Title, persons, new List<GitPersonEvent>());
            return family;
        }

        private static void FillParentsAndChildren(GedcomParseResult parseResult, Dictionary<string, GitPerson> persons)
        {
            foreach (GedcomRelation relation in parseResult.Relations)
            {
                if (relation is ChildRelation childRelation)
                {
                    if (persons.TryGetValue(childRelation.ToId, out GitPerson parent) &&
                        persons.TryGetValue(childRelation.FromId, out GitPerson child))
                    {
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
                    string description = GenerateDescription(result, ev, curDate, dateType);
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
                GenerateDescription(gitPerson, ev, date, dateType) +
                " " + Utils.JoinNotEmpty(gedcomPerson.Gender, gedcomPerson.Education,
                gedcomPerson.Religion, gedcomPerson.Note, gedcomPerson.Changed,
                gedcomPerson.Occupation, gedcomPerson.Health, gedcomPerson.Title);

            return new GitPersonEvent(gitPerson, EventType.Birth, date, description, dateType);
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
