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
using System.Web.UI.WebControls;
using DotNetNuke.Services.Localization;
using System.IO;
using Satrabel.OpenContent.Components;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;

#endregion

namespace Satrabel.OpenForm
{
    public partial class EditTemplate : PortalModuleBase
    {
        public string ModuleTemplateDirectory
        {
            get
            {
                return PortalSettings.HomeDirectory + "OpenForm/Templates/" + ModuleId.ToString() + "/";
            }
        }
        #region Event Handlers

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            cmdSave.Click += cmdSave_Click;
            cmdSaveClose.Click += cmdSaveAndClose_Click;
            cmdCancel.Click += cmdCancel_Click;
            cmdCustom.Click += cmdCustom_Click;
            cmdBuilder.Click += cmdBuilder_Click;
            scriptList.SelectedIndexChanged += scriptList_SelectedIndexChanged;
        }

        private void cmdBuilder_Click(object sender, EventArgs e)
        {

            if (scriptList.SelectedValue.EndsWith("schema.json"))
            {
                string template = ModuleContext.Settings["template"] as string;
                string templateFolder = Path.GetDirectoryName(template);

                var scriptFile = new FileUri(templateFolder, scriptList.SelectedValue.Replace("schema.json", "builder.json"));
                var schema = JsonUtils.LoadJsonFromFile(templateFolder + "/" + scriptList.SelectedValue) as JObject;
                var options = JsonUtils.LoadJsonFromFile(templateFolder + "/" + scriptList.SelectedValue.Replace("schema.json", "options.json")) as JObject;

                JObject builder = new JObject();
                builder["formfields"] = GetBuilder(schema, options);

                if (!scriptFile.FileExists)
                {
                    File.WriteAllText(scriptFile.PhysicalFilePath, builder.ToString());
                }
                Response.Redirect(Globals.NavigateURL(), true);
            }
        }

        private JToken GetBuilder(JObject schema, JObject options)
        {
            var formfields = new JArray();

            var schemaProperties = schema["properties"] as JObject;
            foreach (var schProp in schemaProperties.Properties())
            {
                var sch = schProp.Value as JObject;
                var opt = options != null && options["fields"] != null ? options["fields"][schProp.Name] : null;
                var field = new JObject();
                field["fieldname"] = schProp.Name;
                string schematype = sch["type"] != null ? sch["type"].ToString() : "string";
                string fieldtype = opt != null && opt["type"] != null ? opt["type"].ToString() : "text";
                if (fieldtype.Substring(0, 2) == "ml")
                {
                    fieldtype = fieldtype.Substring(2, fieldtype.Length - 2);
                    field["multilanguage"] = true;
                }
                if (sch["enum"] != null)
                {
                    if (fieldtype == "text")
                    {
                        fieldtype = "select";
                    }
                    JArray optionLabels = null;
                    if (opt != null && opt["optionLabels"] != null)
                    {
                        optionLabels = opt["optionLabels"] as JArray;
                    }
                    JArray fieldoptions = new JArray();
                    int i = 0;
                    foreach (var item in sch["enum"] as JArray)
                    {
                        var fieldOpt = new JObject();
                        fieldOpt["value"] = item.ToString();
                        fieldOpt["text"] = optionLabels != null ? optionLabels[i].ToString() : item.ToString();
                        fieldoptions.Add(fieldOpt);
                        i++;
                    }
                    field["fieldoptions"] = fieldoptions;
                };
                if (schematype == "boolean")
                {
                    fieldtype = "checkbox";
                }
                else if (schematype == "array")
                {
                    if (fieldtype == "checkbox")
                    {
                        fieldtype = "multicheckbox";
                    }
                    else if (fieldtype == "text")
                    {
                        fieldtype = "array";
                    }
                    if (sch["items"] != null)
                    {
                        var b = GetBuilder(sch["items"] as JObject, opt != null && opt["items"] != null ? opt["items"] as JObject : null);
                        field["subfields"] = b;
                    }
                }
                else if (schematype == "object")
                {
                    fieldtype = "object";
                    var b = GetBuilder(sch, opt as JObject);
                    field["subfields"] = b;
                }
                if (fieldtype == "select2" && opt["dataService"] != null && opt["dataService"]["data"] != null)
                {
                    fieldtype = "relation";
                    field["relationoptions"] = new JObject();
                    field["relationoptions"]["datakey"] = opt["dataService"]["data"]["dataKey"];
                    field["relationoptions"]["valuefield"] = opt["dataService"]["data"]["valueField"];
                    field["relationoptions"]["textfield"] = opt["dataService"]["data"]["textField"];
                }
                if ((fieldtype == "date" || fieldtype == "datetime" || fieldtype == "time") && opt["picker"] != null)
                {
                    field["dateoptions"] = new JObject();
                    field["dateoptions"] = opt["picker"];
                }
                field["fieldtype"] = fieldtype;
                if (sch["title"] != null)
                {
                    field["title"] = sch["title"];
                }
                if (sch["default"] != null)
                {
                    field["default"] = sch["default"];
                    field["advanced"] = true;
                }
                if (opt != null && opt["label"] != null)
                {
                    field["title"] = opt["label"];
                }
                if (opt != null && opt["helper"] != null)
                {
                    field["helper"] = opt["helper"];
                    field["advanced"] = true;
                }
                if (opt != null && opt["placeholder"] != null)
                {
                    field["placeholder"] = opt["placeholder"];
                    field["advanced"] = true;
                }
                if (sch["required"] != null)
                {
                    field["required"] = sch["required"];
                    field["advanced"] = true;
                }
                if (opt != null && opt["vertical"] != null)
                {
                    field["vertical"] = opt["vertical"];
                }
                formfields.Add(field);
            }
            return formfields;
        }

        private void cmdCustom_Click(object sender, EventArgs e)
        {
            string Template = ModuleContext.Settings["template"] as string;
            string TemplateFolder = Path.GetDirectoryName(Template);
            string TemplateDir = Server.MapPath(TemplateFolder);
            string ModuleDir = Server.MapPath(ModuleTemplateDirectory);
            if (!Directory.Exists(ModuleDir))
            {
                Directory.CreateDirectory(ModuleDir);
            }
            foreach (var item in Directory.GetFiles(ModuleDir))
            {
                File.Delete(item);
            }
            foreach (var item in Directory.GetFiles(TemplateDir))
            {
                File.Copy(item, ModuleDir + Path.GetFileName(item));
            }
            ModuleController mc = new ModuleController();
            Template = ModuleTemplateDirectory + "schema.json";
            mc.UpdateModuleSetting(ModuleId, "template", Template);
            InitEditor(Template);
        }
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            if (!Page.IsPostBack)
            {
                string Template = ModuleContext.Settings["template"] as string;
                InitEditor(Template);
            }
        }
        private void InitEditor(string Template)
        {
            LoadFiles(Template);
            DisplayFile(Template);

            if (Template.StartsWith(ModuleTemplateDirectory))
            {
                cmdCustom.Visible = false;
            }
        }
        private void DisplayFile(string Template)
        {
            string TemplateFolder = Path.GetDirectoryName(Template);
            TemplateFolder = OpenContentUtils.ReverseMapPath(TemplateFolder);
            string scriptFile = TemplateFolder + "/" + scriptList.SelectedValue;
            plSource.Text = scriptFile;
            string srcFile = Server.MapPath(scriptFile);
            if (File.Exists(srcFile))
            {
                txtSource.Text = File.ReadAllText(srcFile);
            }
            else
            {
                txtSource.Text = "";
            }
            SetFileType(srcFile);
            cmdBuilder.Visible = scriptList.SelectedValue.EndsWith("schema.json");
        }
        private void SetFileType(string filePath)
        {
            string mimeType;
            switch (Path.GetExtension(filePath).ToLowerInvariant())
            {
                case ".vb":
                    mimeType = "text/x-vb";
                    break;
                case ".cs":
                    mimeType = "text/x-csharp";
                    break;
                case ".css":
                    mimeType = "text/css";
                    break;
                case ".js":
                    mimeType = "text/javascript";
                    break;
                case ".json":
                    mimeType = "application/json";
                    break;
                case ".xml":
                case ".xslt":
                    mimeType = "application/xml";
                    break;
                case ".sql":
                case ".sqldataprovider":
                    mimeType = "text/x-sql";
                    break;
                case ".hbs":
                    mimeType = "htmlhandlebars";
                    break;
                default:
                    mimeType = "text/html";
                    break;
            }
            DotNetNuke.UI.Utilities.ClientAPI.RegisterClientVariable(Page, "mimeType", mimeType, true);
        }
        private void LoadFiles(string Template)
        {
            scriptList.Items.Clear();
            if (!(string.IsNullOrEmpty(Template)))
            {
                string templateFilename = Server.MapPath("~/" + Template);
                string builderFilename = Path.GetDirectoryName(templateFilename) + "\\" + "builder.json";
                if (!File.Exists(builderFilename))
                {
                    scriptList.Items.Add(new ListItem("Data Definition", "schema.json"));
                    scriptList.Items.Add(new ListItem("UI Options", "options.json"));
                    foreach (Locale item in LocaleController.Instance.GetLocales(PortalId).Values)
                    {
                        scriptList.Items.Add(new ListItem("UI Options " + item.Code, "options." + item.Code + ".json"));
                    }
                    scriptList.Items.Add(new ListItem("View Layout", "view.json"));
                }
                scriptList.Items.Add(new ListItem("Stylesheet", "template.css"));
                scriptList.Items.Add(new ListItem("Javascript", "template.js"));
                scriptList.Items.Add(new ListItem("After Submit", "aftersubmit.cshtml"));
            }
        }
        protected void cmdSave_Click(object sender, EventArgs e)
        {
            Save();
        }
        protected void cmdSaveAndClose_Click(object sender, EventArgs e)
        {
            Save();
            Response.Redirect(Globals.NavigateURL(), true);
        }
        private void Save()
        {
            string Template = ModuleContext.Settings["template"] as string;
            string TemplateFolder = Path.GetDirectoryName(Template);
            string scriptFile = TemplateFolder + "/" + scriptList.SelectedValue;
            string srcFile = Server.MapPath(scriptFile);
            if (string.IsNullOrWhiteSpace(txtSource.Text))
            {
                if (File.Exists(srcFile))
                {
                    File.Delete(srcFile);
                }
            }
            else
            {
                File.WriteAllText(srcFile, txtSource.Text);
            }
            //DotNetNuke.UI.Skins.Skin.AddModuleMessage(this, "Save Successful", DotNetNuke.UI.Skins.Controls.ModuleMessage.ModuleMessageType.GreenSuccess);
        }
        protected void cmdCancel_Click(object sender, EventArgs e)
        {
            Response.Redirect(Globals.NavigateURL(), true);
        }
        private void scriptList_SelectedIndexChanged(object sender, EventArgs e)
        {
            string Template = ModuleContext.Settings["template"] as string;
            DisplayFile(Template);
        }
        #endregion
    }
}