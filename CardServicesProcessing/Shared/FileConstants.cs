namespace CardServicesProcessor.Shared
{
    public static class FileConstants
    {
        public static string CurrReportFilePath => @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\YTD\Changes\YTD_CardServices.xlsx";

        public static readonly string FilePathPrev = @"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\YTD\Elevance Reimbursement Data YTD - 03142024.xlsx";

        public static class Nations
        {
            public static string SheetRaw => "NB - Raw";
            public static string SheetPrev => "Nations Reimbursements";
            public static string SheetDraft => "NB - Draft";
            public static int Location => 5;
        }

        public static class Elevance
        {
            public static string SheetRaw => "ELV - Raw";
            public static string SheetPrev => "Elevance Reimbursements";
            public static string SheetDraft => "ELV - Draft";
            public static int Location => 2;
        }
    }
}
