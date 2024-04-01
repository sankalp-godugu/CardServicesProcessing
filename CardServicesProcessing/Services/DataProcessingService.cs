using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
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

                        // #5: updated denial reason
                        if (approvedStatus == Statuses.Approved && denialReason.IsTruthy())
                        {
                            denialReason = null;
                        }

                        if (true)
                        {
                            if (totalApprovedAmount > 0)
                            {
                            }
                            if (totalApprovedAmount is null)
                            {
                            }
                            if (totalApprovedAmount == 0)
                            {
                            }
                        }


                        dataRow[ColumnNames.ProcessedDate] = processedDate;

                        // consolidate wallets
                        wallet = wallet.IsTruthy() ? wallet.GetWalletFromCommentsOrWalletCol(closingComments, Wallet.GetCategoryVariations()) : wallet;

                        if (caseTopic == "Card Replacement")
                        {
                            if (caseStatus == "Open")
                            {
                                caseStatus = Statuses.Closed;
                            }
                        }

                        /*if (caseTicketNbr == "EHCM202400065255-1")
                        {
                        }*/

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

                        //dataRow.FillOutlierData(caseTicketNbr, requestedTotalAmount);

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

        public static void FillMissingInfoFromManualReimbursementReport(string filePath, DataTable dataTable)
        {
            // Load data from the second Excel file
            DataTable tblManualReimbursements = ExcelService.ReadReimbursementReport(filePath);

            foreach (DataRow row in dataTable.Rows)
            {
                string? caseTicketNumber = row[ColumnNames.CaseTicketNumber].ToString()?.Trim();

                if (!caseTicketNumber.IsTruthy())
                {
                    continue;
                }

                // Truncate "-1" from the end if present
                caseTicketNumber = caseTicketNumber.Contains("-1") ? caseTicketNumber[..^2] : caseTicketNumber;

                // Search for the corresponding row in the second Excel data
                DataRow[] matchingRows = tblManualReimbursements.Select($"[Case Number] = '{caseTicketNumber}' AND NOT ISNULL([Benefit Wallet], '') = ''");

                if (matchingRows.Length > 0)
                {
                    string? benefitWallet = matchingRows[0][ColumnNames.BenefitWallet].ToString()?.Trim();

                    if (benefitWallet is null)
                    {
                        continue;
                    }

                    // Handle empty "Case Closed Date" cell
                    if (benefitWallet.ContainsNumbers())
                    {
                        benefitWallet = matchingRows[0][ColumnNames.CaseClosedDate].ToString()?.Trim();
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
                        rowTblCurr[ColumnNames.Wallet] = matchingRow[ColumnNames.Wallet].ToString()?.GetWalletFromCommentsOrWalletCol(null, Wallet.GetCategoryVariations());
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
                            rowTblCurr[ColumnNames.ApprovedStatus] = Statuses.Approved;
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