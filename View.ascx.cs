#region Copyright

// 
// Copyright (c) 2015 by Satrabel
// 

#endregion

#region Using Statements

using System;
using DotNetNuke.Entities.Modules;
using DotNetNuke.Framework;
using DotNetNuke.Common;
using DotNetNuke.Services.Localization;
using DotNetNuke.Entities.Modules.Actions;
using DotNetNuke.Security;
using Satrabel.OpenForm.Components;
using System.IO;
using DotNetNuke.Web.Client;
using DotNetNuke.Web.Client.ClientResourceManagement;
using Newtonsoft.Json;
using Satrabel.OpenContent.Components.Handlebars;
using System.Web.UI;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Alpaca;
using Localization = DotNetNuke.Services.Localization.Localization;

#endregion

namespace Satrabel.OpenForm
{

    public partial class View : PortalModuleBase, IActionable
    {

        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {

            base.OnInit(e);
            //cmdSave.NavigateUrl = Globals.NavigateURL("", "result=1");
            ServicesFramework.Instance.RequestAjaxScriptSupport();
            ServicesFramework.Instance.RequestAjaxAntiForgerySupport();
            //JavaScript.RequestRegistration(CommonJs.DnnPlugins); ;
            //JavaScript.RequestRegistration(CommonJs.jQueryFileUpload);

        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            string Template = ModuleContext.Settings["template"] as string;
            if (!Page.IsPostBack)
            {
                InitForm(Template);
            }
            else
            {
                if (Request.QueryString["result"] == "submit")
                {
                    /*
                    int id = int.Parse(Request.QueryString["result"]);
                    OpenFormController ctrl =new OpenFormController();
                    var content = ctrl.GetContent(id, ModuleId);
                     */
                    string json = hfOpenForm.Value;
                    phForm.Visible = false;
                    phResult.Visible = true;
                    string formData = "";
                    dynamic data = OpenFormUtils.GenerateFormData(json, out formData);

                    string jsonSettings = Settings["data"] as string;
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);
                    if (settings?.Settings != null)
                    {
                        HandlebarsEngine hbs = new HandlebarsEngine();
                        lMessage.Text = hbs.Execute(settings.Settings.Message, data);
                        lTracking.Text = settings.Settings.Tracking;
                    }
                }
            }
        }

        private void InitForm(string template)
        {
            bool templateDefined = !string.IsNullOrEmpty(template);
            string settings = ModuleContext.Settings["data"] as string;
            bool settingsDefined = !string.IsNullOrEmpty(settings);

            if (!templateDefined || !settingsDefined)
            {
                pHelp.Visible = true;
            }
            if (ModuleContext.PortalSettings.UserInfo.IsSuperUser)
            {
                hlTempleteExchange.NavigateUrl = ModuleContext.EditUrl("ShareTemplate");
                hlTempleteExchange.Visible = true;
            }
            if (pHelp.Visible && ModuleContext.EditMode)
            {
                hlEditSettings.NavigateUrl = ModuleContext.EditUrl("EditSettings");
                hlEditSettings.Visible = true;
                scriptList.Items.AddRange(OpenFormUtils.GetTemplatesFiles(PortalSettings, ModuleId, template).ToArray());
                scriptList.Visible = true;
            }


            if (string.IsNullOrEmpty(template))
            {
                ScopeWrapper.Visible = false;
            }
            if (templateDefined)
            {
                IncludeResourses(template);
            }
            if (settingsDefined)
            {
                SettingsDTO set = JsonConvert.DeserializeObject<SettingsDTO>(settings);
                if (!string.IsNullOrEmpty(set.Settings.SiteKey))
                {
                    ClientResourceManager.RegisterScript(Page, "https://www.google.com/recaptcha/api.js", FileOrder.Js.DefaultPriority, "DnnPageHeaderProvider");

                    lReCaptcha.Text = "<div class=\"g-recaptcha\" data-sitekey=\"" + set.Settings.SiteKey + "\"></div>";
                }
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
        private void IncludeResourses(string template)
        {
            if (!(string.IsNullOrEmpty(template)))
            {
                var cssfilename = new FileUri(Path.GetDirectoryName(template), "template.css");
                if (cssfilename.FileExists)
                {
                    ClientResourceManager.RegisterStyleSheet(Page, Page.ResolveUrl(cssfilename.UrlFilePath), FileOrder.Css.PortalCss);
                }
                var jsfilename = new FileUri(Path.GetDirectoryName(template), "template.js");
                if (jsfilename.FileExists)
                {
                    ClientResourceManager.RegisterScript(Page, jsfilename.UrlFilePath, FileOrder.Js.DefaultPriority);
                }
            }
        }
        public DotNetNuke.Entities.Modules.Actions.ModuleActionCollection ModuleActions
        {
            get
            {
                var actions = new ModuleActionCollection();
                actions.Add(ModuleContext.GetNextActionID(),
                            Localization.GetString(ModuleActionType.AddContent, LocalResourceFile),
                            ModuleActionType.AddContent,
                            "",
                            "~/DesktopModules/OpenContent/images/editcontent2.png",
                            ModuleContext.EditUrl(),
                            false,
                            SecurityAccessLevel.Edit,
                            true,
                            false);
                actions.Add(ModuleContext.GetNextActionID(),
                          Localization.GetString("EditSettings.Action", LocalResourceFile),
                          ModuleActionType.ContentOptions,
                          "",
                          "~/DesktopModules/OpenContent/images/editsettings2.png",
                          ModuleContext.EditUrl("EditSettings"),
                          false,
                          SecurityAccessLevel.Edit,
                          true,
                          false);



                var scriptFileSetting = Settings["template"] as string;
                if (!string.IsNullOrEmpty(scriptFileSetting))
                {
                    string templateFilename = Server.MapPath("~/" + scriptFileSetting);
                    string builderFilename = Path.GetDirectoryName(templateFilename) + "\\" + "builder.json";

                    if (File.Exists(builderFilename))
                        actions.Add(ModuleContext.GetNextActionID(),
                            Localization.GetString("Builder.Action", LocalResourceFile),
                            ModuleActionType.ContentOptions,
                            "",
                            "~/DesktopModules/OpenForm/images/formbuilder.png",
                            ModuleContext.EditUrl("FormBuilder"),
                            false,
                            SecurityAccessLevel.Edit,
                            true,
                            false);


                    actions.Add(ModuleContext.GetNextActionID(),
                               Localization.GetString("EditTemplate.Action", LocalResourceFile),
                               ModuleActionType.ContentOptions,
                               "",
                               "~/DesktopModules/OpenForm/images/edittemplate.png",
                               ModuleContext.EditUrl("EditTemplate"),
                               false,
                               SecurityAccessLevel.Host,
                               true,
                               false);



                    actions.Add(ModuleContext.GetNextActionID(),
                        Localization.GetString("EditData.Action", LocalResourceFile),
                        ModuleActionType.EditContent,
                        "",
                        "~/DesktopModules/OpenForm/images/edit.png",
                        //ModuleContext.EditUrl("EditData"),
                        ModuleContext.EditUrl("EditData"),
                        false,
                        SecurityAccessLevel.Host,
                        true,
                        false);
                }
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
                actions.Add(ModuleContext.GetNextActionID(),
                           Localization.GetString("ShareTemplate.Action", LocalResourceFile),
                           ModuleActionType.ContentOptions,
                           "",
                           "~/DesktopModules/OpenContent/images/exchange.png",
                           ModuleContext.EditUrl("ShareTemplate"),
                           false,
                           SecurityAccessLevel.Host,
                           true,
                           false);

                return actions;
            }
        }
        protected string AlpacaCulture
        {
            get
            {
                string cultureCode = LocaleController.Instance.GetCurrentLocale(PortalId).Code;
                return AlpacaEngine.AlpacaCulture(cultureCode);
            }
        }
        protected void scriptList_SelectedIndexChanged(object sender, EventArgs e)
        {
            ModuleController mc = new ModuleController();
            mc.UpdateModuleSetting(ModuleId, "template", scriptList.SelectedValue);
        }
        protected void lbSave_Click(object sender, EventArgs e)
        {
        }
        protected string PostBackStr()
        {
            //return Page.GetPostBackEventReference(lbSave);
            PostBackOptions pb = new PostBackOptions(lbSave, null, Globals.NavigateURL("", "result=submit"), false, false, false, true, false, null);
            return Page.ClientScript.GetPostBackEventReference(pb);

        }

        protected string GetString(string resourceKey)
        {
            return Localization.GetString(resourceKey, LocalResourceFile);
        }

    }
}

