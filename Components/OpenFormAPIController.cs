#region Copyright

// 
// Copyright (c) 2015
// by Satrabel
// 

#endregion

#region Using Statements

using System;
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
                    string optionsFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "options." + PortalSettings.CultureCode + ".json";
                    if (!File.Exists(optionsFilename))
                    {
                        optionsFilename = Path.GetDirectoryName(TemplateFilename) + "\\" + "options.json";
                    }
                    if (File.Exists(optionsFilename))
                    {
                        JObject optionsJson = JObject.Parse(File.ReadAllText(optionsFilename));
                        json["options"] = optionsJson;
                    }
                    int ModuleId = ActiveModule.ModuleID;
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
            var res = new ResultDTO()
            {
                Message = "Form sunmitted."
            };
            string FormEmail = (form != null && form["email"] == null) ? "" : form["email"].ToString();
            string FormName = (form != null && form["name"] == null) ? "" : form["name"].ToString();

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
                        MailAddress from = GenerateMailAddress(notification.From, notification.FromEmail, notification.FromName, FormEmail, FormName);
                        MailAddress to = GenerateMailAddress(notification.To, notification.ToEmail, notification.ToName, FormEmail, FormName);
                        MailAddress reply = null;
                        if (!string.IsNullOrEmpty(notification.ReplyToEmail))
                        {
                            reply = new MailAddress(notification.ReplyToEmail, notification.ReplyToName);
                        }
                        string body = FormData.ToString();
                        if (!string.IsNullOrEmpty(notification.EmailBody))
                        {
                            body = hbs.Execute(notification.EmailBody, form);
                        }
                        SendMail(from.ToString(), to.ToString(), (reply == null ? "" : reply.ToString()), notification.EmailSubject, body);
                    }
                }
                if (settings != null && settings.Settings != null)
                {

                    res.Message = hbs.Execute(settings.Settings.Message, form);
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

        private MailAddress GenerateMailAddress(string TypeOfAddress, string Email, string Name, string FormEmail, string FormName)
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
                adr = new MailAddress(FormEmail, FormName);
            }
            else if (TypeOfAddress == "custom")
            {
                adr = new MailAddress(Email, Name);
            }
            return adr;
        }
        private void SendMail(string mailFrom, string mailTo, string replyTo, string subject, string body)
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

        }
    }

    public class NotificationDTO
    {
        public string From { get; set; }
        public string FromName { get; set; }
        public string FromEmail { get; set; }
        public string To { get; set; }
        public string ToName { get; set; }
        public string ToEmail { get; set; }
        public string ReplyTo { get; set; }
        public string ReplyToName { get; set; }
        public string ReplyToEmail { get; set; }
        public string EmailSubject { get; set; }
        public string EmailBody { get; set; }
    }
    public class GenSettingsDTO
    {
        public string Message { get; set; }
    }

    public class SettingsDTO
    {
        public List<NotificationDTO> Notifications { get; set; }
        public GenSettingsDTO Settings { get; set; }
    }
    class ResultDTO
    {
        public string Message { get; set; }
    }
}

