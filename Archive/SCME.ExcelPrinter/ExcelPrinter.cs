using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace SCME.ExcelPrinting
{
    public class ExcelPrinter
    {
        private const int NUM_OF_ITEMS_IN_FIRST_PAGE = 32;
        private const int NUM_OF_ITEMS_IN_THE_REST_OF_THE_PAGES = 46;
        private const int FIRST_ROW_INDEX = 18;
        private const int ROWS_ON_PAGE = 50;
        private const int THIRD_PAGE_1 = 100;
        private const int THIRD_ROW_2 = 147;
        private const int SHAPE_CONST_1 = 74;
        private const int SHAPE_CONST_2 = 54;

        private readonly object m_MissValue = Type.Missing;

        private bool m_PrivateDocument;
        private Microsoft.Office.Interop.Excel.Application m_XlApp;
        private Microsoft.Office.Interop.Excel.Workbook m_XlWorkBook;
        private Microsoft.Office.Interop.Excel.Worksheet m_XlWorkSheet;

        public ExcelPrinter()
        {
            TemplateXlsFilePath = @"";
            Copies = 1;
            PrinterName = "";
            SaveXlsFilePath = "";
        }

        public string SaveXlsFilePath { get; set; }

        public string PrinterName { get; set; }

        public int Copies { get; set; }

        public string TemplateXlsFilePath { get; set; }

        public bool SaveToFile { get; set; }

        public void CreateXlsReport(bool Use2Pos, ReportInfo Info, List<DeviceItemWithParams> Data, bool PrivateDocument, bool NoPrint)
        {
            m_PrivateDocument = PrivateDocument;

            var fi = new FileInfo(TemplateXlsFilePath);

            if (fi.Exists)
            {
                m_XlApp = new Microsoft.Office.Interop.Excel.Application();

                try
                {
                    m_XlWorkBook = m_XlApp.Workbooks.Open(TemplateXlsFilePath, m_MissValue, m_MissValue, m_MissValue,
                                                          m_MissValue, m_MissValue, m_MissValue, m_MissValue,
                                                          m_MissValue, m_MissValue, m_MissValue, m_MissValue, m_MissValue,
                                                          m_MissValue, m_MissValue);
                    try
                    {
                        m_XlWorkSheet = (Microsoft.Office.Interop.Excel.Worksheet) m_XlWorkBook.Worksheets.Item[1];
                        m_XlWorkSheet.EnableSelection =
                            Microsoft.Office.Interop.Excel.XlEnableSelection.xlNoRestrictions;

                        int pageCount;

                        var groupedData = Data.GroupBy(Arg => Arg.GeneralInfo.Code).OrderBy(Arg => Arg.Key).ToList();

                        var actualNumOfItemsInFirstPage = Use2Pos
                            ? NUM_OF_ITEMS_IN_FIRST_PAGE / 2
                            : NUM_OF_ITEMS_IN_FIRST_PAGE;
                        var actualNumOfItemsRestPage = Use2Pos
                            ? NUM_OF_ITEMS_IN_THE_REST_OF_THE_PAGES / 2
                            : NUM_OF_ITEMS_IN_THE_REST_OF_THE_PAGES;

                        if (groupedData.Count <= actualNumOfItemsInFirstPage)
                        {
                            pageCount = 1;
                            m_XlWorkSheet.Range["A50", "O99"].Delete(
                                Microsoft.Office.Interop.Excel.XlDeleteShiftDirection.xlShiftUp);
                        }
                        else
                        {
                            var itemsWithoutFirstPage = Data.Count - actualNumOfItemsInFirstPage;
                            pageCount = 1 + itemsWithoutFirstPage/actualNumOfItemsRestPage;

                            if (itemsWithoutFirstPage%actualNumOfItemsRestPage != 0)
                                pageCount++;
                        }

                        PopulateHeader(Data, Info, pageCount);
                        PopulateBody(Use2Pos, groupedData, pageCount);

                        m_XlWorkSheet.PageSetup.CenterFooter = Info.GroupName;
                        m_XlWorkSheet.PageSetup.LeftFooter = Info.CustomerName;
                        m_XlWorkBook.CheckCompatibility = false;
                        if (!NoPrint)
                            m_XlWorkBook.PrintOutEx(1, pageCount, Copies, false, PrinterName, false, true, m_MissValue,
                                true);

                        if (SaveToFile)
                        {
                            m_XlApp.DisplayAlerts = false;
                            m_XlWorkBook.SaveAs(SaveXlsFilePath, Microsoft.Office.Interop.Excel.XlFileFormat.xlExcel8,
                                m_MissValue, m_MissValue,
                                false, false,
                                Microsoft.Office.Interop.Excel.XlSaveAsAccessMode.xlNoChange,
                                m_MissValue, m_MissValue,
                                m_MissValue, m_MissValue, m_MissValue);
                            m_XlApp.DisplayAlerts = true;
                        }
                    }
                    finally
                    {
                        m_XlWorkBook.Close(false, m_MissValue, m_MissValue);
                    }
                }
                finally
                {
                    m_XlApp.Quit();
                    m_XlApp = null;
                }
            }
            else
                throw new FileNotFoundException(String.Format("Template '{0}' not found", TemplateXlsFilePath));
        }

        private void PopulateHeader(ICollection<DeviceItemWithParams> Data, ReportInfo Info, int PageCount)
        {
            m_XlWorkSheet.Range["C2", m_MissValue].Value2 = Info.ModuleType;
            m_XlWorkSheet.Range["C3", m_MissValue].Value2 = Info.GroupName;
            m_XlWorkSheet.Range["I2", m_MissValue].Value2 = DateTime.Now.ToShortDateString();
            m_XlWorkSheet.Range["I3", m_MissValue].Value2 = PageCount.ToString(CultureInfo.InvariantCulture);
            m_XlWorkSheet.Range["M2", m_MissValue].Value2 = Data.Count;
            m_XlWorkSheet.Range["M3", m_MissValue].Value2 = Info.CustomerName;

            if (Info.Conditions.Any(C => C.Name == "SL_ITM"))
                m_XlWorkSheet.Range["F8", m_MissValue].Value2 =
                  Info.Conditions.First(C => C.Name == "SL_ITM").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["F8", m_MissValue].Value2 = "-";

            if (Info.Conditions.Any(C => C.Name == "BVT_I"))
                m_XlWorkSheet.Range["I8", m_MissValue].Value2 = m_XlWorkSheet.Range["I9", m_MissValue].Value2 =
                                                                Info.Conditions.First(C => C.Name == "BVT_I").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["I8", m_MissValue].Value2 = m_XlWorkSheet.Range["I9", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["E15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "RG"))
                (m_XlWorkSheet.Range["E16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "RG").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["E16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["F15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "IGT"))
                (m_XlWorkSheet.Range["F16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "IGT").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["F16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["G15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "VGT"))
                (m_XlWorkSheet.Range["G16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "VGT").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["G16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["H15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "IH"))
                (m_XlWorkSheet.Range["H16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "IH").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["H16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["I15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "IL"))
                (m_XlWorkSheet.Range["I16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "IL").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["I16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["J15", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "VTM"))
                (m_XlWorkSheet.Range["J16", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "VTM").Max ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["J16", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["K16", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "VRRM"))
                (m_XlWorkSheet.Range["K15", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "VRRM").Min ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["K15", m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["L16", m_MissValue].Value2 = "-";
            if (Info.Normatives.Any(N => N.Name == "VDRM"))
                (m_XlWorkSheet.Range["L15", m_MissValue].Value2 = Info.Normatives.First(N => N.Name == "VDRM").Min ?? 0.0f).ToString();
            else
                m_XlWorkSheet.Range["L15", m_MissValue].Value2 = "-";

            if (!m_PrivateDocument)
                m_XlWorkSheet.Shapes.Item("WaterMark").Delete();
        }

        private void PopulateBody(bool Use2Pos, IList<IGrouping<string, DeviceItemWithParams>> Data, int PageCount)
        {
            var actualNumOfItemsInFirstPage = Use2Pos
                ? NUM_OF_ITEMS_IN_FIRST_PAGE / 2
                : NUM_OF_ITEMS_IN_FIRST_PAGE;
            var actualNumOfItemsRestPage = Use2Pos
                ? NUM_OF_ITEMS_IN_THE_REST_OF_THE_PAGES / 2
                : NUM_OF_ITEMS_IN_THE_REST_OF_THE_PAGES;
            var mpy = Use2Pos ? 2 : 1;

            var k = Data.Count;

            if (k >= actualNumOfItemsInFirstPage)
                k = actualNumOfItemsInFirstPage;
            else
                m_XlWorkSheet.Range[
                    "A" + (FIRST_ROW_INDEX + Data.Count * mpy), "O" + (FIRST_ROW_INDEX + actualNumOfItemsInFirstPage * mpy)]
                    .Delete(Microsoft.Office.Interop.Excel.XlDeleteShiftDirection.xlShiftToLeft);
            
            for (var i = 0; i < k; i++)
                PopulateOneBlock(Use2Pos, Data[i], i, FIRST_ROW_INDEX);

            if (Data.Count > actualNumOfItemsInFirstPage)
            {
                var body = m_XlWorkSheet.Range["A50", "O99"];
                body.Copy();
                
                if (PageCount > 2)
                {
                    for (var p = 0; p < PageCount - 2; p++)
                    {
                        int a = THIRD_PAGE_1 + ROWS_ON_PAGE * p, t = THIRD_ROW_2 + ROWS_ON_PAGE * p;
                        
                        m_XlWorkSheet.Range["A" + a, "O" + t].PasteSpecial(
                            Microsoft.Office.Interop.Excel.XlPasteType.xlPasteAll, Microsoft.Office.Interop.Excel.XlPasteSpecialOperation.xlPasteSpecialOperationNone,
                            false, false);

                        for (var i = a; i <= t; i++)
                            m_XlWorkSheet.Range["A" + i, "O" + i].RowHeight =
                                body.Range["A" + (1 + i - a), "O" + (1 + i - a)].RowHeight;
                    }
                }

                var rest = (Data.Count - actualNumOfItemsInFirstPage) % actualNumOfItemsRestPage;
                if (rest > 0)
                    m_XlWorkSheet.Range["A" + (ROWS_ON_PAGE * (PageCount - 1) + 4 + rest * mpy), "O" + (ROWS_ON_PAGE * PageCount)]
                        .Delete(Microsoft.Office.Interop.Excel.XlDeleteShiftDirection.xlShiftToLeft);
                
                for (var p = 0; p < PageCount - 1; p++)
                {
                    if (m_PrivateDocument)
                    {
                        var shape = m_XlWorkSheet.Shapes.Item("WaterMark");
                        shape.Copy();
                        
                        m_XlWorkSheet.Range["A" + (SHAPE_CONST_1 + ROWS_ON_PAGE * p)].Select();
                        m_XlWorkSheet.Paste();
                    }

                    for (var i = 0; i < rest; i++)
                    {
                        var item = actualNumOfItemsInFirstPage + actualNumOfItemsRestPage * p + i;
                        PopulateOneBlock(Use2Pos, Data[item], i, SHAPE_CONST_2 + ROWS_ON_PAGE * p);
                    }
                }
            }
        }

        private void PopulateOneBlock(bool Use2Pos, IGrouping<string, DeviceItemWithParams> Data, int CellIndex, int CellOffset)
        {
            CellIndex *= Use2Pos ? 2 : 1;
           
            m_XlWorkSheet.Range["A" + (CellOffset + CellIndex), m_MissValue].Value2 = Data.Key;
            m_XlWorkSheet.Range["C" + (CellOffset + CellIndex), m_MissValue].Value2 = "1 - 2";
            if(Use2Pos)
                m_XlWorkSheet.Range["C" + (CellOffset + CellIndex + 1), m_MissValue].Value2 = "1 - 3";

            var dc = Data.First().DefectCode;
            m_XlWorkSheet.Range["O" + (CellOffset + CellIndex), m_MissValue].Value2 = (dc == 0)
                ? "QC"
                : dc.ToString(
                    CultureInfo.InvariantCulture);

            var orderedData = Data.OrderBy(Arg => Arg.GeneralInfo.Position).ToList();

            PopulateOneRow(orderedData[0], CellOffset + CellIndex);
            if(Use2Pos && orderedData.Count > 1)
                PopulateOneRow(orderedData[1], CellOffset + CellIndex + 1);
        }

        private void PopulateOneRow(DeviceItemWithParams Data, int CellAddress)
        {
            if (Data.Parameters.Any(P => P.Name == "RG"))
                m_XlWorkSheet.Range["E" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "RG").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["E" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "IGT"))
                m_XlWorkSheet.Range["F" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "IGT").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["F" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "VGT"))
                m_XlWorkSheet.Range["G" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "VGT").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["G" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "IH"))
                m_XlWorkSheet.Range["H" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "IH").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["H" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "IL"))
                m_XlWorkSheet.Range["I" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "IL").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["I" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "VTM"))
                m_XlWorkSheet.Range["J" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "VTM").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["J" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "VRRM"))
                m_XlWorkSheet.Range["K" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "VRRM").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["K" + CellAddress, m_MissValue].Value2 = "-";

            if (Data.Parameters.Any(P => P.Name == "VDRM"))
                m_XlWorkSheet.Range["L" + CellAddress, m_MissValue].Value2 =
                   Data.Parameters.First(P => P.Name == "VDRM").Value.ToString(CultureInfo.InvariantCulture);
            else
                m_XlWorkSheet.Range["L" + CellAddress, m_MissValue].Value2 = "-";

            m_XlWorkSheet.Range["M" + CellAddress, m_MissValue].Value2 = Data.GeneralInfo.StructureOrd;
            m_XlWorkSheet.Range["N" + CellAddress, m_MissValue].Value2 = Data.GeneralInfo.StructureID;
        }
    }
}
