using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using Microsoft.Extensions.Caching.Memory;
using System.ComponentModel;
using System.Data;
using System.Text.RegularExpressions;

namespace CardServicesProcessor.Services
{
    public static partial class DataProcessingService
    {
        [GeneratedRegex(@"\$(\d+(?:\.\d{1,2})?)")]
        private static partial Regex ApprovedAmountRegex();

        [GeneratedRegex(@"\b(\d+(?:\.\d{1,2})?)\b")]
        private static partial Regex ApprovedAmountWithoutDollarSignRegex();

        public static DataTable ToDataTable<T>(this IEnumerable<T> items)
        {
            DataTable dataTable = new(typeof(T).Name);

            // Get all public properties of the type T
            System.Reflection.PropertyInfo[] properties = typeof(T).GetProperties();

            // Define a dictionary to store column names
            Dictionary<System.Reflection.PropertyInfo, string> columnNames = [];

            // Get column names from properties
            foreach (System.Reflection.PropertyInfo prop in properties)
            {
                // Use the display name if available, otherwise use the property name
                string columnName = prop.GetCustomAttributes(typeof(DisplayNameAttribute), true)
                                        .FirstOrDefault() is DisplayNameAttribute displayNameAttribute ? displayNameAttribute.DisplayName : prop.Name;

                // Add column name to dictionary
                columnNames[prop] = columnName;

                // Add the column to the DataTable if it doesn't exist already
                if (!dataTable.Columns.Contains(columnName))
                {
                    _ = dataTable.Columns.Add(columnName, Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType);
                }
            }

            // Populate the DataTable with the data from the IEnumerable<T>
            foreach (T? item in items)
            {
                DataRow row = dataTable.NewRow();
                foreach (System.Reflection.PropertyInfo prop in properties)
                {

                    // Set the value in the row using the column name
                    row[columnNames[prop]] = prop.GetValue(item) ?? DBNull.Value;
                }
                dataTable.Rows.Add(row);
            }

            return dataTable;
        }

        public static DataTable ValidateCases(IEnumerable<CardServicesResponse> cssCases)
        {
            DataTable dt = new();
            bool firstRow = true;

            foreach (CardServicesResponse cssCase in cssCases)
            {
                // Use the first row to add columns to DataTable
                if (firstRow)
                {
                    _ = dt.Columns.Add(ColumnNames.InsuranceCarrier);
                    _ = dt.Columns.Add(ColumnNames.HealthPlan);
                    _ = dt.Columns.Add(ColumnNames.CaseTicketNumber);
                    _ = dt.Columns.Add(ColumnNames.CaseCategory);
                    _ = dt.Columns.Add(ColumnNames.CreateDate);
                    _ = dt.Columns.Add(ColumnNames.TransactionDate);
                    _ = dt.Columns.Add(ColumnNames.MemberFirstName);
                    _ = dt.Columns.Add(ColumnNames.MemberLastName);
                    _ = dt.Columns.Add(ColumnNames.DateOfBirth);
                    _ = dt.Columns.Add(ColumnNames.City);
                    _ = dt.Columns.Add(ColumnNames.State);
                    _ = dt.Columns.Add(ColumnNames.NhMemberId);
                    _ = dt.Columns.Add(ColumnNames.InsuranceNumber);
                    _ = dt.Columns.Add(ColumnNames.CaseTopic);
                    _ = dt.Columns.Add(ColumnNames.CaseType);
                    _ = dt.Columns.Add(ColumnNames.CaseTicketData);
                    _ = dt.Columns.Add(ColumnNames.Wallet);
                    _ = dt.Columns.Add(ColumnNames.DenialReason);
                    _ = dt.Columns.Add(ColumnNames.RequestedTotalReimbursementAmount);
                    _ = dt.Columns.Add(ColumnNames.ApprovedTotalReimbursementAmount);
                    _ = dt.Columns.Add(ColumnNames.CaseStatus);
                    _ = dt.Columns.Add(ColumnNames.ApprovedStatus);
                    _ = dt.Columns.Add(ColumnNames.ProcessedDate);
                    _ = dt.Columns.Add(ColumnNames.ClosingComments);
                    firstRow = false;
                }
                else
                {
                    DataRow dataRow = dt.NewRow();
                    try
                    {
                        string? insuranceCarrier = cssCase.InsuranceCarrierName?.Trim();
                        string? healthPlan = cssCase.HealthPlanName?.Trim();
                        string? caseTicketNbr = cssCase.CaseTicketNumber?.Trim();
                        string? caseCategory = cssCase.CaseCategory?.Trim() == "Case" ? "Card Services" : "Unknown";
                        DateTime? createDate = cssCase.CreateDate?.ParseAndConvertDateTime(ColumnNames.CreateDate);
                        //DateTime? transactionDate = cssCase.TransactionDate?.ParseAndConvertDateTime(ColumnNames.TransactionDate);
                        string? firstName = cssCase.FirstName?.ToPascalCase().Trim();
                        string? lastName = cssCase.LastName?.ToPascalCase().Trim();
                        DateTime? dob = cssCase.DateOfBirth?.ParseAndConvertDateTime(ColumnNames.DateOfBirth);
                        string? city = cssCase.City?.Trim();
                        string? state = cssCase.State?.Trim();
                        string? nhMemberId = cssCase.NhMemberId?.Trim();
                        string? insuranceNbr = cssCase.InsuranceNbr?.Trim();
                        string? caseTopic = cssCase.CaseTopic?.Trim();
                        string? caseType = cssCase.CaseType?.Trim();
                        string? caseTicketData = cssCase.CaseTicketData?.Trim();
                        string? wallet = cssCase.WalletValue?.Trim();
                        string? denialReason = cssCase.DenialReason?.Trim();
                        //decimal? requestedTotalAmount = cssCase.RequestedTotalReimbursementAmount?.ParseAmount(ColumnNames.RequestedTotalReimbursementAmount);
                        decimal? totalApprovedAmount = cssCase.ApprovedTotalReimbursementAmount?.ParseAmount(ColumnNames.ApprovedTotalReimbursementAmount);
                        string? caseStatus = cssCase.CaseStatus?.Trim();
                        string? approvedStatus = cssCase.ApprovedStatus?.Trim();
                        DateTime? processedDate = cssCase.ProcessedDate?.Trim().ParseAndConvertDateTime(ColumnNames.ProcessedDate);
                        string? closingComments = cssCase.ClosingComments?.Trim();

                        // requested total amount --------------------
                        bool isValidAmount = false;
                        decimal tempAmount = -1;
                        if (caseTicketData.IsValidJson() && caseTopic == "Reimbursement")
                        {
                            isValidAmount = decimal.TryParse(caseTicketData?.GetJsonValue(@"New Reimbursement Request.TransactionDetails.TotalReimbursmentAmount"), out tempAmount);
                        }
                        decimal? totalRequestedAmount = isValidAmount ? tempAmount : null;

                        // transaction date -------------------
                        string? transactionDate = null;
                        if (caseTicketData.IsValidJson()
                                && caseTicketData.PathExists("TransactionDate"))
                        {
                            transactionDate = caseTicketData.GetJsonValue("TransactionDate")?.Trim();
                        }
                        else if (caseTicketData.IsValidJson()
                                && caseTicketData.PathExists(@"New Reimbursement Request.TransactionDate"))
                        {
                            transactionDate = caseTicketData.GetJsonValue(@"New Reimbursement Request.TransactionDate")?.Trim();
                        }

                        // approved status -------------------
                        if (closingComments.IsTruthy())
                        {
                            if (approvedStatus == Statuses.Approved
                                && closingComments.ContainsAny(Variations.DeclinedVariations))
                            {
                                approvedStatus = Statuses.Declined;
                            }
                            if (approvedStatus == Statuses.Declined
                                && closingComments.ContainsAny(Variations.ApprovedVariations)
                                && !closingComments.ContainsAny(["Duplicate"]))
                            {
                                approvedStatus = Statuses.Approved;
                            }
                        }

                        // case status ----------------------
                        if (caseStatus == Statuses.PendingProcessing)
                        {
                            caseStatus = Statuses.Closed;
                        }
                        if (caseStatus == Statuses.Failed)
                        {
                            caseStatus = Statuses.Closed;
                            if (caseTopic == "Reimbursement")
                            {
                                approvedStatus = approvedStatus == Statuses.Approved || (closingComments.IsTruthy() && closingComments.ContainsAny(Variations.ApprovedVariations))
                                ? Statuses.Approved
                                : Statuses.Declined;
                            }
                        }
                        if (caseTopic == "Reimbursement"
                            && !totalApprovedAmount.IsNA()
                            && closingComments.IsTruthy()
                            && closingComments.ContainsAny(Variations.ApprovedVariations))
                        {
                            caseStatus = Statuses.Closed;
                            approvedStatus = Statuses.Approved;
                        }

                        if (caseTicketNbr.IsTruthy() && caseTicketNbr.ContainsAny(
                            "EHCM202400065507-1", "EHCM202400063562-1", "NBCM202400069903-1", // missing processed date and/or denial reason
                            "NBCM202400075178-1", "NBCM202400075627-1", "NBCM202400079733-1", // missing approved amount
                            "NBCM202400069903-1", "NBCM202400079345-1") // missing denial reason
                            ) 
                        {

                        }

                        // for cases older than 14 days, close them out if they are in review, declined, or due to IT issues
                        if (caseStatus != Statuses.Closed && createDate < DateTime.Now.AddDays(-14))
                        {
                            caseStatus = Statuses.Closed;
                            approvedStatus = Statuses.Declined;
                            if (caseTopic == "Reimbursement")
                            {
                                totalApprovedAmount = 0;
                            }
                        }

                        // denial reason
                        if (caseTopic == "Reimbursement")
                        {
                            if (approvedStatus == Statuses.Declined) denialReason = denialReason.IsTruthy() ? denialReason : DenialReasons.BenefitUtilized;
                            else denialReason = null;
                        }

                        // processed date
                        if (!processedDate.HasValue && caseStatus == Statuses.Closed)
                        {
                            processedDate = createDate.Value.AddDays(14);
                        }
                        
                        // #2: update ApprovedAmount to value if Approved and amount is 0
                        if (caseTopic == "Reimbursement"
                            && caseTicketData.IsTruthy()
                            && caseTicketData.IsValidJson()
                            && approvedStatus == Statuses.Approved
                            && totalApprovedAmount.IsNA()
                            && closingComments.IsTruthy()
                            )
                        {
                            if (closingComments.Contains('$'))
                            {
                                Match match = ApprovedAmountRegex().Match(closingComments);
                                if (match.Success)
                                {
                                    totalApprovedAmount = decimal.TryParse(match.Groups[1].Value, out decimal val) ? val : null;
                                }
                            }
                            else
                            {
                                Match match = ApprovedAmountWithoutDollarSignRegex().Match(closingComments);
                                if (match.Success)
                                {
                                    totalApprovedAmount = decimal.TryParse(match.Groups[1].Value, out decimal val) ? val : null;
                                }
                            }
                        }

                        // #4: update ApprovedAmount to 0 if declined
                        if (caseTopic == "Reimbursement"
                            && approvedStatus == Statuses.Declined)
                        {
                            totalApprovedAmount = 0;
                        }

                        // consolidate wallets
                        wallet = wallet.IsTruthy() ? wallet : wallet.GetWalletFromCommentsOrWalletCol(closingComments);

                        // update rows
                        dataRow.FormatForExcel(ColumnNames.InsuranceCarrier, insuranceCarrier);
                        dataRow.FormatForExcel(ColumnNames.HealthPlan, healthPlan);
                        dataRow.FormatForExcel(ColumnNames.CaseTicketNumber, caseTicketNbr);
                        dataRow.FormatForExcel(ColumnNames.CaseCategory, caseCategory);
                        dataRow.FormatForExcel(ColumnNames.CreateDate, createDate?.ToShortDateString());
                        dataRow.FormatForExcel(ColumnNames.TransactionDate, transactionDate);
                        dataRow.FormatForExcel(ColumnNames.MemberFirstName, firstName);
                        dataRow.FormatForExcel(ColumnNames.MemberLastName, lastName);
                        dataRow.FormatForExcel(ColumnNames.DateOfBirth, dob?.ToShortDateString());
                        dataRow.FormatForExcel(ColumnNames.City, city);
                        dataRow.FormatForExcel(ColumnNames.State, state);
                        dataRow.FormatForExcel(ColumnNames.NhMemberId, nhMemberId);
                        dataRow.FormatForExcel(ColumnNames.InsuranceNumber, insuranceNbr);
                        dataRow.FormatForExcel(ColumnNames.CaseTopic, caseTopic);
                        dataRow.FormatForExcel(ColumnNames.CaseType, caseType);
                        dataRow.FormatForExcel(ColumnNames.CaseTicketData, caseTicketData);
                        dataRow.FormatForExcel(ColumnNames.Wallet, wallet);
                        dataRow.FormatForExcel(ColumnNames.DenialReason, denialReason);
                        dataRow.FormatForExcel(ColumnNames.RequestedTotalReimbursementAmount, totalRequestedAmount?.ToString("C2"));
                        dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, totalApprovedAmount?.ToString("C2"));
                        dataRow.FormatForExcel(ColumnNames.CaseStatus, caseStatus);
                        dataRow.FormatForExcel(ColumnNames.ApprovedStatus, approvedStatus);
                        dataRow.FormatForExcel(ColumnNames.ProcessedDate, processedDate?.ToShortDateString());
                        dataRow.FormatForExcel(ColumnNames.ClosingComments, closingComments);

                        dataRow.FillOutlierData(caseTicketNbr, totalRequestedAmount);

                        dt.Rows.Add(dataRow);
                    }
                    catch (Exception ex)
                    {
                        // Log the exception and the cell address to help with debugging
                        Console.WriteLine($"Error processing row {dataRow}: {ex.Message}");
                    }
                }
            }
            return dt;
        }

        public static DataTable ReadManualReimbursementReport(string filePath)
        {
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

            DataTable dataTable = new();

            // Load the Excel file
            using (XLWorkbook workbook = new(filePath))
            {
                IXLWorksheet combinedWorksheet = workbook.Worksheets.Add("2023 reimbursements - All");

                foreach (IXLWorksheet? worksheet in workbook.Worksheets.Where(worksheet => worksheet.Name.ContainsAny("BCBSRI 2023 Reimbursements", "2023 Reimbursements-completed")))
                {
                    ExcelService.CopyWorksheetData(worksheet, combinedWorksheet, startRow: combinedWorksheet.LastRowUsed()?.RowNumber() + 1 ?? 1);
                }

                // Assume the first row contains column headers
                bool isFirstRow = true;

                // Iterate over each row in the worksheet
                foreach (IXLRow row in combinedWorksheet.RowsUsed())
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

        public static void FillMissingInfoFromManualReimbursementReport(string filePath, DataTable tblCMT)
        {
            DataTable tblManualReimbursements = ReadManualReimbursementReport(filePath);

            // Load data from the second Excel file
            /*if (!CacheManager.Cache.TryGetValue("manualReimbursementReport", out DataTable tblManualReimbursements))
            {
                // Data not found in cache, fetch from source and store in cache
                tblManualReimbursements = ReadManualReimbursementReport(filePath);
                _ = CacheManager.Cache.Set("manualReimbursementReport", tblManualReimbursements, TimeSpan.FromDays(1));
            }*/

            foreach (DataRow row in tblCMT.Rows)
            {
                string? caseTicketNumber = row[ColumnNames.CaseTicketNumber].ToString()?.Trim();
                string? benefitWalletFromCMT = row[ColumnNames.Wallet].ToString()?.Trim();

                if (!caseTicketNumber.IsTruthy() || benefitWalletFromCMT.IsTruthy())
                {
                    continue;
                }

                // Truncate "-1" from the end if present
                caseTicketNumber = caseTicketNumber.Contains("-1") ? caseTicketNumber[..^2] : caseTicketNumber;

                // Search for the corresponding row in the second Excel data
                DataRow[] matchingRows = tblManualReimbursements.Select($"[Case Number] like '%{caseTicketNumber}%' AND NOT ISNULL([Benefit Wallet], '') = ''");

                if (matchingRows.Length > 0)
                {
                    string? benefitWalletFromManualReport = matchingRows[0][ColumnNames.BenefitWallet].ToString()?.Trim();

                    // Handle empty "Case Closed Date" cell or missing "Case Closed Date" column altogether
                    if (benefitWalletFromManualReport.ContainsNumbersOnly())
                    {
                        if (matchingRows[0].Table.Columns.Contains(ColumnNames.CaseClosedDate))
                        {
                            benefitWalletFromManualReport = matchingRows[0][ColumnNames.CaseClosedDate]?.ToString()?.Trim();
                        }
                        else
                        {
                            benefitWalletFromManualReport = matchingRows[0][ColumnNames.AmountRequested]?.ToString()?.Trim();
                        }
                    }

                    benefitWalletFromManualReport = benefitWalletFromManualReport.StripNumbers();

                    // Map the benefit description using dictionaries
                    if (Wallet.zPurseToBenefitDesc.TryGetValue(benefitWalletFromManualReport, out string? value)
                        || Wallet.benefitTypeToBenefitDesc.TryGetValue(benefitWalletFromManualReport, out value))
                    {
                        //row[ColumnNames.Wallet] = Wallet.BenefitDescToWalletName.TryGetValue(value, out string? walletName) ? walletName : value;
                        row[ColumnNames.Wallet] = value.GetWalletFromCommentsOrWalletCol(null);
                    }
                    else
                    {
                        // Handle case where benefit wallet doesn't have a corresponding benefit description
                        //row[ColumnNames.Wallet] = Wallet.BenefitDescToWalletName.TryGetValue(benefitWalletFromManualReport, out string? walletName) ? walletName : benefitWalletFromManualReport;
                        row[ColumnNames.Wallet] = benefitWalletFromManualReport.GetWalletFromCommentsOrWalletCol(null);
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

        public static DataTable? ReadPrevYTDExcelToDataTable(string filePath, string sheetName)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            // Open the Excel file
            using XLWorkbook workbook = new(filePath);

            bool worksheetExists = workbook.Worksheets.TryGetWorksheet(sheetName, out IXLWorksheet? worksheet);

            if (!worksheetExists)
            {
                return null;
            }

            // Define the columns you want to extract
            int caseTicketNumberColIndex = 3; // Assuming the first column (A) contains the first desired column
            int walletColIndex = 17; // Assuming the second column (B) contains the second desired column
            int caseStatusColIndex = 21;
            int approvedStatusColIndex = 22;
            int closingCommentsColIndex = 24;

            // Create a DataTable to store the extracted data
            DataTable dataTable = new();
            _ = dataTable.Columns.Add(ColumnNames.CaseTicketNumber);
            _ = dataTable.Columns.Add(ColumnNames.Wallet);
            _ = dataTable.Columns.Add(ColumnNames.CaseStatus);
            _ = dataTable.Columns.Add(ColumnNames.ApprovedStatus);
            _ = dataTable.Columns.Add(ColumnNames.ClosingComments);

            // Iterate over the rows of the worksheet
            foreach (IXLRow row in worksheet.RowsUsed())
            {
                // Extract values from the desired columns
                string caseTicketNbr = row.Cell(caseTicketNumberColIndex).Value.ToString();
                string wallet = row.Cell(walletColIndex).Value.ToString();
                string caseStatus = row.Cell(caseStatusColIndex).Value.ToString();
                string approvedStatus = row.Cell(approvedStatusColIndex).Value.ToString();
                string closingComments = row.Cell(closingCommentsColIndex).Value.ToString();

                // Add the values to the DataTable
                _ = dataTable.Rows.Add(caseTicketNbr, wallet, caseStatus, approvedStatus, closingComments);
            }

            return dataTable;
        }

        public static void FillMissingDataFromPrevReport(DataTable tblCurr, DataTable? tblPrev)
        {
            if (tblPrev is null)
            {
                return;
            }

            // Ensure that the Wallet column exists in tblCurr
            if (!tblCurr.Columns.Contains(ColumnNames.Wallet))
            {
                _ = tblCurr.Columns.Add(ColumnNames.Wallet);
            }

            // Iterate over the rows of tblCurr
            foreach (DataRow rowTblCurr in tblCurr.Rows)
            {
                string? caseTicketNumber = rowTblCurr[ColumnNames.CaseTicketNumber].ToString() ?? "";

                if (caseTicketNumber == null)
                {
                    continue;
                }

                // Check if the Wallet value is missing
                string? wallet = rowTblCurr[ColumnNames.Wallet].ToString();
                if (!wallet.IsTruthy())
                {
                    // Find the corresponding row in tblPrev based on the case number
                    DataRow? matchingRow = tblPrev.AsEnumerable().FirstOrDefault(r => r.Field<string>(ColumnNames.CaseTicketNumber) == caseTicketNumber);

                    // If a matching row is found, fill in the missing Wallet value
                    if (matchingRow != null)
                    {
                        rowTblCurr[ColumnNames.Wallet] = matchingRow[ColumnNames.Wallet].ToString()?.GetWalletFromCommentsOrWalletCol(null);
                    }
                    else
                    {
                        // Handle case when no matching row is found (optional)
                        if (rowTblCurr[ColumnNames.CaseTopic].ToString() == "Reimbursement")
                        {
                            //Console.WriteLine($"Missing wallet for {caseTicketNumber}");
                        }
                    }
                }

                // Check if the case is in review and update its status if necessary
                string? caseStatus = rowTblCurr[ColumnNames.CaseStatus].ToString();
                if (!caseStatus.IsTruthy() || caseStatus.ToString().Trim().ContainsAny([Statuses.InReview, Statuses.New, Statuses.Failed]))
                {
                    // Find the corresponding row in tblPrev based on the case number
                    DataRow? matchingRow = tblPrev.AsEnumerable().FirstOrDefault(r => r.Field<string>(ColumnNames.CaseTicketNumber) == caseTicketNumber);

                    // If a matching row is found, update case status, approved status, and approved amount
                    if (matchingRow != null)
                    {
                        bool hasProcessedDate = DateTime.TryParse(rowTblCurr[ColumnNames.ProcessedDate].ToString(), out DateTime processedDate);

                        if (!hasProcessedDate)
                        {
                            rowTblCurr[ColumnNames.ProcessedDate] = matchingRow[ColumnNames.ProcessedDate].ToString();
                            //rowTblCurr[ColumnNames.ApprovedStatus] = Statuses.Approved;
                        }

                        // Format the approved amount
                        bool hasApprovedTotalAmount = decimal.TryParse(rowTblCurr[ColumnNames.ApprovedTotalReimbursementAmount].ToString(), out decimal approvedTotalAmount);
                        if (!hasApprovedTotalAmount)
                        {
                            hasApprovedTotalAmount = decimal.TryParse(matchingRow[ColumnNames.ApprovedTotalReimbursementAmount].ToString(), out approvedTotalAmount);
                        }
                        rowTblCurr.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, approvedTotalAmount.ToString("C2"));
                    }
                    else
                    {
                        // Handle case when no matching row is found (optional)
                        if (rowTblCurr[ColumnNames.CaseTopic].ToString() == "Reimbursement")
                        {
                            //Console.WriteLine($"Missing status for {caseTicketNumber}");
                        }
                    }
                }
            }
        }
    }
}