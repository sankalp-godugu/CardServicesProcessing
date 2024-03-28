namespace CardServicesProcessor.Models.Response
{
    public class RawData
    {
        public string? NhMemberId { get; set; }
        public string? InsuranceCarrierName { get; set; }
        public string? CardReferenceNumber { get; set; }
        public string? TxnId { get; set; }
        public string? TxnReferenceId { get; set; }
        public string? PurseSlot { get; set; }
        public string? AmountDeductedFromMember { get; set; }
        public string? RequestDate { get; set; }
        public string? CardholderFirstName { get; set; }
        public string? CardholderLastName { get; set; }
        public string? CompanyName { get; set; }
        public string? VendorName { get; set; }
        public string? PrintOnCheck { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
    }
}
