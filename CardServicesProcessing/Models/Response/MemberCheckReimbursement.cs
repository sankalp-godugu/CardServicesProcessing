using System.ComponentModel;

namespace CardServicesProcessor.Models.Response
{
    public class MemberCheckReimbursement
    {
        [DisplayName("Carrier Class-Completed by AP")]
        public string? CarrierClassCompletedByAp { get; set; }

        [DisplayName("Case Number")]
        public string? CaseNumber { get; set; }

        [DisplayName("Vendor name/Member NHID")]
        public string? VendorName { get; set; }

        [DisplayName("Memo")]
        public string? Memo { get; set; }

        [DisplayName("Date")]
        public string? RequestDate { get; set; }

        [DisplayName("Amount")]
        public decimal? TotalAmountDeductedFromMember { get; set; }

        [DisplayName("Bill Due")]
        public string? BillDue { get; set; }

        [DisplayName("Account-Completed by AP")]
        public string? AccountCompletedByAP { get; set; }
    }
}