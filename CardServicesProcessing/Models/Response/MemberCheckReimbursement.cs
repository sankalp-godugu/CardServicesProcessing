namespace CardServicesProcessor.Models.Response
{
    public class MemberCheckReimbursement
    {
        public string? TxnId { get; set; }
        public string? VendorName { get; set; }
        public string? Memo { get; set; }
        public string? RequestDate { get; set; }
        public decimal? R { get; set; }
    }
}