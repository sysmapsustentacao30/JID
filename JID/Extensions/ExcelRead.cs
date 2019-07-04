using JID.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.XSSF.UserModel;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace JID.Extensions
{
    public class ExcelRead : IExcelRead
    {
        private IHostingEnvironment _hostingEnvironment;

        public ExcelRead(IHostingEnvironment hostingEnvironment)
        {
            _hostingEnvironment = hostingEnvironment;
        }

        #region Lendo planilha wex
        public List<WexPlan> ReadWexXls(IFormFile file)
        {
            List<WexPlan> listWex = new List<WexPlan>();

            string webRootPath = _hostingEnvironment.WebRootPath;

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

                        WexPlan wexPlan = new WexPlan
                        {
                            IdWex = Convert.ToInt32(row.GetCell(3).ToString()),
                            OrdemServico = row.GetCell(8)?.ToString(),
                            Responsavel = row.GetCell(21)?.ToString(),
                            Documento = row.GetCell(5)?.ToString().Replace("\"", ""),
                            Status = row.GetCell(18)?.ToString(),
                            Data = DateTime.Now

                        };

                        //Lista de responsaveis para utilizar no filtro.
                        var listaResponsaveis = new[] { "fmares", "fli005", "kamoraes", "sba006", "pol027" };

                        if (listaResponsaveis.Contains(wexPlan.Responsavel))
                        {
                            if (wexPlan.OrdemServico is null)
                            {
                                wexPlan.OrdemServico = "NULL";
                            }
                            listWex.Add(wexPlan);
                        }
                    }

                    stream.Close();
                    stream.Dispose();
                }

                //Deleta arquivo criado
                FileInfo fileInfo = new FileInfo(Path.Combine(webRootPath, file.FileName));
                fileInfo.Delete();
            }


            return listWex;
        }
        #endregion

        #region Lendo planilha com lista de iccids
        public List<IccidModel> ReadICCIDXls(IFormFile file, int qtdRow)
        {
            List<IccidModel> iccidList = new List<IccidModel>();

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

                    for (int i = (sheet.FirstRowNum + 1); i <= qtdRow; i++) //Read Excel File
                    {
                        IRow row = sheet.GetRow(i);

                        IccidModel iccid = new IccidModel
                        {
                            NumIccid = row.GetCell(0).ToString(),
                            Disponivel = false
                           
                        };

                        iccidList.Add(iccid);
                    }

                    stream.Dispose();
                }

                //Deleta arquivo criado
                FileInfo fileInfo = new FileInfo(Path.Combine(webRootPath, file.FileName));
                fileInfo.Delete();
            }

            return iccidList;
        }
        #endregion
    }

    public interface IExcelRead
    {
        List<WexPlan> ReadWexXls(IFormFile file);
        List<IccidModel> ReadICCIDXls(IFormFile file, int qtdRow);
    }
}
