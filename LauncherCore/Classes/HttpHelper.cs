using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace LauncherCore.Classes
{
    public class HttpHelper
    {
        public enum HttpMethod { GET, HEAD, PUT, POST, OPTIONS }

        private static readonly HttpClient _client = new HttpClient();

        private readonly MultipartFormDataContent _form;
        private readonly string _url;
        private Dictionary<string, string> _files = new Dictionary<string, string>();

        public static void SetHeader()
        {
            if (!_client.DefaultRequestHeaders.Contains("user-agent"))
                _client.DefaultRequestHeaders.Add("user-agent", Program.ProgramName.RemoveSpecialCharacters());
        }

        public static async Task<bool> CheckForInternet()
        {
            if (Program.DisableInternet)
                return false;
            try
            {
                HttpResponseMessage response = await _client.GetAsync(Program.URL);
                if (response.IsSuccessStatusCode)
                    return true;
            }
            catch
            {
            }

            return false;
        }

        public static async void DownloadStringAsync(string url, Action<HttpResponseMessage> callback,
            string token = "")
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

        public static async void DownloadStringAsync(string url, Action<HttpResponseMessage, object[]> callback,
            string token = "", object[] extraData = null)
        {
            if (token == null || !token.Equals(""))
            {
                if (_client.DefaultRequestHeaders.Contains("Authorization"))
                    _client.DefaultRequestHeaders.Remove("Authorization");
                _client.DefaultRequestHeaders.Add("Authorization", "Token " + token);
            }

            if (callback != null)
                callback.Invoke(await _client.GetAsync(url), extraData);

            if (_client.DefaultRequestHeaders.Contains("Authorization"))
                _client.DefaultRequestHeaders.Remove("Authorization");
        }

        public static void DownloadFile(string url, string path, DownloadProgressChangedEventHandler downloadProgress,
            AsyncCompletedEventHandler downloadComplete)
        {
            WebClient client = new WebClient();
            client.Headers.Add("user-agent", Program.ProgramName.RemoveSpecialCharacters());
            client.DownloadProgressChanged += downloadProgress;
            client.DownloadFileCompleted += downloadComplete;
            client.DownloadFileAsync(new Uri(url), path);
        }

        public static void DownloadFile(string url, string path, Action<CustomWebClient.RequestData> downloadProgress,
            Action<CustomWebClient.RequestData> downloadComplete, object[] extraData = null)
        {
            CustomWebClient client = new CustomWebClient();
            client.Headers.Add("user-agent", Program.ProgramName.RemoveSpecialCharacters());
            client.DownloadProgress += downloadProgress;
            client.DownloadComplete += downloadComplete;
            client.DownloadFileAsync(new Uri(url), path, extraData);
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
            _form.Add(new StreamContent(stream), field, fileName);
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

        public async void SendDataAsync(HttpMethod method, Action<HttpResponseMessage> callback)
        {
            HttpResponseMessage response = await SendDataAsync(method);
            callback?.Invoke(response);
        }
    }
}