﻿using Gtd.Client.Models;

namespace Gtd.Client.Views.Actions
{
    public sealed class ProjectController : IHandle<UI.DisplayProject>
    {
        readonly ProjectView _control;
        readonly ClientPerspective _view;
        readonly Region _mainRegion;
        readonly IPublisher _bus;

        ProjectController(ProjectView control, ClientPerspective view, Region mainRegion, IPublisher bus)
        {
            _control = control;
            _view = view;
            _mainRegion = mainRegion;
            _bus = bus;
        }

        public static void Wire(Region mainRegion, IPublisher queuedHandler, ISubscriber bus, ClientPerspective view)
        {
            // passed from external wire as interface implementor
            var control = new ProjectView();
            

            var adapter = new ProjectController(control, view, mainRegion, queuedHandler);


            control.AttachTo(adapter);

            mainRegion.RegisterDock(control, "project");

            bus.Subscribe(adapter);
        }

        public void Handle(UI.DisplayProject message)
        {
            var project = _view.GetProjectOrNull(message.Id);
            
            _control.Sync(() => _control.DisplayProject(project));
            _mainRegion.SwitchTo("project");
            _bus.Publish(new UI.ProjectDisplayed(message.Id));
        }



        public sealed class ProjectDisplayModel
        {
            
        }

        public void RequestActionCheck(ActionId id)
        {
            _bus.Publish(new UI.CompleteActionClicked(id));
        }
    }
}