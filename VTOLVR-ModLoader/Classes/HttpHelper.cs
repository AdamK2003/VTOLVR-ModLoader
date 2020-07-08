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
using VTOLVR_ModLoader.Windows;

namespace VTOLVR_ModLoader.Classes
{
    class HttpHelper
    {
        private static readonly HttpClient _client = new HttpClient();

        private readonly MultipartFormDataContent _form;
        private readonly string _url;
        private Dictionary<string, string> _files = new Dictionary<string, string>();

        public HttpHelper(string url)
        {
            _url = url;
            _form = new MultipartFormDataContent();
        }
        public void SetToken(string token)
        {
            if (!_client.DefaultRequestHeaders.Contains("Authorization"))
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
        public async Task<HttpContent> SendDataAsync()
        {
            var message = await _client.PostAsync(_url, _form);

            return message.Content;
        }
        public static async Task<bool> CheckForInternet()
        {
            HttpResponseMessage response = await _client.GetAsync("https://vtolvr-mods.com");
            if (response.StatusCode == HttpStatusCode.OK)
                return true;
            else
                return false;
        }
    }
}
