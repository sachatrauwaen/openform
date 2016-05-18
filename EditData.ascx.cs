#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using System.Linq;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Common;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Framework;
using System.Web.UI.WebControls;
using DotNetNuke.Services.Localization;
using System.IO;
using Satrabel.OpenContent.Components;
using Newtonsoft.Json.Linq;
using System.Globalization;
using DotNetNuke.Common.Utilities;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenForm.Components;

#endregion

namespace Satrabel.OpenForm
{
    public partial class EditData : PortalModuleBase
    {
        private const string cData = "Data";
        private const string cSettings = "Settings";

        #region Event Handlers
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            cmdSave.Click += cmdSave_Click;
            cmdCancel.Click += cmdCancel_Click;
            sourceList.SelectedIndexChanged += sourceList_SelectedIndexChanged;
        }
      
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            if (!Page.IsPostBack)
            {
                InitEditor();
            }
        }
        private void InitEditor()
        {
            LoadFiles();
            DisplayFile(cData);
        }

        private void DisplayFile(string selectedDataType)
        {
            string json = string.Empty;
            switch (selectedDataType)
            {
                case cData:
                    {
                        OpenFormController ctrl = new OpenFormController();
                        var data = ctrl.GetContents(ModuleId).OrderByDescending(c => c.CreatedOnDate);
                        var arr = new JArray();
                        foreach (var item in data)
                        {
                            var o = JObject.Parse(item.Json);
                            o["CreatedOnDate"] = item.CreatedOnDate;
                            arr.Add(o);                            
                        }
                        json = arr.ToString();
                    }
                    break;
                case cSettings:
                    json = ModuleContext.Settings["data"] as string;
                    break;
            }

            txtSource.Text = json;
            cmdSave.Visible = selectedDataType == cSettings;
            cmdCancel.Visible = selectedDataType == cSettings;
        }

        private void LoadFiles()
        {
            
            sourceList.Items.Clear();
            sourceList.Items.Add(new ListItem(cData, cData));
            sourceList.Items.Add(new ListItem(cSettings, cSettings));
        }

        protected void cmdSave_Click(object sender, EventArgs e)
        {
            if (sourceList.SelectedValue == cData)
            {
                SaveData();
            }
            else if (sourceList.SelectedValue == cSettings)
            {
                SaveSettings();
            }
           
            Response.Redirect(Globals.NavigateURL(), true);
        }
        private void SaveData()
        {
            
        }
        private void SaveSettings()
        {
            ModuleController mc = new ModuleController();
            if (string.IsNullOrEmpty(txtSource.Text))
                mc.DeleteModuleSetting(ModuleId, "data");
            else
                mc.UpdateModuleSetting(ModuleId, "data", txtSource.Text);
        }
        protected void cmdCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect(Globals.NavigateURL(), true);
        }
        private void sourceList_SelectedIndexChanged(object sender, EventArgs e)
        {
            DisplayFile(sourceList.SelectedValue);
        }
        #endregion
    }
}

