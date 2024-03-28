namespace CardServicesProcessor.Shared
{
    public static class CheckIssuanceConstants
    {
        public static string FilePathCurr => @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Approved\{DateTime.Today:M-d}\ReimbursementCheckIssuance_{DateTime.Today:MMddyyyy}.xlsx";

        public static readonly string FilePathPrev = @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Approved\{DateTime.Today.AddDays(-7):M-d}\ReimbursementCheckIssuance_{DateTime.Today.AddDays(-7):MMddyyyy}.xlsx";

        public static readonly Dictionary<int, string> Sheets = new() {
            { 1, "Raw Data" },
            { 2, "Member Mailing Info" },
            { 3, "Member Check Reimbursement" }
        };

        public static class Nations
        {
            public static string SheetName => typeof(Nations).Name;
            public static string SheetPrev => "Nations Reimbursements";
            public static string SheetRaw => "NB - Raw";
            public static string SheetDraft => "NB - Draft";
            public static string SheetFinal => "NB - Final";
            public static int SheetDraftIndex => 5;
        }

        public static class Elevance
        {
            public static string SheetName => typeof(Elevance).Name;
            public static string SheetPrev => "Elevance Reimbursements";
            public static string SheetRaw => "ELV - Raw";
            public static string SheetDraft => "ELV - Draft";
            public static string SheetFinal => "ELV - Final";
            public static int SheetDraftIndex => 2;
        }
    }
}
