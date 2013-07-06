﻿using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Gtd.Shell.Filters;

namespace Gtd.Client
{
    public partial class MainForm : Form, ILogControl
    {
        MainFormController _controller;
        public readonly Region MainRegion;
        public readonly Region NavigationRegion;

        public MainForm()
        {
            InitializeComponent();

            MainRegion = new Region(splitContainer1.Panel2);
            NavigationRegion = new Region(splitContainer1.Panel1);
        }



        public void SetAdapter(MainFormController controller)
        {
            _controller = controller;

            Load += (sender, args) => _controller.Publish(new FormLoading());

            _menuCaptureThought.Click += (sender, args) => _controller.Publish(new Ui.CaptureThoughtClicked());
            _menuDefineProject.Click += (sender, args) => _controller.Publish(new Ui.DefineProjectClicked());
            _menuGoToInbox.Click += (sender, args) => _controller.Publish(new Ui.DisplayInbox());

        }

        public void ShowInboxMenu()
        {
            this.Sync(() =>
                {
                    _menuGoToInbox.Visible = false;
                    _menuCaptureThought.Visible = true;
                    _menuDefineProject.Visible = true;
                });
        }

        public void DisplayFilters(ICollection<IFilterCriteria> filters)
        {
            if (filters.Count == 0)
                throw new ArgumentException("Filters can't be empty","filters");
            this.Sync(() =>
                {
                    foreach (var filterCriteria in filters)
                    {
                        _filter.Items.Add("(WIP): " + filterCriteria.Title);
                    }
                    _filter.SelectedIndex = 0;

                });
            
        }


        public void ShowProjectMenu(ProjectId id)
        {
            this.Sync(() =>
                {
                    _menuCaptureThought.Visible = true;
                    _menuGoToInbox.Visible = true;
                    _menuDefineProject.Visible = true;
                });
        }

        public void Log(string toString)
        {
            _log.Sync(() =>
                {
                    var format = string.Format("{0:HH:mm:ss} {1}{2}", DateTime.UtcNow, toString, Environment.NewLine);
                    _log.AppendText(format);
                    _log.ScrollToCaret();
                });
        }
    }


    public static class ExtendControl
    {
        public static void Sync(this Control self, Action act)
        {
            if (self.InvokeRequired)
            {
                self.Invoke(act);
            }
            else
            {
                act();
            }
        }
    }

   
   








    public enum AppState
    {
        Loading,
        
    }



    




    


}
