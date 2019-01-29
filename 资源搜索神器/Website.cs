using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Runtime.Serialization;
using System.IO;
using System.Text.RegularExpressions;

namespace 资源搜索神器
{
    public class Website
    {
        public string Url { get => url; set => url = value; }
        private string url;
        public string content { get; private set; }
        private HttpWebRequest req;
        private HttpWebResponse response;
        public string[] Addresses { get; private set; }
        public Website(string url)
        {
            this.url = url;
        }
        public void Get(params string[] parameters)
        {

            if (parameters.Length % 2 == 1)
            {
                throw new ParameterError();
            }
            if (parameters.Length == 0)
            {
                req = WebRequest.CreateHttp(url);
            }
            else
            {
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < parameters.Length - 2; i += 2)
                {
                    sb.Append(WebUtility.UrlEncode(parameters[i]) + "=" + WebUtility.UrlEncode(parameters[i + 1]) + "&");
                }
                sb.Append(WebUtility.UrlEncode(parameters[parameters.Length - 2]) + "=" + WebUtility.UrlEncode(parameters[parameters.Length - 1]));
                string parameter = sb.ToString();
                req = WebRequest.CreateHttp(url + "?" + parameter);
            }
            req.Method = "Get";
            using (response = (HttpWebResponse)req.GetResponse())
            {
                content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            Init();

        }
        public void Post(params string[] parameters)
        {
            if (parameters.Length % 2 == 1)
            {
                throw new ParameterError();
            }
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < parameters.Length - 2; i += 2)
            {
                sb.Append(WebUtility.UrlEncode(parameters[i]) + "=" + WebUtility.UrlEncode(parameters[i + 1]) + "&");
            }
            sb.Append(WebUtility.UrlEncode(parameters[parameters.Length - 2]) + "=" + WebUtility.UrlEncode(parameters[parameters.Length-1]));
            string parameter = sb.ToString();
            req = WebRequest.CreateHttp(url);
            req.Method = "Post";
            req.ContentType = "application/x-www-form-urlencoded; charset=UTF-8";
            byte[] data = Encoding.ASCII.GetBytes(parameter);
            req.ContentLength = data.Length;
            req.GetRequestStream().Write(data, 0, data.Length);
            using (response = (HttpWebResponse)req.GetResponse())
            {
                content = new StreamReader(response.GetResponseStream()).ReadToEnd();
            }
            Init();
        }
        public void Init()
        {
            MatchCollection ms = Regex.Matches(content, "<a href=\"" + @"https?://([^\s]+)" + "\"");
            Addresses = new string[ms.Count];
            for (int i = 0; i < ms.Count; i++)
                Addresses[i] = "http://" + ms[i].Groups[1].Value;

        }
        public Match Search(string pattern)
        {
            Match m = Regex.Match(content, pattern);
            return m;
        }
        public bool IsMatch(string pattern) { 
            return Regex.IsMatch(content, pattern);
        }
        
}
    public class ParameterError : Exception
    {
        public ParameterError() : base("参数的数目不对，是不是少了key或值？") { }
    }
}
