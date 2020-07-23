using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Policy;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using VTOLVR_ModLoader.Windows;

namespace VTOLVR_ModLoader.Classes
{
    class HttpHelper
    {
        public enum HttpMethod { GET, HEAD, PUT, POST, OPTIONS}
        private static readonly HttpClient _client = new HttpClient();

        private readonly MultipartFormDataContent _form;
        private readonly string _url;
        private Dictionary<string, string> _files = new Dictionary<string, string>();
        
        public static void SetHeader()
        {
            if  (!_client.DefaultRequestHeaders.Contains("user-agent"))
                _client.DefaultRequestHeaders.Add("user-agent", Program.ProgramName.RemoveSpecialCharacters());
        }
        public static async Task<bool> CheckForInternet()
        {
            HttpResponseMessage response = await _client.GetAsync(Program.url);
            if (response.IsSuccessStatusCode)
                return true;
            else
                return false;
        }
        public static async Task DownloadStringAsync(string url, Action<HttpResponseMessage> callback, string token = "")
        {
            if (token == null || !token.Equals(""))
            {
                if (_client.DefaultRequestHeaders.Contains("Authorization"))
                    _client.DefaultRequestHeaders.Remove("Authorization");
                _client.DefaultRequestHeaders.Add("Authorization", "Token " + token);
            }

            if (callback != null)
                callback.Invoke(await _client.GetAsync(url));

            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");
        }
        public static async Task DownloadFileAsync(string url, string path, Action finishedCallback)
        {
            var response = await _client.GetAsync(url);
            
            if (!response.IsSuccessStatusCode)
            {
                //Failed
                return;
            }

            using (var stream = await response.Content.ReadAsStreamAsync())
            using (var streamReader = new StreamReader(stream))
            using (FileStream fileStream = File.Create(path))
            {
                await stream.CopyToAsync(fileStream);
            }

            if (finishedCallback != null)
                finishedCallback.Invoke();
        }

        public HttpHelper(string url)
        {
            _url = url;
            _form = new MultipartFormDataContent();
        }
        public void SetToken(string token)
        {
            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");
            _client.DefaultRequestHeaders.Add("Authorization", "Token " + token);
        }
        public void AttachFile(string field, string fileName, string filePath)
        {
            Stream stream = File.OpenRead(filePath);
            _form.Add(new StreamContent(stream),field,fileName);
        }
        public void SetValue(string field, string value)
        {
            _form.Add(new StringContent(value), field);
        }
        public async Task<HttpResponseMessage> SendDataAsync(HttpMethod method)
        {
            HttpResponseMessage message = null;
            switch (method)
            {
                case HttpMethod.PUT:
                    message = await _client.PutAsync(_url, _form);
                    break;
                case HttpMethod.POST:
                    message = await _client.PostAsync(_url, _form);
                    break;
                default:
                    Views.Console.Log("SendDataAsync has been given a wrong method");
                    break;
            }

            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");

            return message;
        }
    }
}
