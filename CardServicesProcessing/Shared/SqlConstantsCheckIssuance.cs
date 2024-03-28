namespace CardServicesProcessor.Shared
{
    public static class SqlConstantsCheckIssuance
    {
		public static readonly string DropReimbursementPayments = @"DROP TABLE IF EXISTS #reimbursement_payments";

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
ORDER BY mc.NHMemberID";

		public static readonly string DropReimbursementAddress1 = @"DROP TABLE IF EXISTS #reimbursement_address_1";

		public static readonly string SelectIntoReimbursementAddress1 = @"SELECT DISTINCT
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
ORDER BY [Vendor Name]";

		public static readonly string DropReimbursementAddress2 = @"DROP TABLE IF EXISTS #reimbursement_address_2";

		public static readonly string SelectIntoReimbursementAddress2 = @"SELECT DISTINCT
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
ORDER BY [Vendor Name]";

		public static readonly string DropReimbursementAddress3 = @"DROP TABLE IF EXISTS #reimbursement_address_3";

		public static readonly string SelectIntoReimbursementAddress3 = @"SELECT DISTINCT
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
ORDER BY NHMemberID";

		public static readonly string DropTempFinal = @"DROP TABLE IF EXISTS #temp_final";

		public static readonly string SelectIntoTempFinal = @"SELECT ra1.NHMemberID,
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
LEFT JOIN #reimbursement_address_3 ra3 ON ra1.txnreferenceid=ra3.txnreferenceid";

		public static readonly string DropTableReimbursementFinal = @"DROP TABLE IF EXISTS #reimbursement_final";

		public static readonly string SelectIntoReimbursementFinal = @"SELECT 
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
    NHMemberID";

		public static readonly string SelectRawData = @"SELECT * FROM #reimbursement_final";

		public static readonly string SelectMemberMailingInfo = @"SELECT 
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
ORDER BY CardholderFirstName";

		public static readonly string SelectMemberCheckReimbursement = @"SELECT
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
