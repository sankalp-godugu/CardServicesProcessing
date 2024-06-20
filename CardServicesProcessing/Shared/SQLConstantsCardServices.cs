namespace CardServicesProcessor.Shared
{
    public static class SQLConstantsCardServices
    {
        public static readonly string DropTblAllCases = @"DROP TABLE IF EXISTS #AllCSCases;";
        public static readonly string DropTblMemberInsuranceMax = @"DROP TABLE IF EXISTS #MemberInsuranceMax;";
        public static readonly string DropTblReimbursementAmount = @"DROP TABLE IF EXISTS #ReimbursementAmount;";
		public static readonly string SelectIntoTblAllCases = @"
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
					CONVERT(DATETIME, mct.CreateDate AT TIME ZONE 'Eastern Standard Time') CreateDate,
					mct.TransactionStatus,
					mct.ClosingComments,
					mlu.Name AS DenialReason,
					mct.ClosedDate
			INTO	#AllCases
			FROM	ServiceRequest.MemberCases mc
			JOIN	ServiceRequest.MemberCaseTickets mct ON mct.CaseID=mc.CaseID
			JOIN	ServiceRequest.MemberCaseCategory mcc ON mcc.CaseCategoryID = mct.CaseCategoryID AND mcc.CaseCategoryID = @caseCategoryId
			JOIN	master.Members mem ON mem.NHMemberID = mc.NHMemberID
			JOIN	Insurance.InsuranceHealthPlans ihp WITH (NOLOCK) ON ihp.InsuranceHealthPlanID = mc.InsuranceHealthPlanID
			JOIN	Insurance.InsuranceCarriers ic WITH (NOLOCK) ON ic.InsuranceCarrierID = ihp.InsuranceCarrierID AND ic.IsActive = @isActive
			JOIN	ServiceRequest.MemberCaseTopics mcto ON mcto.casetopicid = mct.CaseTopicID
			JOIN	ServiceRequest.MemberCaseTypeTopicMapping mcttm ON mcttm.CaseTopicID = mcto.CaseTopicID
			JOIN	ServiceRequest.membercasetypes mcty ON mcty.CaseTypeID = mcttm.CaseTypeID
			JOIN	ServiceRequest.MemberCaseStatus mcs ON mcs.CaseStatusID = mc.CaseStatusID
			LEFT JOIN	ServiceRequest.MemberLookUp mlu ON mlu.Id = mct.DenialReasonID
			JOIN	master.Addresses addr ON addr.MemberID = mem.MemberID
			WHERE	FirstName NOT LIKE '%test%'
			AND		LastName NOT LIKE '%test%'
			AND		mc.IsActive = @isActive
			AND		addr.AddressTypeCode = @addressTypeCode
			--AND		mct.CaseTopicID = @caseTopicId
			--AND		ic.InsuranceCarrierId = 444
			AND		YEAR(CONVERT(DATETIME, mct.CreateDate AT TIME ZONE 'Eastern Standard Time')) in (@year)
			--OR		YEAR(CONVERT(DATETIME, mct.ClosedDate AT TIME ZONE 'Eastern Standard Time')) = @closedYear);";
        public static readonly string SelectIntoTblMemberInsuranceMax = @"
            SELECT		MAX(mi.CreateDate) AS CreateDate,
						mi.MemberID,
						allCases.InsuranceHealthPlanID
			INTO		#MemberInsuranceMax
			FROM		master.MemberInsurances mi
			JOIN		#AllCases allCases
			ON			allCases.memberid = mi.MemberID
			AND			mi.InsuranceHealthPlanID = allCases.InsuranceHealthPlanID
			WHERE		mi.IsActive = @isActive
			GROUP BY	mi.MemberID,
						allCases.InsuranceHealthPlanID;";
        public static readonly string SelectIntoTblReimbursementAmount = @"
			SELECT
					allCases.CaseTicketID,
					ri.IsProcessEligible,
					SUM(ri.ApprovedAmount) AS ApprovedTotalAmount
			INTO		#ReimbursementAmount
			FROM		#AllCases allCases
			JOIN		ServiceRequest.ReimbursementItems ri
			ON			allCases.CaseTicketID = ri.CaseTicketId
			GROUP BY	allCases.CaseTicketID, ri.IsProcessEligible
			HAVING		ri.IsProcessEligible = @isProcessEligible;";
        public static readonly string SelectFromTblCases = @"
			SELECT DISTINCT
				allCases.InsuranceCarrierName,
	 			allCases.HealthPlanName,
				allCases.CaseTicketNumber,
				allCases.CaseCategory,
				allCases.CreateDate,
				allCases.FirstName,
				allCases.LastName,
				allCases.DateOfBirth,
				allCases.City,
				allCases.State,
				allCases.NHMemberID,
				mid.InsuranceNbr,
				allCases.CaseTopic,
				allCases.CaseType,
				allCases.CaseTicketData,
				allCases.AssignedTo,
				ri.WalletValue,
				allCases.DenialReason,
				ra.ApprovedTotalAmount,
				allCases.CaseStatus,
				allCases.ApprovedStatus,
				allCases.ClosedDate AS ProcessedDate,
				allCases.ClosingComments
		FROM		#AllCases allCases
		JOIN		#MemberInsuranceMax mix ON mix.MemberID = allCases.MemberID
		JOIN		master.MemberInsurances mi ON allCases.MemberID = mi.MemberID AND mix.CreateDate = mi.CreateDate
		JOIN		master.MemberInsuranceDetails mid ON mi.ID = mid.MemberInsuranceID
		LEFT JOIN	ServiceRequest.ReimbursementItems ri ON allCases.CaseTicketId = ri.CaseTicketID AND ri.IsProcessEligible = @isProcessEligible
		LEFT JOIN	#ReimbursementAmount ra ON allCases.CaseTicketID = ra.CaseTicketID;";
    }
}