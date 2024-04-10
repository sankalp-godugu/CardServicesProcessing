namespace CardServicesProcessor.Shared
{
    public static class SqlConstantsReimbursementItems
    {
        public static readonly string GetProductNames = @"
        select distinct
	        mct.CaseTicketNumber
	        ,ri.ProductName
	        ,ri.WalletValue
        from ServiceRequest.ReimbursementItems ri
        join ServiceRequest.MemberCaseTickets mct
        on ri.CaseTicketID = mct.CaseTicketID";
    }
}
