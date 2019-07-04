using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace JID.Extensions
{
    public class JiraConn : IJiraConn
    {
        #region Coleta todas as issues do projeto
        public dynamic GetAllIssues(string urlAtlassin, string project,int maxResult,int startAt,string username, string password)
        {
            dynamic result = "";
            string credentials = String.Format("{0}:{1}", username, password);

            using(var client = new HttpClient())
            {
                string url = $"{urlAtlassin}/rest/api/2/search?jql=project={project}&maxResults={maxResult}&startAt={startAt}";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                    new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;
                result = JsonConvert.DeserializeObject(apiResult);

                client.Dispose();

            }

            return result;
        }
        #endregion

        #region Coleta as transiçoes possiveis para a issues x
        public dynamic GetTransitions(string urlAtlassian, string username, string password, int issueID)
        {
            dynamic transition = "";

            string credentials = String.Format("{0}:{1}", username, password);

            using (var client = new HttpClient())
            {
                string url = $"{urlAtlassian}/rest/api/2/issue/{issueID}/transitions";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;

                transition = JsonConvert.DeserializeObject(apiResult);

                client.Dispose();
            }

            return transition;

        }
        #endregion

        #region Faz a transição de status da issue
        public bool TransitionIssue(string urlAtlassian, string username, string password, int issuesId, string jsonData)
        {
            bool transition = true;
            try
            {
                string postUrl = $"{urlAtlassian}/rest/api/2/issue/{issuesId}/transitions";
                string credentials = String.Format("{0}:{1}", username, password);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(postUrl);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(credentials));

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonData);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var response = streamReader.ReadToEnd();

                    }

                    streamWriter.Dispose();
                }
            }
            catch(Exception ex)
            {
                transition = false;
                throw ex;
            }

            return transition;
        }
        #endregion

        #region Cadastra uma nova issue
        public dynamic CreateIssue(string urlAtlassian, string username, string password, string projectAtlassian, string jsonData)
        {
            dynamic issue = null;

            try
            {

                string postUrl = $"{urlAtlassian}/rest/api/2/issue"; ;
                string credentials = String.Format("{0}:{1}", username, password);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(postUrl);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "POST";
                httpWebRequest.Headers["Authorization"] = "Basic " + Convert.ToBase64String(Encoding.Default.GetBytes(credentials));

                using (var streamWriter = new StreamWriter(httpWebRequest.GetRequestStream()))
                {
                    streamWriter.Write(jsonData);
                    streamWriter.Flush();
                    streamWriter.Close();

                    var httpResponse = (HttpWebResponse)httpWebRequest.GetResponse();

                    using (var streamReader = new StreamReader(httpResponse.GetResponseStream()))
                    {
                        var response = streamReader.ReadToEnd();
                        issue = JsonConvert.DeserializeObject(response);

                        streamReader.Dispose();

                    }

                    streamWriter.Dispose();
                }
            }
            catch (Exception ex)
            {
                issue = null;
                throw ex;
            }

            return issue;
        }
        #endregion

        #region Get issue x
        public dynamic GetIssue(string urlAtlassian, string username, string password, int issueID)
        {
            dynamic result;

            string credentials = String.Format("{0}:{1}", username, password);

            using (var client = new HttpClient())
            {
                string url = $"{urlAtlassian}/rest/api/3/issue/{issueID}";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;

                result = JsonConvert.DeserializeObject(apiResult);

                client.Dispose();

            }

            return result;
        }
        #endregion

    }

    public interface IJiraConn
    {
        dynamic CreateIssue(string urlAtlassian, string username, string password, string projectAtlassian, string jsonData);
        dynamic GetAllIssues(string urlAtlassin, string project, int maxResult, int startAt, string username, string password);
        dynamic GetIssue(string urlAtlassian, string username, string password, int issueID);
        dynamic GetTransitions(string urlAtlassian, string username, string password, int issueID);
        bool TransitionIssue(string urlAtlassian, string username, string password, int iD, string jsonData);
    }
}
