using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace OpenXml
{
    public static class OpenXmlRoutines
    {
        public enum NumberFormatId : uint
        {
            String = 0,
            Integer = 1,
            Double = 2,
            Date = 14,
            Time = 21,
            DateTime = 22
        }

        private static string RCColumnNumToA1ColumnNum(uint columnIndex)
        {
            //преобразование номера столбца ColumnNum из цифровой идентификации в строковую идентификацию
            string columnName = string.Empty;

            int columnInd = (int)columnIndex;

            while (columnInd > 0)
            {
                int modulo = (columnInd - 1) % 26;
                columnName = string.Concat(Convert.ToChar('A' + modulo), columnName);
                columnInd = (columnInd - modulo) / 26;
            }

            return columnName;
        }

        private static string XlRCtoA1(uint rowIndex, uint columnIndex)
        {
            //преобразование цифровой идентификации ячейки (по номеру столбца, номеру строки) в строковую идентификацию
            return string.Concat("$", RCColumnNumToA1ColumnNum(columnIndex), rowIndex.ToString());
        }

        public static uint ColumnNameToNumber(string columnName)
        {
            //вычисляет по буквенному обозначению столбца его номер (возвращает номер столбца как в Excel стиль ссылок R1C1)
            if (string.IsNullOrEmpty(columnName)) throw new ArgumentNullException("columnName");

            columnName = columnName.ToUpperInvariant();

            uint sum = 0;

            for (int i = 0; i < columnName.Length; i++)
            {
                sum *= 26;
                sum += (uint)(columnName[i] - 'A' + 1);
            }

            return sum;
        }

        public static SpreadsheetDocument Create(string fileName)
        {
            SpreadsheetDocument spreadsheetDocument = SpreadsheetDocument.Create(fileName, SpreadsheetDocumentType.Workbook);

            //добавляем WorkbookPart в spreadsheetDocument
            WorkbookPart workbookpart = spreadsheetDocument.AddWorkbookPart();
            workbookpart.Workbook = new Workbook();

            //создаём стиль по умолчанию, который будет применятся к ячейке без установки индекса стиля
            //в этом стиле все индексы могут ссылаться только на нулевые ID, другие значения стиль по умолчанию игнорирует
            CreateStyle(spreadsheetDocument, "Arial Narrow", 11, false, PatternValues.None, "0", false, HorizontalAlignmentValues.Center, VerticalAlignmentValues.Bottom, NumberFormatId.String);

            //создаём зарезервированное значение заливки - в Fills оно будет иметь индекс 2
            StyleObjects(spreadsheetDocument, out Fonts fonts, out Fills fills, out Borders borders, out CellFormats cellFormats);
            Fill fill = NewFill(PatternValues.Gray125, "0");
            fills.Append(fill);
            fills.Count++;

            //создаём список Sheets в Workbook
            Sheets sheets = spreadsheetDocument.WorkbookPart.Workbook.AppendChild(new Sheets());

            return spreadsheetDocument;
        }

        public static Worksheet CreateSheet(SpreadsheetDocument spreadsheetDocument, string sheetName)
        {
            //добавляем WorksheetPart в WorkbookPart
            WorksheetPart worksheetPart = spreadsheetDocument.WorkbookPart.AddNewPart<WorksheetPart>();

            worksheetPart.Worksheet = new Worksheet(new SheetData());

            //создаём новый лист
            Sheet sheet = new Sheet()
            {
                Id = spreadsheetDocument.WorkbookPart.GetIdOfPart(worksheetPart),
                SheetId = UInt32Value.FromUInt32(1),
                Name = StringValue.FromString(sheetName)
            };

            //добавляем созданный лист в список листов
            spreadsheetDocument.WorkbookPart.Workbook.Sheets.Append(sheet);
            //spreadsheetDocument.WorkbookPart.Workbook.Save();

            return worksheetPart.Worksheet;
        }

        public static void CreateHeaderFooter(Worksheet worksheetsheet, string headerText, string footerText)
        {
            if (worksheetsheet != null)
            {
                HeaderFooter headerFooter = null;

                if (!string.IsNullOrEmpty(headerText))
                {
                    headerFooter = new HeaderFooter();

                    OddHeader oddHeader = new OddHeader(headerText);
                    headerFooter.AddChild(oddHeader);
                }

                if (!string.IsNullOrEmpty(footerText))
                {
                    if (headerFooter == null)
                        headerFooter = new HeaderFooter();

                    //OddFooter oddFooter = new OddFooter("&L&\"Times New Roman,Regular\"Page &P of &N&C&\"Times New Roman,Regular\"Generated On: &D Central&R&\"Times New Roman,Regular\"Report"); //"&L&\"Лист &P, листов &N"
                    OddFooter oddFooter = new OddFooter(footerText);

                    headerFooter.AddChild(oddFooter);
                }

                if (headerFooter != null)
                    worksheetsheet.Append(headerFooter);
            }
        }

        public static void FreezePane(Worksheet worksheet, uint freezeRowCount)
        {
            //замораживает freezeRowCount записей
            //записи замораживаются как минимум с первой видимой записи в количестве freezeRowCount

            if (worksheet != null)
            {
                //вычисляем номер записи, которая будет первой прокручиваемой записью из списка всех прокручиваемых записей
                uint rowNum = 1 + freezeRowCount;

                Pane pane = new Pane()
                {
                    VerticalSplit = new DoubleValue(double.Parse(freezeRowCount.ToString())),
                    TopLeftCell = string.Concat("A", rowNum.ToString()),
                    ActivePane = PaneValues.BottomLeft,
                    State = PaneStateValues.Frozen
                };

                SheetView sheetView = new SheetView(pane)
                {
                    TabSelected = true,
                    WorkbookViewId = new UInt32Value(0u)
                };

                SheetViews sheetViews = worksheet.ChildElements.OfType<SheetViews>().FirstOrDefault();

                if (sheetViews == null)
                {
                    //важно, чтобы SheetViews располагался перед SheetData (если это не так - SheetData ссылается на SheetViews и не находит SheetViews когда SheetViews располагается после SheetData (это касается только Excel, Libre Calc прекрасно работает в такой ситуации))
                    worksheet.InsertAt(new SheetViews(sheetView), 0);
                }
            }
        }

        private static int InsertSharedStringItem(SpreadsheetDocument spreadsheetDocument, string value)
        {
            //формирование хранящихся в файле в SharedStringTable всего возможного списка значений

            //проверяем создана ли SharedStringTablePart, если она не создана - создаём её
            SharedStringTablePart shareStringPart;

            if (spreadsheetDocument.WorkbookPart.GetPartsOfType<SharedStringTablePart>().Count() > 0)
            {
                shareStringPart = spreadsheetDocument.WorkbookPart.GetPartsOfType<SharedStringTablePart>().First();
            }
            else
                shareStringPart = spreadsheetDocument.WorkbookPart.AddNewPart<SharedStringTablePart>();

            //если SharedStringTable не создана - создаём её
            if (shareStringPart.SharedStringTable == null)
                shareStringPart.SharedStringTable = new SharedStringTable();

            int i = 0;

            //ищем в SharedStringTable принятый value и возвращаем его индекс
            foreach (SharedStringItem item in shareStringPart.SharedStringTable.Elements<SharedStringItem>())
            {
                if (item.InnerText == value)
                    return i;

                i++;
            }

            //раз мы здесь - value в SharedStringItem не найден - размещаем value в SharedStringItem и возвращаем его индекс
            shareStringPart.SharedStringTable.AppendChild(new SharedStringItem(new DocumentFormat.OpenXml.Spreadsheet.Text(value)));
            //shareStringPart.SharedStringTable.Save();

            return i;
        }

        private static string ValueByNumberFormatId(CellValue value, uint numberFormatId)
        {
            //возвращает принятый value в том виде, который будет после применения numberFormatId
            if (value == null)
            {
                return string.Empty;
            }
            else
            {
                switch ((NumberFormatId)numberFormatId)
                {
                    case NumberFormatId.Date:
                        return value.TryGetDateTime(out DateTime dt) ? dt.ToString("dd.MM.yyyy") : null;

                    case NumberFormatId.Double:
                        return value.TryGetDouble(out double dbl) ? string.Format("{0:0.00}", dbl) : null;

                    case NumberFormatId.Integer:
                        return value.TryGetInt(out int i) ? i.ToString() : null;

                    default:
                        return value.InnerText;
                }
            }
        }

        private static string GetColumnName(string cellReference)
        {
            //извлекает буквенную часть обозначения столбца из принятого cellReference
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex("[A-Za-z]+");
            System.Text.RegularExpressions.Match match = regex.Match(cellReference);

            return match.Value;
        }

        private static uint GetRowIndex(string cellReference)
        {
            //извлекает цифровую часть обозначения столбца из принятого cellReference
            System.Text.RegularExpressions.Regex regex = new System.Text.RegularExpressions.Regex(@"\d+");
            System.Text.RegularExpressions.Match match = regex.Match(cellReference);

            return uint.Parse(match.Value);
        }

        public static double WidthToOpenXml(double width, double systemScale)
        {
            //пересчитываем пиксели в ширину Excel с учётом текущего масштабирования в операционной системе: https://stackoverflow.com/questions/7716078/formula-to-convert-net-pixels-to-excel-width-in-openxml-format/7902415
            return ((width - 7) / 7d + 1) / systemScale;
        }

        private static double HeightToOpenXml(double height)
        {
            //пересчитываем пиксели в высоту Excel
            return Math.Truncate(height / 1.5 * 256) / 256;
        }

        private static bool ParseInterval(string interval, out string bottom, out string top)
        {
            //получает интервал interval и разбирает его на нижнюю bottom и верхнюю top границы
            //при успешном извлечении границ из принятого interval возвращает true
            //если извлечь границы не удалось - возвращает false

            bottom = null;
            top = null;

            if (!string.IsNullOrEmpty(interval))
            {
                //разбираем описание интервала на левую и правую границу
                int delimeterIndex = interval.IndexOf(':');

                if (delimeterIndex > 0)
                {
                    bottom = interval.Substring(0, delimeterIndex);
                    top = interval.Remove(0, delimeterIndex + 1);

                    return true;
                }
                else
                    return false;
            }

            return false;
        }

        private static bool IsCellReferenceInInterval(string reference, string bottom, string top)
        {
            //проверяет вхождение reference (пример A1) в интервал [bottom, top] (пример [A1, B1])

            if (!string.IsNullOrEmpty(reference))
            {
                if (reference == bottom || reference == top)
                    return true;

                //раз мы здесь - reference не равен ни нижней, ни верхней границе интервала - проверяем входит ли reference в interval
                uint bottomColumnIndex = ColumnNameToNumber(bottom);
                uint bottomRowIndex = GetRowIndex(bottom);

                uint topColumnIndex = ColumnNameToNumber(top);
                uint topRowIndex = GetRowIndex(top);

                uint referenceColumnIndex = ColumnNameToNumber(reference);
                uint referenceRowIndex = GetRowIndex(reference);

                if (bottomRowIndex <= referenceRowIndex && referenceRowIndex <= topRowIndex && bottomColumnIndex <= referenceColumnIndex && referenceColumnIndex <= topColumnIndex)
                    return true;
            }

            return false;
        }

        private static bool IsMergedByHorizontal(string bottom, string top)
        {
            //получает описание интервала в виде [bottom, top]
            //отвечает на вопрос включает ли данный интервал более одной ячейки по горизонтали

            //интервал не объединяет больше одной ячейки по горизонтали если имя столбца в bottom и top одно и тоже
            return GetColumnName(bottom) != GetColumnName(top);
        }

        public static bool IsCellMergedByHorizontal(Worksheet worksheet, string cellReference)
        {
            //отвечает на вопрос является ли ячейка с принятым cellReference Merged ячейкой (по горизонтали объеденено больше чем одна ячейка)
            //если cellReference описывает множество ячеек которые объеденены только по вертикали - возвращает False

            if ((worksheet != null) && (!string.IsNullOrEmpty(cellReference)))
            {
                IEnumerable<MergeCells> ieMergeCells = worksheet.Descendants<MergeCells>();

                if (ieMergeCells.Count() > 0)
                {
                    MergeCells mergeCells = ieMergeCells.First();

                    if (mergeCells != null)
                    {
                        //просматриваем всё множество merged ячеек чтобы найти в какую из merged ячеек входит ячейка с cellReference
                        foreach (MergeCell mergeCell in mergeCells)
                        {
                            if (ParseInterval(mergeCell.Reference, out string bottom, out string top))
                            {
                                if (IsMergedByHorizontal(bottom, top) && IsCellReferenceInInterval(cellReference, bottom, top))
                                    return true;
                            }
                        }
                    }
                }

                return false;
            }

            return false;
        }

        public static Dictionary<uint, double> MaxWidthColumnsInPixel(SpreadsheetDocument spreadsheetDocument, Worksheet worksheet, double? rowHeight)
        {
            //формирует в возвращаемом результате множество максимальных значений ширины столбцов хранящихся в sheetData
            //игнорирует Merged ячейки, ибо они плохо влияют на результат
            //эти значения ширины перед применением надо пересчитать через WidthToOpenXml()
            //устанавливает высоту Row по самой высокой ячейке в Row если rowHeight=Null иначе сам вычисляет оптимальную высоту row и устанавливает её
            Dictionary<uint, double> result = new Dictionary<uint, double>();

            if ((spreadsheetDocument != null) && (worksheet != null))
            {
                SheetData sheetData = worksheet.Descendants<SheetData>().First();

                if (sheetData != null)
                {
                    CellFormats(spreadsheetDocument, out Fonts fonts, out CellFormats cellFormats);

                    IEnumerable<Row> rows = sheetData.Elements<Row>();

                    foreach (Row row in rows)
                    {
                        Cell[] cells = row.Elements<Cell>().ToArray();

                        //попутно вычисляем максимальную высоту ячейки в текущем row
                        double maxHeight = 0;

                        for (int i = 0; i < cells.Length; i++)
                        {
                            Cell cell = cells[i];

                            //получаем тест в соответствии с NumberFormatId из стиля, который применён к данной ячейке
                            uint styleIndex = cell.StyleIndex ?? 0;
                            CellFormat cellFormat = (CellFormat)cellFormats.ElementAt((int)styleIndex);

                            DocumentFormat.OpenXml.Spreadsheet.Font font = (DocumentFormat.OpenXml.Spreadsheet.Font)fonts.ElementAt((int)cellFormat.FontId.Value);
                            string fontName = font.FontName.Val;
                            float fontSize = (float)font.FontSize.Val;
                            FontStyle fontStyle = font.Bold.Val ? FontStyle.Bold : FontStyle.Regular;

                            string cellText = cell.CellValue == null ? string.Empty : ValueByNumberFormatId(cell.CellValue, cellFormat.NumberFormatId);

                            SizeF size;
                            using (System.Drawing.Graphics graphics = System.Drawing.Graphics.FromImage(new Bitmap(1, 1)))
                            {
                                graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.AntiAlias;
                                graphics.PageUnit = GraphicsUnit.Pixel;

                                //ширина шрифта в Excel задаётся в point, но size будет в pixel
                                size = graphics.MeasureString(cellText, new System.Drawing.Font(fontName, fontSize, fontStyle, GraphicsUnit.Point));

                                float cellPadding = size.Width * 0.0936f;
                                size.Width = (int)Math.Round(cellPadding + size.Width);
                            }

                            //используем для вычисления максимальной ширины только ячейки, не являющиеся Merged ячейками ибо учёт Merged плохо сказывается на результате
                            if (!IsCellMergedByHorizontal(worksheet, cell.CellReference))
                            {
                                //вычисляем номер столбца
                                string columnName = GetColumnName(cell.CellReference);
                                uint columnNumber = ColumnNameToNumber(columnName);

                                if (result.ContainsKey(columnNumber))
                                {
                                    if (size.Width > result[columnNumber])
                                        result[columnNumber] = size.Width;
                                }
                                else
                                    result.Add(columnNumber, size.Width);
                            }

                            if (maxHeight < size.Height)
                                maxHeight = size.Height;
                        }

                        //все ячейки Row просмотрены, высота самой высокой ячейки Row в maxHeight, устанавливаем высоту Row
                        row.CustomHeight = true;
                        double height = (rowHeight == null) ? HeightToOpenXml(maxHeight) : (double)rowHeight;
                        row.Height = DoubleValue.FromDouble(height);
                    }
                }
            }

            return result;
        }

        public static Columns Columns(Worksheet worksheet)
        {
            Columns columns = null;

            if (worksheet != null)
            {
                columns = worksheet.ChildElements.OfType<Columns>().FirstOrDefault();

                if (columns == null)
                {
                    //добавляем пустой список столбцов
                    columns = new Columns();

                    //важно, чтобы описание столбцов было вставлено перед SheetData
                    worksheet.InsertAt(columns, 0);

                    //worksheet.Save();
                }
            }

            return columns;
        }

        public static void ColumnsAutoFit(SpreadsheetDocument spreadsheetDocument, Worksheet worksheet, double? rowHeight)
        {
            Dictionary<uint, double> maxWidthColumnsInPixel = MaxWidthColumnsInPixel(spreadsheetDocument, worksheet, rowHeight);
            Columns columns = Columns(worksheet);

            if (columns.ChildElements.Count() != 0)
                columns.RemoveAllChildren();

            foreach (KeyValuePair<uint, double> entry in maxWidthColumnsInPixel)
            {
                //формируем описание столбцов в columns - устанавливаем ширину столбца
                columns.Append(new Column() { Min = UInt32Value.FromUInt32(entry.Key), Max = UInt32Value.FromUInt32(entry.Key), Width = DoubleValue.FromDouble(entry.Value), CustomWidth = BooleanValue.FromBoolean(true), BestFit = BooleanValue.FromBoolean(true) });
            }
        }

        private static Row GetRow(Worksheet worksheet, uint rowIndex)
        {
            Row result = null;

            if (worksheet != null)
            {
                IEnumerable<Row> rows = worksheet.Descendants<Row>().Where(r => r.RowIndex.Value == rowIndex);

                if (rows.Count() == 0)
                {
                    result = new Row() { RowIndex = UInt32Value.FromUInt32(rowIndex) };

                    rows = worksheet.Descendants<Row>();
                    SheetData sheetData = worksheet.Descendants<SheetData>().First();

                    switch (rows.Where(r => r.RowIndex.Value > rowIndex).Count())
                    {
                        case 0:
                            sheetData.Append(result);
                            break;

                        default:
                            uint minRowIndex = rows.Min(r => r.RowIndex.Value);
                            Row row = rows.Where(r => r.RowIndex.Value == minRowIndex).First();

                            sheetData.InsertBefore(result, row);
                            break;
                    }

                    //worksheet.Save();
                }
                else
                    result = rows.First();
            }

            return result;
        }

        public static Cell GetCell(Worksheet worksheet, uint rowIndex, uint columnNumber)
        {
            Cell result = null;

            if (worksheet != null)
            {
                Row row = GetRow(worksheet, rowIndex);

                string cellReference = string.Concat(RCColumnNumToA1ColumnNum(columnNumber), rowIndex);
                IEnumerable<Cell> cells = row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference);

                if (cells.Count() == 0)
                {
                    //ячейка с cellReference отсутствует в списке ячеек row
                    result = new Cell() { CellReference = StringValue.FromString(cellReference) };

                    if (row.Elements<Cell>().Count() == 0)
                    {
                        //созданная ячейка на данный момент станет единственной в row
                        row.Append(result);
                    }
                    else
                    {
                        //необходимо добавить созданную ячейку так, чтобы она заняла своё положение в row в соответствии со своим CellReference
                        uint columnNumberReference = ColumnNameToNumber(GetColumnName(cellReference));

                        cells = row.Elements<Cell>().Where(c => ColumnNameToNumber(GetColumnName(c.CellReference.Value)) > columnNumberReference);

                        if (cells.Count() == 0)
                        {
                            row.Append(result);
                        }
                        else
                        {
                            uint minColumnNumber = cells.Min(c => ColumnNameToNumber(GetColumnName(c.CellReference.Value)));
                            Cell cell = row.Elements<Cell>().Where(c => c.CellReference.Value == string.Concat(RCColumnNumToA1ColumnNum(minColumnNumber), rowIndex)).First();

                            row.InsertBefore(result, cell);
                        }
                    }

                    //worksheet.Save();
                }
                else
                    result = cells.First();
            }

            return result;
        }

        public static void SetCellValue(Cell cell, double value)
        {
            if (cell != null)
            {
                cell.CellValue = new CellValue(value);
                cell.DataType = CellValues.Number; //new EnumValue<CellValues>(CellValues.Number);
            }
        }

        public static void SetCellValue(Cell cell, int value)
        {
            if (cell != null)
            {
                cell.CellValue = new CellValue(value);
                cell.DataType = CellValues.Number; //new EnumValue<CellValues>();
            }
        }

        public static void SetCellValue(Cell cell, DateTime value)
        {
            if (cell != null)
            {
                cell.CellValue = new CellValue(value);
                cell.DataType = CellValues.Date; //new EnumValue<CellValues>(CellValues.Date);
            }
        }

        public static void SetCellValue(Cell cell, string value)
        {
            if (cell != null)
            {
                cell.CellValue = new CellValue(value);
                cell.DataType = CellValues.String;  //new EnumValue<CellValues>(CellValues.String);
            }
        }

        public static void SetCellValueBySharedString(SpreadsheetDocument spreadsheetDocument, Cell cell, string sharedStringValue)
        {
            if (cell != null)
            {
                //получаем индекс принятого value в SharedStringTablePart
                int indexOfSharedString = InsertSharedStringItem(spreadsheetDocument, sharedStringValue);

                cell.CellValue = new CellValue(indexOfSharedString.ToString());
                cell.DataType = CellValues.SharedString;
            }
        }

        public static Cell SetCellSharedValue(SpreadsheetDocument spreadsheetDocument, Worksheet worksheet, uint rowIndex, uint columnIndex, string sharedStringValue)
        {
            //сохранение shared string

            //вставляем ячейку в созданный лист
            Cell cell = GetCell(worksheet, rowIndex, columnIndex);

            //устанавливаем значение ячейки
            SetCellValueBySharedString(spreadsheetDocument, cell, sharedStringValue);

            //worksheet.Save();

            return cell;
        }

        public static void SetCellValue(Worksheet worksheet, uint rowIndex, uint columnIndex, object value, uint? styleIndex)
        {
            //сохранение принятого значения value в ячейку с координатами rowIndex, columnIndex

            //вставляем ячейку в созданный лист
            Cell cell = GetCell(worksheet, rowIndex, columnIndex);

            if (cell != null)
            {
                if (value != null)
                {
                    //устанавливаем значение ячейки
                    string typeOfValue = value.GetType().ToString();

                    switch (typeOfValue)
                    {
                        case "System.Double":
                            SetCellValue(cell, double.Parse(value.ToString()));
                            break;

                        case "System.Int32":
                            SetCellValue(cell, int.Parse(value.ToString()));
                            break;

                        case "System.DateTime":
                            SetCellValue(cell, DateTime.Parse(value.ToString()));
                            break;

                        default:
                            SetCellValue(cell, value.ToString());
                            break;
                    }

                    //worksheet.Save();
                }

                if (styleIndex != null)
                {
                    cell.StyleIndex = UInt32Value.FromUInt32((uint)styleIndex);

                    /*
                                    if (worksheet == null)
                                    {
                                        WorksheetPart worksheetPart = spreadsheetDocument.WorkbookPart.GetPartsOfType<WorksheetPart>().First();
                                        worksheet = worksheetPart.Worksheet;
                                    }

                                    worksheet.Save();
                    */
                }
            }
        }

        public static NumberFormatId NumberFormatIdByType(Type type)
        {
            string sType = type.ToString();

            switch (sType)
            {
                case "System.Int32":
                    return NumberFormatId.Integer;

                case "System.Double":
                case "System.Decimal":
                    return NumberFormatId.Double;

                case "System.DateTime":
                    return NumberFormatId.Date;

                default:
                    return NumberFormatId.String;
            }
        }

        public static void SetRowsHeight(Worksheet worksheet, double height)
        {
            if (worksheet != null)
            {
                IEnumerable<Row> rows = worksheet.Descendants<Row>();

                foreach (Row row in rows)
                {
                    row.CustomHeight = BooleanValue.FromBoolean(true);
                    row.Height = DoubleValue.FromDouble(height);
                }
            }
        }

        private static void SetStyleForMergedCells(Worksheet worksheet, uint rowIndexBeg, uint columnIndexBeg, uint rowIndexEnd, uint columnIndexEnd, UInt32Value styleIndex)
        {
            //установка стиля для множества объединённых ячеек
            if (worksheet != null)
            {
                IEnumerable<Row> rows = worksheet.Descendants<Row>().Where(r => r.RowIndex.Value >= rowIndexBeg && r.RowIndex.Value <= rowIndexEnd);

                foreach (Row row in rows)
                {
                    for (uint columnIndex = columnIndexBeg; columnIndex <= columnIndexEnd; columnIndex++)
                    {
                        string cellReference = string.Concat(RCColumnNumToA1ColumnNum(columnIndex), row.RowIndex.Value);

                        Cell cell = row.Elements<Cell>().Where(c => c.CellReference.Value == cellReference).FirstOrDefault();
                        cell.StyleIndex = styleIndex;
                    }
                }
            }
        }

        public static void MergeCells(Worksheet worksheet, uint rowIndexBeg, uint columnIndexBeg, uint rowIndexEnd, uint columnIndexEnd, object value, uint? styleIndex)
        {
            if (worksheet != null)
            {
                //все объединяемые ячейки должны быть созданы
                for (uint rowIndex = rowIndexBeg; rowIndex <= rowIndexEnd; rowIndex++)
                {
                    for (uint columnIndex = columnIndexBeg; columnIndex <= columnIndexEnd; columnIndex++)
                    {
                        Cell cell = GetCell(worksheet, rowIndex, columnIndex);

                        if (styleIndex != null)
                            cell.StyleIndex = UInt32Value.FromUInt32((uint)styleIndex);
                    }
                }

                //все объединяемые ячейки успешно созданы и в них установлен принятый styleIndex
                //в начальную ячейку пишем значение, которое будет видно в объединённых ячейках
                SetCellValue(worksheet, rowIndexBeg, columnIndexBeg, value, null);

                MergeCells mergeCells;

                if (worksheet.Descendants<MergeCells>().Count() > 0)
                {
                    mergeCells = worksheet.Descendants<MergeCells>().First();
                }
                else
                {
                    mergeCells = new MergeCells();
                    worksheet.InsertAfter(mergeCells, worksheet.Descendants<SheetData>().First());
                }

                string reference = string.Concat(RCColumnNumToA1ColumnNum(columnIndexBeg), rowIndexBeg, ":", RCColumnNumToA1ColumnNum(columnIndexEnd), rowIndexEnd);
                mergeCells.Append(new MergeCell() { Reference = StringValue.FromString(reference) });
                mergeCells.Count = (mergeCells.Count == null) ? UInt32Value.FromUInt32(1) : UInt32Value.FromUInt32(mergeCells.Count.Value + 1);

                //worksheet.Save();
            }
        }

        private static DocumentFormat.OpenXml.Spreadsheet.Font NewFont(string fontName, byte fontSize, bool bold)
        {
            DocumentFormat.OpenXml.Spreadsheet.Font result = new DocumentFormat.OpenXml.Spreadsheet.Font()
            {
                FontName = new FontName() { Val = StringValue.FromString(fontName) },
                FontSize = new FontSize() { Val = DoubleValue.FromDouble(fontSize) },
                Bold = new Bold() { Val = BooleanValue.FromBoolean(bold) },
                FontFamilyNumbering = new FontFamilyNumbering() { Val = Int32Value.FromInt32(2) }
            };

            return result;
        }

        private static Fill NewFill(PatternValues patternType, string hexForegroundColor)
        {
            Fill result = new Fill()
            {
                PatternFill = new PatternFill(new ForegroundColor() { Rgb = new HexBinaryValue() { Value = hexForegroundColor } })
                {
                    PatternType = patternType
                }
            };

            /*
            Fill result = new Fill();
            //ForegroundColor foregroundColor = new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "8EA9DB" } };
            //result.Append(foregroundColor);

            PatternFill patternFill = new PatternFill(new ForegroundColor() { Rgb = new HexBinaryValue() { Value = "8EA9DB" } }) { PatternType = PatternValues.Solid };

            //BackgroundColor backgroundColor = new BackgroundColor() { Indexed = (UInt32Value)64U };


            //patternFill.Append(backgroundColor);
            result.Append(patternFill);
            */
            return result;
        }

        private static Border NewBorder(bool borderOn)
        {
            Border result = null;

            if (borderOn)
            {
                result = new Border()
                {
                    LeftBorder = new LeftBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = BooleanValue.FromBoolean(true) }) { Style = BorderStyleValues.Hair },
                    RightBorder = new RightBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = BooleanValue.FromBoolean(true) }) { Style = BorderStyleValues.Hair },
                    TopBorder = new TopBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = BooleanValue.FromBoolean(true) }) { Style = BorderStyleValues.Hair },
                    BottomBorder = new BottomBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = BooleanValue.FromBoolean(true) }) { Style = BorderStyleValues.Hair },
                    DiagonalBorder = new DiagonalBorder(new DocumentFormat.OpenXml.Spreadsheet.Color() { Auto = BooleanValue.FromBoolean(true) }) { Style = BorderStyleValues.None }
                };
            }
            else
            {
                result = new Border()
                {
                    LeftBorder = new LeftBorder() { Style = BorderStyleValues.None },
                    RightBorder = new RightBorder() { Style = BorderStyleValues.None },
                    TopBorder = new TopBorder() { Style = BorderStyleValues.None },
                    BottomBorder = new BottomBorder() { Style = BorderStyleValues.None },
                    DiagonalBorder = new DiagonalBorder() { Style = BorderStyleValues.None }
                };
            }

            return result;
        }

        private static CellFormat NewCellFormat(uint borderId, uint fillId, uint fontId, uint numberFormatId, HorizontalAlignmentValues horizontalAlignment, VerticalAlignmentValues verticalAlignment)
        {
            CellFormat result = new CellFormat()
            {
                BorderId = UInt32Value.FromUInt32(borderId),
                FillId = UInt32Value.FromUInt32(fillId),
                FontId = UInt32Value.FromUInt32(fontId),
                NumberFormatId = UInt32Value.FromUInt32(numberFormatId),
                FormatId = UInt32Value.FromUInt32(0),
                ApplyNumberFormat = BooleanValue.FromBoolean(true),
                ApplyBorder = BooleanValue.FromBoolean(true),
                ApplyFill = BooleanValue.FromBoolean(true),
                ApplyFont = BooleanValue.FromBoolean(true),
                Alignment = new Alignment()
                {
                    Horizontal = horizontalAlignment,
                    Vertical = verticalAlignment
                }
            };

            return result;
        }

        private static void CellFormats(SpreadsheetDocument spreadsheetDocument, out Fonts fonts, out CellFormats cellFormats)
        {
            fonts = null;
            cellFormats = null;

            if (spreadsheetDocument != null)
            {
                WorkbookStylesPart workbookStylesPart = spreadsheetDocument.WorkbookPart.WorkbookStylesPart;

                if (workbookStylesPart != null)
                {
                    if (workbookStylesPart.Stylesheet != null)
                    {
                        fonts = workbookStylesPart.Stylesheet.Fonts;
                        cellFormats = workbookStylesPart.Stylesheet.CellFormats;
                    }
                }
            }
        }

        private static uint? FindFont(Fonts fonts, string fontName, byte fontSize, bool bold)
        {
            uint? result = null;

            if (fonts != null)
            {
                uint index = 0;

                foreach (DocumentFormat.OpenXml.Spreadsheet.Font font in fonts)
                {
                    if ((font.FontName.Val.Value == fontName) && (font.FontSize.Val.Value == fontSize) && (font.Bold.Val.Value == bold))
                    {
                        result = index;
                        break;
                    }

                    index++;
                }
            }

            return result;
        }

        private static uint? FindFill(Fills fills, PatternValues patternType, string hexForegroundColor)
        {
            uint? result = null;

            if (fills != null)
            {
                uint index = 0;

                foreach (Fill fill in fills)
                {
                    if ((fill.PatternFill.PatternType == patternType) && (fill.PatternFill.ForegroundColor.Rgb.Value == hexForegroundColor))
                    {
                        result = index;
                        break;
                    }

                    index++;
                }
            }

            return result;
        }

        private static uint? FindBorder(Borders borders, bool borderOn)
        {
            uint? result = null;

            if (borders != null)
            {
                uint index = 0;

                foreach (Border border in borders)
                {
                    if (borderOn)
                    {
                        //ищем описание стиля очерченых ячеек
                        if ((border.LeftBorder.Style == BorderStyleValues.Thin) && (border.RightBorder.Style == BorderStyleValues.Thin) && (border.TopBorder.Style == BorderStyleValues.Thin) && (border.BottomBorder.Style == BorderStyleValues.Thin) && (border.DiagonalBorder.Style == BorderStyleValues.None))
                        {
                            result = index;
                            break;
                        }
                    }
                    else
                    {
                        //ищем описание стиля ячеек не имеющих очерчивания
                        if ((border.LeftBorder.Style == BorderStyleValues.None) && (border.RightBorder.Style == BorderStyleValues.None) && (border.TopBorder.Style == BorderStyleValues.None) && (border.BottomBorder.Style == BorderStyleValues.None) && (border.DiagonalBorder.Style == BorderStyleValues.None))
                        {
                            result = index;
                            break;
                        }
                    }

                    index++;
                }
            }

            return result;
        }

        private static uint? FindCellFormat(CellFormats cellFormats, uint borderIndex, uint fillIndex, uint fontIndex, uint numberFormatId, HorizontalAlignmentValues horizontalAlignment, VerticalAlignmentValues verticalAlignment)
        {
            uint? result = null;

            if (cellFormats != null)
            {
                uint index = 0;

                foreach (CellFormat cellFormat in cellFormats)
                {
                    if ((cellFormat.BorderId.Value == borderIndex) && (cellFormat.FillId.Value == fillIndex) && (cellFormat.FontId.Value == fontIndex) && (cellFormat.NumberFormatId.Value == numberFormatId) && (cellFormat.FormatId.Value == 0) && (cellFormat.ApplyNumberFormat.Value == true) && (cellFormat.Alignment.Horizontal == horizontalAlignment) && (cellFormat.Alignment.Vertical == verticalAlignment))
                    {
                        result = index;
                        break;
                    }

                    index++;
                }
            }

            return result;
        }

        private static Stylesheet StyleObjects(SpreadsheetDocument spreadsheetDocument, out Fonts fonts, out Fills fills, out Borders borders, out CellFormats cellFormats)
        {
            fonts = null;
            fills = null;
            borders = null;
            cellFormats = null;

            if (spreadsheetDocument != null)
            {
                WorkbookStylesPart workbookStylesPart = spreadsheetDocument.WorkbookPart.WorkbookStylesPart;

                if (workbookStylesPart == null)
                    workbookStylesPart = spreadsheetDocument.WorkbookPart.AddNewPart<WorkbookStylesPart>();

                if (workbookStylesPart.Stylesheet == null)
                {
                    workbookStylesPart.Stylesheet = new Stylesheet();

                    fonts = new Fonts();
                    fills = new Fills();
                    borders = new Borders();
                    cellFormats = new CellFormats();

                    workbookStylesPart.Stylesheet.Fonts = fonts;
                    workbookStylesPart.Stylesheet.Fills = fills;
                    workbookStylesPart.Stylesheet.Borders = borders;
                    workbookStylesPart.Stylesheet.CellFormats = cellFormats;
                }
                else
                {
                    fonts = workbookStylesPart.Stylesheet.Fonts;
                    fills = workbookStylesPart.Stylesheet.Fills;
                    borders = workbookStylesPart.Stylesheet.Borders;
                    cellFormats = workbookStylesPart.Stylesheet.CellFormats;
                }

                return workbookStylesPart.Stylesheet;
            }

            return null;
        }

        public static uint CreateStyle(SpreadsheetDocument spreadsheetDocument, string fontName, byte fontSize, bool bold, PatternValues patternType, string hexForegroundColor, bool borderOn, HorizontalAlignmentValues horizontalAlignment, VerticalAlignmentValues verticalAlignment, NumberFormatId numberFormatId)
        {
            Stylesheet stylesheet = StyleObjects(spreadsheetDocument, out Fonts fonts, out Fills fills, out Borders borders, out CellFormats cellFormats);

            uint? fontIndex = FindFont(fonts, fontName, fontSize, bold);

            if (fontIndex == null)
            {
                DocumentFormat.OpenXml.Spreadsheet.Font font = NewFont(fontName, fontSize, bold);
                fonts.AppendChild(font);
                fonts.Count = (fonts.Count == null) ? UInt32Value.FromUInt32(1) : UInt32Value.FromUInt32(fonts.Count.Value + 1);
                fontIndex = fonts.Count.Value - 1;
            }

            uint? fillIndex = FindFill(fills, patternType, hexForegroundColor);

            if (fillIndex == null)
            {
                Fill fill = NewFill(patternType, hexForegroundColor);
                fills.AppendChild(fill);
                fills.Count = (fills.Count == null) ? UInt32Value.FromUInt32(1) : UInt32Value.FromUInt32(fills.Count.Value + 1);
                fillIndex = fills.Count.Value - 1;
            }

            uint? borderIndex = FindBorder(borders, borderOn);

            if (borderIndex == null)
            {
                Border border = NewBorder(borderOn);
                borders.AppendChild(border);
                borders.Count = (borders.Count == null) ? UInt32Value.FromUInt32(1) : UInt32Value.FromUInt32(borders.Count.Value + 1);
                borderIndex = borders.Count.Value - 1;
            }

            uint? cellFormatIndex = FindCellFormat(cellFormats, (uint)borderIndex, (uint)fillIndex, (uint)fontIndex, (uint)numberFormatId, horizontalAlignment, verticalAlignment);

            if (cellFormatIndex == null)
            {
                CellFormat cellFormat = NewCellFormat((uint)borderIndex, (uint)fillIndex, (uint)fontIndex, (uint)numberFormatId, horizontalAlignment, verticalAlignment);
                cellFormats.AppendChild(cellFormat);
                cellFormats.Count = (cellFormats.Count == null) ? UInt32Value.FromUInt32(1) : UInt32Value.FromUInt32(cellFormats.Count.Value + 1);
                cellFormatIndex = cellFormats.Count.Value - 1;
            }

            /*
            if (anyCreated)
                stylesheet.Save();
            */

            return (uint)cellFormatIndex;
        }
    }
}