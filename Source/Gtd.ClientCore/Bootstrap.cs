﻿using System.IO;
using Btw.Portable;
using Gtd.Client.Controllers;
using Gtd.Client.Models;
using Gtd.Client.Views.AddStuffToInbox;
using Gtd.Client.Views.CaptureThought;
using Gtd.Client.Views.Navigation;
using Gtd.Client.Views.Project;

namespace Gtd.Client
{
    public static class Bootstrap
    {
        public static void WireControlLogic(UserInterface ui)
        {
            #region This WinForms host uses its own in-memory message bus to manage the UI...

            // It uses this in-memory bus to wire user-interface "UI" elements ("controls") to the
            // logic (in controllers) that will be triggered when the user interacts with those elements.
            // This allows us to send messages to the uiBus so that interested UI classes can 
            // subscribe to the messages they care about, and react to them
            // when the bus publishes the messages to tell the UI controls what happened.

            #endregion

            var uiBus = new InMemoryBus("UI");

            // .dat file to write Domain Events to this session
            var fileToStoreEventsIn =
                new FileAppendOnlyStore(new DirectoryInfo(Directory.GetCurrentDirectory()));
            fileToStoreEventsIn.Initialize();

            // provide serialization stuff for our file storage
            var messageStore = new MessageStore(fileToStoreEventsIn);
            messageStore.LoadDataContractsFromAssemblyOf(typeof(ActionDefined));

            // this WinForm App's own local file-based event storage
            var appEventStore = new AppEventStore(messageStore);

            #region Does App care about this msg? This class controls the App's "main message loop"...

            // It reads all msgs from in-memory queue (mainQueue below) and determines which messages 
            // the App will/will not handle at a given time (based on specific app state).
            // For each queued message that it determines should be handled by
            // uiBus subscribers, it passes the messages through to our in-memory uiBus,
            // so bus subscribers can be called to react to the current message when the bus publishes it.

            #endregion

            var appController = new AppController(uiBus, appEventStore);

            #region In-memory structure that all events we defined will go through...

            // All domain & system messages we defined are captured 
            // and accumulated in this queue until some code picks up
            // each message and processes it.
            // (ex: AppController, declared above, will do that processing in this case).

            #endregion

            var mainQueue = new QueuedHandler(appController, "Main Queue");

            appController.SetMainQueue(mainQueue);
            appEventStore.SetDispatcher(mainQueue);

            var provider = new ClientPerspective();

            ClientModelController.WireTo(appEventStore, provider, uiBus, mainQueue);

            // create services and bind them to the bus

            // we wire all controls together in a native way.
            // then we add adapters on top of that


            LogController.Wire(ui.Log, uiBus);

            #region View Controllers - Decoupling (UI) Views from their Controllers...

            // The intent with this design was to enable us to
            // write components or UI elements in a separated manner.
            // Provide the ability to develop new functionality independently and
            // it will sit in its own kind of "sandbox" so you can work on your stuff
            // without impacting everyone else.
            // It also sets us up for potential controller/code reuse and sharing in the future.

            // The UI is broken down into kinda "SEDA standalone controllers"
            // that communicate with each other via events.  This event-driven
            // separation allows for cleanly implementing logic like:
            // "If the Inbox is selected, then only display these two menu items,
            // but if a project is displayed, then display these additional menu items."
            // See the Handle methods inside of MainFormController.cs for an example.

            // Event-centric approaches are one of the nicest ways to build
            // plug-in systems because plug-ins have services and contracts which are linked
            // to behaviors (and in our case, these are events).
            // Example:  All CaptureThoughtController knows is that it gets handed a View
            // that it controls (CaptureThoughtForm) and then it has two other injections points,
            // a Bus and a MainQueue.
            // See CaptureThoughtController for more comments on how this design works.

            // The big idea here is that in the future, a controller can be passed an
            // INTERFACE (say, ICaptureThoughtForm) INSTEAD OF a concrete Windows Forms
            // implementation (CaptureThoughtForm) like it currently uses.
            // So we may have a WPF form in the future that implements ICaptureThoughtForm
            // and we could use that View implementation with the SAME CONTROLLER we already have.
            // Very similar to how you could use the MVVM pattern with MvvmCross to
            // reuse common code from the ViewModel down, but implement
            // platform-specific Views on top of that common/shared code.

            #endregion

            #region Wiring up our Views to Controllers... 

            // A "Controller" or "Service" in this client-side ecosystem would usually
            // define at least two parameters:
            // MainQueue
            // and
            // "Bus"
            // MainQueue is the place where it would send events that happen inside of it.
            // Bus is what it subscribes to so that it will be called when specifc events happen.

            // "Wire" is a static method defined on these controllers that our setup
            // can call to let them know which form they control,
            // the bus they can use as a source to subscribe to UI events to react to,
            // and the target queue that they can use tell the rest of the world about events they generate.

            #endregion

            MainMenuController.Wire(ui.Menu, mainQueue, uiBus);

            AddStuffToInboxController.Wire(ui.AddStuffToInboxWizard, uiBus, mainQueue);

            AddActionToProjectController.Wire(ui.AddActionToProjectWizard, uiBus, mainQueue);

            DefineProjectController.Wire(ui.DefineProjectWizard, uiBus, mainQueue);
            InboxController.Wire(ui.InboxView, mainQueue, uiBus, provider);
            NavigationController.Wire(ui.Navigation, mainQueue, uiBus, provider);
            ProjectController.Wire(ui.ProjectView, mainQueue, uiBus, provider);

            NavigateBackController.Wire(uiBus, mainQueue, ui.BackView);

            mainQueue.Enqueue(new AppInit());
            mainQueue.Start();
        }
    }
}