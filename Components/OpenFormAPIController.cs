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
using DotNetNuke.Instrumentation;
using System.Collections.Generic;
using Newtonsoft.Json;
using DotNetNuke.Services.Mail;
using System.Net.Mail;
using System.Text;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Users;
using Satrabel.OpenForm.Components;
using Satrabel.OpenContent.Components.Json;
using Satrabel.OpenContent.Components.Handlebars;
using DotNetNuke.Common;
using DotNetNuke.Services.Localization;
using RecaptchaV2.NET;
using Satrabel.OpenContent.Components;
using Satrabel.OpenContent.Components.Logging;

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

                    JObject schemaJson = JsonUtils.GetJsonFromFile(schemaFilename);
                    json["schema"] = schemaJson;

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
                if (!string.IsNullOrEmpty(jsonSettings))
                {
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);
                    HandlebarsEngine hbs = new HandlebarsEngine();
                    dynamic data = null;
                    string formData = "";
                    if (form != null)
                    {
                        if (!string.IsNullOrEmpty(settings.Settings.SiteKey))
                        {
                            Recaptcha recaptcha = new Recaptcha(settings.Settings.SiteKey, settings.Settings.SecretKey);
                            RecaptchaValidationResult validationResult = recaptcha.Validate(form["recaptcha"].ToString());
                            if (!validationResult.Succeeded)
                            {
                                return Request.CreateResponse(HttpStatusCode.Forbidden);
                            }
                            form.Remove("recaptcha");
                        }

                        data = OpenFormUtils.GenerateFormData(form.ToString(), out formData);
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
                                if (!string.IsNullOrEmpty(notification.ReplyTo))
                                {
                                    reply = GenerateMailAddress(notification.ReplyTo, notification.ReplyToEmail, notification.ReplyToName, notification.ReplyToEmailField, notification.ReplyToNameField, form);
                                }
                                string body = formData;
                                if (!string.IsNullOrEmpty(notification.EmailBody))
                                {
                                    body = hbs.Execute(notification.EmailBody, data);
                                }

                                string send = SendMail(from.ToString(), to.ToString(), (reply == null ? "" : reply.ToString()), notification.EmailSubject, body);
                                if (!string.IsNullOrEmpty(send))
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
                        if (!string.IsNullOrEmpty(settings.Settings.Message))
                        {
                            res.Message = hbs.Execute(settings.Settings.Message, data);
                        }
                        else
                        {
                            res.Message = "Message sended.";
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
        private static string GetProperty(JObject obj, string PropertyName)
        {
            string PropertyValue = "";
            var Property = obj.Children<JProperty>().SingleOrDefault(p => p.Name.ToLower() == PropertyName.ToLower());
            if (Property != null)
            {
                PropertyValue = Property.Value.ToString();
            }
            return PropertyValue;
        }

        private MailAddress GenerateMailAddress(string TypeOfAddress, string Email, string Name, string FormEmailField, string FormNameField, JObject form)
        {
            MailAddress adr = null;

            if (TypeOfAddress == "host")
            {
                adr = new MailAddress(Host.HostEmail, Host.HostTitle);
            }
            else if (TypeOfAddress == "admin")
            {
                var user = UserController.GetUserById(PortalSettings.PortalId, PortalSettings.AdministratorId);
                adr = new MailAddress(user.Email, user.DisplayName);
            }
            else if (TypeOfAddress == "form")
            {
                if (string.IsNullOrEmpty(FormNameField))
                    FormNameField = "name";
                if (string.IsNullOrEmpty(FormEmailField))
                    FormEmailField = "email";

                string FormEmail = GetProperty(form, FormEmailField);
                string FormName = GetProperty(form, FormNameField);
                adr = new MailAddress(FormEmail, FormName);
            }
            else if (TypeOfAddress == "custom")
            {
                adr = new MailAddress(Email, Name);
            }
            else if (TypeOfAddress == "current")
            {
                if (UserInfo == null)
                    throw new Exception(string.Format("Can't send email to current user, as there is no current user. Parameters were TypeOfAddress: [{0}], Email: [{1}], Name: [{2}], FormEmailField: [{3}], FormNameField: [{4}], FormNameField: [{5}]", TypeOfAddress, Email, Name, FormEmailField, FormNameField, form));
                if (string.IsNullOrEmpty(UserInfo.Email))
                    throw new Exception(string.Format("Can't send email to current user, as email address of current user is unknown. Parameters were TypeOfAddress: [{0}], Email: [{1}], Name: [{2}], FormEmailField: [{3}], FormNameField: [{4}], FormNameField: [{5}]", TypeOfAddress, Email, Name, FormEmailField, FormNameField, form));

                adr = new MailAddress(UserInfo.Email, UserInfo.DisplayName);
            }

            if (adr == null)
            {
                throw new Exception(string.Format("Can't determine email address. Parameters were TypeOfAddress: [{0}], Email: [{1}], Name: [{2}], FormEmailField: [{3}], FormNameField: [{4}], FormNameField: [{5}]", TypeOfAddress, Email, Name, FormEmailField, FormNameField, form));
            }

            return adr;
        }
        private string SendMail(string mailFrom, string mailTo, string replyTo, string subject, string body)
        {

            //string mailFrom
            //string mailTo, 
            string cc = "";
            string bcc = "";
            //string replyTo, 
            DotNetNuke.Services.Mail.MailPriority priority = DotNetNuke.Services.Mail.MailPriority.Normal;
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

