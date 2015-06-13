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

#endregion

namespace Satrabel.OpenForm.Components
{
    [AllowAnonymous]
    public class OpenFormAPIController : DnnApiController
    {
        private static readonly ILog Logger = LoggerSource.Instance.GetLogger(typeof(OpenFormAPIController));
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
            string Template = (string)ActiveModule.ModuleSettings["template"];

            JObject json = new JObject();
            try
            {
                if (!string.IsNullOrEmpty(Template))
                {
                    string TemplateFilename = HostingEnvironment.MapPath(Template);
                    string schemaFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "schema.json";
                    JObject schemaJson = JObject.Parse(File.ReadAllText(schemaFilename));
                    json["schema"] = schemaJson;

                    // default options
                    string optionsFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "options.json";
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
                    optionsFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "options." + PortalSettings.CultureCode + ".json";
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
                    string viewFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "view.json";
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
                Logger.Error(exc);
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

                JObject schemaJson = JObject.Parse(File.ReadAllText(schemaFilename));
                json["schema"] = schemaJson;
                string optionsFilename = path + "settings-options." + PortalSettings.CultureCode + ".json";
                if (!File.Exists(optionsFilename))
                {
                    optionsFilename = path + "settings-options.json";
                }
                if (File.Exists(optionsFilename))
                {
                    JObject optionsJson = JObject.Parse(File.ReadAllText(optionsFilename));
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
                Logger.Error(exc);
                return Request.CreateErrorResponse(HttpStatusCode.InternalServerError, exc);
            }
        }
        public HttpResponseMessage Submit(JObject form)
        {
            try
            {
                var res = new ResultDTO()
                {
                    Message = "Form submitted."
                };

                int ModuleId = ActiveModule.ModuleID;
                string jsonSettings = ActiveModule.ModuleSettings["data"] as string;
                if (!string.IsNullOrEmpty(jsonSettings))
                {
                    HandlebarsEngine hbs = new HandlebarsEngine();
                    SettingsDTO settings = JsonConvert.DeserializeObject<SettingsDTO>(jsonSettings);
                    StringBuilder FormData = new StringBuilder();
                    if (form != null)
                    {
                        FormData.Append("<table boder=\"1\">");
                        foreach (var item in form.Properties())
                        {
                            FormData.Append("<tr>").Append("<td>").Append(item.Name).Append("</td>").Append("<td>").Append(" : ").Append("</td>").Append("<td>").Append(item.Value).Append("</td>").Append("</tr>");
                        }
                        FormData.Append("</table>");
                        //form["FormData"] = FormData.ToString();
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
                                string body = FormData.ToString();
                                if (!string.IsNullOrEmpty(notification.EmailBody))
                                {
                                    body = hbs.Execute(notification.EmailBody, form);
                                }

                                string send = SendMail(from.ToString(), to.ToString(), (reply == null ? "" : reply.ToString()), notification.EmailSubject, body);
                                if (!string.IsNullOrEmpty(send))
                                {
                                    res.Errors.Add("From:" + from.ToString() + " - To:" + to.ToString() + " - " + send);
                                }
                            }
                            catch (Exception exc)
                            {
                                res.Errors.Add("Notification "+(settings.Notifications.IndexOf(notification)+1)+ " : " + exc.Message + " - " + (UserInfo.IsSuperUser ? exc.StackTrace : ""));
                                Logger.Error(exc);
                            }
                        }
                    }
                    if (settings != null && settings.Settings != null)
                    {
                        res.Message = hbs.Execute(settings.Settings.Message, form);
                        res.Tracking = settings.Settings.Tracking;
                    }
                }
                OpenFormController ctrl = new OpenFormController();
                var content = new OpenFormInfo()
                {
                    ModuleId = ModuleId,
                    Json = form.ToString(),
                    CreatedByUserId = UserInfo.UserID,
                    CreatedOnDate = DateTime.Now,
                    LastModifiedByUserId = UserInfo.UserID,
                    LastModifiedOnDate = DateTime.Now,
                    Html = "",
                    Title = "Form submitted - " + DateTime.Now.ToString()
                };
                ctrl.AddContent(content);
                return Request.CreateResponse(HttpStatusCode.OK, res);
            }
            catch (Exception exc)
            {
                Logger.Error(exc);
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
    }
}

