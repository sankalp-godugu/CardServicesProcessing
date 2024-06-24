namespace CardServicesProcessor.Shared
{
    public static class SqlConstantsCheckIssuance
    {
        public static readonly string Query = @"";
        public static readonly string DropReimbursementPayments = @"DROP TABLE IF EXISTS #ReimbursementPayments;";
        public static readonly string DropReimbursementAddress1 = @"DROP TABLE IF EXISTS #ReimbursementAddress1;";
        public static readonly string DropReimbursementAddress2 = @"DROP TABLE IF EXISTS #ReimbursementAddress2;";
        public static readonly string DropReimbursementAddress3 = @"DROP TABLE IF EXISTS #ReimbursementAddress3;";
        public static readonly string DropTempFinal = @"DROP TABLE IF EXISTS #TempFinal;";
        public static readonly string DropReimbursementFinal = @"DROP TABLE IF EXISTS #ReimbursementFinal;";
        public static readonly string DropMemberMailingInfo = @"DROP TABLE IF EXISTS #MemberMailingInfo;";
        public static readonly string DropMemberCheckReimbursement = @"DROP TABLE IF EXISTS #MemberCheckReimbursement;";
        public static readonly string SelectIntoReimbursementPayments = @"
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
	            JSON_VALUE(bm.ClientResData, '$.TxnAmount') AS AmountDeductedFromMember,
	            CONVERT(VARCHAR, bm.TxnResponseDate AT TIME ZONE 'Eastern Standard Time', 110) AS RequestDate
	        INTO	#ReimbursementPayments
	        FROM	ServiceRequest.MemberCaseTickets mct
	        JOIN	ServiceRequest.MemberCases mc ON mct.CaseID=mc.CaseID
	        JOIN	PaymentGateway.BenefitManagement bm WITH(NOLOCK)
						ON bm.NHMemberID=mc.NHMemberID
						AND bm.TxnGroupReferenceID = mc.CaseNumber
	        WHERE	CaseTopicID = @caseTopicId
	        AND		CaseTicketStatusID IN (9,3)
	        AND		ApprovedStatus = @approvedStatus
			AND		TransactionStatus = @transactionStatus
	        AND		bm.TxnResponseDate AT TIME ZONE 'Eastern Standard Time' > @fromDate;
			--AND	mct.CaseNumber NOT IN ('')
			--AND	IsCheckSent = 0;";
        public static readonly string SelectIntoReimbursementAddress1_NAT = @"
            SELECT DISTINCT
	c.NHMemberID, 
	ic.InsuranceCarrierName,
	c.CardReferenceNumber,
	rp.TxnID,
	rp.TxnReferenceID,
	rp.TxnGroupReferenceID,
	rp.PurseSlot,
	rp.RequestDate,
	rp.AmountDeductedFromMember,
	m.CardholderFirstName,
	m.CardholderLastName,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS CompanyName,
	CASE WHEN m.cardholderlastname IS NOT NULL THEN CONCAT('Member - ', c.NHMemberID) ELSE 'Member - ' END AS VendorName,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS PrintOnCheck,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS Address1,
	CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address1""'),'',JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address2""')) AS Address2,
	CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""city""'), ',', ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""state""'), ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""zipCode""')) AS Address3,
	'' AS type
INTO #ReimbursementAddress1
FROM #ReimbursementPayments rp 
LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid and c.CardIssuer = 'BANCORP'
LEFT JOIN fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
WHERE ih.IsActive = 1
	AND ic.IsActive = 1
	AND c.IsActive = 1
	AND ic.InsuranceCarrierID NOT IN (141, 270, 302)
	AND ih.HealthPlanNumber IS NOT NULL;";
        public static readonly string SelectIntoReimbursementAddress1_ELV = @"
            SELECT DISTINCT
	c.NHMemberID, 
	ic.InsuranceCarrierName,
	c.CardReferenceNumber,
	rp.TxnID,
	rp.TxnReferenceID,
	rp.TxnGroupReferenceID,
	rp.PurseSlot,
	rp.RequestDate,
	rp.AmountDeductedFromMember,
	m.CardholderFirstName,
	m.CardholderLastName,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS CompanyName,
	CASE WHEN m.cardholderlastname IS NOT NULL THEN CONCAT('Member - ', c.NHMemberID) ELSE 'Member - ' END AS VendorName,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS PrintOnCheck,
	CONCAT(m.CardholderFirstName, ' ', m.CardholderLastName) AS Address1,
	CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address1""'),'',JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""address2""')) AS Address2,
	CONCAT(JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""city""'), ',', ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""state""'), ' ', JSON_VALUE(JSON_VALUE(CaseticketData, '$.""New Reimbursement Request"".TransactionDetails.Address'),'$.""zipCode""')) AS Address3,
	'' AS type
INTO #ReimbursementAddress1
FROM #ReimbursementPayments rp 
LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid
LEFT JOIN fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
WHERE ih.IsActive = 1
	AND ic.IsActive = 1
	AND c.IsActive = 1
	AND ic.InsuranceCarrierID NOT IN (141, 270, 302)
	AND ih.HealthPlanNumber IS NOT NULL;";
        public static readonly string SelectIntoReimbursementAddress2 = @"
SELECT DISTINCT
	c.NHMemberID, 
	ic.InsuranceCarrierName,
	c.CardReferenceNumber,
	rp.TxnID,
	rp.TxnReferenceID,
	rp.TxnGroupReferenceID,
	rp.PurseSlot,
	rp.RequestDate,
	rp.AmountDeductedFromMember,
	mm.FirstName,
	mm.LastName,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS CompanyName,
	CONCAT('Member - ', mm.NHMemberID) AS VendorName,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS PrintOnCheck,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS Address1,
	a.Address1 AS Address2,
	CONCAT(a.city, ',', ' ', a.State, ' ', LEFT(a.ZipCode, 5)) AS Address3,
	a.AddressTypeCode
INTO #ReimbursementAddress2
FROM member.membercards c 
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
JOIN #ReimbursementPayments rp ON c.NHMemberID = rp.nhmemberid
WHERE
	ih.IsActive = 1
	AND ic.IsActive = 1
	AND c.IsActive = 1
	AND ih.HealthPlanNumber IS NOT NULL
	AND ic.InsuranceCarrierID NOT IN (141, 270, 302) 
	AND a.AddressTypeCode = 'MAIL';";
        public static readonly string SelectIntoReimbursementAddress3 = @"
SELECT DISTINCT
	c.NHMemberID, 
	ic.InsuranceCarrierName,
	c.CardReferenceNumber,
	rp.TxnID,
	rp.TxnReferenceID,
	rp.TxnGroupReferenceID,
	rp.PurseSlot,
	rp.RequestDate,
	rp.AmountDeductedFromMember,
	mm.FirstName AS CardholderFirstName,
	mm.LastName AS CardholderLastName,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS CompanyName,
	CONCAT('Member - ', mm.NHMemberID) AS VendorName,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS PrintOnCheck,
	CONCAT(mm.FirstName, ' ', mm.LastName) AS Address1,
	a.Address1 AS Address2,
	CONCAT(a.city, ',', ' ', a.State, ' ', LEFT(a.ZipCode, 5)) AS Address3,
	'' AS type
INTO #ReimbursementAddress3
FROM #ReimbursementPayments rp 
LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid 
Left join fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID=mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
WHERE ih.IsActive = 1
	AND ic.IsActive = 1
	AND c.IsActive = 1
	AND ih.HealthPlanNumber IS NOT NULL
	AND c.IsActive = 1
	AND a.AddressTypeCode = 'PERM';";
        public static readonly string SelectIntoTempFinal = @"
SELECT
	ra1.NHMemberID,
	ra1.InsuranceCarrierName,
	ra1.CardReferenceNumber,
	ra1.TxnID,
	ra1.TxnReferenceID,
	ra1.TxnGroupReferenceID,
	ra1.PurseSlot,
	ra1.RequestDate,
	ra1.AmountDeductedFromMember,
	COALESCE(NULLIF(ra1.CardholderFirstName, ''), ra2.FirstName, ra3.CardholderFirstName) AS CardholderFirstName,
	COALESCE(NULLIF(ra1.CardholderLastName, ''), ra2.LastName, ra3.CardholderLastName) AS CardholderLastName,
	COALESCE(NULLIF(ra1.CompanyName, ''), ra2.CompanyName, ra3.CompanyName) AS CompanyName,
	COALESCE(NULLIF(ra1.VendorName, 'Member -'), ra2.VendorName, ra3.VendorName) AS VendorName,
	COALESCE(NULLIF(ra1.PrintOnCheck, ''), ra2.PrintOnCheck, ra3.PrintOnCheck) AS PrintOnCheck,
	CASE WHEN ra1.type = '' AND ra1.Address1 != '' THEN ra1.Address1
		 WHEN ra2.AddressTypeCode = 'Mail' THEN ra2.Address1
		 ELSE ra3.Address1
	END AS Address1,
	CASE WHEN ra1.type = '' AND ra1.Address2 != '' THEN ra1.Address2
		 WHEN ra2.AddressTypeCode = 'Mail' THEN ra2.Address2
		 ELSE ra3.Address2
	END AS Address2,
	CASE WHEN ra1.type = '' AND ra1.Address2 != '' THEN ra1.Address3
		 WHEN ra2.AddressTypeCode = 'Mail' THEN ra2.Address3
		 ELSE ra3.Address3
	END AS Address3,
	CASE WHEN ra1.type = '' THEN ''
		 ELSE ra2.AddressTypeCode
	END AS AddressTypeCode
INTO #TempFinal
FROM #ReimbursementAddress1 ra1
LEFT JOIN #ReimbursementAddress2 ra2 ON ra1.txnreferenceid=ra2.txnreferenceid
LEFT JOIN #ReimbursementAddress3 ra3 ON ra1.txnreferenceid=ra3.txnreferenceid;";
        public static readonly string SelectIntoReimbursementFinal = @" 
SELECT 
	t.NHMemberID, 
	t.InsuranceCarrierName,
	t.CardReferenceNumber,
	t.txnid,
	t.TxnReferenceID,
	t.purseslot,
	t.AmountDeductedFromMember,
	t.RequestDate,
	t.CardholderFirstName,
	t.CardholderLastName,
	t.CompanyName,
	t.VendorName,
	t.PrintOnCheck,
	t.Address1,
	t.Address2,
	t.Address3
INTO #ReimbursementFinal
FROM #TempFinal t
GROUP BY 
	t.NHMemberID, 
	t.InsuranceCarrierName,
	t.CardReferenceNumber,
	t.txnid,
	t.TxnReferenceID,
	t.purseslot,
	t.AmountDeductedFromMember,
	t.RequestDate,
	t.CardholderFirstName,
	t.CardholderLastName,
	t.CompanyName,
	t.VendorName,
	t.PrintOnCheck,
	t.Address1,
	t.Address2,
	t.Address3;";
        public static readonly string SelectIntoMemberMailingInfo = @" 
SELECT 
	CardholderFirstName,
	CardholderLastName,
	CompanyName,
	VendorName,
	PrintOnCheck,
	Address1,
	Address2,
	Address3,
	--'Due on Receipt' AS Terms,
	SUM(CAST(AmountDeductedFromMember AS DECIMAL(7,2))) AS TotalAmountDeductedFromMember
INTO #MemberMailingInfo
FROM #ReimbursementFinal
GROUP BY
	CompanyName,
	CardholderFirstName,
	CardholderLastName,
	VendorName,
	PrintOnCheck,
	Address1,
	Address2,
	Address3;";
        public static readonly string SelectIntoMemberCheckReimbursement = @"
SELECT
	TxnID AS CaseNumber,
	VendorName,
	TxnID AS 'Memo',
	RequestDate,
	SUM(CAST(AmountDeductedFromMember AS DECIMAL(7,2))) AS TotalAmountDeductedFromMember
INTO #MemberCheckReimbursement
FROM #ReimbursementFinal
GROUP BY
	nhmemberid,
	VendorName,
	RequestDate,
	TxnID;";
        public static readonly string SelectRawData = @"SELECT * FROM #ReimbursementFinal ORDER BY NHMemberID;";
        public static readonly string SelectMemberMailingInfo = @"SELECT * FROM #MemberMailingInfo ORDER BY CardholderFirstName;";
        public static readonly string SelectMemberCheckReimbursement = @"SELECT * FROM #MemberCheckReimbursement ORDER BY VendorName;";
    }
}
