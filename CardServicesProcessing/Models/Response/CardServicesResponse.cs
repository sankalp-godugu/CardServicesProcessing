using System;

namespace ReimbursementReporting.Models.Response
{
    public class CardServicesResponse
    {
        public string InsuranceCarrier { get; set; }
        public string HealthPlan { get; set; }
        public string CaseTicketNumber { get; set; }
        public string CaseCategory { get; set; }
        public DateTime? CreateDate { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string MemberFirstName { get; set; }
        public string MemberLastName { get; set; }
        public DateTime? DateOfBirth { get; set; }
        public string City { get; set; }
        public string State { get; set; }
        public string NhMemberId { get; set; }
        public long InsuranceNbr { get; set; }
        public string CaseTopic { get; set; }
        public string CaseType { get; set; }
        public string CaseTicketData { get; set; }
        public string Wallet { get; set; }
        public string DenialReason { get; set; }
        public decimal? RequestedReimbursementTotalAmount { get; set; }
        public decimal? ApprovedReimbursementTotalAmount { get; set; }
        public string CaseStatus { get; set; }
        public string ApprovedStatus { get; set; }
        public DateTime? ProcessedDate { get; set; }
        public string ClosingComments { get; set; }
    }
}
