/* Post multiple files and form values using .NET (console application)
 * https://stackoverflow.com/questions/9142804/post-multiple-files-and-form-values-using-net-console-application
 * 
 * This is a helper class for filling out http forms, this has been change to suit the my needs.
 * Original Question's link is above if you want to see the original class.
 */
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace VTOLVR_ModLoader.Classes
{
    public class HttpForm
    {
        private const string UserAgent = "VTOL VR Mod Loader";

        private string _method = string.Empty;
        private string _url = string.Empty;
        private string _token = string.Empty;

        private Dictionary<string, string> _files = new Dictionary<string, string>();
        private Dictionary<string, string> _values = new Dictionary<string, string>();

        public HttpForm(string url)
        {
            _url = url;
            _method = "POST";
        }

        public void SetToken(string token)
        {
            _token = token;
        }
        public void AttachFile(string field, string fileName)
        {
            _files[field] = fileName;
        }

        public void ResetForm()
        {
            _files.Clear();
            _values.Clear();
        }

        public void SetValue(string field, string value)
        {
            _values[field] = value;
        }
        public HttpWebRequest SubmitAsync(Action<IAsyncResult> action)
        {
            return UploadFiles(_files, _values, action);
        }
        private HttpWebRequest UploadFiles(Dictionary<string, string> files, Dictionary<string, string> otherValues, Action<IAsyncResult> action)
        {
            HttpWebRequest req = (HttpWebRequest)WebRequest.Create(_url);

            req.Timeout = 10000 * 1000;
            req.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,*/*;q=0.8";
            req.AllowAutoRedirect = false;
            req.UserAgent = UserAgent;
            req.Proxy = null;
            if (!_token.Equals(string.Empty))
                req.Headers.Add("Authorization", "Token " + _token);

            var mimeParts = new List<MimePart>();
            try
            {
                if (otherValues != null)
                {
                    foreach (var fieldName in otherValues.Keys)
                    {
                        var part = new MimePart();

                        part.Headers["Content-Disposition"] = "form-data; name=\"" + fieldName + "\"";
                        part.Data = new MemoryStream(Encoding.UTF8.GetBytes(otherValues[fieldName]));

                        mimeParts.Add(part);
                    }
                }

                if (files != null)
                {
                    foreach (var fieldName in files.Keys)
                    {
                        var part = new MimePart();

                        part.Headers["Content-Disposition"] = "form-data; name=\"" + fieldName + "\"; filename=\"" + files[fieldName] + "\"";
                        part.Headers["Content-Type"] = "application/octet-stream";
                        part.Data = File.OpenRead(files[fieldName]);

                        mimeParts.Add(part);
                    }
                }

                string boundary = "----------" + DateTime.Now.Ticks.ToString("x");

                req.ContentType = "multipart/form-data; boundary=" + boundary;
                req.Method = this._method;

                long contentLength = 0;

                byte[] _footer = Encoding.UTF8.GetBytes("--" + boundary + "--\r\n");

                foreach (MimePart part in mimeParts)
                {
                    contentLength += part.GenerateHeaderFooterData(boundary);
                }

                req.ContentLength = contentLength + _footer.Length;

                byte[] buffer = new byte[8192];
                byte[] afterFile = Encoding.UTF8.GetBytes("\r\n");
                int read;

                using (Stream s = req.GetRequestStream())
                {
                    foreach (MimePart part in mimeParts)
                    {
                        s.Write(part.Header, 0, part.Header.Length);

                        while ((read = part.Data.Read(buffer, 0, buffer.Length)) > 0)
                            s.Write(buffer, 0, read);

                        part.Data.Dispose();

                        s.Write(afterFile, 0, afterFile.Length);
                    }

                    s.Write(_footer, 0, _footer.Length);
                }
                req.BeginGetResponse(new AsyncCallback(action), null);
                return req;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                foreach (MimePart part in mimeParts)
                    if (part.Data != null)
                        part.Data.Dispose();

                req.BeginGetResponse(new AsyncCallback(action), null);
                return req;
            }
        }

        private class MimePart
        {
            private NameValueCollection _headers = new NameValueCollection();
            public NameValueCollection Headers { get { return _headers; } }

            public byte[] Header { get; protected set; }

            public long GenerateHeaderFooterData(string boundary)
            {
                StringBuilder sb = new StringBuilder();

                sb.Append("--");
                sb.Append(boundary);
                sb.AppendLine();
                foreach (string key in _headers.AllKeys)
                {
                    sb.Append(key);
                    sb.Append(": ");
                    sb.AppendLine(_headers[key]);
                }
                sb.AppendLine();

                Header = Encoding.UTF8.GetBytes(sb.ToString());

                return Header.Length + Data.Length + 2;
            }

            public Stream Data { get; set; }
        }
    }
}
