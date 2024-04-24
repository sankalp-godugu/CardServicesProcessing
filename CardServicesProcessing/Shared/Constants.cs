namespace CardServicesProcessor.Shared
{
    public class Constants
    {
        public const string NotReimbursement = "Refund (not a reimbursement)";
        public const string IneligibleRetailer = "Ineligible Retailer (not allowed)";
        public const string BenefitUtilized = "Benefit utilized (no money available)";
        public const string Duplicate = "Duplicate";

        public const string Reimbursement = "Reimbursement";
        public readonly List<string> wordsToExclude = ["a", "an", "the", "for", "and", "of", "by", "&"];
    }
}
