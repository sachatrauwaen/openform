using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using DotNetNuke.Entities.Host;
using DotNetNuke.Entities.Portals;
using DotNetNuke.Entities.Users;
using DotNetNuke.Services.Mail;
using Newtonsoft.Json.Linq;

namespace Satrabel.OpenForm.Components
{
    public static class FormUtils
    {
        internal static MailAddress GenerateMailAddress(string typeOfAddress, string email, string name, string formEmailField, string formNameField, JObject form)
        {
            MailAddress adr = null;
            var portalSettings = PortalSettings.Current;

            if (typeOfAddress == "host")
            {
                adr = GenerateMailAddress(Host.HostEmail, Host.HostTitle);
            }
            else if (typeOfAddress == "admin")
            {
                var user = UserController.GetUserById(portalSettings.PortalId, portalSettings.AdministratorId);
                adr = GenerateMailAddress(user.Email, user.DisplayName);
            }
            else if (typeOfAddress == "form")
            {
                if (string.IsNullOrEmpty(formNameField))
                    formNameField = "name";
                if (string.IsNullOrEmpty(formEmailField))
                    formEmailField = "email";

                string formEmail = GetProperty(form, formEmailField);
                string formName = GetProperty(form, formNameField);
                adr = GenerateMailAddress(formEmail, formName);
            }
            else if (typeOfAddress == "custom")
            {
                adr = GenerateMailAddress(email, name);
            }
            else if (typeOfAddress == "current")
            {
                var userInfo = portalSettings.UserInfo;
                if (userInfo == null)
                    throw new Exception($"Can't send email to current user, as there is no current user. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");

                adr = GenerateMailAddress(userInfo.Email, userInfo.DisplayName);
                if (adr == null)
                    throw new Exception($"Can't send email to current user, as email address of current user is unknown. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");
            }

            if (adr == null)
            {
                throw new Exception($"Can't determine email address. Parameters were TypeOfAddress: [{typeOfAddress}], Email: [{email}], Name: [{name}], FormEmailField: [{formEmailField}], FormNameField: [{formNameField}], FormNameField: [{form}]");
            }

            return adr;
        }

        private static MailAddress GenerateMailAddress(string email, string title)
        {
            email = email.Trim(); //normalize email

            return IsValidEmail(email) ? new MailAddress(email, title) : null;
        }

        private static string GetProperty(JObject obj, string propertyName)
        {
            string propertyValue = "";
            var property = obj.Children<JProperty>().SingleOrDefault(p => p.Name.ToLower() == propertyName.ToLower());
            if (property != null)
            {
                propertyValue = property.Value.ToString();
            }
            return propertyValue;
        }

        /// <summary>
        /// Determines whether email is valid.
        /// </summary>
        /// <remarks>
        /// https://technet.microsoft.com/nl-be/library/01escwtf(v=vs.110).aspx
        /// </remarks>
        public static bool IsValidEmail(string strIn)
        {
            if (string.IsNullOrEmpty(strIn)) return false;

            bool invalid = false;

            // Use IdnMapping class to convert Unicode domain names.
            try
            {
                strIn = Regex.Replace(strIn, @"(@)(.+)$", DomainMapper);
            }
            catch (Exception e)
            {
                invalid = true;
            }

            if (invalid)
                return false;

            // Return true if strIn is in valid e-mail format.
            return Regex.IsMatch(strIn,
                @"^(?("")(""[^""]+?""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))" +
                @"(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9]{2,17}))$",
                RegexOptions.IgnoreCase);
        }

        private static string DomainMapper(Match match)
        {
            // IdnMapping class with default property values.
            IdnMapping idn = new IdnMapping();
            string domainName = match.Groups[2].Value;
            domainName = idn.GetAscii(domainName);
            return match.Groups[1].Value + domainName;
        }
        public static string SendMail(string mailFrom, string mailTo, string replyTo, string subject, string body, List<Attachment> attachments = null)
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
            //List<Attachment> attachments = new List<Attachment>();
            if (attachments == null) attachments = new List<Attachment>();
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
        
}