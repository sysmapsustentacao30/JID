using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using Sysmap.Sustentacao.JID.JiraCon;
using Sysmap.Sustentacao.JID.Models;

namespace Sysmap.Sustentacao.JID.Controllers
{
    public class WexController : Controller
    {
        private IHostingEnvironment _hostingEnvironment;
        private IConfiguration _configuration;


        private readonly string urlAtlassin = "https://sysmapsolutions.atlassian.net";
        private readonly string username = "sustentacao@sysmap.com.br";
        private readonly string password = "$u$t3nt4c40";
        private readonly string projectJira = "FTQPRUD";

        public WexController(IHostingEnvironment hostingEnvironment, IConfiguration configuration)
        {
            _hostingEnvironment = hostingEnvironment;
            _configuration = configuration;
        }

        [HttpGet]
        public IActionResult Index()
        {
            var teste = _configuration.GetSection("JiraSettings").GetSection("url").Value;
            ViewBag.Upload = false;
            return View();
        }

        [HttpPost]
        public IActionResult Index(string tipoPlanilha, IFormFile file)
        {
            if (file is null)
            {
                ViewBag.Upload = false;
                ModelState.AddModelError("", "Planilha não encontrada!");
                return View();
            }
            ViewBag.Upload = true;

            //Lendo os dados da planilha wex
            List<WexPlan> listWex = GetDataXls(file);

            //GET ISSUES 
            JiraPrudential jiraClient = new JiraPrudential();
            List<IssuePrudential> jiraIssues = jiraClient.GetListIssues(urlAtlassin,username,password,projectJira);
            //END JIRA

            //Regra caso a planilha wex seja concluidos pegar as issues Existentes  e atualizar status para fechado
            if (tipoPlanilha == "Concluidos")
            {
                var listIdWex = from wex in listWex
                                select wex.IdWex;


                var oldIssues = from issue in jiraIssues
                                where listIdWex.Contains(issue.WexId)
                                select new { issue.ID, issue.Status };

                foreach(var item in oldIssues)
                {
                    dynamic idStatus = jiraClient.GetTransitions(urlAtlassin, username, password, item.ID);

                    IDictionary<string, string> jiraStatusID = new Dictionary<string, string>();
                    foreach (var status in idStatus.transitions)
                    {
                        string name = Convert.ToString(status.name);
                        string id = Convert.ToString(status.id);
                        jiraStatusID.Add(name.Replace(" ", ""), id.Replace(" ", ""));
                    }
                    //Atualiza Status Fechado
                    string jsonData = "{" +
                                            "\"transition\": {" +
                                                                    "\"id\": " + "\"" + Convert.ToInt32(jiraStatusID["Fechado"]) + "\"" +
                                                            "}" +
                                        "}";

                    bool transition = jiraClient.TransitionIssue(urlAtlassin, username, password, item.ID, jsonData);
                    if (transition)
                    {
                        ViewBag.UpdateIssueQtd += 1;
                    }
                }

                ViewBag.NewIssueQtd = 0;
            }
            else
            {
                //Atualiza as Antigas
                ViewBag.UpdateIssueQtd = UpdateIssues(listWex, jiraIssues);

                //Cria as issues Novas.
                ViewBag.NewIssueQtd = CreateNewIssue(listWex, jiraIssues);
            }
            return View();
        }

        #region Cria uma issue nova
        private int CreateNewIssue(List<WexPlan> listWex, List<IssuePrudential> jiraIssues)
        {
            int qtd = 0;

            var issueWexid = from issue in jiraIssues
                                  select issue.WexId;

            var newIssues = from i in listWex
                            where !issueWexid.Contains(i.IdWex)
                            select i;

            JiraPrudential jiraClient = new JiraPrudential();

            //Cria a Issue no Jira
            foreach (WexPlan item in newIssues)
            {
                string issueType;
                if (item.OrdemServico.Substring(0, 3) == "INC")
                {
                    issueType = "Incidente";
                }
                else
                {
                    issueType = "Demanda de Teste";
                }

                string jsonData = "{" +
                      "\"fields\": {" +
                                        "\"project\": {" + "\"key\": " + "\"" + "FTQPRUD" + "\"" + "}," +
                                        "\"summary\": " + "\"" + item.Documento + "\"" + "," +
                                        "\"issuetype\": {" + "\"name\": " + "\"" + issueType + "\"" + "}," +
                                        "\"customfield_19227\": " + "\"" + item.OrdemServico + "\"" + "," +
                                        "\"customfield_19228\": "  + item.IdWex + "," +
                                        "\"customfield_19224\": " + "\"" + item.Data.ToString("yyyy-MM-dd") + "\"" +

                                  "}" +
                  "}";

                dynamic issueCreated = jiraClient.CreateIssue(urlAtlassin, username, password, projectJira, jsonData);
                // END

                //Verifica se a issue esta com status = Liberado QA caso não atualiza.
                dynamic issue = jiraClient.GetIssue(urlAtlassin, username, password, Convert.ToInt32(issueCreated.id));

                if (issue.fields.status.name == "Liberado QA")
                {

                }
                else
                {
                    dynamic idStatus = jiraClient.GetTransitions(urlAtlassin, username, password, Convert.ToInt32(issueCreated.id));

                    IDictionary<string, string> jiraStatusID = new Dictionary<string, string>();
                    foreach (var status in idStatus.transitions)
                    {
                        string name = Convert.ToString(status.name);
                        string id = Convert.ToString(status.id);
                        jiraStatusID.Add(name.Replace(" ", ""), id.Replace(" ", ""));
                    }


                    jsonData = "{" +
                                    "\"transition\": {" +
                                                        "\"id\": " + "\"" + Convert.ToInt32(jiraStatusID["LiberadoQA"]) + "\"" +
                                                     "}" +
                               "}";

                    int issueID = issueCreated.id;
                    bool transition = jiraClient.TransitionIssue(urlAtlassin, username, password, Convert.ToInt32(issueCreated.id), jsonData);
                }
                //END

                qtd += 1;
            }

            return qtd;
        }
        #endregion

        #region Atualizando Issue
        private int UpdateIssues(List<WexPlan> listWex, List<IssuePrudential> jiraIssues)
        {
            int qtd = 0;

            var listIdWex = from wex in listWex
                                 select wex.IdWex;


            var oldIssues = from issue in jiraIssues
                            where listIdWex.Contains(issue.WexId)
                            select new { issue.ID, issue.Status };



            //Atualiza Status Issue
            foreach(var issue in oldIssues)
            {
                int statusIssue = GetTransitionIssue(issue.Status, issue.ID);

                if(statusIssue == 0)
                {

                }
                else
                {

                    //Atualiza
                    string jsonData = "{" +
                                            "\"transition\": {" +
                                                                    "\"id\": " + "\"" + statusIssue + "\"" +
                                                            "}" +
                                        "}";
                    JiraPrudential jiraClient = new JiraPrudential();
                    bool transition = jiraClient.TransitionIssue(urlAtlassin, username, password, issue.ID, jsonData);
                    if (transition)
                    {
                        qtd += 1;
                    }
                }
            }
            //

            return qtd;
        }
        #endregion

        #region Lendo os dados da planilha 
        private List<WexPlan> GetDataXls(IFormFile file)
        {
            List<WexPlan> listWex = new List<WexPlan>();

            string webRootPath = _hostingEnvironment.WebRootPath;
            StringBuilder sb = new StringBuilder();
            if (file.Length > 0)
            {
                string sFileExtension = Path.GetExtension(file.FileName).ToLower();
                ISheet sheet;
                string fullPath = Path.Combine(webRootPath, file.FileName);
                using (var stream = new FileStream(fullPath, FileMode.Create))
                {
                    file.CopyTo(stream);
                    stream.Position = 0;
                    if (sFileExtension == ".xls")
                    {
                        HSSFWorkbook hssfwb = new HSSFWorkbook(stream); //This will read the Excel 97-2000 formats  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook  
                    }
                    else
                    {
                        XSSFWorkbook hssfwb = new XSSFWorkbook(stream); //This will read 2007 Excel format  
                        sheet = hssfwb.GetSheetAt(0); //get first sheet from workbook   
                    }

                    IRow headerRow = sheet.GetRow(0); //Get Header Row
                    int cellCount = headerRow.LastCellNum;

                    for (int i = (sheet.FirstRowNum + 1); i <= sheet.LastRowNum; i++) //Read Excel File
                    {
                        IRow row = sheet.GetRow(i);

                        string date = row.GetCell(13)?.ToString();
                        DateTime dateTime = DateTime.Now;
                        if (date is null)
                        {
                            
                        }
                        else
                        {
                            dateTime = Convert.ToDateTime(date);
                        }

                        WexPlan wexPlan = new WexPlan
                        {
                            IdWex = Convert.ToInt32(row.GetCell(3).ToString()),
                            OrdemServico = row.GetCell(8)?.ToString(),
                            Responsavel = row.GetCell(21)?.ToString(),
                            Documento = row.GetCell(5)?.ToString().Replace("\"", ""),
                            Status = row.GetCell(18)?.ToString(),
                            Data = dateTime

                        };

                        //Lista de responsaveis para utilizar no filtro.
                        var listaResponsaveis = new[] {"fmares","fli005", "kamoraes", "sba006", "pol027"};

                        if (listaResponsaveis.Contains(wexPlan.Responsavel))
                        {
                            if(wexPlan.OrdemServico is null)
                            {
                                wexPlan.OrdemServico = "NULL";
                            }
                            listWex.Add(wexPlan);
                        }
                    }
                }

                //Deleta arquivo criado
                FileInfo fileInfo = new FileInfo(Path.Combine(webRootPath, file.FileName));
                fileInfo.Delete();
            }

            return listWex;
        }
        #endregion

        #region Verifica se o status do Jira
        private int GetTransitionIssue(string statuIssue, int issueID)
        {
            int statusID = 0;
            JiraPrudential jiraPrudential = new JiraPrudential();
            dynamic idStatus = jiraPrudential.GetTransitions(urlAtlassin, username, password, issueID);

            IDictionary<string, string> jiraStatusID = new Dictionary<string, string>();
            foreach (var status in idStatus.transitions)
            {
                string name = Convert.ToString(status.name);
                string id = Convert.ToString(status.id);
                jiraStatusID.Add(name.Replace(" ", ""), id.Replace(" ",""));
            }

            switch (statuIssue)
            {
                case "Pacote Devolvido":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["LiberadoQA"]);
                    break;

                case "Em Correção":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["LiberadoQA"]);
                    break;

                case "Liberado UAT":
                    //ID do status Reaberto
                    statusID = Convert.ToInt32(jiraStatusID["Reaberto"]);
                    break;

                case "Cancelado":
                    //ID do status Backlog
                    statusID = Convert.ToInt32(jiraStatusID["Backlog"]);
                    break;

                case "Em Execução Prudential":
                    //ID do status Liberado Teste QA
                    statusID = Convert.ToInt32(jiraStatusID["LiberadoQA"]);
                    break;

                default:
                    statusID = 0;
                    break;
            }

            return statusID;
        }
        #endregion

    }
}
