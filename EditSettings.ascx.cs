#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Common;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Framework;
using Satrabel.OpenContent.Components;
using Satrabel.OpenForm.Components;
using Satrabel.OpenContent.Components.Alpaca;
using System.IO;


#endregion

namespace Satrabel.OpenForm
{
    public partial class EditSettings : PortalModuleBase
    {
        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            hlCancel.NavigateUrl = Globals.NavigateURL();
            //ServicesFramework.Instance.RequestAjaxScriptSupport();
            //ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
            AlpacaEngine alpaca = new AlpacaEngine(Page, ModuleContext, "DesktopModules/OpenForm/", "settings");
            alpaca.RegisterAll();
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                hlTemplateExchange.NavigateUrl = EditUrl("ShareTemplate");
                var scriptFileSetting = Settings["template"] as string;
                scriptList.Items.AddRange(OpenFormUtils.GetTemplatesFiles(PortalSettings, ModuleId, scriptFileSetting).ToArray());
            }
        }

        protected void cmdSave_Click(object sender, EventArgs e)
        {
            ModuleController mc = new ModuleController();
            mc.UpdateModuleSetting(ModuleId, "template", scriptList.SelectedValue);
            mc.UpdateModuleSetting(ModuleId, "data", HiddenField.Value);
            Response.Redirect(Globals.NavigateURL(), true);
            //DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, "Update Successful", DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
        }


        protected void cmdCancel_Click(object sender, EventArgs e)
        {
        }

        #endregion
    }
}

