#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Services.Exceptions;
using System.IO;
using System.Web.UI.WebControls;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using Satrabel.OpenForm.Components;

#endregion

namespace Satrabel.OpenForm
{

    public partial class Settings : ModuleSettingsBase
    {
        public string razorScriptFolder
        {
            get
            {
                return ModuleContext.PortalSettings.HomeDirectory + "/OpenForm/Templates/";
            }
        }
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ServicesFramework.Instance.RequestAjaxScriptSupport();
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
            JavaScript.RequestRegistration(CommonJs.DnnPlugins); ;
            JavaScript.RequestRegistration(CommonJs.jQueryFileUpload);
        }

        public override void LoadSettings()
        {
            var scriptFileSetting = Settings["template"] as string;

            scriptList.Items.AddRange(OpenFormUtils.GetTemplatesFiles(PortalSettings, ModuleId, scriptFileSetting).ToArray());
            /*
            string basePath = Server.MapPath(razorScriptFolder);
            
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                foreach (string script in Directory.GetFiles(dir, "*schema.json"))
                {
                    string scriptPath = script.Replace(basePath, "");
                    var item = new ListItem("System : "+scriptPath.Replace("\\"," - "), scriptPath);
                    if (!(string.IsNullOrEmpty(scriptFileSetting)) && scriptPath.ToLowerInvariant() == scriptFileSetting.ToLowerInvariant())
                    {
                        item.Selected = true;
                    }
                    scriptList.Items.Add(item);
                }
            }
             */
            base.LoadSettings();
        }

        public override void UpdateSettings()
        {
            ModuleController mc = new ModuleController();
            mc.UpdateModuleSetting(ModuleId, "template", scriptList.SelectedValue);
            mc.UpdateModuleSetting(ModuleId, "data", hfData.Value);
        }
    }
}