using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Net;
using System.Web.Script.Serialization;

namespace RecaptchaV3.NET
{
    public class Recaptcha
    {
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }

        public Recaptcha()
        {
            SiteKey = ConfigurationManager.AppSettings["GoogleRecaptchaSiteKey"];
            SecretKey = ConfigurationManager.AppSettings["GoogleRecaptchaSecretKey"];
        }

        /// <summary>
        /// Returns the HTML script needed for reCAPTCHA v3
        /// </summary>
        public string GetRecaptchaScript(string actionName = "submit")
        {
            return $@"
                <script src=""https://www.google.com/recaptcha/api.js?render={SiteKey}""></script>
                <script>
                grecaptcha.ready(function() {{
                    grecaptcha.execute('{SiteKey}', {{action: '{actionName}'}}).then(function(token) {{
                        var recaptchaResponse = document.getElementById('g-recaptcha-response');
                        if (recaptchaResponse) {{
                            recaptchaResponse.value = token;
                        }} else {{
                            var input = document.createElement('input');
                            input.type = 'hidden';
                            input.name = 'g-recaptcha-response';
                            input.id = 'g-recaptcha-response';
                            input.value = token;
                            document.forms[0].appendChild(input);
                        }}
                    }});
                }});
                </script>";
        }

        /// <summary>
        /// Verifies the reCAPTCHA v3 token server-side
        /// </summary>
        public RecaptchaValidationResult Validate(string token, string remoteIp = null)
        {
            var result = new RecaptchaValidationResult();
            if (string.IsNullOrEmpty(token))
            {
                result.Succeeded = false;
                result.ErrorMessages.Add("Missing reCAPTCHA token.");
                return result;
            }

            string url = "https://www.google.com/recaptcha/api/siteverify";
            string postData = $"secret={SecretKey}&response={token}";
            if (!string.IsNullOrEmpty(remoteIp))
                postData += $"&remoteip={remoteIp}";

            var request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.ContentType = "application/x-www-form-urlencoded";
            using (var streamWriter = new StreamWriter(request.GetRequestStream()))
            {
                streamWriter.Write(postData);
            }

            string jsonResponse;
            using (var response = request.GetResponse())
            using (var reader = new StreamReader(response.GetResponseStream()))
            {
                jsonResponse = reader.ReadToEnd();
            }

            var js = new JavaScriptSerializer();
            var apiResponse = js.Deserialize<RecaptchaApiResponse>(jsonResponse);

            result.Succeeded = apiResponse.success && apiResponse.score >= 0.5;
            result.Score = apiResponse.score;
            result.Action = apiResponse.action;
            result.ErrorMessages = apiResponse.error_codes ?? new List<string>();

            return result;
        }
    }

    public class RecaptchaApiResponse
    {
        public bool success { get; set; }
        public DateTime challenge_ts { get; set; }
        public string hostname { get; set; }
        public string action { get; set; }
        public float score { get; set; }
        public List<string> error_codes { get; set; }
    }

    public class RecaptchaValidationResult
    {
        public bool Succeeded { get; set; }
        public float Score { get; set; }
        public string Action { get; set; }
        public List<string> ErrorMessages { get; set; }

        public RecaptchaValidationResult()
        {
            ErrorMessages = new List<string>();
        }
    }
}
