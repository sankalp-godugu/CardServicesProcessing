namespace CardServicesProcessor.Shared
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
            "approved case",
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
            "case denied",
            "Case closed due to being a storefront order reshipment request"
        ];
        public static readonly string[] AssistiveDevicesVariations = ["assistive devices"];
        public static readonly string[] ActiveFitnessVariations = ["fitness", "AAF"];
        public static readonly string[] DvhVariations = ["dvh", "dhv", "vdh", "vhd", "hvd", "hdv", "dental", "vision", "hearing", "ZVD2300"];
        public static readonly string[] HgVariations = ["hg", "grocer", "heg", "healthy food", "FOD2312"];
        public static readonly string[] OtcVariations = ["otc", "over the counter", "ESM"];
        public static readonly string[] PersVariations = [" pers "];
        public static readonly string[] RewardsVariations = ["Wellness your way", "Open Spend - Unrestricted Rewards"];
        public static readonly string[] ServiceVariations = ["service dog", " dog "];
        public static readonly string[] UtilityVariations = ["utility", "utilities", "utl", "parking", "bathroom safety"];
        public static readonly string[] NaVariations = ["not a valid reimbursement",
            "not a reimbursement",
            "not reimbursement",
            "reshipment was already submitted",
            "needed reshipment not reimbursement",
            "already reshipped",
            "should have been a reship or refund",
            "Item requested for reimbursement was reshipped",
            "Item was reshipped on storefront",
            "Reshipped on storefront",
            "Member had the items reshipped through storefront",
            "Nations reship/reship",
            "Items reshipped in storefront",
            "Refund or reshipment",
            "Request for reship was submitted",
            "Reshipment reflected as re-shipped",
            "Reship was done on store front",
            "Reshipment completed on storefront",
            "Reshipment was sent out",
            "Reshipment was already sent out",
            "Reshipment placed online",
            "Reshipment don't on store front",
            "Reshipment has already been placed",
            "Reshipment is now in review",
            "Reshipment was put in place",
            "Reshipment is already underway",
            "Reship request made",
            "Reshipment in progress",
            "Reshipment request submitted",
            "Reship request submitted",
            "Reshipment already processed",
            "Reship request has been submitted",
            "Reshipment is already in review",
            "Reshipment In Transit",
            "Reshipment requested",
            "Reship requested",
            "Refund was approved",
            "Refund processed",
            "Refund submitted",
            "Refund requested",
            "Refund already processed",
            "Refund in process",
            "Refund/reship processed through storefront",
            "Refund/reship requested",
            "Refund case closed",
            "Refund/reship submitted",
            "Refund already completed",
            "Refund requested in the storefront",
            "Storefront order reshipment",
            "MCO handles refund/reship",
        ];
        public static readonly string[] OtherBenefitTypes = ["y", "yes", "n", "no", "denied", "ptc", "none"];
    }
}