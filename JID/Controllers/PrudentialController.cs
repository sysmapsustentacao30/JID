using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using JID.Extensions;
using JID.Models;
using KissLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace JID.Controllers
{
    public class PrudentialController : Controller
    {
        private readonly IExcelRead _excelRead;
        private readonly IJiraConn _jiraConn;
        private readonly ILogger _logger;
        private IConfiguration _config;

        readonly string urlAtlassian;
        readonly string projectAtlassian;
        readonly string username;
        readonly string password;

        public PrudentialController(IJiraConn jiraConn,IExcelRead excelRead, ILogger logger,IConfiguration configuration)
        {
            _excelRead = excelRead;
            _jiraConn = jiraConn;
            _logger = logger;
            _config = configuration;

            urlAtlassian = _config.GetSection("AtlassianConfig").GetSection("urlAtlassian").Value;
            projectAtlassian = _config.GetSection("AtlassianConfig").GetSection("projectAtlassian").Value;
            username = _config.GetSection("AtlassianConfig").GetSection("userAtlassian").Value;
            password = _config.GetSection("AtlassianConfig").GetSection("passwordAtlassian").Value;
        }


        [HttpGet]
        public IActionResult UploadExcel()
        {
            ViewBag.Upload = false;
            return View();
        }

        [HttpPost]
        public IActionResult UploadExcel(IFormFile file)
        {
            _logger.Info($"Prudential Automation Started - {DateTime.Now}");

            if(file is null)
            {
                ViewBag.Upload = false;
                ModelState.AddModelError("", "Arquivo não encontrado!");
                return View();
            }
            try
            {

                List<WexPlan> wexList = _excelRead.ReadWexXls(file);

                List<IssuePrudent> issueList = GetListIssue();

                ViewBag.QtdUpdate = UpdateIssues(wexList, issueList);

                ViewBag.QtdCreate = CreateNewIssue(wexList, issueList);

                ViewBag.Upload = true;
            }
            catch(Exception ex)
            {
                ViewBag.Upload = false;
                _logger.Error(ex.ToString());
            }

            _logger.Info($"Prudential Automation Finished - {DateTime.Now}");
            return View();
        }

        #region Criando uma nova issue
        private int CreateNewIssue(List<WexPlan> wexList, List<IssuePrudent> issueList)
        {
            int qtd = 0;

            var issuesWexId = from issue in issueList
                              select issue.WexId;

            var newIssues = from wexData in wexList
                            where !issuesWexId.Contains(wexData.IdWex)
                            select wexData;

            foreach (WexPlan wexData in newIssues)
            {
                string issueType;
                if (wexData.OrdemServico.Substring(0, 3) == "INC")
                {
                    issueType = "Incidente";
                }
                else
                {
                    issueType = "Demanda de Teste";
                }

                string jsonData = "{" +
                      "\"fields\": {" +
                                        "\"project\": {" + "\"key\": " + "\"" + projectAtlassian + "\"" + "}," +
                                        "\"summary\": " + "\"" + wexData.Documento + "\"" + "," +
                                        "\"issuetype\": {" + "\"name\": " + "\"" + issueType + "\"" + "}," +
                                        "\"customfield_19227\": " + "\"" + wexData.OrdemServico + "\"" + "," +
                                        "\"customfield_19228\": " + wexData.IdWex + "," +
                                        "\"customfield_13701\": {" + "\"id\": " + "\"" + "-1" + "\"" + "}," +
                                        "\"customfield_19224\": " + "\"" + wexData.Data.ToString("yyyy-MM-dd") + "\"" +
                                  "}" +
                  "}";

                dynamic issueCreated = _jiraConn.CreateIssue(urlAtlassian, username, password, projectAtlassian, jsonData);

                //Verifica se a issue esta com status = Liberado QA caso não atualiza.
                dynamic issue = _jiraConn.GetIssue(urlAtlassian, username, password, Convert.ToInt32(issueCreated.id));

                if (issue.fields.status.name == "Liberado QA")
                {

                }
                else
                {
                    dynamic idStatus = _jiraConn.GetTransitions(urlAtlassian, username, password, Convert.ToInt32(issueCreated.id));

                    IDictionary<string,string> jiraStatusID = new Dictionary<string, string>();
                    foreach(var status in idStatus.transitions)
                    {
                        string name = Convert.ToString(status.name);
                        string id = Convert.ToString(status.id);
                        jiraStatusID.Add(name.Replace(" ", "").ToLower(), id.Replace(" ", ""));
                    }

                    jsonData = "{" +
                                    "\"transition\": {" +
                                                        "\"id\": " + "\"" + Convert.ToInt32(jiraStatusID["liberadoqa"]) + "\"" +
                                                    "}" +
                               "}";

                    int issueID = issueCreated.id;
                    bool transition = _jiraConn.TransitionIssue(urlAtlassian, username, password, Convert.ToInt32(issueCreated.id), jsonData);
                }

                qtd += 1;

            }

            return qtd;
        }
        #endregion

        #region Atualizando status das issues
        private int UpdateIssues(List<WexPlan> wexList, List<IssuePrudent> issueList)
        {
            int qtd = 0;

            var wexIds = from wex in wexList
                         select wex.IdWex;

            var oldIssues = from issue in issueList
                            where wexIds.Contains(issue.WexId)
                            select new { issue.WexId, issue.ID, issue.Status };

            foreach(var issue in oldIssues)
            {
                int statusIssue = GetTransitionIssue(issue.Status, issue.ID);

                if(statusIssue == 0)
                {

                }
                else
                {
                    //Json para atualizar Issue
                    string jsonData = "{" +
                                            "\"transition\": {" +
                                                                    "\"id\": " + "\"" + statusIssue + "\"" +
                                                            "}" +
                                        "}";

                    bool transition = _jiraConn.TransitionIssue(urlAtlassian, username, password, issue.ID, jsonData);
                    if (transition)
                    {
                        qtd += 1;
                    }
                }
            }
            return qtd;
        }
        #endregion

        #region Pegando id dos status para alteração.
        private int GetTransitionIssue(string statuIssue, int issueID)
        {
            int statusID = 0;
            dynamic response = _jiraConn.GetTransitions(urlAtlassian, username, password, issueID);

            IDictionary<string, string> jiraStatusID = new Dictionary<string, string>();
            foreach (var status in response.transitions)
            {
                string name = Convert.ToString(status.name);
                string id = Convert.ToString(status.id);
                jiraStatusID.Add(name.Replace(" ", "").ToLower(), id.Replace(" ", ""));
            }

            switch (statuIssue.Replace(" ", "").ToLower())
            {
                // Pacote Devolvido -> Liberado QA
                case "pacotedevolvido":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["liberadoqa"]);
                    break;

                // Em correção -> Liberado QA
                case "emcorreção":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["liberadoqa"]);
                    break;

                // Liberado UAT -> Reaberto
                case "liberadouat":
                    //ID do status Reaberto
                    statusID = Convert.ToInt32(jiraStatusID["reaberto"]);
                    break;

                // Cancelado -> Backlog
                case "cancelado":
                    //ID do status Backlog
                    statusID = Convert.ToInt32(jiraStatusID["backlog"]);
                    break;

                // Em execução prudential -> Liberado QA
                case "emexecuçãoprudential":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["liberadoqa"]);
                    break;

                // Pendente Prudential -> Liberado QA
                case "pendenteprudential":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["liberadoqa"]);
                    break;

                default:
                    statusID = 0;
                    break;
            }

            return statusID;
        }
        #endregion

        #region Pegando issues do projeto
        private List<IssuePrudent> GetListIssue()
        {
            List<IssuePrudent> issues = new List<IssuePrudent>();

            dynamic responseJira = _jiraConn.GetAllIssues(urlAtlassian, projectAtlassian, 0, 0, username, password);

            int totalIssues = Convert.ToInt32(responseJira.total);

            for (int qtdIssue = 0; qtdIssue < totalIssues;)
            {
                dynamic response = _jiraConn.GetAllIssues(urlAtlassian, projectAtlassian, 100, qtdIssue, username, password);

                foreach (var issue in response.issues)
                {
                    if (issue.fields.customfield_19228.Value is null)
                    {
                        issue.fields.customfield_19228 = "0";
                    }
                    IssuePrudent jira = new IssuePrudent
                    {
                        ID = Convert.ToInt32(issue.id),
                        Summary = issue.fields.summary,
                        IssueType = issue.fields.issuetype.name,
                        ServiceNow = issue.fields.customfield_19227,
                        WexId = Convert.ToInt32(issue.fields.customfield_19228),
                        Status = issue.fields.status.name
                    };
                    issues.Add(jira);
                }

                qtdIssue = issues.Count();

            }

            return issues;
        }
        #endregion
    }
}