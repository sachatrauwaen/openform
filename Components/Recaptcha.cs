using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Security.Cryptography;
using System.Text;
using System.Web;
using System.Web.Script.Serialization;

namespace RecaptchaV2.NET
{
    /// <summary>
    /// Helper Methods for the Google Recaptcha V2 Library
    /// </summary>
    /// <remarks>
    /// 8/4/2015 - Matt Olson (www.keelio.com): Initial Creation
    /// </remarks>
    public class Recaptcha
    {
        public string SiteKey { get; set; }
        public string SecretKey { get; set; }
        public Guid SessionId { get; set; }

        /// <summary>
        /// Recaptcha constructor with passed in site key and secret key
        /// </summary>
        /// <param name="siteKey">Google Recaptcha Site Key</param>
        /// <param name="secretKey">Google Recaptcha Secret Key</param>
        public Recaptcha(string siteKey, string secretKey)
        {
            SessionId = Guid.NewGuid();
            SiteKey = siteKey;
            SecretKey = secretKey;
        }

        /// <summary>
        /// Default constructor loads the site key and secret key from configuration file. The AppSettings keys are GoogleRecaptchaSiteKey and GoogleRecaptchaSecretKey
        /// </summary>
        /// <remarks>
        /// 8/4/2015 - MRO: Initial Creation
        /// </remarks>
        public Recaptcha()
        {
            SessionId = Guid.NewGuid();

            SiteKey = ConfigurationManager.AppSettings["GoogleRecaptchaSiteKey"];
            SecretKey = ConfigurationManager.AppSettings["GoogleRecaptchaSecretKey"];
        }

        /// <summary>
        /// Gets the secure token version of the recaptcha HTML that must be injected onto the HTML page
        /// </summary>
        /// <returns></returns>
        public string GetSecureTokenHTML()
        {
            string result = string.Empty;
            string jsonToken = GetJsonToken();
            //Test token with known result:
            //SecretKey = "6Lc0MgoTAAAAAAXFM388zn66iPtjOdQgREfZAgqZ";
            //jsonToken = @"{""session_id"":""1"",""ts_ms"":1437712654577}";
            //Known result: XlPyYFtyfzmsf5rnRIzyuZ4MZo5GoCSxNcI_wAeOqb18zCxhSM5cYxU8fFerrdcC
            string base64SecureToken = EncryptJsonToken(jsonToken);

            result = @"<div class=""g-recaptcha"" data-sitekey=""{0}"" data-stoken=""{1}""></div>";
            result = string.Format(result, SiteKey, base64SecureToken);
            return result;
        }

        /// <summary>
        /// Gets the non-secure token version (site specific) version of the recaptcha code
        /// </summary>
        /// <returns></returns>
        public string GetSiteSpecificRecaptchaHTML()
        {
            return string.Format(@"<div class=""g-recaptcha"" data-sitekey=""{0}""></div>", SiteKey);
        }

        /// <summary>
        /// Encrypts the plain text (JSON string) in the format Googles recaptcha expects. Must be ECB and PKCS7 (aka PKCS5) padding. 
        /// </summary>
        /// <param name="plainText">Plain text (aka JSON string google will validate)</param>
        /// <param name="Key">First 16 bytes of a SHA1 encoded Google recaptcha SecretKey</param>
        /// <param name="IV">First 16 bytes of a SHA1 encoded Google recaptcha SecretKey</param>
        /// <returns></returns>
        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments. 
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;
            // Create an AesManaged object 
            // with the specified key and IV. 
            using (AesManaged aesAlg = new AesManaged())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;
                aesAlg.Padding = PaddingMode.PKCS7;
                aesAlg.Mode = CipherMode.ECB;

                // Create a decrytor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption. 
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {

                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }
            // Return the encrypted bytes from the memory stream. 
            return encrypted;
        }

        private string EncryptJsonToken(string jsonToken)
        {
            byte[] encrypted = EncryptStringToBytes_Aes(jsonToken, getKey(SecretKey), getKey(SecretKey));

            //Base64 encode the encrypted data
            //Also applys the URL variant of base64 encoding, unfortunately the HttpServerUtility.UrlTokenEncode(encrypted) seems to truncate the last value from the string so we can't use it?
            return Convert.ToBase64String(encrypted, Base64FormattingOptions.None).Replace("=", String.Empty).Replace('+', '-').Replace('/', '_');
        }

        /// <summary>
        /// Gets the first 16 bytes of the SHA1 version of the SecretKey. (This is not documented ANYWHERE on googles dev site, you have to READ the java code to figure this out!!!!)
        /// </summary>
        /// <param name="secretKey">Googles recaptcha SecretKey</param>
        /// <returns>First 16 bytes of the SHA1 hash of the SecretKey</returns>
        private byte[] getKey(string secretKey)
        {
            SHA1 sha = SHA1.Create();
            byte[] dataToHash = Encoding.UTF8.GetBytes(secretKey);
            byte[] shaHash = sha.ComputeHash(dataToHash);
            byte[] first16OfHash = new byte[16];
            Array.Copy(shaHash, first16OfHash, 16);
            return first16OfHash;
        }

        //Could use this method instead of AesManaged
        //private RijndaelManaged GetRijndaelManaged(String secretKey)
        //{
        //  var keyBytes = getKey(secretKey);
        //  //var secretKeyBytes = Encoding.UTF8.GetBytes(secretKey);
        //  //Array.Copy(secretKeyBytes, keyBytes, Math.Min(keyBytes.Length, secretKeyBytes.Length));
        //  RijndaelManaged encryption = new RijndaelManaged
        //  {
        //    Mode = CipherMode.ECB,
        //    FeedbackSize = 128,
        //    Padding = PaddingMode.PKCS7, //PKCS5 and PKCS7 are apparently the same
        //    KeySize = 128,
        //    BlockSize = 128,
        //    Key = keyBytes,
        //    IV = keyBytes
        //  };
        //  return encryption;
        //}

        public string GetJsonToken()
        {
            //Example: {"session_id": e6e9c56e-a7da-43b8-89fa-8e668cc0b86f,"ts_ms":1421774317718}
            string jsonRequest = "{" + string.Format("\"session_id\": {0},\"ts_ms\":{1}", SessionId, CurrentTimeMillis()) + "}";
            return jsonRequest;
        }

        private static readonly DateTime Jan1st1970 = new DateTime
        (1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        /// <summary>
        /// Implementation of Java's currentTimeMillis
        /// </summary>
        /// <returns></returns>
        public static long CurrentTimeMillis()
        {
            return (long)(DateTime.UtcNow - Jan1st1970).TotalMilliseconds;
        }

        /// <summary>
        /// Validates a Recaptcha V2 response.
        /// </summary>
        /// <param name="recaptchaResponse">g-recaptcha-response form response variable (HttpContext.Current.Request.Form["g-recaptcha-response"])</param>
        /// <returns>RecaptchaValidationResult</returns>
        /// <remarks>
        /// 8/4/2015 - Matt Olson: Initial creation.
        /// </remarks>
        public RecaptchaValidationResult Validate(string recaptchaResponse)
        {
            RecaptchaValidationResult result = new RecaptchaValidationResult();

            HttpWebRequest req = (HttpWebRequest)WebRequest.Create("https://www.google.com/recaptcha/api/siteverify?secret=" + SecretKey + "&response="
              + recaptchaResponse + "&remoteip=" + GetClientIp());
            //Google recaptcha Response
            using (WebResponse wResponse = req.GetResponse())
            {
                using (StreamReader readStream = new StreamReader(wResponse.GetResponseStream()))
                {
                    string jsonResponse = readStream.ReadToEnd();

                    JavaScriptSerializer js = new JavaScriptSerializer();
                    result = js.Deserialize<RecaptchaValidationResult>(jsonResponse.Replace("error-codes", "ErrorMessages").Replace("success", "Succeeded"));// Deserialize Json
                }
            }

            return result;
        }

        private string GetClientIp()
        {
            // Look for a proxy address first
            String _ip = HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"];

            // If there is no proxy, get the standard remote address
            if (string.IsNullOrWhiteSpace(_ip) || _ip.ToLower() == "unknown")
                _ip = HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"];

            return _ip;
        }
    }

    public class RecaptchaValidationResult
    {
        public RecaptchaValidationResult()
        {
            ErrorMessages = new List<string>();
            Succeeded = false;
        }

        public List<string> ErrorMessages { get; set; }
        public bool Succeeded { get; set; }

        public string GetErrorMessagesString()
        {
            return string.Join("<br/>", ErrorMessages.ToArray());
        }
    }
}