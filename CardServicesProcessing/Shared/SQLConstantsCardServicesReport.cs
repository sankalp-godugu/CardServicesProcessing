namespace CardServicesProcessor.Shared
{
	public static class SQLConstantsCardServicesReport
	{
		public static readonly string CardServicesQuery = @"-- 1) get all active CS cases
		DROP TABLE IF EXISTS #AllCSCases
		SELECT
				ic.InsuranceCarrierName,
				mc.CaseID,
				mc.CreateUser,
				mct.CaseTicketNumber,
				mct.CaseTicketID,
				mct.ApprovedStatus,
				mcc.CaseCategory,
				mct.AssignedTo,
				mc.CaseNumber,
				mem.NHMemberID,
				mem.MemberID,
				mem.FirstName,
				mem.LastName,
				mem.DateOfBirth,
				addr.City,
				addr.State,
				ihp.HealthPlanName,
				ihp.InsuranceHealthPlanID,
				mcto.CaseTopic,
				mcto.CaseTopicID,
				mcty.CaseType,
				mct.CaseTicketData,
				mc.RequestorName,
				mcs.CaseStatus,
				mct.CreateDate,
				mct.TransactionStatus,
				mct.ClosingComments,
				mlu.Name AS DenialReason,
				mct.ClosedDate
		INTO	#AllCSCases
		FROM	ServiceRequest.MemberCases mc
		JOIN	ServiceRequest.MemberCaseTickets mct ON mct.CaseID=mc.CaseID
		JOIN	ServiceRequest.MemberCaseCategory mcc ON mcc.CaseCategoryID = mct.CaseCategoryID AND mcc.CaseCategoryID = 1
		JOIN	master.Members mem ON mem.NHMemberID = mc.NHMemberID
		JOIN	Insurance.InsuranceHealthPlans ihp WITH(NOLOCK) ON ihp.InsuranceHealthPlanID = mc.InsuranceHealthPlanID
		JOIN	Insurance.InsuranceCarriers ic WITH(NOLOCK) ON ic.InsuranceCarrierID = ihp.InsuranceCarrierID AND ic.IsActive = 1
		JOIN ServiceRequest.MemberCaseTopics mcto ON mcto.casetopicid = mct.CaseTopicID
		JOIN ServiceRequest.MemberCaseTypeTopicMapping mcttm ON mcttm.CaseTopicID = mcto.CaseTopicID
		JOIN ServiceRequest.membercasetypes mcty ON mcty.CaseTypeID = mcttm.CaseTypeID
		JOIN ServiceRequest.MemberCaseStatus mcs ON mcs.CaseStatusID = mc.CaseStatusID
		LEFT JOIN ServiceRequest.MemberLookUp mlu ON mlu.Id = mct.DenialReasonID
		JOIN master.Addresses addr ON addr.MemberID = mem.MemberID
		WHERE FirstName NOT LIKE '%test%'
		AND LastName NOT LIKE '%test%'
		AND mc.IsActive = 1
		AND addr.AddressTypeCode = 'PERM'

		-- 2) get newest CS cases for each member-health plan combo (only for active health plans)
		DROP TABLE IF EXISTS #MemberInsuranceMax
		SELECT   MAX(mi.CreateDate) AS CreateDate, -- why are we doing this?
				 mi.MemberID,
				 allcs.InsuranceHealthPlanID
		INTO     #MemberInsuranceMax
		FROM     master.MemberInsurances mi
		JOIN     #AllCSCases allcs
		ON       allcs.memberid = mi.MemberID
		AND      mi.InsuranceHealthPlanID = allcs.InsuranceHealthPlanID
		WHERE    mi.IsActive = 1
		GROUP BY mi.MemberID,
				 allcs.InsuranceHealthPlanID

		-- 3) table of total reimbursement amount for each case (only including those items that are eligible for reimbursement)
		DROP TABLE IF EXISTS #ReimbursementAmount
		SELECT allcs.CaseTicketID, ri.IsProcessEligible, SUM(ri.ApprovedAmount) AS ApprovedTotalAmount
		INTO #ReimbursementAmount
		FROM #AllCSCases allcs
		JOIN ServiceRequest.ReimbursementItems ri
		ON allcs.CaseTicketID = ri.CaseTicketId
		GROUP BY allcs.CaseTicketID, ri.IsProcessEligible
		HAVING ri.IsProcessEligible = 1

		-- get reimbursement amounts and transaction dates corresponding to each requested amount
		--DROP TABLE IF EXISTS #Final
		SELECT DISTINCT
				allcs.InsuranceCarrierName,
	 			allcs.HealthPlanName,
				allcs.CaseTicketNumber,
				allcs.CaseCategory,
				allcs.CreateDate,
				allcs.FirstName,
				allcs.LastName,
				allcs.DateOfBirth,
				allcs.City,
				allcs.State,
				allcs.NHMemberID,
				mid.InsuranceNbr,
				allcs.CaseTopic,
				allcs.CaseType,
				allcs.CaseTicketData,
				ri.WalletValue,
				allcs.DenialReason,
				ra.ApprovedTotalAmount AS ApprovedTotalReimbursementAmount,
				allcs.CaseStatus,
				allcs.ApprovedStatus,
				allcs.ClosedDate AS ProcessedDate,
				allcs.ClosingComments
		--INTO		#Final
		FROM		#AllCSCases allcs
		JOIN		#MemberInsuranceMax mix ON mix.MemberID = allcs.MemberID
		JOIN		master.MemberInsurances mi ON allcs.MemberID = mi.MemberID AND mix.CreateDate = mi.CreateDate
		JOIN		master.MemberInsuranceDetails mid ON mi.ID = mid.MemberInsuranceID
		LEFT JOIN	ServiceRequest.ReimbursementItems ri ON allcs.CaseTicketId = ri.CaseTicketID AND ri.IsProcessEligible = 1
		LEFT JOIN	#ReimbursementAmount ra ON allcs.CaseTicketID = ra.CaseTicketID
		ORDER BY allcs.CreateDate

		/*SELECT
			fin.InsuranceCarrierName,
			fin.HealthPlanName,
			fin.CaseTicketNumber,
			fin.CaseCategory,
			fin.CreateDate,
			--fin.TransactionDate,
			fin.FirstName,
			fin.LastName,
			fin.DateOfBirth,
			fin.City,
			fin.State,
			fin.NHMemberID,
			fin.InsuranceNbr,
			fin.CaseTopic,
			fin.CaseType,
			fin.CaseTicketData,
			fin.WalletValue AS Wallet,
			fin.DenialReason,
			--fin.RequestedTotalReimbursementAmount,
			fin.ApprovedTotalAmount AS ApprovedTotalReimbursementAmount,
			fin.CaseStatus,
			fin.ApprovedStatus,
			fin.ClosedDate AS ProcessedDate,
			fin.ClosingComments
		FROM #Final fin
		ORDER BY fin.CreateDate*/";

		public static readonly string DropAllCSCases = @"-- 1) get all active CS casesDROP TABLE IF EXISTS #AllCSCases";

		public static readonly string SelectIntoAllCSCases = @"SELECT
				ic.InsuranceCarrierName,
				mc.CaseID,
				mc.CreateUser,
				mct.CaseTicketNumber,
				mct.CaseTicketID,
				mct.ApprovedStatus,
				mcc.CaseCategory,
				mct.AssignedTo,
				mc.CaseNumber,
				mem.NHMemberID,
				mem.MemberID,
				mem.FirstName,
				mem.LastName,
				mem.DateOfBirth,
				addr.City,
				addr.State,
				ihp.HealthPlanName,
				ihp.InsuranceHealthPlanID,
				mcto.CaseTopic,
				mcto.CaseTopicID,
				mcty.CaseType,
				mct.CaseTicketData,
				mc.RequestorName,
				mcs.CaseStatus,
				mct.CreateDate,
				mct.TransactionStatus,
				mct.ClosingComments,
				mlu.Name AS DenialReason,
				mct.ClosedDate
		INTO	#AllCSCases
		FROM	ServiceRequest.MemberCases mc
		JOIN	ServiceRequest.MemberCaseTickets mct ON mct.CaseID=mc.CaseID
		JOIN	ServiceRequest.MemberCaseCategory mcc ON mcc.CaseCategoryID = mct.CaseCategoryID AND mcc.CaseCategoryID = 1
		JOIN	master.Members mem ON mem.NHMemberID = mc.NHMemberID
		JOIN	Insurance.InsuranceHealthPlans ihp WITH(NOLOCK) ON ihp.InsuranceHealthPlanID = mc.InsuranceHealthPlanID
		JOIN	Insurance.InsuranceCarriers ic WITH(NOLOCK) ON ic.InsuranceCarrierID = ihp.InsuranceCarrierID AND ic.IsActive = 1
		JOIN ServiceRequest.MemberCaseTopics mcto ON mcto.casetopicid = mct.CaseTopicID
		JOIN ServiceRequest.MemberCaseTypeTopicMapping mcttm ON mcttm.CaseTopicID = mcto.CaseTopicID
		JOIN ServiceRequest.membercasetypes mcty ON mcty.CaseTypeID = mcttm.CaseTypeID
		JOIN ServiceRequest.MemberCaseStatus mcs ON mcs.CaseStatusID = mc.CaseStatusID
		LEFT JOIN ServiceRequest.MemberLookUp mlu ON mlu.Id = mct.DenialReasonID
		JOIN master.Addresses addr ON addr.MemberID = mem.MemberID
		WHERE FirstName NOT LIKE '%test%'
		AND LastName NOT LIKE '%test%'
		AND mc.IsActive = 1
		AND addr.AddressTypeCode = 'PERM'";

		public static readonly string DropTblMemberInsuranceMax = @"DROP TABLE IF EXISTS #MemberInsuranceMax";

		public static readonly string SelectIntoMemberInsuranceMax = @"SELECT   MAX(mi.CreateDate) AS CreateDate, -- why are we doing this?
				 mi.MemberID,
				 allcs.InsuranceHealthPlanID
		INTO     #MemberInsuranceMax
		FROM     master.MemberInsurances mi
		JOIN     #AllCSCases allcs
		ON       allcs.memberid = mi.MemberID
		AND      mi.InsuranceHealthPlanID = allcs.InsuranceHealthPlanID
		WHERE    mi.IsActive = 1
		GROUP BY mi.MemberID,
				 allcs.InsuranceHealthPlanID";

        public static readonly string DropTblReimbursementAmount = @"DROP TABLE IF EXISTS #ReimbursementAmount";

		public static readonly string SelectIntoTblReimbursementAmount = @"SELECT allcs.CaseTicketID, ri.IsProcessEligible, SUM(ri.ApprovedAmount) AS ApprovedTotalAmount
		INTO #ReimbursementAmount
		FROM #AllCSCases allcs
		JOIN ServiceRequest.ReimbursementItems ri
		ON allcs.CaseTicketID = ri.CaseTicketId
		GROUP BY allcs.CaseTicketID, ri.IsProcessEligible
		HAVING ri.IsProcessEligible = 1";

		public static readonly string SelectCases = @"SELECT DISTINCT
				allcs.InsuranceCarrierName,
	 			allcs.HealthPlanName,
				allcs.CaseTicketNumber,
				allcs.CaseCategory,
				allcs.CreateDate,
				allcs.FirstName,
				allcs.LastName,
				allcs.DateOfBirth,
				allcs.City,
				allcs.State,
				allcs.NHMemberID,
				mid.InsuranceNbr,
				allcs.CaseTopic,
				allcs.CaseType,
				allcs.CaseTicketData,
				ri.WalletValue AS Wallet,
				allcs.DenialReason,
				ra.ApprovedTotalAmount AS ApprovedTotalReimbursementAmount,
				allcs.CaseStatus,
				allcs.ApprovedStatus,
				allcs.ClosedDate AS ProcessedDate,
				allcs.ClosingComments
		--INTO		#Final
		FROM		#AllCSCases allcs
		JOIN		#MemberInsuranceMax mix ON mix.MemberID = allcs.MemberID
		JOIN		master.MemberInsurances mi ON allcs.MemberID = mi.MemberID AND mix.CreateDate = mi.CreateDate
		JOIN		master.MemberInsuranceDetails mid ON mi.ID = mid.MemberInsuranceID
		LEFT JOIN	ServiceRequest.ReimbursementItems ri ON allcs.CaseTicketId = ri.CaseTicketID AND ri.IsProcessEligible = 1
		LEFT JOIN	#ReimbursementAmount ra ON allcs.CaseTicketID = ra.CaseTicketID
		ORDER BY allcs.CreateDate";
    }
}