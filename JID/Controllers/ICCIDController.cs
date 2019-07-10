using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JID.Extensions;
using JID.Models;
using KissLog;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;

namespace JID.Controllers
{
    public class ICCIDController : Controller
    {
        private readonly IExcelRead _excelRead;
        private readonly IUipathConn _uipathConn;
        private readonly ILogger _logger;

        public ICCIDController(IExcelRead excelRead, IUipathConn uipathConn, ILogger logger)
        {
            _excelRead = excelRead;
            _uipathConn = uipathConn;
            _logger = logger;
        }

        #region Upload Excel
        [HttpGet]
        public IActionResult UploadExcel()
        {
            return View();
        }

        [HttpPost]
        public IActionResult UploadExcel(IFormFile file,int qtdRows)
        {
            _logger.Info($"Iccid Automation Started - {DateTime.Now}");

            if(file is null) { ModelState.AddModelError("", "Arquivo não encontrado"); }
            if(qtdRows == 0) { ModelState.AddModelError("", "Informar quantidade de iccids");}


            if (!ModelState.IsValid)
            {
                return View();
            }

            try
            {
                List<IccidModel> iccids = _excelRead.ReadICCIDXls(file, qtdRows);

                StringBuilder txtListIccid = new StringBuilder();

                foreach (var item in iccids)
                {
                    txtListIccid.Append(item.NumIccid);
                    txtListIccid.Append(";");
                }

                iccids = _uipathConn.InitVerifyIccid(iccids);
            }
            catch (Exception ex)
            {
                _logger.Error(ex.ToString());
            }

            _logger.Info($"Iccid Automation Finished - {DateTime.Now}");
            return View();
        }
        #endregion
    }
}