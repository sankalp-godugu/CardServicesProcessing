namespace CardServicesProcessor.Models.Response
{
    public class CheckIssuance
    {
        public required IEnumerable<RawData> RawData { get; set; }
        public required IEnumerable<MemberMailingInfo> MemberMailingInfos { get; set; }
        public required IEnumerable<MemberCheckReimbursement> MemberCheckReimbursements { get; set; }
    }
}
