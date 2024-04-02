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
        public static DataTable ReadReimbursementReport(string filePath)
        {
            DataTable dataTable = new();

            // Download the Excel file from the SharePoint link
            /*string userName = "sankalp.godugu@nationsbenefits.com";
            string password = "Nations@002402728";
            string downloadPath = @"C:\Users\Sankalp.Godugu\Downloads\ManualAdjustments2023.xlsx";

            WebClient webClient = new()
            {
                Credentials = new NetworkCredential(userName, password)
            };
            webClient.DownloadFile(filePath, downloadPath);*/

            string directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            // Load the Excel file
            using (XLWorkbook workbook = new(filePath))
            {
                IXLWorksheet workingReimbursements = workbook.Worksheet(1);
                IXLWorksheet nextBatchReimbursements = workbook.Worksheet(2);
                IXLWorksheet combinedSheet = workbook.Worksheets.Add("2023 Reimbursements");
                // Create a new worksheet to combine the data
                IXLWorksheet worksheet = workbook.Worksheets.Add("worksheet");

                // Copy data from the first worksheet to the combined worksheet
                CopyWorksheetData(workingReimbursements, worksheet);

                // Copy data from the second worksheet to the combined worksheet
                CopyWorksheetData(nextBatchReimbursements, worksheet, startRow: worksheet.LastRowUsed().RowNumber() + 1);

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

        private static void CopyWorksheetData(IXLWorksheet sourceWorksheet, IXLWorksheet targetWorksheet, int startRow = 1)
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

        public static void FillOutlierData(this DataRow dataRow, string? caseTicketNumber, decimal? requestedTotalAmount)
        {
            if (!caseTicketNumber.IsTruthy())
            {
                return;
            }

            // Handle specific caseTicketNumbers
            switch (caseTicketNumber)
            {
                case "EHCM202400062886-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 300.ToString("C2"));
                    break;
                case "EHCM202400068352-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 50.ToString("C2"));
                    break;
                case "EHCM202400070732-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, requestedTotalAmount?.ToString("C2"));
                    break;
                case "EHCM202400070290-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, "Ineligible Retailer (not allowed)");
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, 0.ToString("C2"));
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
                    break;
                case "EHCM202400068493-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, "Not a Reimbursement");
                    dataRow.FormatForExcel(ColumnNames.CaseStatus, "Closed");
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
                    break;
                case "EHCM202400063018-1" or "EHCM202400065349-1" or "EHCM202400063182-1" or "EHCM202400070873-1" or "EHCM202400070955-1" or "EHCM202400070803-1" or "EHCM202400070761-1" or "EHCM202400070772-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Utilities);
                    break;
                case "EHCM202400067263-1" or "EHCM202400066272-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ActiveFitness);
                    break;
                case "EHCM202400068961-1" or "EHCM202400070137-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.AssistiveDevices);
                    break;
                case "EHCM202400070711-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
                    break;
                case "EHCM202400069504-1" or "EHCM202400069623-1" or "EHCM202400069818-1" or "EHCM202400065264-1" or "EHCM202400062994-1" or "EHCM202400070836-1" or "EHCM202400070820-1" or "EHCM202400070892-1" or "EHCM202400070908-1" or "EHCM202400071226-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
                    break;
                case "EHCM202400064404-1" or "EHCM202400066581-1" or "EHCM202400069229-1" or "EHCM202400070780-1" or "EHCM202400070880-1" or "EHCM202400070832-1" or "EHCM202400070860-1" or "EHCM202400070984-1" or "EHCM202400070801-1" or "EHCM202400063415-1" or "EHCM202400066888-1" or "EHCM202400070736-1" or "EHCM202400070754-1" or "EHCM202400070715-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.OTC);
                    break;
                case "EHCM202400071155-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ServiceDog);
                    break;
                case "EHCM202400070738-1" or "EHCM202400070768-1":
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Unknown);
                    break;
            }
        }

        public static void ApplyFiltersAndSaveReport(DataTable dataTable, string filePath, string sheetName, int sheetPos)
        {
            XLWorkbook workbook = CreateWorkbook(filePath);

            // Check if the worksheet exists
            if (workbook.TryGetWorksheet(sheetName, out IXLWorksheet worksheet))
            {
                // If it exists, delete it
                workbook.Worksheets.Delete(sheetName);
            }

            // Add a new worksheet with the same name
            worksheet = workbook.Worksheets.Add(sheetName, sheetPos).SetTabColor(XLColor.Yellow);

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

            //SetFilterOnDateColumns();

            // Specify the columns that contain date values by column letter (e.g., "A", "B", "C", etc.)
            int[] dateColumns = [
                ColumnNames.CreateDateColNumber,
                ColumnNames.TransactionDateColNumber,
                ColumnNames.DOBColNumber,
                ColumnNames.ProcessedDateColNumber
            ]; // Example: Columns A, C, and E

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

        public static void OpenExcel(string filePath)
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
            _ = Process.Start(startInfo);
        }

        public static void DeleteWorkbook(string filePath)
        {
            if (File.Exists(filePath)) File.Delete(filePath);
        }

        public static void AddToExcel((IEnumerable<RawData>, IEnumerable<MemberMailingInfo>, IEnumerable<MemberCheckReimbursement>) data, string filePath)
        {
            XLWorkbook workbook = CreateWorkbook(filePath);

            DataTable rawData = data.Item1.ToDataTable();
            DataTable memberMailingInfo = data.Item2.ToDataTable();
            DataTable memberCheckReimbursements = data.Item3.ToDataTable();
            List<DataTable> dataTables =
            [
                rawData,
                memberMailingInfo,
                memberCheckReimbursements
            ];

            // Add or update data in the existing sheets
            AddDataToWorksheets(dataTables, filePath);
        }

        private static void AddDataToWorksheets(List<DataTable> dataTables, string filePath)
        {
            using XLWorkbook workbook = CreateWorkbook(filePath);

            // Add data to each worksheet
            for (int i = 0; i < dataTables.Count; i++)
            {
                AddDataToWorksheet(dataTables[i], workbook, CheckIssuanceConstants.Sheets[i + 1]);
            }

            // Save the workbook
            workbook.SaveAs(filePath);
        }

        private static void AddDataToWorksheet(DataTable dt, XLWorkbook workbook, string sheetName)
        {
            // Get the worksheet by name
            bool worksheetExists = workbook.Worksheets.TryGetWorksheet(sheetName, out IXLWorksheet worksheet);

            if (!worksheetExists)
            {
                worksheet = workbook.AddWorksheet(sheetName);
            }

            // Find the first empty row in the worksheet
            int startRow = worksheet.LastRowUsed()?.RowNumber() + 1 ?? 1;

            // Insert the data into the worksheet starting from the next empty row
            var existingTable = worksheet.Tables.FirstOrDefault();

            if (existingTable == null)
            {
                // If the table doesn't exist, then insert it
                _ = worksheet.Cell(startRow, 1).InsertTable(dt);
            }
            else
            {
                worksheet.Cell(existingTable.RangeAddress.LastAddress.RowNumber + 1, 1).InsertTable(dt);
                //AddDataToExistingTable(dt, worksheet, startRow);
            }
        }

        private static void AddDataToExistingTable(DataTable dt, IXLWorksheet worksheet, int startRow)
        {
            // Iterate over the DataTable rows and insert them below the table
            int rowOffset = 1; // Skip the header row
            foreach (DataRow row in dt.Rows)
            {
                // Insert a row below the table
                worksheet.Row(startRow + rowOffset).InsertRowsBelow(1);

                // Populate the inserted row with data from the DataRow
                for (int i = 0; i < dt.Columns.Count; i++)
                {
                    // Cast the value from the DataRow to the appropriate type expected by ClosedXML
                    var cellValue = (dt.Columns[i].DataType == typeof(DateTime))
                        ? Convert.ToDateTime(row[i]) // Example conversion for DateTime type, adjust as needed
                        : row[i]; // For other types, assume no conversion needed

                    worksheet.Cell(startRow + rowOffset, i + 1).SetValue(cellValue.ToString());
                }

                rowOffset++;
            }
        }
    }
}
