namespace CardServicesProcessor.Shared
{
    public class SQLConstants
    {
        public static string CardServicesQuery => @"-- 1) get all active CS cases
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
		--WHERE 1=1
		ORDER BY fin.CreateDate";

        public static string CheckIssuanceQuery => @"DROP TABLE IF EXISTS #reimbursement_payments
			SELECT
			mc.NHMemberID,
			mc.CaseNumber,
			mct.CaseticketData,
			mct.CaseTicketNumber,
			mc.CaseStatusID,
			mct.CaseTicketStatusID,
			mct.TransactionStatus,
			mct.ApprovedStatus,
			bm.TxnID,
			bm.TxnGroupReferenceID,
			bm.TxnReferenceID,
			JSON_VALUE(bm.ClientResData, '$.PurseSlot') AS PurseSlot,
			JSON_VALUE(bm.ClientResData, '$.TxnAmount') AS 'Amount deducted from Member',
			CONVERT(VARCHAR, bm.TxnResponseDate, 110) AS 'Request Date'
			INTO #reimbursement_payments
			FROM ServiceRequest.MemberCaseTickets mct
			INNER JOIN ServiceRequest.MemberCases mc ON mct.CaseID=mc.CaseID
			INNER JOIN PaymentGateway.BenefitManagement bm WITH(NOLOCK) ON bm.NHMemberID=mc.NHMemberID AND bm.TxnGroupReferenceID=mc.CaseNumber
			WHERE 1=1
			AND CaseTopicID=24
			AND CaseTicketStatusID IN (3,9)
			AND ApprovedStatus = @ApprovedStatus
			AND cast(bm.TxnResponseDate as date) >= @FromDate
			ORDER BY mc.NHMemberID

			--SELECT * FROM #reimbursement_payments

			/*RAW DATA TAB*/

			-- Address from the Reimbursement page on the OTC portal
			DROP TABLE IF EXISTS #reimbursement_address_1
			SELECT DISTINCT
			c.NHMemberID, 
			ic.InsuranceCarrierName,
			c.CardReferenceNumber,
			rp.TxnID,
			rp.TxnReferenceID,
			rp.TxnGroupReferenceID,
			rp.PurseSlot,
			rp.[Request Date],
			rp.[Amount deducted from Member],
			m.CardholderFirstName,
			m.CardholderLastName,
			CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS 'Company Name',
			CASE WHEN m.cardholderlastname IS NOT NULL THEN CONCAT('Member - ', c.NHMemberID) ELSE 'Member - ' END AS 'Vendor Name',
			CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS 'Print on Check',
			CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS 'Address 1',
			CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address1""'),'',JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address2""')) AS 'Address 2',
			CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""city""'), ',', ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""state""'), ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""zipCode""')) AS 'Address 3',
			'' AS 'type'
			INTO #reimbursement_address_1
			FROM #reimbursement_payments rp 
			LEFT JOIN member.membercards c ON c.NHMemberID=rp.nhmemberid and c.CardIssuer='BANCORP'
			LEFT JOIN fisxtract.NonMonetary m ON m.PANProxyNumber=c.CardReferenceNumber
			LEFT JOIN master.Members mm ON c.NHMemberID=mm.NHMemberID
			LEFT JOIN master.MemberInsurances mi ON mi.MemberID=mm.MemberID
			LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID=mi.InsuranceCarrierID
			LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID=mi.InsuranceHealthPlanID
			INNER JOIN master.Addresses a ON a.MemberID=mm.MemberID
			WHERE
			ih.IsActive=1
			AND ic.IsActive=1
			AND c.IsActive=1
			AND ic.InsuranceCarrierID NOT IN (302, 270) 
			AND ih.HealthPlanNumber IS NOT NULL
			ORDER BY [Vendor Name]

			--SELECT * FROM #reimbursement_address_1 ORDER BY [Request Date] DESC

			--- Mailing Address from the Members table
			DROP TABLE IF EXISTS #reimbursement_address_2
			SELECT DISTINCT
			c.NHMemberID, 
			ic.InsuranceCarrierName,
			c.CardReferenceNumber,
			rp.TxnID,
			rp.TxnReferenceID,
			rp.TxnGroupReferenceID,
			rp.PurseSlot,
			rp.[Request Date],
			rp.[Amount deducted from Member],
			mm.FirstName,
			mm.LastName,
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Company Name',
			CONCAT('Member - ', mm.NHMemberID) AS 'Vendor Name',
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Print on Check',
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Address 1',
			a.Address1 AS 'Address 2',
			CONCAT(a.city, ',', ' ', a.State, ' ', LEFT(a.ZipCode, 5)) AS 'Address 3',
			a.AddressTypeCode
			INTO #reimbursement_address_2
			FROM 
			member.membercards c 
			LEFT JOIN master.Members mm ON c.NHMemberID=mm.NHMemberID
			LEFT JOIN master.MemberInsurances mi ON mi.MemberID=mm.MemberID
			LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID=mi.InsuranceCarrierID
			LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID=mi.InsuranceHealthPlanID
			INNER JOIN master.Addresses a ON a.MemberID=mm.MemberID
			INNER JOIN #reimbursement_payments rp ON c.NHMemberID=rp.nhmemberid
			WHERE
			ih.IsActive=1
			AND ic.IsActive=1
			AND c.IsActive=1
			AND ih.HealthPlanNumber IS NOT NULL
			AND ic.InsuranceCarrierID NOT IN (302, 270) 
			AND a.AddressTypeCode='MAIL'
			ORDER BY [Vendor Name]

			--SELECT * FROM #reimbursement_address_2 ra1	WHERE nhmemberid='EH202314892004'

			-- Perm Address from the Members table
			DROP TABLE IF EXISTS #reimbursement_address_3
			SELECT DISTINCT
			c.NHMemberID, 
			ic.InsuranceCarrierName,
			c.CardReferenceNumber,
			rp.TxnID,
			rp.TxnReferenceID,
			rp.TxnGroupReferenceID,
			rp.PurseSlot,
			rp.[Request Date],
			rp.[Amount deducted from Member],
			mm.FirstName AS CardholderFirstName,
			mm.LastName AS CardholderLastName,
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Company Name',
			CONCAT('Member - ', mm.NHMemberID) AS 'Vendor Name',
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Print on Check',
			CONCAT(mm.FirstName, ' ', mm.LastName) AS 'Address 1',
			a.Address1 AS 'Address 2',
			CONCAT(a.city, ',', ' ', a.State, ' ', LEFT(a.ZipCode, 5)) AS 'Address 3',
			'' AS 'type'
			INTO #reimbursement_address_3
			FROM #reimbursement_payments rp 
			LEFT JOIN member.membercards c ON c.NHMemberID=rp.nhmemberid 
			Left join fisxtract.NonMonetary m ON m.PANProxyNumber=c.CardReferenceNumber
			LEFT JOIN master.Members mm ON c.NHMemberID=mm.NHMemberID
			LEFT JOIN master.MemberInsurances mi ON mi.MemberID=mm.MemberID
			LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID=mi.InsuranceCarrierID
			LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID=mi.InsuranceHealthPlanID
			INNER JOIN master.Addresses a ON a.MemberID=mm.MemberID
			WHERE
			ih.IsActive=1
			AND ic.IsActive=1
			AND c.IsActive=1
			AND ih.HealthPlanNumber IS NOT NULL
			AND c.IsActive=1
			AND a.AddressTypeCode='PERM'
			ORDER BY NHMemberID

			--SELECT * FROM #reimbursement_address_3 WHERE nhmemberid='EH202314892004'

			DROP TABLE IF EXISTS #temp_final
			SELECT ra1.NHMemberID,
			ra1.InsuranceCarrierName,
			ra1.CardReferenceNumber,
			ra1.TxnID,
			ra1.TxnReferenceID,
			ra1.TxnGroupReferenceID,
			ra1.PurseSlot,
			ra1.[Request Date],
			ra1.[Amount deducted from Member],
			CASE WHEN (ra1.CardholderFirstName IS NULL OR ra1.CardholderFirstName= '') AND (ra2.FirstName IS NULL OR ra2.FirstName ='')
					THEN ra3.CardholderFirstName
				 WHEN (ra1.CardholderFirstName IS NULL OR ra1.CardholderFirstName= '')
					THEN ra2.FirstName ELSE ra1.CardholderFirstName
			END AS CardholderFirstName,
			CASE WHEN (ra1.CardHolderLastName IS NULL OR ra1.CardholderLastName ='') AND (ra2.LastName IS NULL OR ra2.LastName='')
					THEN ra3.CardholderLastName
				 WHEN (ra1.CardHolderLastName IS NULL OR ra1.CardholderLastName='')
					THEN ra2.LastName
					ELSE ra1.CardholderLastName
			END AS CardholderLastName,
			CASE WHEN (ra1.[Company Name] IS NULL OR ra1.[Company Name]='') AND (ra2.[Company Name] IS NULL OR ra2.[Company Name]='')
					THEN ra3.[Company Name]
				 WHEN (ra1.[Company Name] IS NULL OR ra1.[Company Name]='')
					THEN ra2.[Company Name]
					ELSE ra1.[Company Name]
			END AS [Company Name],
			CASE WHEN (ra1.[Vendor Name]='Member - ' OR ra1.[Vendor Name] IS NULL) AND (ra2.[Vendor Name]='Member - ' OR ra2.[Vendor Name] IS NULL)
					THEN ra3.[Vendor Name]
				 WHEN (ra1.[Vendor Name]='Member - ' OR ra1.[Vendor Name] IS NULL)
					THEN ra2.[Vendor Name]
					ELSE ra1.[Vendor Name]
			END AS [Vendor Name],
			CASE WHEN (ra1.[Print on Check]='' OR ra1.[Print on Check] IS NULL) AND (ra2.[Print on Check]='' OR ra2.[Print on Check] IS NULL)
					THEN ra3.[Print on Check]
				 WHEN (ra1.[Print on Check]='' OR ra1.[Print on Check] IS NULL)
					THEN ra2.[Print on Check]
					ELSE ra1.[Print on Check]
			END AS [Print on Check],
			CASE WHEN ra1.type = '' AND ra1.[Address 1] <>''
					THEN ra1.[Address 1]
				 WHEN ra2.AddressTypeCode='Mail'
					THEN ra2.[Address 1]
					ELSE ra3.[Address 1]
			END AS [Address 1],
			CASE WHEN ra1.type = '' AND ra1.[Address 2] <>''
					THEN ra1.[Address 2]
				 WHEN ra2.AddressTypeCode='Mail'
					THEN ra2.[Address 2]
					ELSE ra3.[Address 2]
			END AS 'Address 2',
			CASE WHEN ra1.type = '' AND ra1.[Address 2] <>''
					THEN ra1.[Address 3]
				 WHEN ra2.AddressTypeCode='Mail'
					THEN ra2.[Address 3]
					ELSE ra3.[Address 3]
			END AS 'Address 3',
			CASE WHEN ra1.type = ''
					THEN ''
					ELSE ra2.AddressTypeCode
			END AS AddressTypeCode
			INTO #temp_final
			FROM #reimbursement_address_1 ra1
			LEFT JOIN #reimbursement_address_2 ra2 ON ra1.txnreferenceid=ra2.txnreferenceid 
			LEFT JOIN #reimbursement_address_3 ra3 ON ra1.txnreferenceid=ra3.txnreferenceid 

			--SELECT * FROM #temp_final WHERE nhmemberid='EH202314888786'

			DROP TABLE IF EXISTS #reimbursement_final
			SELECT 
				t.NHMemberID, 
				t.InsuranceCarrierName,
				t.CardReferenceNumber,
				t.txnid,
				t.TxnReferenceID,
				t.purseslot,
				[Amount deducted from Member],
				[Request Date],
				t.CardholderFirstName,
				t.CardholderLastName,
				t.[Company Name],
				t.[Vendor Name],
				t.[Print on Check],
				t.[Address 1],
				t.[Address 2] AS 'Address 2',
				t.[Address 3] AS 'Address 3'
			INTO #reimbursement_final
			FROM 
				#temp_final t
			GROUP BY 
				t.NHMemberID, 
				t.InsuranceCarrierName,
				t.CardReferenceNumber,
				t.txnid,
				t.TxnReferenceID,
				t.purseslot,
				[Amount deducted from Member],
				[Request Date],
				t.CardholderFirstName,
				t.CardholderLastName,
				t.[Company Name],
				t.[Vendor Name],
				t.[Print on Check],
				t.[Address 1],
				t.[Address 2],
				t.[Address 3] 
			ORDER BY 
				NHMemberID

			SELECT * FROM #reimbursement_final

			/**Member Mailing Info Tab**/ --#reimbursement_payments_final_table
			SELECT 
			CardholderFirstName,
			CardholderLastName,
			[Company Name],
			[Vendor Name],
			[Print on Check],
			[Address 1],
			[Address 2],
			[Address 3],
			'Due on Receipt' AS Terms,
			SUM(CAST([Amount deducted from Member] AS DECIMAL(7,2))) AS r
			FROM #reimbursement_final
			GROUP BY
			[Company Name],
			CardholderFirstName,
			CardholderLastName,
			[Vendor Name],
			[Print on Check],
			[Address 1],
			[Address 2],
			[Address 3]
			ORDER BY CardholderFirstName

			/*Member Check Reimbursement*/
			SELECT
			TxnID,
			[Vendor Name],
			TxnID AS 'Memo',
			[Request Date],
			SUM(CAST([Amount deducted from Member] AS DECIMAL(7,2))) AS r
			FROM #reimbursement_final
			GROUP BY
			nhmemberid,
			[Vendor Name],
			[Request Date],
			TxnID
			ORDER BY [Request Date]";
    }
}