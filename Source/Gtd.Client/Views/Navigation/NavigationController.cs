﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gtd.Client.Models;
using Gtd.Client.Views.Navigation;
using System.Linq;

namespace Gtd.Client
{
    public sealed class NavigationController : 
        IHandle<AppInit>, 
        IHandle<Dumb.ClientModelLoaded>,
    IHandle<Dumb.ThoughtAdded>, 
        IHandle<ThoughtArchived>,
        IHandle<Dumb.ProjectAdded>, IHandle<ActionDefined>, IHandle<UI.FilterChanged>
    {
        readonly NavigationView _tree;
        readonly Region _region;
        readonly IPublisher _queue;
        readonly ClientPerspective _view;
        

        bool _loaded;

        NavigationController(Region region, IPublisher queue, ClientPerspective view)
        {
            _tree = new NavigationView(this);
            _region = region;
            _queue = queue;
            _view = view;
        }

        public static NavigationController Wire(Region control, IPublisher queue, ISubscriber bus, ClientPerspective view)
        {
            var adapter  = new NavigationController(control, queue, view);

            bus.Subscribe<AppInit>(adapter);
            bus.Subscribe<Dumb.ThoughtAdded>(adapter);
            bus.Subscribe<ThoughtArchived>(adapter);
            bus.Subscribe<Dumb.ProjectAdded>(adapter);
            bus.Subscribe<ActionDefined>(adapter);
            bus.Subscribe<Dumb.ClientModelLoaded>(adapter);
            bus.Subscribe<UI.FilterChanged>(adapter);


            return adapter ;
        }

        public void Handle(AppInit message)
        {
            _region.RegisterDock(_tree, "nav-tree");
            _region.SwitchTo("nav-tree");

            //_tree.Sync(() => _tree.AddNode("inbox","Inbox"));
        }

        

        void ReloadInboxNode()
        {
            _tree.UpdateNode("inbox",string.Format("Inbox ({0})", _view.ListInbox().Count));
        }

        

        public void Handle(Dumb.ThoughtAdded message)
        {
            if (!_loaded)
                return;

            Sync(ReloadInboxNode);
        }

        public void Handle(ThoughtArchived message)
        {
            if (!_loaded)
                return;
            Sync(ReloadInboxNode);
        }

        void Sync(Action act)
        {
            if (_tree.InvokeRequired)
            {
                _tree.Invoke(act);
                return;
            }
            act();
        }


        readonly IDictionary<ProjectId, string> _projectNodes = new Dictionary<ProjectId, string>(); 

        

        public void Handle(Dumb.ProjectAdded message)
        {
            if (!_loaded)
                return;
            Sync(() => AddProjectNode(
                message.ProjectId, 
                message.ProjectOutcome,
                message.UniqueKey));
        }

        void AddProjectNode(ProjectId projectId, string outcome, string uniqueKey)
        {
            
            _tree.AddNode(uniqueKey, outcome);
            _projectNodes[projectId] = uniqueKey;
            _nodes[uniqueKey] = projectId;
        }

        IDictionary<string,object> _nodes = new Dictionary<string, object>(); 

        public void Handle(ActionDefined message)
        {
            //Sync(() => 
            //    _v
            //    _projectNodes[message.ProjectId].Text);
        }

        public void Handle(Dumb.ClientModelLoaded message)
        {
            _loaded = true;

            Sync(LoadNavigation);

        }

        void LoadNavigation()
        {
            _tree.Clear();
            _tree.AddNode("inbox", string.Format("Inbox ({0})", _view.ListInbox().Count));
            foreach (var project in _view.ListProjects())
            {
                var actions = _view.CurrentFilter.FilterActions(project);
                var count = _view.CurrentFilter.FormatActionCount(actions.Count());
                AddProjectNode(project.ProjectId, string.Format("{0} ({1})", project.Outcome, count), project.UniqueKey);
            }
        }

        public void WhenNodeSelected(string tag)
        {
            if (tag == "inbox")
            {
                _queue.Publish(new UI.DisplayInbox());
                return;
            }
            var node = _nodes[tag];

            if (node is ProjectId)
            {
                _queue.Publish(new UI.DisplayProject((ProjectId) node));
            }
        }

        public void Handle(UI.FilterChanged message)
        {
            if (!_loaded) return;
            Sync(LoadNavigation);
        }
    }



    public interface IFormCommand
    {
        
    }
}