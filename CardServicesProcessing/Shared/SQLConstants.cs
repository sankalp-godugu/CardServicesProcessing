namespace CardServicesProcessor.Shared
{
    public class SQLConstants
    {
        public static string Query => @"--CREATE OR ALTER PROCEDURE ServiceRequest.GetCardServicesCasesYTD
--AS

-- 1) get all active CS cases
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
-- 2024 reimbursements only
--AND (mcto.CaseTopicID = 24 OR mcto.CaseTopic = 'Reimbursement')
--AND YEAR(CONVERT(datetime, mct.CreateDate AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time')) = 2024
--AND MONTH(mct.CreateDate) = 2
--AND ihp.HealthPlanName like '%CDPHP%'

--select * from #AllCSCases

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

--select * from #MemberInsuranceMax

-- 3) table of total reimbursement amount for each case (only including those items that are eligible for reimbursement)
DROP TABLE IF EXISTS #ReimbursementAmount
SELECT allcs.CaseTicketID, ri.IsProcessEligible, SUM(ri.ApprovedAmount) AS ApprovedTotalAmount
INTO #ReimbursementAmount
FROM #AllCSCases allcs
JOIN ServiceRequest.ReimbursementItems ri
ON allcs.CaseTicketID = ri.CaseTicketId
GROUP BY allcs.CaseTicketID, ri.IsProcessEligible
HAVING ri.IsProcessEligible = 1

--select * from #ReimbursementAmount

-- get reimbursement amounts and transaction dates corresponding to each requested amount
DROP TABLE IF EXISTS #Final
SELECT DISTINCT
		allcs.InsuranceCarrierName,
	 	allcs.HealthPlanName,
		allcs.CaseTicketNumber,
		allcs.CaseCategory,

		-- CREATE DATE
		allcs.CreateDate,
		--CONVERT(VARCHAR(MAX), CONVERT(DATETIME, allcs.CreateDate AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time'), 101) AS CreateDate,
		
		-- TRANSACTION DATE
		CASE
			WHEN ISJSON(allcs.CaseTicketData) = 1
				AND JSON_PATH_EXISTS(allcs.CaseTicketData, '$.TransactionDate') = 1
				AND TRY_CONVERT(DATETIME, JSON_VALUE(allcs.CaseTicketData, '$.TransactionDate')) IS NULL
				THEN 'Invalid Date'
			WHEN ISJSON(allcs.CaseTicketData) = 1
				AND JSON_PATH_EXISTS(allcs.CaseTicketData, '$.""New Reimbursement Request"".TransactionDate') = 1
				AND	TRY_CONVERT(DATETIME, JSON_VALUE(allcs.CaseTicketData, '$.""New Reimbursement Request"".TransactionDate')) IS NULL
				THEN 'Invalid Date'
			WHEN ISJSON(allcs.CaseTicketData) = 1
				AND JSON_PATH_EXISTS(allcs.CaseTicketData, '$.TransactionDate') = 1
				AND TRY_CONVERT(DATETIME, JSON_VALUE(allcs.CaseTicketData, '$.TransactionDate')) IS NOT NULL
				THEN CONVERT(VARCHAR(MAX), TRY_CONVERT(DATETIME, JSON_VALUE(CaseTicketData, '$.TransactionDate')), 121)
			WHEN ISJSON(CaseTicketData) = 1
				AND JSON_PATH_EXISTS(CaseTicketData, '$.""New Reimbursement Request"".TransactionDate') = 1
				AND TRY_CONVERT(DATETIME, JSON_VALUE(CaseTicketData, '$.""New Reimbursement Request"".TransactionDate')) IS NOT NULL
				THEN CONVERT(VARCHAR(MAX), TRY_CONVERT(DATETIME,JSON_VALUE(CaseTicketData, '$.""New Reimbursement Request"".TransactionDate')), 121)
			ELSE 'Invalid Date'
		END AS TransactionDate,

		-- MEMBER INFO
		allcs.FirstName,
		allcs.LastName,
		--CONVERT(VARCHAR(MAX),DateOfBirth, 101) DateOfBirth,
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

		-- REQUESTED AMOUNT
		CASE WHEN ISJSON(allcs.CaseTicketData) = 1 AND allcs.CaseTopicID = 24
			 THEN CONVERT(DECIMAL(10,2), JSON_VALUE(allcs.CaseTicketData, '$.""New Reimbursement Request"".TransactionDetails.TotalReimbursmentAmount'))
		END AS RequestedTotalReimbursementAmount,

		--ra.ApprovedAmount
		CASE WHEN (ISJSON(allcs.CaseTicketData) = 1 and allcs.CaseTopicID=24 AND ra.ApprovedTotalAmount > 0)
				THEN ra.ApprovedTotalAmount
             ELSE '0.00'
		END AS ApprovedTotalReimbursementAmount,

		--ra.AvailableAmount,

		allcs.CaseStatus,
		allcs.ApprovedStatus,

		-- PROCESSED DATE
		allcs.ClosedDate,
		--CONVERT(VARCHAR(MAX), CONVERT(DATETIME, allcs.ClosedDate AT TIME ZONE 'UTC' AT TIME ZONE 'Eastern Standard Time'), 101) AS ProcessedDate,

		allcs.ClosingComments
INTO		#Final
FROM		#AllCSCases allcs
JOIN		#MemberInsuranceMax mix ON mix.MemberID = allcs.MemberID
JOIN		master.MemberInsurances mi ON allcs.MemberID = mi.MemberID AND mix.CreateDate = mi.CreateDate
JOIN		master.MemberInsuranceDetails mid ON mi.ID = mid.MemberInsuranceID
LEFT JOIN	ServiceRequest.ReimbursementItems ri ON allcs.CaseTicketId = ri.CaseTicketID AND ri.IsProcessEligible = 1
LEFT JOIN	#ReimbursementAmount ra ON allcs.CaseTicketID = ra.CaseTicketID

-- Final query
--SELECT
--	fin.InsuranceCarrierName AS [Insurance Carrier],
--	fin.HealthPlanName AS [Health Plan],
--	fin.CaseTicketNumber AS [Case Ticket Number],
--	fin.CaseCategory AS [Case Category],
--	fin.CreateDate AS [Create Date],
--	fin.TransactionDate AS [Transaction Date],
--	fin.FirstName AS [Member First Name],
--	fin.LastName AS [Member Last Name],
--	fin.DateOfBirth AS [Date Of Birth],
--	fin.City AS City,
--	fin.State AS State,
--	fin.NHMemberID AS [NH/EH Member ID],
--	fin.InsuranceNbr AS [Insurance Number],
--	fin.CaseTopic AS [Case Topic],
--	fin.CaseType AS [Case Type],
--	fin.CaseTicketData AS [Case Ticket Data],
--	fin.WalletValue AS Wallet,
--	fin.DenialReason AS [Denial Reason],
--	fin.RequestedTotalReimbursementAmount AS [Requested Total Reimbursement Amount],
--	fin.ApprovedTotalReimbursementAmount AS [Approved Total Reimbursement Amount],
--	--fin.AvailableAmount,
--	fin.CaseStatus AS [Case Status],
--	fin.ApprovedStatus AS [Approved Status],
--	fin.ClosedDate AS [Processed Date],
--	fin.ClosingComments AS [Closing Comments]
--FROM #Final fin
--WHERE 1=1
--AND fin.CaseTicketNumber = 'EHCM202400063372-1'
--AND fin.ClosingComments like '%- Reimbursement Processed%.'
--WHERE fin.CaseTicketNumber = 'EHCM202400062736-1'
--AND fin.[CaseStatus] in ('Pending Processing', 'In Review', 'Failed')
--AND fin.CaseStatus = 'In Review'
--AND fin.CaseTopic = 'Reimbursement'
--AND YEAR(fin.CreateDate) = 2024
--AND ISDATE(fin.TransactionDate) != 1
--AND (fin.TransactionDate is null OR fin.TransactionDate like '%Invalid%')
--AND fin.CaseTicketNumber = 'NBCM202400079733-1'
--AND fin.CaseTicketNumber = 'NBCM202400078578-1'
--ORDER BY fin.CreateDate

SELECT
	fin.InsuranceCarrierName,
	fin.HealthPlanName,
	fin.CaseTicketNumber,
	fin.CaseCategory,
	fin.CreateDate,
	fin.TransactionDate,
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
	fin.RequestedTotalReimbursementAmount,
	fin.ApprovedTotalReimbursementAmount,
	fin.CaseStatus,
	fin.ApprovedStatus,
	fin.ClosedDate AS ProcessedDate,
	fin.ClosingComments
FROM #Final fin
WHERE 1=1
ORDER BY
	fin.CreateDate
--GO
";
    }
}
