using CardServicesProcessor.Models.Response;

namespace CardServicesProcessor.Shared
{
    public static class CheckIssuanceConstants
    {
        public static readonly string FilePathCurr = @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Approved\{DateTime.Today:M-d}\ReimbursementCheckIssuanceAutomation_{DateTime.Today:MMddyyyy}.xlsx";

        public static readonly string FilePathPrev = @$"C:\Users\Sankalp.Godugu\OneDrive - NationsBenefits\Documents\Business\Case Management\Reimbursement\Approved\{DateTime.Today.AddDays(-7):M-d}\ReimbursementCheckIssuance_{DateTime.Today.AddDays(-7):MMddyyyy}.xlsx";

        public const string RawData = "Raw Data";
        public const string MemberMailingInfo = "Member Mailing Info";
        public const string MemberCheckReimbursement = "Member Check Reimbursement";

        public static readonly Dictionary<int, string> SheetIndexToNameMap = new() {
            { 1, "Raw Data" },
            { 2, "Member Mailing Info" },
            { 3, "Member Check Reimbursement" }
        };

        public static readonly Dictionary<string, Type> SheetNameToTypeMap = new()
        {
            { "Raw Data", typeof(RawData) },
            { "Member Mailing Info", typeof(MemberMailingInfo) },
            { "Member Check Reimbursement", typeof(MemberCheckReimbursement) },
        };

        public static class Nations
        {
            public static string SheetName => typeof(Nations).Name;
            public static string SheetPrev => "Nations Reimbursements";
            public static string SheetCurr => "Nations";
            public static int SheetIndex => 1;
        }

        public static class Elevance
        {
            public static string SheetName => typeof(Elevance).Name;
            public static string SheetPrev => "Elevance Reimbursements";
            public static string SheetCurr => "Elevance";
            public static int SheetIndex => 2;
        }

        public const string Subject = "Reimbursement Check Issuance";
    }
}
