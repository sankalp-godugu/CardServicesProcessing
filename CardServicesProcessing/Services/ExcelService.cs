using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using System.Data;
using System.Diagnostics;

namespace CardServicesProcessor.Services
{
    public static class ExcelService
    {
        public static DataTable ReadWorksheetToDataTable(string filePath, string sheetName)
        {
            DataTable dataTable = new();

            using (XLWorkbook workbook = new(filePath))
            {
                IXLWorksheet worksheet = workbook.Worksheet(sheetName);

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

        public static DataTable ReadPrevCheckIssuanceReport(string filePath, string worksheetName)
        {
            DataTable dataTable = new();

            // Load the Excel file
            using (XLWorkbook workbook = CreateWorkbook(filePath))
            {
                // Get the worksheet by name
                IXLWorksheet worksheet = workbook.Worksheet(worksheetName) ?? throw new ArgumentException($"Worksheet '{worksheetName}' not found in the Excel file.");

                // Assume the first row contains column headers
                bool isFirstRow = true;

                // Iterate over each row in the worksheet
                foreach (IXLRow row in worksheet.RowsUsed())
                {
                    // If it's the first row, add column headers to the DataTable
                    if (isFirstRow)
                    {
                        foreach (IXLCell cell in row.CellsUsed())
                        {
                            _ = dataTable.Columns.Add(cell.Value.ToString().Trim());
                        }
                        isFirstRow = false;
                    }
                    else
                    {
                        // Add data rows to the DataTable
                        DataRow newRow = dataTable.Rows.Add();
                        int columnIndex = 0;
                        foreach (IXLCell cell in row.CellsUsed())
                        {
                            newRow[columnIndex++] = cell.Value.ToString().Trim();
                        }
                    }
                }
            }
            return dataTable;
        }

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

        public static void FillOutlierData(this DataRow dataRow, string? caseTicketNumber, decimal? totalRequestedAmount)
        {
            if (!caseTicketNumber.IsTruthy())
            {
                return;
            }

            // Handle specific caseTicketNumbers
            switch (caseTicketNumber)
            {
                case "EHCM202400067062-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 50.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    break;
                case "EHCM202400068489-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 16.95.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    break;
                case "EHCM202400070637-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 13.50.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Utilities);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    break;
                case "EHCM202400070871-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 178.40.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved);
                    break;
                case "EHCM202400070901-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 1.29.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.OTC);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Approved); // IT issue w/ wallet dropdown
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
                    dataRow.FormatForExcel(ColumnNames.ClosingComments, null);
                    break;
                case "EHCM202400070290-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, DenialReasons.IneligibleRetailer);
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    break;
                case "EHCM202400068493-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, DenialReasons.NotReimbursement);
                    dataRow.FormatForExcel(ColumnNames.CaseStatus, Statuses.Closed);
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, Statuses.Declined);
                    break;
                case "EHCM202400063018-1":
                case "EHCM202400065349-1":
                case "EHCM202400063182-1":
                case "EHCM202400070873-1":
                case "EHCM202400070955-1":
                case "EHCM202400070803-1":
                case "EHCM202400070761-1":
                case "EHCM202400070772-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Utilities);
                    break;
                case "EHCM202400067263-1":
                case "EHCM202400066272-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ActiveFitness);
                    break;
                case "EHCM202400068961-1":
                case "EHCM202400070137-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.AssistiveDevices);
                    break;
                case "EHCM202400070711-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
                    break;
                case "EHCM202400069504-1":
                case "EHCM202400069623-1":
                case "EHCM202400069818-1":
                case "EHCM202400065264-1":
                case "EHCM202400062994-1":
                case "EHCM202400070836-1":
                case "EHCM202400070820-1":
                case "EHCM202400070892-1":
                case "EHCM202400070908-1":
                case "EHCM202400071226-1":
                case "NBCM202400082278-1":
                case "NBCM202400082281-1":
                case "NBCM202400082283-1":
                case "NBCM202400082285-1":
                case "NBCM202400082286-1":
                case "NBCM202400082287-1":
                case "NBCM202400082288-1":
                case "NBCM202400082289-1":
                case "NBCM202400084107-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    break;
                case "EHCM202400064404-1":
                case "EHCM202400066581-1":
                case "EHCM202400069229-1":
                case "EHCM202400070780-1":
                case "EHCM202400070880-1":
                case "EHCM202400070832-1":
                case "EHCM202400070860-1":
                case "EHCM202400070984-1":
                case "EHCM202400070801-1":
                case "EHCM202400063415-1":
                case "EHCM202400066888-1":
                case "EHCM202400070736-1":
                case "EHCM202400070754-1":
                case "EHCM202400070715-1":
                case "NBCM202400087650-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.OTC);
                    break;
                case "EHCM202400071155-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ServiceDog);
                    break;
                case "EHCM202400070738-1":
                case "EHCM202400070768-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Unknown);
                    break;
                case "NBCM202400076542-1":
                case "NBCM202400081906-1":
                case "NBCM202400082920-1":
                case "NBCM202400083104-1":
                case "NBCM202400083283-1":
                case "NBCM202400083445-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Rewards);
                    break;
                case "NBCM202400083351-1":
                case "NBCM202400083346-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 150.ToString("C2"));
                    break;
            }
        }

        public static void ApplyFiltersAndSaveReport(DataTable dataTable, string filePath, string sheetName, int sheetPos)
        {
            XLWorkbook workbook = CreateWorkbook(filePath);

            // Check if the worksheet exists
            if (workbook.TryGetWorksheet(sheetName, out _))
            {
                // If it exists, delete it
                workbook.Worksheets.Delete(sheetName);
            }

            // Add a new worksheet with the same name
            IXLWorksheet worksheet = workbook.Worksheets.Add(sheetName, sheetPos).SetTabColor(XLColor.Yellow);

            // Remove empty columns (if any)
            int totalColumns = dataTable.Columns.Count;
            // Remove columns after the startIndex
            for (int i = totalColumns - 1; i > dataTable.Columns.IndexOf("Closing Comments"); i--)
            {
                dataTable.Columns.RemoveAt(i);
            }

            // Insert the DataTable into the new worksheet starting from cell A1
            _ = worksheet.Cell(1, 1).InsertTable(dataTable, false);

            IXLRow headerRow = worksheet.Row(1);
            headerRow.Style.Fill.BackgroundColor = XLColor.Orange;
            headerRow.Style.Font.Bold = true;

            // Freeze the header row
            worksheet.SheetView.FreezeRows(1); // Freeze the first row (header row)

            // Set the number format of the entire date column to short date format
            _ = worksheet.Column(5).Style.NumberFormat.Format = "MM/dd/yyyy";
            _ = worksheet.Column(6).Style.NumberFormat.Format = "MM/dd/yyyy";
            _ = worksheet.Column(23).Style.NumberFormat.Format = "MM/dd/yyyy";
            _ = worksheet.Column(9).Style.NumberFormat.Format = "MM/dd/yyyy";
            _ = worksheet.Column(19).Style.NumberFormat.Format = "$#,##0.00";
            _ = worksheet.Column(20).Style.NumberFormat.Format = "$#,##0.00";

            // Set column widths to fit data except for JSON cols
            double staticWidth = 20; // Static width size
            for (int colIndex = 1; colIndex <= worksheet.ColumnsUsed().Count(); colIndex++)
            {
                // Set static width for column 5 and 10
                worksheet.Column(colIndex).Width = staticWidth;
            }

            // Apply filters to all columns
            _ = worksheet.SetAutoFilter(true).Column(14).AddFilter("Reimbursement");
            _ = worksheet.SetAutoFilter(true).Column(5).Contains("2024");

            // Apply sort filter to the "Date" column in ascending order
            int lastRow = worksheet.LastRowUsed().RowNumber();
            IXLRange rangeToSort = worksheet.Range(worksheet.Cell(2, 5), worksheet.Cell(lastRow, 5));
            _ = rangeToSort.Column(5).Sort(XLSortOrder.Ascending, false, true);

            workbook.SaveAs(filePath);
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

        public static async Task OpenExcel(string filePath)
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

            Process? process = Process.Start(startInfo);
            if (process != null)
            {
                await process.WaitForExitAsync();
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

            // Get the worksheet by name
            bool worksheetExists = workbook.Worksheets.TryGetWorksheet(sheetName, out IXLWorksheet worksheet);

            if (!worksheetExists)
            {
                worksheet = workbook.AddWorksheet(sheetName);
            }

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
    }
}
