using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using System.ComponentModel;
using System.Data;
using System.Reflection;
using System.Text.RegularExpressions;
using Path = System.IO.Path;

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
            PropertyInfo[] properties = typeof(T).GetProperties();

            // Define a dictionary to store column names
            Dictionary<PropertyInfo, string> columnNames = [];

            // Get column names from properties
            foreach (PropertyInfo prop in properties)
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
                foreach (PropertyInfo prop in properties)
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
                        decimal? totalApprovedAmount = cssCase.ApprovedTotalAmount?.ParseAmount(ColumnNames.ApprovedTotalReimbursementAmount);
                        string? caseStatus = cssCase.CaseStatus?.Trim();
                        string? approvedStatus = cssCase.ApprovedStatus?.Trim();
                        DateTime? processedDate = cssCase.ProcessedDate?.Trim().ParseAndConvertDateTime(ColumnNames.ProcessedDate);
                        string? closingComments = cssCase.ClosingComments?.Trim();

                        if (caseTicketNbr == "EHCM202400071230-1")
                        {

                        }

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

                        // for cases older than 14 days, close them out if they are in review, declined, or due to IT issues
                        if (caseStatus == Statuses.InReview && createDate < DateTime.Now.AddDays(-14))
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
                            denialReason = approvedStatus == Statuses.Declined ? denialReason.IsTruthy() ? denialReason : Constants.BenefitUtilized : null;
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

                        // if NULL wallet, get from comments
                        wallet = wallet.IsTruthy() ? SetWalletName(dataRow, wallet) : Wallet.GetWalletNameFromComments(closingComments);

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
                        dataRow.FillOutlierData(caseTicketNbr);
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
            string? directoryPath = Path.GetDirectoryName(filePath);
            if (!Directory.Exists(directoryPath))
            {
                _ = Directory.CreateDirectory(directoryPath);
            }

            DataTable dataTable = new();

            // Load the Excel file
            using (XLWorkbook workbook = new(filePath))
            {
                IXLWorksheet combinedWorksheet = ExcelService.CreateWorksheet(workbook, "2023 reimbursements - All");

                foreach (IXLWorksheet? worksheet in workbook.Worksheets.Where(worksheet => worksheet.Name.ContainsAny("BCBSRI 2023", "2023 Reimbursements-complet")))
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

            foreach (DataRow row in tblCMT.AsEnumerable().Where(r =>
                    r.Field<string>(ColumnNames.CaseTopic) == Constants.Reimbursement
                    && !r.Field<string>(ColumnNames.Wallet).IsTruthy()))
            {
                string? caseTicketNumber = row[ColumnNames.CaseTicketNumber].ToString()?.Trim();
                string? benefitWalletFromCMT = row[ColumnNames.Wallet].ToString()?.Trim();

                if (!caseTicketNumber.IsTruthy())
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
                        benefitWalletFromManualReport = matchingRows[0].Table.Columns.Contains(ColumnNames.CaseClosedDate)
                            ? (matchingRows[0][ColumnNames.CaseClosedDate]?.ToString()?.Trim())
                            : (matchingRows[0][ColumnNames.AmountRequested]?.ToString()?.Trim());
                    }

                    benefitWalletFromManualReport = benefitWalletFromManualReport.StripNumbers();

                    // Map the benefit description using dictionaries
                    SetWalletName(row, benefitWalletFromManualReport);
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

        private static string SetWalletName(DataRow row, string? benefitWalletFromManualReport)
        {
            if (Wallet.zPurseToBenefitDesc.TryGetValue(benefitWalletFromManualReport, out string? benefitDesc)
                                    || Wallet.benefitTypeToBenefitDesc.TryGetValue(benefitWalletFromManualReport, out benefitDesc))
            {
                row[ColumnNames.Wallet] = Wallet.GetWalletNameFromBenefitDesc(benefitDesc);
            }
            else
            {
                row[ColumnNames.Wallet] = Wallet.GetWalletNameFromBenefitDesc(benefitWalletFromManualReport);
            }
            return row[ColumnNames.Wallet].ToString();
        }

        public static void FillInMissingWallets(DataTable dataTable, IEnumerable<ReimbursementItem> reimbursementItems)
        {
            try
            {
                Dictionary<string, HashSet<string>> walletContents = new(StringComparer.OrdinalIgnoreCase);
                foreach ((string wallet, string filePath) in FilePathConstants.ReimbursementItemFilePaths)
                {
                    IEnumerable<string> lines = File.ReadLines(filePath).Where(line => !string.IsNullOrWhiteSpace(line)).Select(line => line.Trim());

                    // contains all the line items from the file for this specific wallet
                    walletContents[wallet] = new HashSet<string>(lines);
                }

                //_ = Parallel.ForEach(dataTable.Rows.Cast<DataRow>(), dataRow =>
                var rowsWithoutWallets = dataTable.AsEnumerable().Where(r =>
                    r.Field<string>(ColumnNames.CaseTopic) == Constants.Reimbursement
                    && !r.Field<string>(ColumnNames.Wallet).IsTruthy());

                foreach (DataRow dataRow in rowsWithoutWallets)
                {

                    string? caseTicketNbr = (string?)dataRow[ColumnNames.CaseTicketNumber] ?? "";
                    IEnumerable<ReimbursementItem> reimbursementItemsResult = reimbursementItems.Where(ri => ri.CaseTicketNumber == caseTicketNbr).ToList();
                    IEnumerable<string?> products = reimbursementItemsResult.Select(ri => ri.ProductName?.Trim());

                    if (!caseTicketNbr.IsTruthy() || !reimbursementItemsResult.Any() || products is null || !products.Any())
                    {
                        continue;
                    }

                    // Search for wallet types in consolidated text file
                    string? matchedWallet = null;
                    foreach ((string walletType, HashSet<string> contents) in walletContents)
                    {
                        HashSet<string> caseInsensitiveContents = new(contents, StringComparer.OrdinalIgnoreCase);
                        if (caseInsensitiveContents.Overlaps(products))
                        {
                            matchedWallet = walletType;
                            break;
                        }
                    }
                    if (matchedWallet == null)
                    { }
                    dataRow.FormatForExcel(ColumnNames.Wallet, Wallet.GetWalletNameFromBenefitDesc(matchedWallet));
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        private static bool IsValueInTextFile(string filePath, IEnumerable<string?> searchValues)
        {
            try
            {
                if (searchValues is null || !searchValues.Any())
                {
                    return false;
                }

                // Read all lines from the text file once and store in memory
                HashSet<string> fileContents = new(File.ReadAllLines(filePath)
                    .Where(line => !string.IsNullOrWhiteSpace(line))
                    .Select(line => line.Trim()), StringComparer.OrdinalIgnoreCase);

                // Parallelize the search process
                foreach (string searchValue in searchValues)
                {
                    if (fileContents.Any(line => line.IndexOf(searchValue.Trim(), StringComparison.OrdinalIgnoreCase) >= 0))
                    {
                        // Value found, terminate the loop
                        return true;
                    }
                }

                return false; // Value not found
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error reading the text file: " + ex.Message);
                return false; // Error occurred
            }
        }
    }
}