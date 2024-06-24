namespace CardServicesProcessor.Models.Response
{
    public class CheckIssuance
    {
        public IEnumerable<RawData> RawData { get; set; }
        public IEnumerable<MemberMailingInfo> MemberMailingInfos { get; set; }
        public IEnumerable<MemberCheckReimbursement> MemberCheckReimbursements { get; set; }
    }
}
