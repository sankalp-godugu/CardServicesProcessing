namespace CardServicesProcessor.Shared
{
    public static class CardServicesConstants
    {
        public static readonly string FilePathCurr = @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\YTD\{DateTime.Today:M-dd}\CardServicesReport_{DateTime.Today:MMddyyyy}.xlsx";

        public static readonly string FilePathPrev = @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\YTD\{DateTime.Now.AddDays(7):M-dd}\CardServicesReport_{DateTime.Now.AddDays:MMddyyyy}.xlsx";

        public static readonly string ManualReimbursements2023SrcFilePath = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Manual Adjustments 2023.xlsx";

        public static readonly string ManualReimbursements2024SrcFilePath = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Manual Reimbursements.xlsx";

        public static readonly string TestFilePath = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\TestPurposesOnly.xlsx";

        public static class Elevance
        {
            public static readonly string SheetName = typeof(Elevance).Name;
            public static readonly string SheetPrev = "Elevance Reimbursements";
            public static readonly string SheetRaw = "ELV - Raw";
            public static readonly string SheetDraft = "ELV - Draft";
            public static readonly string SheetFinal = "ELV - Final";
            public static readonly int SheetFinalIndex = 1;
        }

        public static class Nations
        {
            public static readonly string SheetName = typeof(Nations).Name;
            public static readonly string SheetPrev = "Nations Reimbursements";
            public static readonly string SheetRaw = "NB - Raw";
            public static readonly string SheetDraft = "NB - Draft";
            public static readonly string SheetFinal = "NB - Final";
            public static readonly int SheetFinalIndex = 2;
        }
    }
}
