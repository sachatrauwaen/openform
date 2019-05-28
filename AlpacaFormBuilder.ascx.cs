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
using Satrabel.OpenContent.Components.Alpaca;

using DotNetNuke.Web.Client.ClientResourceManagement;
using DotNetNuke.Web.Client;
using Satrabel.OpenContent.Components.Settings;

#endregion

namespace Satrabel.OpenForm
{
    public partial class AlpacaFormBuilder : PortalModuleBase
    {
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            hlCancel.NavigateUrl = Globals.NavigateURL();
            cmdSave.NavigateUrl = Globals.NavigateURL();
            var ocGlobalSettings = new DnnGlobalSettingsRepository(ModuleContext.PortalId);
            bool bootstrap = ocGlobalSettings.GetEditLayout() != AlpacaLayoutEnum.DNN;
            bool loadBootstrap = bootstrap && ocGlobalSettings.GetLoadBootstrap();
            bool loadGlyphicons = bootstrap && ocGlobalSettings.GetLoadGlyphicons();

            AlpacaEngine alpaca = new AlpacaEngine(Page, ModuleContext.PortalId, "" /*settings.Template.Uri().FolderPath*/, "builder");
            alpaca.RegisterAll(true, loadBootstrap, loadGlyphicons);
            ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenForm/js/builder/formbuilder.js", FileOrder.Js.DefaultPriority);
            ClientResourceManager.RegisterStyleSheet(Page, "~/DesktopModules/OpenForm/js/builder/formbuilder.css", FileOrder.Css.DefaultPriority);
            ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/js/bootstrap/js/bootstrap.min.js", FileOrder.Js.DefaultPriority);
            ClientResourceManager.RegisterStyleSheet(Page, "~/DesktopModules/OpenContent/js/bootstrap/css/bootstrap.min.css", FileOrder.Css.DefaultPriority);
        }
    }
}

