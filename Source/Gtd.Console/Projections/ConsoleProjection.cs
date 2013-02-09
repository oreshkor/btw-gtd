﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Gtd.Shell.Projections
{
    public sealed class ConsoleView
    {
        public IDictionary<TrustedSystemId, TrustedSystem> Systems = new Dictionary<TrustedSystemId, TrustedSystem>();
    }

    public sealed class Thought
    {
        public Guid ItemId;
        public string Subject;
        public DateTime Added;
    }

    public sealed class Project
    {
        public ProjectId ProjectId;
        public string Outcome;
    }

    public sealed class TrustedSystem
    {
        public List<Thought> Thoughts = new List<Thought>(); 
        public List<Project> Projects = new List<Project>(); 

        public Project GetProjectById(string match)
        {
            var matches =
                Projects
                .Where(p => p.ProjectId.Id.ToString().ToLowerInvariant().Replace("-", "").StartsWith(match, StringComparison.InvariantCultureIgnoreCase))
                        .ToArray();
            if (matches.Length == 0)
            {
                var message = string.Format("No projects match criteria '{0}'", match);
                throw new InvalidOperationException(message);
            }
            if (matches.Length > 1)
            {
                var message = string.Format("Multiple projects match criteria '{0}'", match);
                throw new InvalidOperationException(message);
            }
            return matches[0];
        } 

        public void CaptureThought(Guid thoughtId, string thought, DateTime date)
        {
            Thoughts.Add(new Thought()
                {
                    Added = date,
                    ItemId = thoughtId,
                    Subject = thought
                });
        }
        public void ArchiveThought(Guid thoughtId)
        {
            Thoughts.RemoveAll(t => t.ItemId == thoughtId);
        }

       
        public void DefineProject(ProjectId projectId, string projectOutcome)
        {
            Projects.Add(new Project
                {
                    ProjectId = projectId,
                    Outcome = projectOutcome
                });
        }
    }

    public sealed class ConsoleProjection
    {
        public ConsoleView ViewInstance = new ConsoleView();

        void Update(TrustedSystemId id, Action<TrustedSystem> update)
        {
            update(ViewInstance.Systems[id]);
        }

        public void When(TrustedSystemCreated evnt)
        {
            ViewInstance.Systems.Add(evnt.Id, new TrustedSystem());
        }

        public void When(ThoughtCaptured evnt)
        {
            Update(evnt.Id, s => s.CaptureThought(evnt.ThoughtId, evnt.Thought, evnt.TimeUtc));
        }
        public void When(ThoughtArchived evnt)
        {
            Update(evnt.Id, s => s.ArchiveThought(evnt.ThoughtId));
        }

        public void When(ProjectDefined evnt)
        {
            Update(evnt.Id, s => s.DefineProject(evnt.ProjectId, evnt.ProjectOutcome));
        }
    }
}