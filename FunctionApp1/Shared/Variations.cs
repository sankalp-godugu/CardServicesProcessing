namespace ReimbursementReporting.Shared
{
    public static class Variations
    {
        public static readonly string[] ApprovedVariations = [
            "approval/denial: approv",
            "manual approv",
            "approved manual",
            "partial approv",
            "approved partial",
            "approved for",
            "approval for",
            "case approved",
            "advised of partial approval amount"
        ];
        public static readonly string[] DeclinedVariations = [
            "approval/denial: deni",
            "approval/denial: deny",
            "approval/denial: decline",
            "denied for",
            "declined for",
            "denial for",
            "denied",
            "duplicate",
            "case declined",
            "case denied"
        ];
    }
}