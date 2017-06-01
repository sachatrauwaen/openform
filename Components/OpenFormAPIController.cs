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
using MailPriority = DotNetNuke.Services.Mail.MailPriority;

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
                if (!String.IsNullOrEmpty(template))
                {
                    string templateFilename = HostingEnvironment.MapPath("~/" + template);
                    string schemaFilename = Path.GetDirectoryName(templateFilename) + "\\" + "schema.json";

                    json["schema"] = GetJsonFromFile(schemaFilename);
                    if (UserInfo.UserID > 0 && json["schema"] is JObject)
                    {
                        json["schema"] = FormUtils.InitFields(json["schema"] as JObject, UserInfo);
                    }

                    // default options
                    string optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options.json";
                    if (File.Exists(optionsFilename))
                    {
                        string fileContent = File.ReadAllText(optionsFilename);
                        if (!String.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject optionsJson = JObject.Parse(fileContent);
                            json["options"] = optionsJson;
                        }
                    }
                    // language options
                    optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options." + DnnLanguageUtils.GetCurrentCultureCode() + ".json";
                    if (File.Exists(optionsFilename))
                    {
                        string fileContent = File.ReadAllText(optionsFilename);
                        if (!String.IsNullOrWhiteSpace(fileContent))
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
                        if (!String.IsNullOrWhiteSpace(fileContent))
                        {
                            JObject viewJson = JObject.Parse(fileContent);
                            json["view"] = viewJson;
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
        public HttpResponseMessage Settings()
        {
            string Data = (string)ActiveModule.ModuleSettings["data"];
            JObject json = new JObject();
            try
            {
                string path = Path.GetDirectoryName(HostingEnvironment.MapPath("~/" + ActiveModule.ModuleControl.ControlSrc)) + "\\";
                string Template = "settings-schema.json";
                string schemaFilename = path + Template;

                JObject schemaJson = GetJsonFromFile(schemaFilename);
                json["schema"] = schemaJson;
                string optionsFilename = path + "settings-options." + DnnUtils.GetCurrentCultureCode() + ".json";
                if (!File.Exists(optionsFilename))
                {
                    optionsFilename = path + "settings-options.json";
                }
                if (File.Exists(optionsFilename))
                {
                    JObject optionsJson = GetJsonFromFile(optionsFilename);
                    json["options"] = optionsJson;
                }

                if (!String.IsNullOrEmpty(Data))
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
        public HttpResponseMessage Submit(JObject form)
        {
            try
            {
                int moduleId = ActiveModule.ModuleID;
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
                var res = new ResultDTO()
                {
                    Message = "Form submitted."
                };

                string jsonSettings = ActiveModule.ModuleSettings["data"] as string;
                if (!String.IsNullOrEmpty(jsonSettings))
                {
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);
                    HandlebarsEngine hbs = new HandlebarsEngine();
                    dynamic data = null;
                    string formData = "";
                    if (form != null)
                    {
                        if (!String.IsNullOrEmpty(settings.Settings.SiteKey))
                        {
                            Recaptcha recaptcha = new Recaptcha(settings.Settings.SiteKey, settings.Settings.SecretKey);
                            RecaptchaValidationResult validationResult = recaptcha.Validate(form["recaptcha"].ToString());
                            if (!validationResult.Succeeded)
                            {
                                return Request.CreateResponse(HttpStatusCode.Forbidden);
                            }
                            form.Remove("recaptcha");
                        }


                        string template = (string)ActiveModule.ModuleSettings["template"];
                        string templateFilename = HostingEnvironment.MapPath("~/" + template);
                        string schemaFilename = Path.GetDirectoryName(templateFilename) + "\\" + "schema.json";
                        JObject schemaJson = GetJsonFromFile(schemaFilename);
                        //form["schema"] = schemaJson;
                        // default options
                        string optionsFilename = Path.GetDirectoryName(templateFilename) + "\\" + "options.json";
                        JObject optionsJson = null;
                        if (File.Exists(optionsFilename))
                        {
                            string fileContent = File.ReadAllText(optionsFilename);
                            if (!String.IsNullOrWhiteSpace(fileContent))
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
                            if (!String.IsNullOrWhiteSpace(fileContent))
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
                                MailAddress from = GenerateMailAddress(notification.From, notification.FromEmail, notification.FromName, notification.FromEmailField, notification.FromNameField, form);
                                MailAddress to = GenerateMailAddress(notification.To, notification.ToEmail, notification.ToName, notification.ToEmailField, notification.ToNameField, form);
                                MailAddress reply = null;
                                if (!String.IsNullOrEmpty(notification.ReplyTo))
                                {
                                    reply = GenerateMailAddress(notification.ReplyTo, notification.ReplyToEmail, notification.ReplyToName, notification.ReplyToEmailField, notification.ReplyToNameField, form);
                                }
                                string body = formData;
                                if (!String.IsNullOrEmpty(notification.EmailBody))
                                {

                                    body = hbs.Execute(notification.EmailBody, data);
                                }

                                string send = SendMail(from.ToString(), to.ToString(), (reply == null ? "" : reply.ToString()), notification.EmailSubject, body);
                                if (!String.IsNullOrEmpty(send))
                                {
                                    res.Errors.Add("From:" + from.ToString() + " - To:" + to.ToString() + " - " + send);
                                }
                            }
                            catch (Exception exc)
                            {
                                res.Errors.Add("Notification " + (settings.Notifications.IndexOf(notification) + 1) + " : " + exc.Message + " - " + (UserInfo.IsSuperUser ? exc.StackTrace : ""));
                                Log.Logger.Error(exc);
                            }
                        }
                    }
                    if (settings != null && settings.Settings != null)
                    {
                        if (!String.IsNullOrEmpty(settings.Settings.Message))
                        {
                            res.Message = hbs.Execute(settings.Settings.Message, data);
                        }
                        else
                        {
                            res.Message = "Message sended.";
                        }
                        res.Tracking = settings.Settings.Tracking;
                        if (!String.IsNullOrEmpty(settings.Settings.Tracking))
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
                if (!String.IsNullOrEmpty(template))
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

        private static string GetProperty(JObject obj, string propertyName)
        {
            string PropertyValue = "";
            var property = obj.Children<JProperty>().SingleOrDefault(p => p.Name.ToLower() == propertyName.ToLower());
            if (property != null)
            {
                PropertyValue = property.Value.ToString();
            }
            return PropertyValue;
        }

        private MailAddress GenerateMailAddress(string typeOfAddress, string email, string name, string formEmailField, string formNameField, JObject form)
        {
            MailAddress adr = null;

            if (typeOfAddress == "host")
            {
                if (Validate.IsValidEmail(Host.HostEmail))
                    adr = new MailAddress(Host.HostEmail, Host.HostTitle);
            }
            else if (typeOfAddress == "admin")
            {
                var user = UserController.GetUserById(PortalSettings.PortalId, PortalSettings.AdministratorId);
                if (Validate.IsValidEmail(user.Email))
                    adr = new MailAddress(user.Email, user.DisplayName);
            }
            else if (typeOfAddress == "form")
            {
                if (String.IsNullOrEmpty(formNameField))
                    formNameField = "name";
                if (String.IsNullOrEmpty(formEmailField))
                    formEmailField = "email";

                string formEmail = GetProperty(form, formEmailField);
                string formName = GetProperty(form, formNameField);
                if (Validate.IsValidEmail(formEmail))
                    adr = new MailAddress(formEmail, formName);
            }
            else if (typeOfAddress == "custom")
            {
                if (Validate.IsValidEmail(email))
                    adr = new MailAddress(email, name);
            }
            else if (typeOfAddress == "current")
            {
                if (UserInfo == null)
                    throw new Exception($"Can't send email to current user, as there is no current user. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");
                if (Validate.IsValidEmail(UserInfo.Email))
                    throw new Exception($"Can't send email to current user, as email address of current user is unknown. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");

                adr = new MailAddress(UserInfo.Email, UserInfo.DisplayName);
            }

            if (adr == null)
            {
                throw new Exception($"Can't determine email address. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");
            }

            return adr;
        }

        private static JObject GetJsonFromFile(string filename)
        {
            JObject retval;
            try
            {
                retval = JObject.Parse(File.ReadAllText(filename));
            }
            catch (Exception ex)
            {
                throw new InvalidJsonFileException($"Invalid json in file {filename}", ex, filename);
            }
            return retval;
        }

        private static string SendMail(string mailFrom, string mailTo, string replyTo, string subject, string body)
        {

            //string mailFrom
            //string mailTo, 
            string cc = "";
            string bcc = "";
            //string replyTo, 
            MailPriority priority = MailPriority.Normal;
            //string subject, 
            MailFormat bodyFormat = MailFormat.Html;
            Encoding bodyEncoding = Encoding.UTF8;
            //string body, 
            List<Attachment> attachments = new List<Attachment>();
            string smtpServer = Host.SMTPServer;
            string smtpAuthentication = Host.SMTPAuthentication;
            string smtpUsername = Host.SMTPUsername;
            string smtpPassword = Host.SMTPPassword;
            bool smtpEnableSSL = Host.EnableSMTPSSL;

            string res = Mail.SendMail(mailFrom,
                            mailTo,
                            cc,
                            bcc,
                            replyTo,
                            priority,
                            subject,
                            bodyFormat,
                            bodyEncoding,
                            body,
                            attachments,
                            smtpServer,
                            smtpAuthentication,
                            smtpUsername,
                            smtpPassword,
                            smtpEnableSSL);

            //Mail.SendEmail(replyTo, mailFrom, mailTo, subject, body);
            return res;
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
    }
}

