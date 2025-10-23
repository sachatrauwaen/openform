#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Web.Http;
using DotNetNuke.Web.Api;
using Newtonsoft.Json.Linq;
using System.Web.Hosting;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using DotNetNuke.Services.Mail;
using System.Net.Mail;
using System.Text;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Users;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Handlebars;
using RecaptchaV2.NET;
using DotNetNuke.Security;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Form;
using Satrabel.OpenContent.Components.Logging;
using System.Net.Http.Formatting;
using DotNetNuke.Common;
using System.Web;
using System.Text.RegularExpressions;
using DotNetNuke.Entities.Icons;
using DotNetNuke.Services.FileSystem;

#endregion

namespace Satrabel.OpenForm.Components
{
    [AllowAnonymous]
    public class OpenFormAPIController : DnnApiController
    {
        public string BaseDir
        {
            get
            {
                return PortalSettings.HomeDirectory + "/OpenForm/Templates/";
            }
        }
        [HttpGet]
        public HttpResponseMessage Form()
        {
            string template = (string)ActiveModule.ModuleSettings["template"];

            JObject json = new JObject();
            try
            {
                if (!string.IsNullOrEmpty(template))
                {
                    string templateFilename = HostingEnvironment.MapPath("~/" + template);
                    string schemaFilename = Path.GetDirectoryName(templateFilename) + "\\" + "schema.json";

                    json["schema"] = JsonUtils.GetJsonFromFile(schemaFilename);
                    if (UserInfo.UserID > 0 && json["schema"] is JObject)
                    {
                        json["schema"] = OpenContent.Components.Form.FormUtils.InitFields(json["schema"] as JObject, UserInfo);
                    }

                    // default options
                    string optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options.json";
                    if (File.Exists(optionsFilename))
                    {
                        string fileContent = File.ReadAllText(optionsFilename);
                        if (!string.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject optionsJson = JObject.Parse(fileContent);
                            json["options"] = optionsJson;
                        }
                    }
                    // language options
                    optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options." + DnnUtils.GetCurrentCultureCode() + ".json";
                    if (File.Exists(optionsFilename))
                    {
                        string fileContent = File.ReadAllText(optionsFilename);
                        if (!string.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject optionsJson = JObject.Parse(fileContent);
                            json["options"] = json["options"].JsonMerge(optionsJson);
                        }
                    }
                    // view
                    string viewFilename = Path.GetDirectoryName(templateFilename) + "\\" + "view.json";
                    if (File.Exists(viewFilename))
                    {
                        string fileContent = File.ReadAllText(viewFilename);
                        if (!string.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject viewJson = JObject.Parse(fileContent);
                            json["view"] = viewJson;
                        }
                    }
                    // language view
                    viewFilename = Path.GetDirectoryName(templateFilename) + "\\" + "view." + DnnUtils.GetCurrentCultureCode() + ".json";
                    if (File.Exists(viewFilename))
                    {
                        string fileContent = File.ReadAllText(viewFilename);
                        if (!string.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject viewJson = JObject.Parse(fileContent);
                            json["view"] = json["view"].JsonMerge(viewJson); ;
                        }
                    }

                }
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                LoggingUtils.ProcessApiLoadException(this, exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [HttpGet]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        public HttpResponseMessage Settings()
        {
            string Data = (string)ActiveModule.ModuleSettings["data"];
            JObject json = new JObject();
            try
            {
                string path = Path.GetDirectoryName(HostingEnvironment.MapPath("~/" + ActiveModule.ModuleControl.ControlSrc)) + "\\";
                string Template = "settings-schema.json";
                string schemaFilename = path + Template;

                JObject schemaJson = JsonUtils.GetJsonFromFile(schemaFilename);
                json["schema"] = schemaJson;
                string optionsFilename = path + "settings-options." + DnnUtils.GetCurrentCultureCode() + ".json";
                if (!File.Exists(optionsFilename))
                {
                    optionsFilename = path + "settings-options.json";
                }
                if (File.Exists(optionsFilename))
                {
                    JObject optionsJson = JsonUtils.GetJsonFromFile(optionsFilename);
                    json["options"] = optionsJson;
                }

                if (!string.IsNullOrEmpty(Data))
                {
                    json["data"] = JObject.Parse(Data);
                }
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        public HttpResponseMessage Submit()
        {
            var res = new ResultDTO()
            {
                Message = "Form submitted."
            };
            var form = JObject.Parse(HttpContextSource.Current.Request.Form["data"].ToString());
            var statuses = new List<FilesStatus>();
            try
            {
                //todo can we eliminate the HttpContext here
                UploadWholeFile(HttpContextSource.Current, statuses);
                var files = new JArray();
                form["Files"] = files;
                int i = 1;
                foreach (var item in statuses)
                {
                    var file = new JObject();
                    file["name"] = item.name;
                    file["url"] = OpenFormUtils.ToAbsoluteUrl(item.url);
                    files.Add(file);
                    //form["File"+i] = OpenFormUtils.ToAbsoluteUrl(item.url);                    
                    i++;
                }
            }
            catch (Exception exc)
            {
                res.Errors.Add(exc.Message);
                Log.Logger.Error(exc);
            }

            try
            {
                form["IPAddress"] = Request.GetIPAddress();
                int moduleId = ActiveModule.ModuleID;


                string template = (string)ActiveModule.ModuleSettings["template"];
                var razorscript = new FileUri(Path.GetDirectoryName(template), "aftersubmit.cshtml");
                res.AfterSubmit = razorscript.FileExists;

                string jsonSettings = ActiveModule.ModuleSettings["data"] as string;
                if (!string.IsNullOrEmpty(jsonSettings))
                {
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);

                    if (!settings.Settings.NotSaveSubmissions)
                    {
                        OpenFormController ctrl = new OpenFormController();
                        var content = new OpenFormInfo()
                        {
                            ModuleId = moduleId,
                            Json = form.ToString(),
                            CreatedByUserId = UserInfo.UserID,
                            CreatedOnDate = DateTime.Now,
                            LastModifiedByUserId = UserInfo.UserID,
                            LastModifiedOnDate = DateTime.Now,
                            Html = "",
                            Title = "Form submitted - " + DateTime.Now.ToString()
                        };
                        ctrl.AddContent(content);
                    }

                    HandlebarsEngine hbs = new HandlebarsEngine();
                    dynamic data = null;
                    string formData = "";
                    if (form != null)
                    {
                        if (!string.IsNullOrEmpty(settings.Settings.SiteKey))
                        {
                            string token = form["recaptcha"]?.ToString();
                            if (string.IsNullOrEmpty(token))
                                return Request.CreateResponse(HttpStatusCode.BadRequest, new { Errors = new[] { "Missing reCAPTCHA token." } });

                            if (settings.Settings.UseRecaptchaV3)
                            {
                                // reCAPTCHA v3
                                var recaptcha = new RecaptchaV3.NET.Recaptcha
                                {
                                    SiteKey = settings.Settings.SiteKey,
                                    SecretKey = settings.Settings.SecretKey
                                };
                                var result = recaptcha.Validate(token, Request.GetIPAddress());

                                float threshold = settings.Settings.RecaptchaScoreThreshold > 0
                                ? settings.Settings.RecaptchaScoreThreshold
                                : 0.5f;

                                if (!result.Succeeded || result.Score < threshold)
                                {
                                    Log.Logger.Warn($"reCAPTCHA v3 failed: score={result.Score}, threshold={threshold}, action={result.Action}");
                                    return Request.CreateResponse(HttpStatusCode.Forbidden, new { Errors = new[] { "reCAPTCHA v3 validation failed." } });
                                }
                            }
                            else
                            {
                                // reCAPTCHA v2
                                var recaptcha = new RecaptchaV2.NET.Recaptcha(settings.Settings.SiteKey, settings.Settings.SecretKey);
                                var result = recaptcha.Validate(token);
                                if (!result.Succeeded)
                                {
                                    Log.Logger.Warn("reCAPTCHA v2 validation failed.");
                                    return Request.CreateResponse(HttpStatusCode.Forbidden, new { Errors = new[] { "reCAPTCHA v2 validation failed." } });
                                }
                            }

                            form.Remove("recaptcha");
                        }

                        string templateFilename = HostingEnvironment.MapPath("~/" + template);
                        string schemaFilename = Path.GetDirectoryName(templateFilename) + "\\" + "schema.json";
                        JObject schemaJson = JsonUtils.GetJsonFromFile(schemaFilename);
                        //form["schema"] = schemaJson;
                        // default options
                        string optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options.json";
                        JObject optionsJson = null;
                        if (File.Exists(optionsFilename))
                        {
                            string fileContent = File.ReadAllText(optionsFilename);
                            if (!string.IsNullOrWhiteSpace(fileContent))
                            {
                                optionsJson = JObject.Parse(fileContent);
                                //form["options"] = optionsJson;
                            }
                        }
                        // language options
                        optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options." + DnnLanguageUtils.GetCurrentCultureCode() + ".json";
                        if (File.Exists(optionsFilename))
                        {
                            string fileContent = File.ReadAllText(optionsFilename);
                            if (!string.IsNullOrWhiteSpace(fileContent))
                            {
                                optionsJson = JObject.Parse(fileContent);
                                //form["options"] = optionsJson;
                            }
                        }
                        var enhancedForm = form.DeepClone() as JObject;
                        OpenFormUtils.ResolveLabels(enhancedForm, schemaJson, optionsJson);
                        data = OpenFormUtils.GenerateFormData(enhancedForm.ToString(), out formData);
                    }

                    if (settings != null && settings.Notifications != null)
                    {
                        foreach (var notification in settings.Notifications)
                        {
                            try
                            {
                                MailAddress from = FormUtils.GenerateMailAddress(notification.From, notification.FromEmail, notification.FromName, notification.FromEmailField, notification.FromNameField, form);
                                MailAddress to = FormUtils.GenerateMailAddress(notification.To, notification.ToEmail, notification.ToName, notification.ToEmailField, notification.ToNameField, form);
                                MailAddress reply = null;
                                if (!string.IsNullOrEmpty(notification.ReplyTo))
                                {
                                    reply = FormUtils.GenerateMailAddress(notification.ReplyTo, notification.ReplyToEmail, notification.ReplyToName, notification.ReplyToEmailField, notification.ReplyToNameField, form);
                                }
                                string body = formData;
                                if (!string.IsNullOrEmpty(notification.EmailBody))
                                {
                                    try
                                    {
                                        body = hbs.Execute(notification.EmailBody, data);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Email Body : " + ex.Message, ex);
                                    }

                                }
                                string subject = notification.EmailSubject;
                                if (!string.IsNullOrEmpty(notification.EmailSubject))
                                {
                                    try
                                    {
                                        subject = hbs.Execute(notification.EmailSubject, data);
                                    }
                                    catch (Exception ex)
                                    {
                                        throw new Exception("Email Subject : " + ex.Message, ex);
                                    }

                                }
                                var attachements = new List<Attachment>();
                                foreach (var item in statuses)
                                {
                                    var file = FileManager.Instance.GetFile(item.id);
                                    attachements.Add(new Attachment(FileManager.Instance.GetFileContent(file), item.name));
                                }
                                string send = FormUtils.SendMail(from.ToString(), to.ToString(), (reply == null ? "" : reply.ToString()), subject, body, attachements);
                                if (!string.IsNullOrEmpty(send))
                                {
                                    res.Errors.Add("From:" + from.ToString() + " - To:" + to.ToString() + " - " + send);
                                }
                            }
                            catch (Exception exc)
                            {
                                res.Errors.Add("Error in Email Notification " + (settings.Notifications.IndexOf(notification) + 1) + " : " + exc.Message + (UserInfo.IsSuperUser ? " - " + exc.StackTrace : ""));
                                Log.Logger.Error(exc);
                            }
                        }
                    }
                    if (settings != null && settings.Settings != null)
                    {
                        if (!string.IsNullOrEmpty(settings.Settings.Message))
                        {
                            res.Message = hbs.Execute(settings.Settings.Message, data);
                        }
                        else
                        {
                            res.Message = "Message sent.";
                        }
                        res.Tracking = settings.Settings.Tracking;
                        if (!string.IsNullOrEmpty(settings.Settings.Tracking))
                        {
                            //res.RedirectUrl = Globals.NavigateURL(ActiveModule.TabID, "", "result=" + content.ContentId);
                        }
                    }
                }

                return Request.CreateResponse(HttpStatusCode.OK, res);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [ValidateAntiForgeryToken]
        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.Edit)]
        [HttpGet]
        public HttpResponseMessage LoadBuilder()
        {
            string template = (string)ActiveModule.ModuleSettings["template"];
            JObject json = new JObject();
            try
            {
                if (!string.IsNullOrEmpty(template))
                {
                    string templateFilename = HostingEnvironment.MapPath("~/" + template);
                    string dataFilename = Path.GetDirectoryName(templateFilename) + "\\" + "builder.json";
                    JObject dataJson = JObject.Parse(File.ReadAllText(dataFilename));
                    if (dataJson != null)
                        json["data"] = dataJson;
                }
                return Request.CreateResponse(HttpStatusCode.OK, json);
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }

        [DnnModuleAuthorize(AccessLevel = SecurityAccessLevel.View)]
        [ValidateAntiForgeryToken]
        [HttpPost]
        public HttpResponseMessage UpdateBuilder(JObject json)
        {
            string template = (string)ActiveModule.ModuleSettings["template"];
            try
            {
                string templateFilename = HostingEnvironment.MapPath("~/" + template);
                string dataDirectory = Path.GetDirectoryName(templateFilename) + "\\";
                if (json["data"] != null && json["schema"] != null && json["options"] != null && json["view"] != null)
                {
                    var schema = json["schema"].ToString();
                    var options = json["options"].ToString();
                    var view = json["view"].ToString();
                    var data = json["data"].ToString();
                    var datafile = dataDirectory + "builder.json";
                    var schemafile = dataDirectory + "schema.json";
                    var optionsfile = dataDirectory + "options.json";
                    var viewfile = dataDirectory + "view.json";
                    try
                    {
                        File.WriteAllText(datafile, data);
                        File.WriteAllText(schemafile, schema);
                        File.WriteAllText(optionsfile, options);
                        File.WriteAllText(viewfile, view);
                    }
                    catch (Exception ex)
                    {
                        string mess = $"Error while saving file [{datafile}]";
                        Log.Logger.Error(mess, ex);
                        throw new Exception(mess, ex);
                    }
                }
                return Request.CreateResponse(HttpStatusCode.OK, "");
            }
            catch (Exception exc)
            {
                Log.Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }


        private void UploadWholeFile(HttpContextBase context, ICollection<FilesStatus> statuses)
        {
            IFileManager _fileManager = FileManager.Instance;
            IFolderManager _folderManager = FolderManager.Instance;
            for (var i = 0; i < context.Request.Files.Count; i++)
            {
                var file = context.Request.Files[i];
                if (file == null) continue;

                var fileName = FileUploadController.CleanUpFileName(Path.GetFileName(file.FileName));


                if (IsAllowedExtension(fileName))
                {
                    string uploadfolder = "OpenForm/Files/" + ActiveModule.ModuleID;

                    if (!string.IsNullOrEmpty(context.Request.Form["uploadfolder"]))
                    {
                        uploadfolder = context.Request.Form["uploadfolder"];
                    }
                    var userFolder = _folderManager.GetFolder(PortalSettings.PortalId, uploadfolder);
                    if (userFolder == null)
                    {
                        // Get folder mapping
                        var folderMapping = FolderMappingController.Instance.GetFolderMapping(PortalSettings.PortalId, "Secure");
                        userFolder = _folderManager.AddFolder(folderMapping, uploadfolder);
                        //userFolder = _folderManager.AddFolder(PortalSettings.PortalId, uploadfolder);
                    }
                    int suffix = 0;
                    string baseFileName = Path.GetFileNameWithoutExtension(fileName);
                    string extension = Path.GetExtension(fileName);
                    var fileInfo = _fileManager.GetFile(userFolder, fileName);
                    while (fileInfo != null)
                    {
                        suffix++;
                        fileName = baseFileName + "-" + suffix + extension;
                        fileInfo = _fileManager.GetFile(userFolder, fileName);
                    }
                    fileInfo = _fileManager.AddFile(userFolder, fileName, file.InputStream, true);
                    var fileIcon = IconController.IconURL("Ext" + fileInfo.Extension, "32x32");
                    if (!File.Exists(context.Server.MapPath(fileIcon)))
                    {
                        fileIcon = IconController.IconURL("File", "32x32");
                    }

                    statuses.Add(new FilesStatus
                    {
                        success = true,
                        name = fileName,
                        extension = fileInfo.Extension,
                        type = fileInfo.ContentType,
                        size = file.ContentLength,
                        progress = "1.0",
                        url = _fileManager.GetUrl(fileInfo),
                        thumbnail_url = fileIcon,
                        message = "success",
                        id = fileInfo.FileId,
                    });
                }
                else
                {
                    statuses.Add(new FilesStatus
                    {
                        success = false,
                        name = fileName,
                        message = "File type not allowed."
                    });
                }
            }

        }

        private static bool IsAllowedExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName);

            //regex matches a dot followed by 1 or more chars followed by a semi-colon
            //regex is meant to block files like "foo.asp;.png" which can take advantage
            //of a vulnerability in IIS6 which treasts such files as .asp, not .png
            return !string.IsNullOrEmpty(extension)
                   && Host.AllowedExtensionWhitelist.IsAllowedExtension(extension)
                   && !Regex.IsMatch(fileName, @"\..+;");
        }

    }



    public class NotificationDTO
    {
        public string From { get; set; }
        public string FromName { get; set; }
        public string FromEmailField { get; set; }
        public string FromNameField { get; set; }
        public string FromEmail { get; set; }
        public string To { get; set; }
        public string ToName { get; set; }
        public string ToEmailField { get; set; }
        public string ToNameField { get; set; }
        public string ToEmail { get; set; }
        public string ReplyTo { get; set; }
        public string ReplyToName { get; set; }
        public string ReplyToEmail { get; set; }
        public string ReplyToNameField { get; set; }
        public string ReplyToEmailField { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }
    public class GenSettingsDTO
    {
        public string Message { get; set; }
        public string Tracking { get; set; }
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }
        public bool UseRecaptchaV3 { get; set; }
        public float RecaptchaScoreThreshold { get; set; } = 0.5f;
        public bool NotSaveSubmissions { get; set; }

    }

    public class SettingsDTO
    {
        public List<NotificationDTO> Notifications { get; set; }
        public GenSettingsDTO Settings { get; set; }
    }
    class ResultDTO
    {
        public ResultDTO()
        {
            Errors = new List<string>();
        }
        public string Message { get; set; }
        public string Tracking { get; set; }
        public List<string> Errors { get; set; }
        public string RedirectUrl { get; set; }
        public bool AfterSubmit { get; set; }
    }
}

