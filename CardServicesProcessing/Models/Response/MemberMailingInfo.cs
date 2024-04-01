using System.ComponentModel;

namespace CardServicesProcessor.Models.Response
{
    public class MemberMailingInfo
    {
        [DisplayName("First")]
        public string? CardHolderFirstName { get; set; }

        [DisplayName("Last")]
        public string? CardHolderLastName { get; set; }

        [DisplayName("Company Name-Formula")]
        public string? CompanyName { get; set; }

        [DisplayName("Vendor Name/Member NHID-Formula")]
        public string? VendorName { get; set; }

        [DisplayName("Print on Checks as Formula")]
        public string? PrintOnCheck { get; set; }

        [DisplayName("Address 1-Formula")]
        public string? Address1 { get; set; }

        [DisplayName("Address 2")]
        public string? Address2 { get; set; }

        [DisplayName("Address 3")]
        public string? Address3 { get; set; }

        public readonly string Terms = "Due on Receipt";

        [DisplayName("Amount Requested")]
        public decimal? TotalAmountDeductedFromMember { get; set; }

        [DisplayName("Check Number")]
        public decimal? CheckNumber { get; set; }

        [DisplayName("Current Rewards Balance")]
        public decimal? CurrentRewardsBalance { get; set; }

        [DisplayName("Current OTC Balance")]
        public decimal? CurrentOTCBalance { get; set; }

        [DisplayName("Notes")]
        public decimal? Notes { get; set; }
    }
}
