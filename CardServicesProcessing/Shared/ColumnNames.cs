namespace CardServicesProcessor.Shared
{
    public class ColumnNames
    {
        // Card Services report
        public const string InsuranceCarrier = "Insurance Carrier";
        public const string HealthPlan = "Health Plan";
        public const string CaseTicketNumber = "Case Ticket Number";
        public const string CaseCategory = "Case Category";
        public const string CreateDate = "Create Date";
        public const string TransactionDate = "Transaction Date";
        public const string MemberFirstName = "Member First Name";
        public const string MemberLastName = "Member Last Name";
        public const string DateOfBirth = "Date Of Birth";
        public const string City = "City";
        public const string State = "State";
        public const string NhMemberId = "NH Member ID";
        public const string InsuranceNumber = "Insurance Number";
        public const string CaseTopic = "Case Topic";
        public const string CaseType = "Case Type";
        public const string CaseTicketData = "Case Ticket Data";
        public const string Wallet = "Wallet";
        public const string DenialReason = "Denial Reason";
        public const string RequestedTotalReimbursementAmount = "Requested Total Reimbursement Amount";
        public const string ApprovedTotalReimbursementAmount = "Approved Total Reimbursement Amount";
        public const string CaseStatus = "Case Status";
        public const string ApprovedStatus = "Approved Status";
        public const string ProcessedDate = "Processed Date";
        public const string ClosingComments = "Closing Comments";

        // Check issuance report
        public const string TxnReferenceId = "Txn Reference ID";
        public const string VendorName = "Vendor Name/Member NHID-Formula";
        public const string CaseNumber = "Case Number";

        // From Manual Adjustments 2023 report
        public const string BenefitWallet = "Benefit Wallet";
        public const string AmountRequested = "Amount Requested";
        public const string CaseClosedDate = "Case Closed Date";

        public const string ReimbursementItem = "ReimbursementItem";

        public static readonly string[] ColumnOrder = ["Insurance Carrier", "Health Plan", "Caseticketnumber", "CaseCategory", "CreateDate", "TransactionDate", "MemberFirstName", "MemberLastName", "DateOfBirth", "City", "State", "Nhmemberid", "InsuranceNumber", "Casetopic", "Casetype", "Caseticketdata", "Wallet", "DenialReason", "RequestedTotalReimbursementAmount", "ApprovedRequestedTotalReimbursementAmount", "CaseStatus", "ApprovedStatus", "ProcessedDate", "ClosingComments"];
        public static readonly string[] HeaderNames = ["Insurance Carrier", "Health Plan", "Case Ticket Number", "Case Category", "Created Date", "Transaction Date", "Member First Name", "Member Last Name", "Date Of Birth", "City", "State", "EH/NH Member Id", "Insurance Number", "Case Topic", "Case Type", "Case Ticket Data", "Wallet", "Denial Reason", "Requested Total Reimbursement Amount", "Approved Total Reimbursement Amount", "Case Status", "Approved Status", "Processed Date", "Closing Comments"];

        public static readonly int CreateDateColNumber = 5;
        public static readonly int TransactionDateColNumber = 6;
        public static readonly int DOBColNumber = 9;
        public static readonly int ProcessedDateColNumber = 23;
    }
}
