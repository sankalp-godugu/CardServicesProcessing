using CardServicesProcessor.Models.Response;
using CardServicesProcessor.Shared;
using CardServicesProcessor.Utilities.Constants;
using ClosedXML.Excel;
using System.Data;
using System.Text.RegularExpressions;

namespace CardServicesProcessor.Services
{
    public static partial class DataService
    {
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
                        string? caseTicketNbr = cssCase.CaseTicketNumber?.Trim();
                        string? caseCategory = cssCase.CaseCategory?.Trim() == "Case" ? "Card Services" : "Unknown";
                        DateTime? createDate = cssCase.CreateDate?.ParseAndConvertDateTime(ColumnNames.CreateDate);
                        DateTime? transactionDate = cssCase.TransactionDate?.ParseAndConvertDateTime(ColumnNames.TransactionDate);
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
                        decimal? requestedTotalAmount = dataRow.ParseAmount(ColumnNames.RequestedTotalReimbursementAmount);
                        decimal? approvedTotalAmount = dataRow.ParseAmount(ColumnNames.ApprovedTotalReimbursementAmount);
                        string? caseStatus = cssCase.CaseStatus?.Trim();
                        string? approvedStatus = cssCase.ApprovedStatus?.Trim();
                        DateTime? processedDate = cssCase.ProcessedDate?.Trim().ParseAndConvertDateTime(ColumnNames.ProcessedDate);
                        string? closingComments = cssCase.ClosingComments?.Trim();

                        // REIMBURSEMENT SPECIFIC PROCESSING
                        if (caseTopic == "Reimbursement")
                        {

                            // transaction date
                            dataRow[ColumnNames.TransactionDate] = transactionDate;

                            // #1: update ApprovedStatus
                            if (!string.IsNullOrWhiteSpace(closingComments))
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

                            // #3: case status
                            if (approvedTotalAmount != 0
                                && !string.IsNullOrWhiteSpace(closingComments)
                                && closingComments.ContainsAny(Variations.ApprovedVariations))
                            {
                                caseStatus = Statuses.Closed;
                                approvedStatus = Statuses.Approved;
                            }
                            if (caseStatus == Statuses.PendingProcessing)
                            {
                                caseStatus = Statuses.Closed;
                                approvedStatus = Statuses.Approved;
                            }
                            if (caseStatus == Statuses.Failed)
                            {
                                caseStatus = Statuses.Closed;
                                approvedStatus = approvedStatus == Statuses.Approved
                                    || (!string.IsNullOrWhiteSpace(closingComments) && closingComments.ContainsAny(Variations.ApprovedVariations))
                                    ? Statuses.Approved
                                    : Statuses.Declined;
                            }

                            // #2: update ApprovedAmount to value if Approved and amount is 0
                            // TODO: need to add another condition for specific cases where '$' does not prefix the approved amount, so need to capture these values too
                            if (!string.IsNullOrWhiteSpace(caseTicketData)
                                && caseTicketData.IsValidJson()
                                && approvedStatus == Statuses.Approved
                                && approvedTotalAmount == 0
                                && !string.IsNullOrWhiteSpace(closingComments)
                                )
                            {
                                if (closingComments.Contains('$'))
                                {
                                    Match match = ApprovedAmountRegex().Match(closingComments);
                                    if (match.Success)
                                    {
                                        approvedTotalAmount = decimal.TryParse(match.Groups[1].Value, out decimal val) ? val : null;
                                    }
                                }
                                else
                                {
                                    Match match = ApprovedAmountWithoutDollarSignRegex().Match(closingComments);
                                    if (match.Success)
                                    {
                                        approvedTotalAmount = decimal.TryParse(match.Groups[1].Value, out decimal val) ? val : null;
                                    }
                                }
                            }

                            // #4: update ApprovedAmount to 0 if declined
                            if (approvedStatus == Statuses.Declined && approvedTotalAmount != 0)
                            {
                                approvedTotalAmount = 0;
                            }

                            // #5: updated denial reason
                            if (approvedStatus == Statuses.Approved && !string.IsNullOrWhiteSpace(denialReason))
                            {
                                denialReason = null;
                            }

                            // processed date
                            /*if (string.IsNullOrWhiteSpace(approvedStatus)
                                && !string.IsNullOrWhiteSpace(processedDate))
                            {
                                processedDate = "NULL";
                            }*/

                            // consolidate wallets
                            wallet = !string.IsNullOrWhiteSpace(wallet) ? wallet.GetWalletFromCommentsOrWalletCol(closingComments, Wallet.GetCategoryVariations()) : wallet;
                        }

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
                        dataRow.FormatForExcel(ColumnNames.InsuranceCarrier, cssCase.InsuranceCarrierName.Trim());
                        dataRow.FormatForExcel(ColumnNames.HealthPlan, cssCase.HealthPlanName.Trim());
                        dataRow.FormatForExcel(ColumnNames.CaseTicketNumber, caseTicketNbr.Trim());
                        dataRow.FormatForExcel(ColumnNames.CaseCategory, caseCategory);
                        dataRow.FormatForExcel(ColumnNames.CreateDate, createDate?.ToShortDateString());
                        dataRow.FormatForExcel(ColumnNames.TransactionDate, transactionDate?.ToShortDateString());
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
                        dataRow.FormatForExcel(ColumnNames.RequestedTotalReimbursementAmount, requestedTotalAmount?.ToString("C2"));
                        dataRow.FormatForExcel(ColumnNames.ApprovedTotalReimbursementAmount, approvedTotalAmount?.ToString("C2"));
                        dataRow.FormatForExcel(ColumnNames.CaseStatus, cssCase.CaseStatus);
                        dataRow.FormatForExcel(ColumnNames.ApprovedStatus, cssCase.ApprovedStatus);
                        dataRow.FormatForExcel(ColumnNames.ProcessedDate, cssCase.ProcessedDate);
                        dataRow.FormatForExcel(ColumnNames.ClosingComments, cssCase.ClosingComments);

                        dataRow.FillOutlierData(caseTicketNbr, wallet, requestedTotalAmount?.ToString("C2"));

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

        public static DataTable? ReadPrevYTDExcelToDataTable(string filePath, string sheetName)
        {
            if (!File.Exists(filePath))
            {
                return null;
            }

            // Open the Excel file
            using XLWorkbook workbook = new(filePath);
            // Get the first worksheet
            IXLWorksheet worksheet = workbook.Worksheet(sheetName);

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
                string column1Value = row.Cell(caseTicketNumberColIndex).Value.ToString();
                string column2Value = row.Cell(walletColIndex).Value.ToString();
                string column3Value = row.Cell(caseStatusColIndex).Value.ToString();
                string column4Value = row.Cell(approvedStatusColIndex).Value.ToString();
                string column5Value = row.Cell(closingCommentsColIndex).Value.ToString();

                // Add the values to the DataTable
                _ = dataTable.Rows.Add(column1Value, column2Value, column3Value, column4Value, column5Value);
            }

            // Now you have the data in the DataTable (you can store it in memory or a database)
            // For example, you can loop through the DataTable and print the values
            /*foreach (DataRow row in dataTable.Rows)
            {
                Console.WriteLine(row["Column1"] + "\t" + row["Column2"]);
            }*/

            return dataTable;
        }

        [GeneratedRegex(@"\$(\d+(?:\.\d{1,2})?)")]
        private static partial Regex ApprovedAmountRegex();

        [GeneratedRegex(@"\b(\d+(?:\.\d{1,2})?)\b")]
        private static partial Regex ApprovedAmountWithoutDollarSignRegex();
    }
}