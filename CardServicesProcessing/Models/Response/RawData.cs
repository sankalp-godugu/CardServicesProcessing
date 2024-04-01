using System.ComponentModel;

namespace CardServicesProcessor.Models.Response
{
    public class RawData
    {
        [DisplayName("NH/EH Member ID")]
        public string? NhMemberId { get; set; }

        [DisplayName("Carrier")]
        public string? InsuranceCarrierName { get; set; }

        [DisplayName("Member Proxy")]
        public string? CardReferenceNumber { get; set; }

        [DisplayName("Txn ID")]
        public string? TxnId { get; set; }

        [DisplayName("Txn Reference ID")]
        public string? TxnReferenceId { get; set; }

        [DisplayName("Purse Slot")]
        public string? PurseSlot { get; set; }

        [DisplayName("Amount Deducted From Member")]
        public decimal? AmountDeductedFromMember { get; set; }

        [DisplayName("Request Date")]
        public DateTime? RequestDate { get; set; }

        [DisplayName("Cardholder First Name")]
        public string? CardholderFirstName { get; set; }

        [DisplayName("Cardholder Last Name")]
        public string? CardholderLastName { get; set; }

        [DisplayName("Company Name")]
        public string? CompanyName { get; set; }

        [DisplayName("Vendor Name")]
        public string? VendorName { get; set; }

        [DisplayName("Print on Check")]
        public string? PrintOnCheck { get; set; }

        [DisplayName("Address 1")]
        public string? Address1 { get; set; }

        [DisplayName("Address 2")]
        public string? Address2 { get; set; }

        [DisplayName("Address 3")]
        public string? Address3 { get; set; }

        [DisplayName("Validation")]
        public string? Validation { get; set; }

        [DisplayName("Deduction Date")]
        public string? DeductionDate { get; set; }

        [DisplayName("Previously Requested")]
        public string? PreviouslyRequested { get; set; }
    }
}
