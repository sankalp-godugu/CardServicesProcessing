﻿namespace CardServicesProcessor.Shared
{
    public static class SqlConstantsCheckIssuance
    {
        public static readonly string Query = @"";

        public static readonly string DropReimbursementPayments = @"DROP TABLE IF EXISTS #reimbursement_payments";
        public static readonly string DropReimbursementAddress1 = @"DROP TABLE IF EXISTS #reimbursement_address_1";
        public static readonly string DropReimbursementAddress2 = @"DROP TABLE IF EXISTS #reimbursement_address_2";
        public static readonly string DropReimbursementAddress3 = @"DROP TABLE IF EXISTS #reimbursement_address_3";
        public static readonly string DropTempFinal = @"DROP TABLE IF EXISTS #temp_final";
        public static readonly string DropReimbursementFinal = @"DROP TABLE IF EXISTS #reimbursement_final";

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
            CONVERT(VARCHAR, bm.TxnResponseDate, 110) AS RequestDate
            INTO #reimbursement_payments
            FROM ServiceRequest.MemberCaseTickets mct
            JOIN ServiceRequest.MemberCases mc ON mct.CaseID = mc.CaseID
            JOIN PaymentGateway.BenefitManagement bm WITH(NOLOCK) ON bm.NHMemberID = mc.NHMemberID AND bm.TxnGroupReferenceID = mc.CaseNumber
            WHERE CaseTopicID = 24
            AND CaseTicketStatusID IN (3,9)
            AND ApprovedStatus = 'Approved'
            AND cast(bm.TxnResponseDate as date) > DATEADD(day, -7, CONVERT(date, GETDATE()))
            ORDER BY mc.NHMemberID";
        public static readonly string SelectIntoReimbursementAddress1_NAT = @$"
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
            INTO #reimbursement_address_1
            FROM #reimbursement_payments rp 
            LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid and c.CardIssuer = 'BANCORP'
            LEFT JOIN fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
            LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
            LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
            LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
            LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
            INNER JOIN master.Addresses a ON a.MemberID = mm.MemberID
            WHERE ih.IsActive = 1
            AND ic.IsActive = 1
            AND c.IsActive = 1
            AND ic.InsuranceCarrierID NOT IN (302, 270) 
            AND ih.HealthPlanNumber IS NOT NULL
            ORDER BY VendorName";
        public static readonly string SelectIntoReimbursementAddress1_ELV = @$"
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
            INTO #reimbursement_address_1
            FROM #reimbursement_payments rp 
            LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid
            LEFT JOIN fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
            LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
            LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
            LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
            LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
            JOIN master.Addresses a ON a.MemberID = mm.MemberID
            WHERE
            ih.IsActive = 1
            AND ic.IsActive = 1
            AND c.IsActive = 1
            AND ic.InsuranceCarrierID NOT IN (302, 270) 
            AND ih.HealthPlanNumber IS NOT NULL
            ORDER BY VendorName";
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
INTO #reimbursement_address_2
FROM member.membercards c 
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
JOIN #reimbursement_payments rp ON c.NHMemberID = rp.nhmemberid
WHERE
ih.IsActive = 1
AND ic.IsActive = 1
AND c.IsActive = 1
AND ih.HealthPlanNumber IS NOT NULL
AND ic.InsuranceCarrierID NOT IN (302, 270) 
AND a.AddressTypeCode = 'MAIL'
ORDER BY VendorName";
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
INTO #reimbursement_address_3
FROM #reimbursement_payments rp 
LEFT JOIN member.membercards c ON c.NHMemberID = rp.nhmemberid 
Left join fisxtract.NonMonetary m ON m.PANProxyNumber = c.CardReferenceNumber
LEFT JOIN master.Members mm ON c.NHMemberID = mm.NHMemberID
LEFT JOIN master.MemberInsurances mi ON mi.MemberID = mm.MemberID
LEFT JOIN Insurance.InsuranceCarriers ic ON ic.InsuranceCarrierID = mi.InsuranceCarrierID
LEFT JOIN Insurance.InsuranceHealthPlans ih ON ih.InsuranceHealthPlanID = mi.InsuranceHealthPlanID
JOIN master.Addresses a ON a.MemberID = mm.MemberID
WHERE ih.IsActive = 1
AND ic.IsActive = 1
AND c.IsActive = 1
AND ih.HealthPlanNumber IS NOT NULL
AND c.IsActive = 1
AND a.AddressTypeCode = 'PERM'
ORDER BY NHMemberID";
        public static readonly string SelectIntoTempFinal = @"
SELECT ra1.NHMemberID,
ra1.InsuranceCarrierName,
ra1.CardReferenceNumber,
ra1.TxnID,
ra1.TxnReferenceID,
ra1.TxnGroupReferenceID,
ra1.PurseSlot,
ra1.RequestDate,
ra1.AmountDeductedFromMember,
CASE WHEN (ra1.CardholderFirstName IS NULL OR ra1.CardholderFirstName = '') AND (ra2.FirstName IS NULL OR ra2.FirstName = '')
		THEN ra3.CardholderFirstName
	 WHEN (ra1.CardholderFirstName IS NULL OR ra1.CardholderFirstName = '')
		THEN ra2.FirstName ELSE ra1.CardholderFirstName
END AS CardholderFirstName,
CASE WHEN (ra1.CardHolderLastName IS NULL OR ra1.CardholderLastName = '') AND (ra2.LastName IS NULL OR ra2.LastName = '')
		THEN ra3.CardholderLastName
	 WHEN (ra1.CardHolderLastName IS NULL OR ra1.CardholderLastName = '')
		THEN ra2.LastName
		ELSE ra1.CardholderLastName
END AS CardholderLastName,
CASE WHEN (ra1.CompanyName IS NULL OR ra1.CompanyName= '') AND (ra2.CompanyName IS NULL OR ra2.CompanyName = '')
		THEN ra3.CompanyName
	 WHEN (ra1.CompanyName IS NULL OR ra1.CompanyName = '')
		THEN ra2.CompanyName
		ELSE ra1.CompanyName
END AS CompanyName,
CASE WHEN (ra1.VendorName = 'Member - ' OR ra1.VendorName IS NULL) AND (ra2.VendorName = 'Member - ' OR ra2.VendorName IS NULL)
		THEN ra3.VendorName
	 WHEN (ra1.VendorName = 'Member - ' OR ra1.VendorName IS NULL)
		THEN ra2.VendorName
		ELSE ra1.VendorName
END AS VendorName,
CASE WHEN (ra1.PrintOnCheck = '' OR ra1.PrintOnCheck IS NULL) AND (ra2.PrintOnCheck = '' OR ra2.PrintOnCheck IS NULL)
		THEN ra3.PrintOnCheck
	 WHEN (ra1.PrintOnCheck = '' OR ra1.PrintOnCheck IS NULL)
		THEN ra2.PrintOnCheck
		ELSE ra1.PrintOnCheck
END AS PrintOnCheck,
CASE WHEN ra1.type = '' AND ra1.Address1 != ''
		THEN ra1.Address1
	 WHEN ra2.AddressTypeCode = 'Mail'
		THEN ra2.Address1
		ELSE ra3.Address1
END AS Address1,
CASE WHEN ra1.type = '' AND ra1.Address2 != ''
		THEN ra1.Address2
	 WHEN ra2.AddressTypeCode = 'Mail'
		THEN ra2.Address2
		ELSE ra3.Address2
END AS Address2,
CASE WHEN ra1.type = '' AND ra1.Address2 != ''
		THEN ra1.Address3
	 WHEN ra2.AddressTypeCode = 'Mail'
		THEN ra2.Address3
		ELSE ra3.Address3
END AS Address3,
CASE WHEN ra1.type = ''
		THEN ''
		ELSE ra2.AddressTypeCode
END AS AddressTypeCode
INTO #temp_final
FROM #reimbursement_address_1 ra1
LEFT JOIN #reimbursement_address_2 ra2 ON ra1.txnreferenceid = ra2.txnreferenceid
LEFT JOIN #reimbursement_address_3 ra3 ON ra1.txnreferenceid = ra3.txnreferenceid";
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
INTO #reimbursement_final
FROM #temp_final t
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
    t.Address3 
ORDER BY 
    NHMemberID";
        public static readonly string SelectRawData = @"SELECT * FROM #reimbursement_final";
        public static readonly string SelectMemberMailingInfo = @" 
SELECT
CardholderFirstName,
CardholderLastName,
CompanyName,
VendorName,
PrintOnCheck,
Address1,
Address2,
Address3,
SUM(CAST(AmountDeductedFromMember AS DECIMAL(7,2))) AS TotalAmountDeductedFromMember
FROM #reimbursement_final
GROUP BY
CompanyName,
CardholderFirstName,
CardholderLastName,
VendorName,
PrintOnCheck,
Address1,
Address2,
Address3
ORDER BY CardholderFirstName";
        public static readonly string SelectMemberCheckReimbursement = @"
SELECT
TxnID AS CaseNumber,
VendorName,
TxnID AS Memo,
RequestDate,
SUM(CAST(AmountDeductedFromMember AS DECIMAL(7,2))) AS TotalAmountDeductedFromMember
FROM #reimbursement_final
GROUP BY
nhmemberid,
VendorName,
RequestDate,
TxnID
ORDER BY VendorName";
    }
}
