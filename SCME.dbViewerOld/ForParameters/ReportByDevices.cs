using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Excel = Microsoft.Office.Interop.Excel;

namespace SCME.dbViewer.ForParameters
{
    public class ReportByDevices : List<object>
    {
        private string LastUsedProfileID { get; set; }

        public void ToExcel()
        {            
            Excel.Application exelApp = new Microsoft.Office.Interop.Excel.Application();
            exelApp.Visible = false;

            try
            {
                exelApp.SheetsInNewWorkbook = 2;
                Excel.Workbook workBook = exelApp.Workbooks.Add(Type.Missing);
                exelApp.DisplayAlerts = false;
                Excel.Worksheet sheet = (Excel.Worksheet)exelApp.Worksheets.get_Item(1);
                sheet.Name = "Отчёт по данным КИПП СПП";

                this.LastUsedProfileID = "";
                int rowNum = 1;
                int number = 1;

                foreach (var p in this)
                {
                    if (p is TParameters)
                    {
                        TParameters parameters = (TParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is TLParameters)
                    {
                        TLParameters parameters = (TLParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is TBParameters)
                    {
                        TBParameters parameters = (TBParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is TBIParameters)
                    {
                        TBIParameters parameters = (TBIParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is TBHParameters)
                    {
                        TBHParameters parameters = (TBHParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is DParameters)
                    {
                        DParameters parameters = (DParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is DLParameters)
                    {
                        DLParameters parameters = (DLParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }

                    if (p is DHParameters)
                    {
                        DHParameters parameters = (DHParameters)p;
                        parameters.ToExcel(exelApp, sheet, number, this.LastUsedProfileID, ref rowNum);
                        this.LastUsedProfileID = parameters.ProfileID;
                        number++;
                    }
                }

                sheet.UsedRange.EntireRow.AutoFit();
                sheet.UsedRange.EntireColumn.AutoFit();
            }

            finally
            {
                exelApp.Visible = true;
            }
        }
    }
}
