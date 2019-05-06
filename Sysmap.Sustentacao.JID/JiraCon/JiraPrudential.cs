using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Sysmap.Sustentacao.JID.Models;

namespace Sysmap.Sustentacao.JID.JiraCon
{
    public class JiraPrudential
    {
       

        internal List<IssuePrudential> GetListIssues(string urlAtlassin,string username,string password,string project)
        {
            List<IssuePrudential> listIssues = new List<IssuePrudential>();

            string credentials = String.Format("{0}:{1}", username, password);

            using (var client = new HttpClient())
            {
                string url = $"{urlAtlassin}/rest/api/2/search?jql=project={project}&fields=summary,issuetype,status,customfield_19227,customfield_19228&maxResults=1000&startAt=0";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;

                dynamic issues = JsonConvert.DeserializeObject(apiResult);

                foreach (var issue in issues.issues)
                {
                    if (issue.fields.customfield_19228.Value is null)
                    {
                        issue.fields.customfield_19228 = "0";
                    }
                    IssuePrudential jira = new IssuePrudential
                    {
                        ID = Convert.ToInt32(issue.id),
                        Summary = issue.fields.summary,
                        IssueType = issue.fields.issuetype.name,
                        ServiceNow = issue.fields.customfield_19227,
                        WexId = Convert.ToInt32(issue.fields.customfield_19228),
                        Status = issue.fields.status.name
                    };
                    listIssues.Add(jira);
                }
            }

            return listIssues;
        }

        internal dynamic CreateIssue (string urlAtlassin, string username, string password, string project,string jsonData)
        {
            dynamic issue = null;
            try
            {

                string postUrl = $"{urlAtlassin}/rest/api/2/issue"; ;
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

                    }
                }
            }catch(Exception ex)
            {
                issue = null;
                throw ex;
            }

            return issue;
        }

        internal bool UpdateIssue(string urlAtlassin, string username, string password, int issueID, dynamic jsonData)
        {
            bool update = true;
            try
            {

                string postUrl = $"{urlAtlassin}/rest/api/2/issue/{issueID}";
                string credentials = String.Format("{0}:{1}", username, password);

                var httpWebRequest = (HttpWebRequest)WebRequest.Create(postUrl);
                httpWebRequest.ContentType = "application/json";
                httpWebRequest.Method = "PUT";
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
                }
            }
            catch (Exception ex)
            {
                update = false;
                throw ex;
            }

            return update;
        }

        internal bool TransitionIssue(string urlAtlassin, string username, string password, int issueID ,dynamic jsonData)
        {
            bool transition = true;
            try
            {

                string postUrl = $"{urlAtlassin}/rest/api/2/issue/{issueID}/transitions";
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
                }
            }
            catch (Exception ex)
            {
                transition = false;
                throw ex;
            }

            return transition;
        }

        internal dynamic GetIssue(string urlAtlassin, string username, string password,int issueID)
        {
            dynamic issues;

            string credentials = String.Format("{0}:{1}", username, password);

            using (var client = new HttpClient())
            {
                string url = $"{urlAtlassin}/rest/api/3/issue/{issueID}";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;

                issues = JsonConvert.DeserializeObject(apiResult);

            }

            return issues;
        }

        internal dynamic GetTransitions(string urlAtlassin, string username, string password, int issueID)
        {
            dynamic issues;

            string credentials = String.Format("{0}:{1}", username, password);

            using (var client = new HttpClient())
            {
                string url = $"{urlAtlassin}/rest/api/2/issue/{issueID}/transitions";

                client.DefaultRequestHeaders.Clear();
                client.BaseAddress = new Uri(url);
                client.DefaultRequestHeaders.Accept.Add(
                     new MediaTypeWithQualityHeaderValue("application/json"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", Convert.ToBase64String(Encoding.UTF8.GetBytes(credentials)));
                HttpResponseMessage response = client.GetAsync("").Result;

                response.EnsureSuccessStatusCode();

                string apiResult = response.Content.ReadAsStringAsync().Result;

                issues = JsonConvert.DeserializeObject(apiResult);

            }

            return issues;
        }
    }
}
