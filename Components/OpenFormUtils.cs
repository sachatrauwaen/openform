using DotNetNuke.Entities.Modules;
using DotNetNuke.Entities.Portals;
using Newtonsoft.Json.Linq;
using Satrabel.OpenContent.Components.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.UI.WebControls;

namespace Satrabel.OpenForm.Components
{
    public class OpenFormUtils
    {
        public static void UpdateModuleTitle(ModuleInfo module, string moduleTitle)
        {
            if (module.ModuleTitle != moduleTitle)
            {
                ModuleController mc = new ModuleController();
                var mod = mc.GetModule(module.ModuleID, module.TabID, true);
                mod.ModuleTitle = moduleTitle;
                mc.UpdateModule(mod);
            }
        }
        public static string GetSiteTemplateFolder(PortalSettings portalSettings)
        {
            return portalSettings.HomeDirectory + "/OpenForm/Templates/";
        }
        public static List<ListItem> GetTemplatesFiles(PortalSettings portalSettings, int moduleId, string selectedTemplate)
        {
            string basePath = HostingEnvironment.MapPath(GetSiteTemplateFolder(portalSettings));
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            List<ListItem> lst = new List<ListItem>();
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                string templateCat = "Site";
                string dirName = Path.GetFileNameWithoutExtension(dir);
                int modId = -1;
                if (int.TryParse(dirName, out modId))
                {
                    if (modId == moduleId)
                    {
                        templateCat = "Module";
                    }
                    else
                    {
                        continue;
                    }
                }
                var files = Directory.EnumerateFiles(dir, "schema.json", SearchOption.AllDirectories);
                foreach (string script in files)
                {
                    string scriptName = script.Remove(script.LastIndexOf(".")).Replace(basePath, "");
                    if (templateCat == "Module")
                    {
                        if (scriptName.ToLower().EndsWith("schema"))
                            scriptName = "for this module only";
                        else
                            scriptName = scriptName.Substring(scriptName.LastIndexOf("\\") + 1);
                    }
                    else if (scriptName.ToLower().EndsWith("schema"))
                        scriptName = scriptName.Remove(scriptName.LastIndexOf("\\"));
                    else
                        scriptName = scriptName.Replace("\\", " - ");

                    string scriptPath = ReverseMapPath(script);
                    var item = new ListItem(templateCat + " : " + scriptName, scriptPath);
                    if (!(string.IsNullOrEmpty(selectedTemplate)) && scriptPath.ToLowerInvariant() == selectedTemplate.ToLowerInvariant())
                    {
                        item.Selected = true;
                    }
                    lst.Add(item);
                }
            }
            return lst;
        }
        public static List<ListItem> GetTemplates(PortalSettings portalSettings, int moduleId, string selectedTemplate)
        {
            string basePath = HostingEnvironment.MapPath(GetSiteTemplateFolder(portalSettings));
            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }
            List<ListItem> lst = new List<ListItem>();
            foreach (var dir in Directory.GetDirectories(basePath))
            {
                string templateCat = "Site";
                string dirName = Path.GetFileNameWithoutExtension(dir);
                int modId = -1;
                if (int.TryParse(dirName, out modId))
                {
                    if (modId == moduleId)
                    {
                        templateCat = "Module";
                    }
                    else
                    {
                        continue;
                    }
                }
                string scriptName = dir;
                if (templateCat == "Module")
                    scriptName = templateCat;
                else
                    scriptName = templateCat + ":" + scriptName.Substring(scriptName.LastIndexOf("\\") + 1);

                string scriptPath = ReverseMapPath(dir);
                var item = new ListItem(scriptName, scriptPath);
                if (!(string.IsNullOrEmpty(selectedTemplate)) && scriptPath.ToLowerInvariant() == selectedTemplate.ToLowerInvariant())
                {
                    item.Selected = true;
                }
                lst.Add(item);
            }
            return lst;
        }
        public static dynamic GenerateFormData(string form, out string formData)
        {
            dynamic data = null;
            formData = "";
            StringBuilder formDataS = new StringBuilder();
            if (form != null)
            {
                formDataS.Append("<table boder=\"1\">");
                foreach (var item in JObject.Parse(form).Properties())
                {
                    formDataS.Append("<tr>").Append("<td>").Append(item.Name).Append("</td>").Append("<td>").Append(" : ").Append("</td>").Append("<td>").Append(HttpUtility.HtmlEncode(item.Value)).Append("</td>").Append("</tr>");
                }
                formDataS.Append("</table>");
                data = JsonUtils.JsonToDynamic(form);
                data.FormData = formDataS.ToString();
                formData = formDataS.ToString();
            }
            return data;
        }
        public static string ReverseMapPath(string path)
        {
            string appPath = HostingEnvironment.MapPath("~");
            string res = string.Format("{0}", path.Replace(appPath, "").Replace("\\", "/"));
            if (!res.StartsWith("/")) res = "/" + res;
            return res;
        }

        public static void ResolveLabels(JObject o, JObject schema, JObject options)
        {
            foreach (var child in o.Children<JProperty>().ToList())
            {
                JObject sch = null;
                if (schema?["properties"] != null)
                {
                    sch = schema["properties"][child.Name] as JObject;
                }
                JObject opt = null;
                if (options?["fields"] != null)
                {
                    opt = options["fields"][child.Name] as JObject;
                }
                if (opt == null) continue;
                
                JArray optionLabels = opt["optionLabels"] as JArray;
                
                var childProperty = child;
                if (childProperty.Value is JArray)
                {
                    var array = childProperty.Value as JArray;
                    JArray newArray = new JArray();
                    foreach (var value in array)
                    {
                        var obj = value as JObject;
                        if (obj != null)
                        {
                            ResolveLabels(obj, sch["items"] as JObject, opt["items"] as JObject);
                        }
                        else if (optionLabels != null)
                        {
                            var val = value as JValue;
                            if (val != null)
                            {
                                try
                                {
                                    var aEnum = sch["enum"] as JArray;
                                    var idx = Array.IndexOf(aEnum.Select(e => e.ToString()).ToArray(), val.ToString());
                                    newArray.Add(optionLabels[idx]);
                                }
                                catch (System.Exception)
                                {
                                }
                            }
                        }
                    }

                    if (optionLabels != null)
                    {
                        childProperty.Value = newArray;
                    }
                }
                else if (childProperty.Value is JObject)
                {
                    var obj = childProperty.Value as JObject;
                    ResolveLabels(obj, sch, opt);
                }
                else if (childProperty.Value is JValue)
                {
                    if (optionLabels != null)
                    {
                        string val = childProperty.Value.ToString();
                        try
                        {
                            var aEnum = sch["enum"] as JArray;
                            var idx = Array.IndexOf(aEnum.Select(e => e.ToString()).ToArray(), val.ToString());
                            o[childProperty.Name] = optionLabels[idx];

                        }
                        catch (System.Exception)
                        {
                            Debugger.Break();
                        }
                    }                   
                }
            }
        }

        public static string ToAbsoluteUrl(string relativeUrl)
        {
            if (string.IsNullOrEmpty(relativeUrl))
                return relativeUrl;

            if (HttpContext.Current == null)
                return relativeUrl;

            if (relativeUrl.StartsWith("/"))
                relativeUrl = relativeUrl.Insert(0, "~");
            if (!relativeUrl.StartsWith("~/"))
                relativeUrl = relativeUrl.Insert(0, "~/");

            var url = HttpContext.Current.Request.Url;
            var port = url.Port != 80 ? (":" + url.Port) : String.Empty;

            return String.Format("{0}://{1}{2}{3}",
                url.Scheme, url.Host, port, VirtualPathUtility.ToAbsolute(relativeUrl));
        }

    }

}