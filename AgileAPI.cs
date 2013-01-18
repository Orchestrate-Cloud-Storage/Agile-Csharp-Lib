namespace AgileAPI
{
    #region Imports

    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Text;
    using System.Web.Services.Protocols;
    using Jayrock.Json;
    using Jayrock.Json.Conversion;
    using System.Globalization;

    #endregion

    public class AgileAPIClient : HttpWebClientProtocol
    {
        private int _id;
        private string token;
        private string Ousername;
        private string Opassword;
        public int maxAuthRetries;
        public Dictionary<string, string> ContentTypeMappings;

        public AgileAPIClient()
        {
            ContentTypeMappings = new Dictionary<string, string> {
                {".fif", "application/fractals"},
                {".hta", "application/hta"},
                {".hqx", "application/mac-binhex40"},
                {".vsi", "application/ms-vsi"},
                {".p10", "application/pkcs10"},
                {".p7m", "application/pkcs7-mime"},
                {".p7s", "application/pkcs7-signature"},
                {".cer", "application/pkix-cert"},
                {".crl", "application/pkix-crl"},
                {".ps", "application/postscript"},
                {".setpay", "application/set-payment-initiation"},
                {".setreg", "application/set-registration-initiation"},
                {".sst", "application/vnd.ms-pki.certstore"},
                {".pko", "application/vnd.ms-pki.pko"},
                {".cat", "application/vnd.ms-pki.seccat"},
                {".stl", "application/vnd.ms-pki.stl"},
                {".wpl", "application/vnd.ms-wpl"},
                {".xps", "application/vnd.ms-xpsdocument"},
                {".z", "application/x-compress"},
                {".tgz", "application/x-compressed"},
                {".gz", "application/x-gzip"},
                {".ins", "application/x-internet-signup"},
                {".iii", "application/x-iphone"},
                {".jtx", "application/x-jtx+xps"},
                {".latex", "application/x-latex"},
                {".nix", "application/x-mix-transfer"},
                {".asx", "application/x-mplayer2"},
                {".application", "application/x-ms-application"},
                {".wmd", "application/x-ms-wmd"},
                {".wmz", "application/x-ms-wmz"},
                {".xbap", "application/x-ms-xbap"},
                {".p12", "application/x-pkcs12"},
                {".p7b", "application/x-pkcs7-certificates"},
                {".p7r", "application/x-pkcs7-certreqresp"},
                {".sit", "application/x-stuffit"},
                {".tar", "application/x-tar"},
                {".man", "application/x-troff-man"},
                {".xaml", "application/xaml+xml"},
                {".aiff", "audio/aiff"},
                {".au", "audio/basic"},
                {".mid", "audio/midi"},
                {".mp3", "audio/mp3"},
                {".m3u", "audio/mpegurl"},
                {".wav", "audio/wav"},
                {".wax", "audio/x-ms-wax"},
                {".wma", "audio/x-ms-wma"},
                {".bmp", "image/bmp"},
                {".gif", "image/gif"},
                {".jpg", "image/jpeg"},
                {".png", "image/png"},
                {".tiff", "image/tiff"},
                {".ico", "image/x-icon"},
                {".dwfx", "model/vnd.dwfx+xps"},
                {".css", "text/css"},
                {".323", "text/h323"},
                {".htm", "text/html"},
                {".uls", "text/iuls"},
                {".txt", "text/plain"},
                {".wsc", "text/scriptlet"},
                {".htt", "text/webviewhtml"},
                {".htc", "text/x-component"},
                {".vcf", "text/x-vcard"},
                {".xml", "text/xml"},
                {".avi", "video/avi"},
                {".mpeg", "video/mpeg"},
                {".wm", "video/x-ms-wm"},
                {".wmv", "video/x-ms-wmv"},
                {".wmx", "video/x-ms-wmx"},
                {".wvx", "video/x-ms-wvx"}
            };
            maxAuthRetries = 10;
        }

        public virtual object Invoke(string method, params object[] args)
        {
            WebRequest request = GetWebRequest(new Uri(Url + "/jsonrpc"));
            request.Method = "POST";
            
            using (Stream stream = request.GetRequestStream())
            using (StreamWriter writer = new StreamWriter(stream))
            {
                JsonObject call = new JsonObject();
                call["id"] = ++_id;
                call["method"] = method;
                call["params"] = args;
                call.Export(new JsonTextWriter(writer));
            }
           
            using (WebResponse response = GetWebResponse(request))
            using (Stream stream = response.GetResponseStream())
            using (StreamReader reader = new StreamReader(stream, Encoding.UTF8))
            {
                JsonObject answer = new JsonObject();
                answer.Import(new JsonTextReader(reader));

                object errorObject = answer["error"];
            
                if (errorObject != null)
                    OnError(errorObject);
            
                return answer["result"];
            }
        }

        protected virtual object AuthCatch(string method, params object[] args)
        {
            for(int i = 0; i < maxAuthRetries; i++){
                try {
                    object jsonresponse = Invoke(method, token, args);
                    return jsonresponse;
                }
                catch (ApiException e)
                {
                    if (e.code.Equals("2"))
                    {
                        Authenticate(Ousername, Opassword);
                    }
                    else
                    {
                        throw new ApiException("1", "Authentication failed.");
                    }
                }
            }
            throw new ApiException("3", "Unable to Update Reauth.");
        }

        protected virtual void OnError(object errorObject) 
        {
            JsonObject error = errorObject as JsonObject;

            if (error != null)
            {
                Console.WriteLine("AAAAAA");
                Console.WriteLine("AA" + error["message"] + "AA");
                if (error["message"].Equals("Server Exception :: Authentication required"))
                {
                    Console.WriteLine("I am finally here");
                    throw new ApiException("2", "Authentication required");
                }
                throw new Exception(error["message"] as string);
            }
            if (((string)errorObject).Equals("Server Exception :: Authentication required"))
            {
                throw new ApiException("2", "Authentication required");
            }
            Console.WriteLine("BBBBBBB");
            Console.WriteLine(errorObject);
            throw new Exception(errorObject as string);
        }

        public virtual void Authenticate(string username, string password)
        {
            this.Opassword = password;
            this.Ousername = username;
            JsonArray foo = (JsonArray) Invoke("login", username, password, "TRUE");
            Console.WriteLine("authe: " + foo);
            if (foo[0] == null)
            {
                throw new ApiException("1", "Authentication Failed");
            }
            token = (string)foo[0];
        }

        public virtual void logout()
        {
            Invoke("logout", token);
        }

        public virtual object noop(string operation)
        {
            return Invoke("noop", token, operation);
        }

        public virtual object listDir(string path)
        {
            JsonObject listDir = (JsonObject)Invoke("listDir", token, path);
            if (Convert.ToInt32(listDir["code"]) == -1)
            {
                throw new ApiException("-1", "Path Not Found");
            }
            return listDir;
        }

        public virtual object listFile(string path)
        {
            try
            {

                JsonObject listFile = (JsonObject)Invoke("listFile", token, path);
                if (Convert.ToInt32(listFile["code"]) == -1)
                {
                    throw new ApiException("-1", "Path Not Found");
                }
                return listFile;
            }
            catch (ApiException e)
            {
                if (e.code.Equals("2"))
                {
                    Authenticate(Ousername, Opassword);
                    return listFile(path);
                }
                throw new ApiException("1", "Authentication failed.");
            }

        }

        public virtual object stat(string path, bool raiseOnNotFound = false)
        {
                JsonObject statreturn = (JsonObject)AuthCatch("stat", path);
                
                if ((Convert.ToInt32(statreturn["code"]) == -1) && (raiseOnNotFound))
                {
                    throw new ApiException("-1", "Object Not Found");
                }
                return statreturn;   
        }

        public virtual object makeDir(string path)
        {
            return Invoke("makeDir", token, path);
        }

        public virtual object makeDir2(string path)
        {
            return Invoke("makeDir2", token, path);
        }

        public virtual object deleteFile(string path)
        {
            return Invoke("deleteFile", token, path);
        }

        public virtual object deleteObject(string path)
        {
            return Invoke("deleteObject", token, path);
        }

        public virtual object rename(string oldpath, string newpath)
        {
            return Invoke("rename", token, oldpath, newpath);
        }

        public virtual object copyFile(string path, string newpath)
        {
            return Invoke("copyFile", token, path, newpath);
        }

        public virtual object setMTime(string path, string mtime)
        {
            return Invoke("setMTime", token, path, mtime);
        }

        public virtual byte[] upload(string myfile, string mydirectory)
        {
            string uploadurl = Url + "/post/file";  
            var request = WebRequest.Create(uploadurl);
            request.Method = "POST";
            request.Headers.Set("X-Agile-Authorization", token);
            var boundary = "---------------------------" + DateTime.Now.Ticks.ToString("x", NumberFormatInfo.InvariantInfo);
            request.ContentType = "multipart/form-data; boundary=" + boundary;
            boundary = "--" + boundary;

            using (var requestStream = request.GetRequestStream())
            {
               
                //directory
                var buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"directory\"{0}{0}", Environment.NewLine));
                requestStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.UTF8.GetBytes(mydirectory + Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);
            
                //basename
                var basename = Path.GetFileName(myfile);
                buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.ASCII.GetBytes(string.Format("Content-Disposition: form-data; name=\"basename\"{0}{0}", Environment.NewLine));
                requestStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.UTF8.GetBytes(basename + Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);

                //send file contents
                buffer = Encoding.ASCII.GetBytes(boundary + Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);
                buffer = Encoding.UTF8.GetBytes(string.Format("Content-Disposition: form-data; name=\"uploadFile\"; filename=\"{0}\"{1}", myfile, Environment.NewLine));
                requestStream.Write(buffer, 0, buffer.Length);
                //Need to set properly for extension types.
                var extension = Path.GetExtension(myfile);
                Console.WriteLine(extension);
                string MyContentType = "octet/stream";
                try
                {
                    MyContentType = ContentTypeMappings[extension];
                }
                catch
                {
                    MyContentType = "octet/stream";
                }
                buffer = Encoding.ASCII.GetBytes(string.Format("Content-Type: {0}{1}{1}", MyContentType, Environment.NewLine));
                requestStream.Write(buffer, 0, buffer.Length);
                using (FileStream myStream = File.OpenRead(myfile))
                {
                    myStream.CopyTo(requestStream);
                }
                buffer = Encoding.ASCII.GetBytes(Environment.NewLine);
                requestStream.Write(buffer, 0, buffer.Length);

                var boundaryBuffer = Encoding.ASCII.GetBytes(boundary + "--");
                requestStream.Write(boundaryBuffer, 0, boundaryBuffer.Length);
            }

            using (var response = request.GetResponse())
            using (var responseStream = response.GetResponseStream())
            using (var stream = new MemoryStream())
            {
                responseStream.CopyTo(stream);
                return stream.ToArray();
            }
            

        }

    }

    [Serializable]
    public class ApiException : System.Exception
    {
        public string code;
        public string message;

        public ApiException(string code, string message)
        {
            this.code = code;
            this.message = message;

        }
    }


}
