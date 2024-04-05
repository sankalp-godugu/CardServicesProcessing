namespace CardServicesProcessor.Shared
{
    public static class SQLConstantsCardServices
    {
        public static readonly string DropAllCSCases = @"DROP TABLE IF EXISTS #AllCSCases";
        public static readonly string DropTblMemberInsuranceMax = @"DROP TABLE IF EXISTS #MemberInsuranceMax";
        public static readonly string DropTblReimbursementAmount = @"DROP TABLE IF EXISTS #ReimbursementAmount";
        public static readonly string SelectIntoAllCSCases = @"
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
			JOIN	Insurance.InsuranceHealthPlans ihp WITH (NOLOCK) ON ihp.InsuranceHealthPlanID = mc.InsuranceHealthPlanID
			JOIN	Insurance.InsuranceCarriers ic WITH (NOLOCK) ON ic.InsuranceCarrierID = ihp.InsuranceCarrierID AND ic.IsActive = 1
			JOIN	ServiceRequest.MemberCaseTopics mcto ON mcto.casetopicid = mct.CaseTopicID
			JOIN	ServiceRequest.MemberCaseTypeTopicMapping mcttm ON mcttm.CaseTopicID = mcto.CaseTopicID
			JOIN	ServiceRequest.membercasetypes mcty ON mcty.CaseTypeID = mcttm.CaseTypeID
			JOIN	ServiceRequest.MemberCaseStatus mcs ON mcs.CaseStatusID = mc.CaseStatusID
			LEFT JOIN	ServiceRequest.MemberLookUp mlu ON mlu.Id = mct.DenialReasonID
			JOIN	master.Addresses addr ON addr.MemberID = mem.MemberID
			WHERE	FirstName NOT LIKE '%test%'
			AND		LastName NOT LIKE '%test%'
			AND		mc.IsActive = 1
			AND		addr.AddressTypeCode = 'PERM'
			--TEMP
			--AND		mcto.CaseTopicID = 24
			--AND		DATEPART(YEAR, mct.CreateDate) = 2024";
        public static readonly string SelectIntoMemberInsuranceMax = @"
			SELECT		MAX(mi.CreateDate) AS CreateDate,
						mi.MemberID,
						allcs.InsuranceHealthPlanID
			INTO		#MemberInsuranceMax
			FROM		master.MemberInsurances mi
			JOIN		#AllCSCases allcs
			ON			allcs.memberid = mi.MemberID
			AND			mi.InsuranceHealthPlanID = allcs.InsuranceHealthPlanID
			WHERE		mi.IsActive = 1
			GROUP BY	mi.MemberID,
						allcs.InsuranceHealthPlanID";
        public static readonly string SelectIntoTblReimbursementAmount = @"
			SELECT
					allcs.CaseTicketID,
					ri.IsProcessEligible,
					SUM(ri.ApprovedAmount) AS ApprovedTotalAmount
			INTO		#ReimbursementAmount
			FROM		#AllCSCases allcs
			JOIN		ServiceRequest.ReimbursementItems ri
			ON			allcs.CaseTicketID = ri.CaseTicketId
			GROUP BY	allcs.CaseTicketID, ri.IsProcessEligible
			HAVING ri.IsProcessEligible = 1";
        public static readonly string SelectCases = @"
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
				ra.ApprovedTotalAmount,
				allcs.CaseStatus,
				allcs.ApprovedStatus,
				allcs.ClosedDate AS ProcessedDate,
				allcs.ClosingComments
		FROM		#AllCSCases allcs
		JOIN		#MemberInsuranceMax mix ON mix.MemberID = allcs.MemberID
		JOIN		master.MemberInsurances mi ON allcs.MemberID = mi.MemberID AND mix.CreateDate = mi.CreateDate
		JOIN		master.MemberInsuranceDetails mid ON mi.ID = mid.MemberInsuranceID
		LEFT JOIN	ServiceRequest.ReimbursementItems ri ON allcs.CaseTicketId = ri.CaseTicketID AND ri.IsProcessEligible = 1
		LEFT JOIN	#ReimbursementAmount ra ON allcs.CaseTicketID = ra.CaseTicketID
		ORDER BY allcs.CreateDate";

        public static readonly List<Tuple<string, string>> QueryToNameMap =
        [
            new (DropAllCSCases, nameof(DropAllCSCases)),
            new (DropTblMemberInsuranceMax, nameof(DropTblMemberInsuranceMax)),
            new (DropTblReimbursementAmount, nameof(DropTblReimbursementAmount)),
            new (SelectIntoAllCSCases, nameof(SelectIntoAllCSCases)),
            new (SelectIntoMemberInsuranceMax, nameof(SelectIntoMemberInsuranceMax)),
            new (SelectIntoTblReimbursementAmount, nameof(SelectIntoTblReimbursementAmount)),
            new (SelectCases, nameof(SelectCases))
        ];
    }
}