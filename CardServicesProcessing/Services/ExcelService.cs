using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using System.Data;
using System.Diagnostics;

namespace CardServicesProcessor.Services
{
    public static class ExcelService
    {
        public static void ReorderColumns(this DataTable dataTable, string[] columnOrder)
        {
            DataTable newTable = new();

            foreach (string columnName in columnOrder)
            {
                DataColumn column = dataTable.Columns[columnName];
                _ = newTable.Columns.Add(column.ColumnName, column.DataType);
            }

            foreach (DataRow row in dataTable.Rows)
            {
                DataRow newRow = newTable.Rows.Add();
                foreach (string columnName in columnOrder)
                {
                    newRow[columnName] = row[columnName];
                }
            }

            dataTable.Clear();
            dataTable.Merge(newTable);
        }

        public static DataTable ReadSecondExcelData(string filePath, string[] sheets)
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

        public static void FillMissingWallet(string excelLink, string[] sheets, DataTable dt)
        {
            // Load data from the second Excel file
            DataTable tblManualReimbursements = ReadSecondExcelData(excelLink, sheets);

            foreach (DataRow row in dt.Rows)
            {
                string caseTicketNumber = row[ColumnNames.CaseTicketNumber].ToString().Trim();

                if (string.IsNullOrWhiteSpace(caseTicketNumber))
                {
                    continue;
                }

                // Truncate "-1" from the end if present
                caseTicketNumber = caseTicketNumber.Contains("-1") ? caseTicketNumber[..^2] : caseTicketNumber;

                // Search for the corresponding row in the second Excel data
                DataRow[] matchingRows = tblManualReimbursements.Select($"[Case Number] = '{caseTicketNumber}' AND NOT ISNULL([Benefit Wallet], '') = ''");

                if (matchingRows.Length > 0)
                {
                    string benefitWallet = matchingRows[0][ColumnNames.BenefitWallet].ToString().Trim();

                    // Handle empty "Case Closed Date" cell
                    if (benefitWallet.ContainsNumbers())
                    {
                        benefitWallet = matchingRows[0][ColumnNames.CaseClosedDate].ToString().Trim();
                    }

                    benefitWallet = benefitWallet.StripNumbers();

                    // Map the benefit description using dictionaries
                    if (Wallet.zPurseToBenefitDesc.TryGetValue(benefitWallet, out string? value)
                        || Wallet.benefitTypeToBenefitDesc.TryGetValue(benefitWallet, out value))
                    {
                        row[ColumnNames.Wallet] = Wallet.BenefitDescToWalletName.TryGetValue(value, out string? walletName) ? walletName : value;
                    }
                    else
                    {
                        // Handle case where benefit wallet doesn't have a corresponding benefit description
                        row[ColumnNames.Wallet] = Wallet.BenefitDescToWalletName.TryGetValue(benefitWallet, out string? walletName) ? walletName : benefitWallet;
                    }
                }
                else
                {
                    // Handle case where no matching row is found in the second Excel data
                    // You may choose to leave the "Wallet" column unchanged or set it to a default value
                    // row[ColumnNames.Wallet] = "Not found";
                }
            }

            // At this point, the "Wallet" column in your first datatable should be updated with the mapped benefit descriptions
        }

        public static void FillOutlierData(this DataRow dataRow, string? caseTicketNumber, string? wallet, string? requestedTotalAmount)
        {
            if (string.IsNullOrWhiteSpace(caseTicketNumber))
            {
                return;
            }

            // Handle specific caseTicketNumbers
            switch (caseTicketNumber)
            {
                case "EHCM202400062886-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, "300");
                    break;
                case "EHCM202400068352-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, "50");
                    break;
                case "EHCM202400070732-1":
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, requestedTotalAmount);
                    break;
                case "EHCM202400070290-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, "Ineligible Retailer (not allowed)");
                    dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, "0");
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
                    break;
                case "EHCM202400068493-1":
                    dataRow.FormatForExcel(ColumnNames.DenialReason, "Not a Reimbursement");
                    dataRow.FormatForExcel(ColumnNames.CaseStatus, "Closed");
                    dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
                    break;
            }

            // Handle WALLET
            if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400067263-1",
                "EHCM202400066272-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ActiveFitness);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400068961-1",
                "EHCM202400070137-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.AssistiveDevices);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400070711-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.DVH);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400069504-1",
                "EHCM202400069623-1",
                "EHCM202400069818-1",
                "EHCM202400065264-1",
                "EHCM202400062994-1",
                "EHCM202400070836-1",
                "EHCM202400070820-1",
                "EHCM202400070892-1",
                "EHCM202400070908-1",
                "EHCM202400071226-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.HealthyGroceries);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400064404-1",
                "EHCM202400066581-1",
                "EHCM202400069229-1",
                "EHCM202400070780-1",
                "EHCM202400070880-1",
                "EHCM202400070832-1",
                "EHCM202400070860-1",
                "EHCM202400070984-1",
                "EHCM202400070801-1",
                "EHCM202400063415-1",
                "EHCM202400066888-1",
                "EHCM202400070736-1",
                "EHCM202400070754-1",
                "EHCM202400070715-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.OTC);
            }
            else if (caseTicketNumber.Contains("EHCM202400071155-1"))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.ServiceDog);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400063018-1",
                "EHCM202400065349-1",
                "EHCM202400063182-1",
                "EHCM202400070873-1",
                "EHCM202400070955-1",
                "EHCM202400070803-1",
                "EHCM202400070761-1",
                "EHCM202400070772-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Utilities);
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400070738-1",
                "EHCM202400070768-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.Unknown);
            }

            // Handle DENIAL REASON
            if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400070290-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.DenialReason, "Ineligible Retailer (not allowed)");
                dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
                dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, "0");
            }
            else if (caseTicketNumber.ContainsAny(
            [
                "EHCM202400068493-1"
            ]))
            {
                dataRow.FormatForExcel(ColumnNames.DenialReason, "Not a Reimbursement");
                dataRow.FormatForExcel(ColumnNames.CaseStatus, "Closed");
                dataRow.FormatForExcel(ColumnNames.ApprovedStatus, "Declined");
            }
        }

        public static DataTable FillMissingDataFromPrevReport(DataTable tblCurr, DataTable? tblPrev)
        {
            if (tblPrev is null)
            {
                return tblCurr;
            }

            // Ensure that the Wallet column exists in tblCurr
            if (!tblCurr.Columns.Contains(ColumnNames.Wallet))
            {
                _ = tblCurr.Columns.Add(ColumnNames.Wallet);
            }

            // Iterate over the rows of tblCurr
            foreach (DataRow row in tblCurr.Rows)
            {
                string caseTicketNumber = row[ColumnNames.CaseTicketNumber].ToString();

                // Check if the Wallet value is missing
                string? wallet = row[ColumnNames.Wallet].ToString();
                if (string.IsNullOrWhiteSpace(wallet) || wallet.Trim() == "NULL")
                {
                    // Find the corresponding row in tblPrev based on the case number
                    DataRow matchingRow = tblPrev.AsEnumerable().FirstOrDefault(r => r.Field<string>(ColumnNames.CaseTicketNumber) == caseTicketNumber);

                    // If a matching row is found, fill in the missing Wallet value
                    if (matchingRow != null)
                    {
                        row[ColumnNames.Wallet] = matchingRow[ColumnNames.Wallet].ToString()?.GetWalletFromCommentsOrWalletCol(null, Wallet.GetCategoryVariations());
                    }
                    else
                    {
                        // Handle case when no matching row is found (optional)
                        if (row[ColumnNames.CaseTopic].ToString() == "Reimbursement")
                        {
                            //Console.WriteLine($"Missing wallet for {caseTicketNumber}");
                        }
                    }
                }

                // Check if the case is in review and update its status if necessary
                string? caseStatus = row[ColumnNames.CaseStatus].ToString();
                if (string.IsNullOrWhiteSpace(caseStatus) || caseStatus.ToString().Trim().ContainsAny([Statuses.InReview, Statuses.New, Statuses.Failed]))
                {
                    // Find the corresponding row in tblPrev based on the case number
                    DataRow matchingRow = tblPrev.AsEnumerable().FirstOrDefault(r => r.Field<string>(ColumnNames.CaseTicketNumber) == caseTicketNumber);

                    // If a matching row is found, update the case status and related fields
                    if (matchingRow != null)
                    {
                        row[ColumnNames.CaseStatus] = matchingRow[ColumnNames.CaseStatus].ToString()?.Trim();
                        row[ColumnNames.ApprovedStatus] = matchingRow[ColumnNames.ApprovedStatus].ToString()?.Trim();

                        // Format the approved amount
                        string? approvedAmount = row[ColumnNames.ApprovedTotalReimbursementAmount].ToString()?.Trim();
                        row[ColumnNames.ApprovedTotalReimbursementAmount] = string.IsNullOrWhiteSpace(approvedAmount) ? "NULL" : decimal.TryParse(approvedAmount, out decimal ata) ? ata.ToString("C2") : approvedAmount;

                        row[ColumnNames.ClosingComments] = matchingRow[ColumnNames.ClosingComments].ToString()?.Trim();
                    }
                    else
                    {
                        // Handle case when no matching row is found (optional)
                        if (row[ColumnNames.CaseTopic].ToString() == "Reimbursement")
                        {
                            //Console.WriteLine($"Missing status for {caseTicketNumber}");
                        }
                    }
                }
            }

            // Return tblCurr with missing Wallet cells filled in from tblPrev
            return tblCurr;
        }

        public static void ApplyFiltersAndSaveReport(DataTable dataTable, string filePath, string sheetName, int sheetPos)
        {
            using XLWorkbook workbook = new(filePath);
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

            workbook.Save();
        }

        public static void OpenExcel(string filePath, string sheetName)
        {
            // Path to Excel executable
            string excelPath = File.Exists(@"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE") ?
                @"C:\Program Files\Microsoft Office\root\Office16\EXCEL.EXE" :
                @"C:\Program Files (x86)\Microsoft Office\root\Office16\EXCEL.EXE";

            // Start Excel process with file and sheet arguments
            ProcessStartInfo startInfo = new()
            {
                FileName = $"\"{excelPath}\"",
                Arguments = $"\"{filePath}\"",
                UseShellExecute = false
            };
            _ = Process.Start(startInfo);



            // method 2
            /*Application excelApp = new()
            {
                Visible = true // Make Excel visible to the user.
            };

            Workbook workbook = excelApp.Workbooks.Open($"\"{filePath}\"");

            try
            {
                Worksheet sheet = (Worksheet)workbook.Sheets[sheetName];
                sheet.Activate();
            }
            catch (Exception ex)
            {
                // Handle the case where the specified sheet doesn't exist
                Console.WriteLine($"Error: {ex.Message}");
            }*/
        }
    }
}
