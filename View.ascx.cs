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
using System.Web.Hosting;
using System.Linq;
using Satrabel.OpenContent.Components.Razor;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using DotNetNuke.Services.FileSystem;

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
            RegisterFields();

        }

        private void RegisterFields()
        {
            Scripts = new List<string>();

            List<string> fieldTypes = new List<string>();
            JToken options = GetOptions();
            if (options != null)
            {
                fieldTypes = FieldTypes(options);
            }

            Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/js/lib/handlebars/handlebars.min.js"));
            Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/js/alpaca/bootstrap/alpaca.min.js"));
            Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/alpaca/js/fields/dnn/CheckboxField.js"));

            if (fieldTypes.Contains("date") || fieldTypes.Contains("datetime") || fieldTypes.Contains("time"))
            {
                //ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/alpaca/js/fields/dnn/DateField.js", FileOrder.Js.DefaultPriority+10, "DnnPageHeaderProvider");
                //ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/js/lib/moment/min/moment-with-locales.min.js", FileOrder.Js.DefaultPriority+10, "DnnPageHeaderProvider");
                //ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/js/lib/eonasdan-bootstrap-datetimepicker/build/js/bootstrap-datetimepicker.min.js", FileOrder.Js.DefaultPriority + 11, "DnnPageHeaderProvider");
                ClientResourceManager.RegisterStyleSheet(Page, "~/DesktopModules/OpenContent/js/lib/eonasdan-bootstrap-datetimepicker/build/css/bootstrap-datetimepicker.css", FileOrder.Css.DefaultPriority+11);
                Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/alpaca/js/fields/dnn/DateField.js"));
                Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/js/lib/moment/min/moment-with-locales.min.js"));
                Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/js/lib/eonasdan-bootstrap-datetimepicker/build/js/bootstrap-datetimepicker.min.js"));                
            }
            if (fieldTypes.Contains("summernote") || fieldTypes.Contains("mlsummernote"))
            {
                //ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/alpaca/js/fields/dnn/SummernoteField.js", FileOrder.Js.DefaultPriority+10, "DnnPageHeaderProvider");
                //ClientResourceManager.RegisterScript(Page, "~/DesktopModules/OpenContent/js/summernote/summernote.min.js", FileOrder.Js.DefaultPriority+10, "DnnPageHeaderProvider");
                ClientResourceManager.RegisterStyleSheet(Page, "~/DesktopModules/OpenContent/js/summernote/summernote.css", FileOrder.Css.DefaultPriority+10);
                Scripts.Add(Page.ResolveUrl("~/DesktopModules/OpenContent/alpaca/js/fields/dnn/SummernoteField.js"));
                Scripts.Add(Page.ResolveUrl( "~/DesktopModules/OpenContent/js/summernote/summernote.min.js"));
            }
        }
        private JToken GetOptions()
        {
            string Template = ModuleContext.Settings["template"] as string;
            string templateFilename = HostingEnvironment.MapPath("~/" + Template);
            //string templateFilename = "~/" + Template;
            string optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options.json";

            // default options
            JToken optionsJson = JsonUtils.GetJsonFromFile(optionsFilename);

            // language options
            optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + $"options.{DnnLanguageUtils.GetCurrentCultureCode()}.json";
            if (File.Exists(optionsFilename))
            {
                JToken languageOptionsJson = JsonUtils.GetJsonFromFile(optionsFilename);
                optionsJson = optionsJson.JsonMerge(languageOptionsJson);
            }
            return optionsJson;
        }

        private static List<string> FieldTypes(JToken options)
        {
            var types = new List<string>();
            var fields = options["fields"];
            if (fields != null)
            {
                foreach (JProperty fieldProp in fields.Children())
                {
                    var field = fieldProp.First();
                    var fieldtype = field["type"];
                    if (fieldtype != null)
                    {
                        types.Add(fieldtype.ToString());
                    }
                    var subtypes = FieldTypes(field);
                    types.AddRange(subtypes);
                }
            }
            else if (options["items"] != null)
            {
                if (options["items"]["type"] != null)
                {
                    var fieldtype = options["items"]["type"] as JValue;
                    types.Add(fieldtype.Value.ToString());
                }
                var subtypes = FieldTypes(options["items"]);
                types.AddRange(subtypes);
            }
            return types;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            pTempleteExchange.Visible = UserInfo.IsSuperUser;
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
                    string json = Request["__OPENFORM" + ModuleId];
                    phForm.Visible = false;
                    phResult.Visible = true;
                    string formData = "";
                    dynamic data = OpenFormUtils.GenerateFormData(json, out formData);

                    string jsonSettings = Settings["data"] as string;
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);
                    if (settings != null && settings.Settings != null)
                    {
                        if (!string.IsNullOrEmpty(settings.Settings.Message))
                        {
                            HandlebarsEngine hbs = new HandlebarsEngine();
                            lMessage.Text = hbs.Execute(settings.Settings.Message, data);
                        }
                        lTracking.Text = settings.Settings.Tracking;
                    }
                    var razorscript = new FileUri(Path.GetDirectoryName(Template), "aftersubmit.cshtml");
                    if (razorscript.FileExists)
                    {
                        data.FeedBackMessage = lMessage.Text;
                        data.IPAddress = Request.UserHostAddress;
                        lMessage.Text = ExecuteRazor(razorscript, data);
                    }
                }
            }
        }
        private string ExecuteRazor(FileUri template, dynamic model)
        {
            string webConfig = template.PhysicalFullDirectory;
            webConfig = webConfig.Remove(webConfig.LastIndexOf("\\")) + "\\web.config";
            if (!File.Exists(webConfig))
            {
                string filename = HostingEnvironment.MapPath("~/DesktopModules/OpenContent/Templates/web.config");
                File.Copy(filename, webConfig);
            }
            var writer = new StringWriter();
            try
            {
                var razorEngine = new RazorEngine("~/" + template.FilePath, ModuleContext, LocalResourceFile);
                razorEngine.Render(writer, model);
            }
            catch (Exception ex)
            {
                App.Services.Logger.Error(ex);
                //LoggingUtils.RenderEngineException(this, ex);
                string stack = string.Join("\n", ex.StackTrace.Split('\n').Where(s => s.Contains("\\Portals\\") && s.Contains("in")).Select(s => s.Substring(s.IndexOf("in"))).ToArray());
                //throw new TemplateException("Failed to render Razor template " + template.FilePath + "\n" + stack, ex, model, template.FilePath);
                return "Failed to render Razor template " + template.FilePath + "\n" + stack;
            }
            return writer.ToString();
        }

        private void InitForm(string template)
        {
            bool templateDefined = !string.IsNullOrEmpty(template);
            string settings = ModuleContext.Settings["data"] as string;
            bool settingsDefined = !string.IsNullOrEmpty(settings);

            if ((!templateDefined || !settingsDefined) && ModuleContext.IsEditable )
            {
                pHelp.Visible = true;
            }
            if (ModuleContext.PortalSettings.UserInfo.IsSuperUser)
            {
                hlTempleteExchange.NavigateUrl = ModuleContext.EditUrl("ShareTemplate");
                hlTempleteExchange.Visible = true;
            }
            if (pHelp.Visible && ModuleContext.IsEditable)
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
                    if (set.Settings.UseRecaptchaV3)
                    {
                        // Use reCAPTCHA v3
                        string siteKey = set.Settings.SiteKey;

                        Page.Header.Controls.Add(new LiteralControl(
                            $"<script src='https://www.google.com/recaptcha/api.js?render={siteKey}' async defer></script>"
                        ));

                        lReCaptcha.Text = $"<input type='hidden' id='recaptchaSiteKey' value='{siteKey}' />";
                    }
                    else
                    {
                        // Use reCAPTCHA v2
                        ClientResourceManager.RegisterScript(Page,
                            "https://www.google.com/recaptcha/api.js",
                            FileOrder.Js.DefaultPriority,
                            "DnnPageHeaderProvider");

                        lReCaptcha.Text = $"<div class='g-recaptcha' data-sitekey='{set.Settings.SiteKey}'></div>";
                    }
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
                               SecurityAccessLevel.Admin,
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

        public List<string> Scripts { get; set; }

        protected void bCopyTemplate_Click(object sender, EventArgs e)
        {

            string Folder = Server.MapPath(Path.GetDirectoryName(scriptList.SelectedValue));
            
            var TemplateName = tbTemplateName.Text;
            
            string FolderName = GetModuleSubDir() + "/Templates/" + TemplateName;
            var folder = FolderManager.Instance.GetFolder(PortalId, FolderName);
            int idx = 1;
            while (folder != null)
            {
                FolderName = GetModuleSubDir() + "/Templates/" + TemplateName + idx;
                folder = FolderManager.Instance.GetFolder(PortalId, FolderName);
                idx++;
            }
            if (folder == null)
            {
                folder = FolderManager.Instance.AddFolder(PortalId, FolderName);
            }
            foreach (var item in Directory.GetFiles(Folder))
            {
                File.Copy(item, folder.PhysicalPath + Path.GetFileName(item));
            }

            var current = PortalSettings.HomeDirectory + FolderName.Replace("\\", "/") + "/schema.json";

            scriptList.Items.Clear();
            scriptList.Items.AddRange(OpenFormUtils.GetTemplatesFiles(PortalSettings, ModuleId, current).ToArray());
            
            DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, "Copy Successful", DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
            ModuleController mc = new ModuleController();
            mc.UpdateModuleSetting(ModuleId, "template", scriptList.SelectedValue);
        }

        protected virtual string GetModuleSubDir()
        {
            string dir = Path.GetDirectoryName(ModuleContext.Configuration.ModuleControl.ControlSrc);
            dir = dir.Substring(dir.IndexOf("DesktopModules") + 15);
            return dir;
        }
    }
}

