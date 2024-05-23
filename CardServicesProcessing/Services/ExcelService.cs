using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using Microsoft.Extensions.Logging;
using System.Data;
using System.Diagnostics;
using DataTable = System.Data.DataTable;

namespace CardServicesProcessor.Services
{
    public static class ExcelService
    {
        /// <summary>
        /// Reads an Excel worksheet
        /// </summary>
        /// <param name="filePath">fully qualified path to excel file</param>
        /// <param name="worksheetName">name of the worksheet to read</param>
        /// <returns>Datatable</returns>
        /// <exception cref="ArgumentException"></exception>
        public static DataTable ReadWorksheetToDataTable(string filePath, string worksheetName)
        {
            DataTable dataTable = new();

            using (XLWorkbook workbook = new(filePath))
            {
                IXLWorksheet worksheet = workbook.Worksheet(worksheetName) ?? throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file.");

                // Get the headers from the first row
                List<string> headers = worksheet.FirstRow().CellsUsed().Select(cell => cell.Value.ToString().Trim()).ToList();

                // Add columns to the DataTable
                foreach (string? header in headers)
                {
                    _ = dataTable.Columns.Add(header);
                }

                // Add rows to the DataTable
                IEnumerable<IXLRow> rows = worksheet.RowsUsed().Skip(1);
                foreach (IXLRow? row in rows)
                {
                    DataRow dataRow = dataTable.Rows.Add();
                    for (int i = 0; i < headers.Count; i++)
                    {
                        dataRow[i] = row.Cell(i + 1).Value.ToString().Trim();
                    }
                }
            }

            return dataTable;
        }

        /// <summary>
        /// Copies data from one worksheet to another
        /// </summary>
        /// <param name="sourceWorksheet">worksheet containing the data to be copied over</param>
        /// <param name="targetWorksheet">worksheet to copy the data to</param>
        /// <param name="startRow">the row to start copying the data from</param>
        public static void CopyWorksheetData(IXLWorksheet sourceWorksheet, IXLWorksheet targetWorksheet, int startRow = 1)
        {
            // Get the range of used cells in the source worksheet
            IXLRange sourceRange = sourceWorksheet.RangeUsed();

            // Get the range of rows to copy
            IXLRangeRows sourceRows = sourceRange.Rows();

            // Copy each row to the target worksheet
            int targetRow = startRow;
            foreach (IXLRangeRow? sourceRow in sourceRows)
            {
                // Copy the row to the target worksheet
                _ = sourceRow.CopyTo(targetWorksheet.Row(targetRow));

                // Increment the target row number
                targetRow++;
            }
        }

        /// <summary>
        /// Fill in missing data for outliers
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="caseTicketNbr"></param>
        /// <param name="totalRequestedAmount"></param>
        public static void FillOutlierData(this DataRow dataRow, string? caseTicketNbr)
        {
            if (!caseTicketNbr.IsTruthy())
            {
                return;
            }

            switch (caseTicketNbr)
            {
                case "NBCM202400076542-1":
                case "NBCM202400081906-1":
                case "NBCM202400082920-1":
                case "NBCM202400083104-1":
                case "NBCM202400083283-1":
                case "NBCM202400083445-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Rewards);
                    break;
                case "EHCM202400067062-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 50.ToString("C2"));
                    //dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, null);
                    break;
                case "EHCM202400068489-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 16.95.ToString("C2"));
                    //dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, null);
                    break;
                case "EHCM202400070637-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 13.50.ToString("C2"));
                    //dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Utilities);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, null);
                    break;
                case "EHCM202400070871-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 178.40.ToString("C2"));
                    //dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, null);
                    break;
                case "EHCM202400070901-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 1.29.ToString("C2"));
                    //dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.OTC);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved); // IT issue w/ wallet dropdown
                    dataRow.FormatForExcel(ColumnNames.DenialReason, null);
                    break;
                case "EHCM202400062886-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 300.ToString("C2"));
                    break;
                case "EHCM202400068352-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 50.ToString("C2"));
                    break;
                case "EHCM202400070732-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.BenefitUtilized);
                    break;
                case "EHCM202400070290-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.IneligibleRetailer);
                    break;
                case "EHCM202400068493-1":
                    dataRow.FormatForExcel(ColumnNames.CaseStatus, Statuses.Closed);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.NotReimbursement);
                    break;
                case "EHCM202400071230-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.Duplicate);
                    break;
                case "NBCM202400083351-1":
                case "NBCM202400083346-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 150.ToString("C2"));
                    break;
                case "NBCM202400075178-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.NotReimbursement);
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    break;
                case "NBCM202400075627-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.IneligibleRetailer);
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    break;
                case "NBCM202400079733-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 27.39.ToString("C2"));
                    break;
                case "NBCM202400086471-1":
                case "NBCM202400085882-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.Expired);
                    break;
                case "NBCM202400087468-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.Duplicate);
                    break;
                case "NBCM202400075011-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
                    break;
                case "NBCM202400091233-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    dataRow.FormatForExcel(ColumnNames.DenialReason, Constants.IneligibleService);
                    break;
                case "EHCM202400074748-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 351.41.ToString("C2"));
                    break;
            }
        }

        /// <summary>
        /// Apply default filters on Excel worksheet
        /// </summary>
        /// <param name="dataTable"></param>
        /// <param name="filePath"></param>
        /// <param name="sheetName"></param>
        /// <param name="sheetPos"></param>
        public static void ApplyFiltersAndSaveReport(DataTable dataTable, string filePath, string sheetName, int sheetPos)
        {
            // Check if the worksheet exists
            //DeleteWorkbook(filePath);

            XLWorkbook workbook = CreateWorkbook(filePath);

            // Add a new worksheet with the same name
            IXLWorksheet worksheet = ClearWorksheet(workbook, sheetName, sheetPos);
            //IXLWorksheet worksheet = CreateWorksheet(workbook, sheetName, sheetPos).SetTabColor(XLColor.Yellow);

            // Remove empty columns after the specified index
            RemoveEmptyColumns(dataTable, ColumnNames.AssignedTo);

            // Insert DataTable into the worksheet
            _ = worksheet.Cell(1, 1).InsertTable(dataTable, false);

            // Format header row
            FormatHeaderRow(worksheet.Row(1));

            // Freeze header row
            FreezeHeaderRow(worksheet);

            // Set number formats for specific columns
            SetNumberFormats(worksheet);

            // Set column widths
            SetColumnWidths(worksheet);

            // Apply filters to specific columns
            //ApplyFilters(worksheet, 14, "Reimbursement");
            //ApplyFilters(worksheet, 5, "2024");

            SaveWorkbook(filePath, workbook);
        }

        private static IXLWorksheet ClearWorksheet(XLWorkbook workbook, string sheetName, int sheetPos)
        {
            // Get the worksheet or add a new one if it doesn't exist
            IXLWorksheet? worksheet = workbook.Worksheets.FirstOrDefault(w => w.Name == sheetName);
            if (worksheet == null)
            {
                worksheet = workbook.Worksheets.Add(sheetName, sheetPos);
            }
            else
            {
                // Clear existing data if the worksheet already exists
                worksheet.AutoFilter.Clear();
                _ = worksheet.Clear();
            }

            return worksheet;
        }

        private static void SaveWorkbook(string filePath, XLWorkbook workbook)
        {
            // Save the workbook
            if (!string.IsNullOrEmpty(filePath))
            {
                // If the workbook has a file path, it means it has been previously saved
                workbook.SaveAs(filePath); // Use Save() to save the workbook using its existing file name or location
            }
            else
            {
                // If the workbook has not been previously saved, use SaveAs()
                workbook.SaveAs(filePath); // Provide the desired file path
            }
        }

        private static void RemoveEmptyColumns(DataTable dataTable, string columnName)
        {
            int totalColumns = dataTable.Columns.Count;
            for (int i = totalColumns - 1; i > dataTable.Columns.IndexOf(columnName); i--)
            {
                dataTable.Columns.RemoveAt(i);
            }
        }

        private static void FormatHeaderRow(IXLRow headerRow)
        {
            headerRow.Style.Fill.BackgroundColor = XLColor.Orange;
            headerRow.Style.Font.Bold = true;
        }

        private static void FreezeHeaderRow(IXLWorksheet worksheet)
        {
            worksheet.SheetView.FreezeRows(1);
        }

        private static void SetNumberFormats(IXLWorksheet worksheet)
        {
            foreach (IXLColumn column in worksheet.Columns())
            {
                XLDataType dataType = GetColumnType(column);
                string format = GetFormatForDataType(dataType);
                column.Style.NumberFormat.Format = format;
            }
        }

        private static XLDataType GetColumnType(IXLColumn column)
        {
            // Check the data type of the cells in the column
            bool isNumeric = column.Cells().All(cell => cell.DataType == XLDataType.Number);
            bool isDate = column.Cells().All(cell => cell.DataType == XLDataType.DateTime);

            return isNumeric ? XLDataType.Number : isDate ? XLDataType.DateTime : XLDataType.Text;
        }

        private static string GetFormatForDataType(XLDataType dataType)
        {
            return dataType switch
            {
                XLDataType.Number => "$#,##0.00",
                XLDataType.DateTime => "MM/dd/yyyy",
                XLDataType.Text => "@",// General text format
                _ => "",
            };
        }

        private static void SetColumnWidths(IXLWorksheet worksheet, int width = 20)
        {
            foreach (IXLColumn column in worksheet.ColumnsUsed())
            {
                column.Width = width;
            }
        }

        private static void ApplyFilters(IXLWorksheet worksheet, int columnIndex, string filterCriteria)
        {
            
            _ = worksheet.SetAutoFilter().Column(columnIndex).AddFilter(filterCriteria);
        }

        public static XLWorkbook CreateWorkbook(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            return File.Exists(filePath) ? new XLWorkbook(filePath) : new XLWorkbook();
        }

        /// <summary>
        /// Opens the Excel file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public static void OpenExcel(string filePath, ILogger log)
        {
            // Path to Excel executable
            string excelPath = File.Exists(@"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE") ?
                @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE" :
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE";

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            // Start Excel process with file and sheet arguments
            ProcessStartInfo startInfo = new()
            {
                FileName = $"\"{excelPath}\"",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false
            };

            int maxRetries = 25;
            int attempts = 0;
            int retryDelayMilliseconds = 1000;

            while (attempts < maxRetries)
            {
                try
                {
                    Process.Start(startInfo);
                    log.LogInformation("Process started successfully.");
                    break; // Exit the loop if process starts successfully
                }
                catch (Exception ex)
                {
                    log.LogInformation($"Failed to start process: {ex.Message}\nTrying again...");
                    attempts++;
                    Thread.Sleep(retryDelayMilliseconds); // Wait before next retry
                }
            }
        }

        public static void DeleteWorkbook(string filePath)
        {
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }

        public static T ReadFromExcel<T>(string filePath) where T : new()
        {
            T result = new();

            using (XLWorkbook workbook = new(filePath))
            {
                if (result is CheckIssuance checkIssuance)
                {
                    checkIssuance.RawData = ReadSheet<RawData>(workbook, "RawData");
                    checkIssuance.MemberMailingInfos = ReadSheet<MemberMailingInfo>(workbook, "MemberMailingInfo");
                    checkIssuance.MemberCheckReimbursements = ReadSheet<MemberCheckReimbursement>(workbook, "MemberCheckReimbursement");
                }
                // Add more conditions for other types if necessary
            }

            return result;
        }

        private static List<TItem> ReadSheet<TItem>(XLWorkbook workbook, string sheetName) where TItem : new()
        {
            List<TItem> result = [];
            IXLWorksheet? worksheet = workbook.Worksheets.FirstOrDefault(ws => ws.Name == sheetName);

            if (worksheet != null)
            {
                foreach (IXLRow? row in worksheet.RowsUsed())
                {
                    TItem item = new();
                    // Implement your mapping logic here based on the structure of TItem
                    // Populate item properties with cell values from the row
                    // Example: item.Property1 = row.Cell(1).Value;
                    result.Add(item);
                }
            }

            return result;
        }

        public static void AddToExcel<T>(T data) where T : CheckIssuance
        {
            DataTable rawData = data.RawData.ToDataTable();
            DataTable memberMailingInfo = data.MemberMailingInfos.ToDataTable();
            DataTable memberCheckReimbursements = data.MemberCheckReimbursements.ToDataTable();
            List<DataTable> dataTables =
            [
                rawData,
                memberMailingInfo,
                memberCheckReimbursements
            ];

            // Add or update data in the existing sheets
            AddDataToWorksheets(dataTables);
        }

        private static void AddDataToWorksheets<T>(List<T> dataTables) where T : DataTable
        {
            using XLWorkbook workbook = CreateWorkbook(CheckIssuanceConstants.FilePathCurr);

            // Add data to each worksheet
            for (int i = 0; i < dataTables.Count; i++)
            {
                AddDataToWorksheet(dataTables[i], workbook, CheckIssuanceConstants.SheetIndexToNameMap[i + 1]);
            }

            // Save the workbook
            workbook.SaveAs(CheckIssuanceConstants.FilePathCurr);
        }

        private static void AddDataToWorksheet<T>(T dt, XLWorkbook workbook, string sheetName) where T : DataTable
        {
            DataTable dtPrev = ReadWorksheetToDataTable(CheckIssuanceConstants.FilePathPrev, sheetName);
            IXLWorksheet worksheet = CreateWorksheet(workbook, sheetName);

            // Find the first empty row in the worksheet
            _ = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 1;

            // Insert the data into the worksheet starting from the next empty row
            _ = worksheet.Tables.FirstOrDefault();

            InsertIntoExcelWithComparison(dt, worksheet, dtPrev);

            /*if (existingTable == null)
            {
                // If the table doesn't exist, then insert it
                _ = worksheet.Cell(startRow, 1).InsertTable(dt);
            }
            else
            {
                _ = worksheet.Cell(existingTable.RangeAddress.LastAddress.RowNumber + 1, 1).InsertTable(dt);
            }*/
        }

        public static IXLWorksheet CreateWorksheet(XLWorkbook workbook, string sheetName, int sheetPos = 1)
        {
            // Get the worksheet by name
            bool worksheetExists = workbook.Worksheets.TryGetWorksheet(sheetName, out IXLWorksheet worksheet);

            if (!worksheetExists)
            {
                worksheet = workbook.AddWorksheet(sheetName, sheetPos);
            }

            return worksheet;
        }

        public static void InsertIntoExcelWithComparison(DataTable sourceDataTable, IXLWorksheet worksheet, DataTable comparisonDataTable)
        {
            string columnName = worksheet.Name switch
            {
                CheckIssuanceConstants.RawData => ColumnNames.TxnReferenceId,
                CheckIssuanceConstants.MemberMailingInfo => ColumnNames.VendorName,
                CheckIssuanceConstants.MemberCheckReimbursement => ColumnNames.CaseNumber,
                _ => throw new NotImplementedException(),
            };

            // Assuming the first row contains headers
            bool isFirstRow = true;

            // Iterate over each row in the source DataTable
            foreach (DataRow sourceRow in sourceDataTable.Rows)
            {
                // Perform your comparison with the values in the comparison DataTable
                bool shouldInsert = true;
                foreach (DataRow comparisonRow in comparisonDataTable.Rows)
                {
                    // Compare values from source and comparison rows
                    // Example: If sourceRow["ColumnName"] matches comparisonRow["ColumnName"], then set shouldInsert to false
                    // Adjust the comparison logic based on your specific requirements
                    if (sourceRow[columnName].Equals(comparisonRow[columnName]))
                    {
                        shouldInsert = false;
                        break; // No need to continue looping through comparison rows if a match is found
                    }
                }

                // If the row meets the criteria for insertion, add it to the worksheet
                if (shouldInsert)
                {
                    if (isFirstRow)
                    {
                        // Add headers if it's the first row
                        for (int i = 0; i < sourceDataTable.Columns.Count; i++)
                        {
                            worksheet.Cell(1, i + 1).Value = sourceDataTable.Columns[i].ColumnName;
                        }
                        isFirstRow = false;
                    }

                    // Insert data row into the worksheet
                    int nextRow = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 1;
                    for (int i = 0; i < sourceDataTable.Columns.Count; i++)
                    {
                        worksheet.Cell(nextRow, i + 1).Value = sourceRow[i].ToString();
                    }
                }
            }
        }

        public static void WriteToExcel(DataTable dt, string filePath)
        {
            // Create a new Excel workbook
            using (XLWorkbook wb = new())
            {
                // Add a worksheet to the workbook
                IXLWorksheet ws = wb.Worksheets.Add("Sheet1");

                // Write DataTable headers to the first row of the worksheet
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    ws.Cell(1, i + 1).Value = dt.Columns[i].ColumnName;
                }

                // Write DataTable data to the worksheet
                for (int i = 0; i < dt.Rows.Count; i++)
                {
                    for (int j = 0; j < dt.Columns.Count; j++)
                    {
                        ws.Cell(i + 2, j + 1).Value = dt.Rows[i][j].ToString();
                    }
                }

                // Save the workbook
                wb.SaveAs(filePath);
            }

            Console.WriteLine("Excel file with DataTable has been created.");
        }
    }
}
