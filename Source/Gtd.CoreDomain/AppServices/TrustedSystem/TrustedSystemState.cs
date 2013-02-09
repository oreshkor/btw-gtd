using System;
using System.Collections.Generic;

namespace Gtd.CoreDomain.AppServices.TrustedSystem
{
    public sealed class TrustedSystemState : ITrustedSystemState
    {
        public static TrustedSystemState BuildStateFromEventHistory(IEnumerable<Event> events)
        {
            var aggState = new TrustedSystemState();

            foreach (var eventThatHappened in events)
            {
                aggState.MakeStateRealize((ITrustedSystemEvent) eventThatHappened);
            }
            return aggState;
        }

        public TrustedSystemState()
        {
            Inbox = new HashSet<Guid>();
        }

        public void MakeStateRealize(ITrustedSystemEvent thisEventTypeHappened)
        {
            #region What Is This Code/Syntax Doing?
            // Announce that a specific Type of Event Message occured
            // so that this AggregateState instance can Realize it and change its state because of it.
            // We are telling the compiler to call one of the "When" methods in this class to achieve this realization.
            // The "dynamic" syntax below is just a shortcut we are using so we don't
            // have to create a large if/else block for a bunch of type-specific static Event types.
            // This shortcut using the "dynamic" keyword syntax means:
            // "Call the 'When' method of 'this' AggregateState instance
            // that has a method signature of:
            // When(WhateverTheEventTypeIsOf-thisEventTypeHappened)".
            #endregion

            ((dynamic)this).When((dynamic)thisEventTypeHappened);
        }

        public TrustedSystemId Id { get; private set; }

        public HashSet<Guid> Inbox { get; private set; }

        // When Methods

        public void When(TrustedSystemCreated evnt)
        {
            Id = evnt.Id;
        }

        public void When(ThoughtCaptured evnt)
        {
            Inbox.Add(evnt.ThoughtId);
        }

        public void When(ThoughtArchived evnt)
        {
            Inbox.Remove(evnt.ThoughtId);
        }

        public void When(ActionDefined evnt)
        {
            Actions.Add(evnt.ActionId, new ActionInfo(evnt.ActionId, evnt.Outcome));
        }

        public void When(ProjectDefined evnt)
        {
            Projects.Add(evnt.ProjectId, new ProjectInfo(evnt.ProjectId, evnt.ProjectOutcome));
        }

        public void When(ActionAssignedToProject evnt)
        {
            var action = Actions[evnt.ActionId];
            var project = Projects[evnt.NewProject];

            action.LinkToProject(project.Id);
            project.AddAction(action.Id);
        }

        public void When(ActionRemovedFromProject evnt)
        {
            Actions[evnt.ActionId].RemoveFromProject(evnt.OldProject);
            Projects[evnt.OldProject].RemoveAction(evnt.ActionId);
        }

        public void When(ActionMovedToProject evnt)
        {
            var action = Actions[evnt.ActionId];
            var oldProject = Projects[evnt.OldProject];
            var newProject = Projects[evnt.NewProject];

            action.MoveToProject(evnt.OldProject, evnt.NewProject);
            
            newProject.AddAction(action.Id);
            oldProject.RemoveAction(action.Id);
        }

        public void When(ActionRemoved evnt)
        {
            Actions[evnt.ActionId].EnsureCleanRemoval();
            Actions.Remove(evnt.ActionId);
        }

        public void When(ActionRenamed evnt)
        {
            Actions[evnt.ActionId].Rename(evnt.Name);
        }

        public void When(ActionCompleted evnt)
        {
            Actions[evnt.ActionId].MarkAsCompleted();
        }


        public readonly IDictionary<ActionId, ActionInfo> Actions = new Dictionary<ActionId, ActionInfo>(); 
        public readonly IDictionary<ProjectId, ProjectInfo> Projects = new Dictionary<ProjectId, ProjectInfo>(); 
    }

    /// <summary>
    /// Action entity for the aggregate state. It maintains only invariants within itself.
    /// Invariants between entities are maintained by the state
    /// </summary>
    public sealed class ActionInfo // VO
    {
        public ActionId Id { get; private set; }
        public string Outcome { get; private set; }

        public ProjectId Project { get; private set; }

        public bool HasProject { get { return !Project.IsEmpty; } }

        public bool Completed { get; private set; }

        public ActionInfo(ActionId id, string name)
        {
            Id = id;
            Outcome = name;
        }

        public void LinkToProject(ProjectId id)
        {
            if (HasProject)
                throw new InvalidOperationException("Action already assigned to a project");
            Project = id;

        }

        public void MarkAsCompleted()
        {
            if (Completed == true)
                throw new InvalidOperationException("Action is already completed");
            Completed = true;
        }

        public void MoveToProject(ProjectId oldProject, ProjectId newProject)
        {
            if (oldProject != Project)
                throw new InvalidOperationException("OldProject != CurrentProject");
            if (newProject == ProjectId.Empty)
                throw new InvalidOperationException("NewProject == Empty");
            Project = newProject;
        }
        public void RemoveFromProject(ProjectId project)
        {
            if (project != Project)
                throw new InvalidOperationException("OldProject != CurrentProject");
            Project = ProjectId.Empty;
        }

        public void Rename(string newName)
        {
            Outcome = newName;
        }
        public void EnsureCleanRemoval()
        {
            if (HasProject)
                throw new InvalidOperationException("Can't remove action that is still assigned to project");
        }
    }


    public sealed class ProjectInfo
    {
        public ProjectId Id { get; private set; }
        public string Name { get; private set; }

        readonly List<ActionId> _actions = new List<ActionId>();
         

        public ProjectInfo(ProjectId id, string name)
        {
            Id = id;
            Name = name;
        }

        public void Rename(string newName)
        {
            Name = newName;
        }

        public void AddAction(ActionId action)
        {
            _actions.Add(action);
        }
        public void RemoveAction(ActionId action)
        {
            _actions.Remove(action);
        }
    }
}