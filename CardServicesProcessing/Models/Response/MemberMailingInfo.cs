namespace CardServicesProcessor.Models.Response
{
    public class MemberMailingInfo
    {
        public string? CardHolderFirstName { get; set; }
        public string? CardHolderLastName { get; set; }
        public string? CompanyName { get; set; }
        public string? VendorName { get; set; }
        public string? PrintOnCheck { get; set; }
        public string? Address1 { get; set; }
        public string? Address2 { get; set; }
        public string? Address3 { get; set; }
        public string? Terms { get; set; }
        public decimal? R { get; set; }
    }
}
