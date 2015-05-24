#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Framework.JavaScriptLibraries;
using DotNetNuke.Common;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Security;

#endregion

namespace Satrabel.OpenForm
{

	public partial class View : PortalModuleBase, IActionable
	{

		#region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            
            base.OnInit(e);
            //hlCancel.NavigateUrl = Globals.NavigateURL();
            cmdSave.NavigateUrl = Globals.NavigateURL("","result=1");

            //cmdSave.Click += cmdSave_Click;
            //cmdCancel.Click += cmdCancel_Click;

            ServicesFramework.Instance.RequestAjaxScriptSupport();
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
            JavaScript.RequestRegistration(CommonJs.DnnPlugins); ;
            JavaScript.RequestRegistration(CommonJs.jQueryFileUpload);

        }
		protected override void OnLoad(EventArgs e)
		{
			base.OnLoad(e);
			
			if (!Page.IsPostBack)
			{
			}
		}
		
		protected void cmdSave_Click(object sender, EventArgs e)
		{
            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, "Update Successful", DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
		}


		protected void cmdCancel_Click(object sender, EventArgs e)
		{
		}

		#endregion
        public DotNetNuke.Entities.Modules.Actions.ModuleActionCollection ModuleActions
        {
            get
            {
                var Actions = new ModuleActionCollection();
                Actions.Add(ModuleContext.GetNextActionID(),
                            Localization.GetString(ModuleActionType.AddContent, LocalResourceFile),
                            ModuleActionType.AddContent,
                            "",
                            "",
                            ModuleContext.EditUrl(),
                            false,
                            SecurityAccessLevel.Edit,
                            true,
                            false);

                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("EditTemplate.Action", LocalResourceFile),
                           ModuleActionType.ContentOptions,
                           "",
                           "~/DesktopModules/OpenContent/images/edittemplate.png",
                           ModuleContext.EditUrl("EditTemplate"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);
                /*
                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("EditData.Action", LocalResourceFile),
                           ModuleActionType.EditContent,
                           "",
                           "",
                           ModuleContext.EditUrl("EditData"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);
                */
                Actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("ShareTemplate.Action", LocalResourceFile),
                           ModuleActionType.ContentOptions,
                           "",
                           "~/DesktopModules/OpenContent/images/exchange.png",
                           ModuleContext.EditUrl("ShareTemplate"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);

                return Actions;
            }
        }
        public string CurrentCulture { get {
            string lang = LocaleController.Instance.GetCurrentLocale(PortalId).Code.Substring(0, 2);
            return lang + "_" + lang.ToUpper();
        } }
	}
}

