using CardServicesProcessor.Utilities.Constants;

namespace CardServicesProcessor.Shared
{
    public static class Wallet
    {
        // wallets from portal
        public const string ActiveFitness = "Active Fitness";
        public const string AssistiveDevices = "Assistive Devices";
        public const string DVH = "DVH";
        public const string HealthyGroceriesAndOTC = "OTC / Healthy Groceries";
        public const string HealthyGroceries = "Healthy Groceries";
        public const string OTC = "OTC";
        public const string PERS = "PERS";
        public const string Rewards = "Rewards";
        public const string ServiceDog = "Service Dog";
        public const string Transportation = "Transportation";
        public const string Utilities = "Utilities";
        public const string NA = "N/A"; // for reimbursements that are actually reship or refund requests
        public const string Unknown = "Unknown";

        public static Dictionary<string, string[]> GetWalletVariations()
        {
            return new Dictionary<string, string[]>
            {
                { ActiveFitness, Variations.ActiveFitnessVariations },
                { AssistiveDevices, Variations.AssistiveDevicesVariations },
                { DVH, Variations.DvhVariations },
                { HealthyGroceries, Variations.HgVariations },
                { OTC, Variations.OtcVariations },
                { PERS, Variations.PersVariations },
                { Rewards, Variations.RewardsVariations },
                { ServiceDog, Variations.ServiceVariations },
                { Utilities, Variations.UtilityVariations },
                { NA, Variations.NaVariations }
            };
        }

        public static string? GetWalletNameFromComments(string? closingComments)
        {
            foreach (KeyValuePair<string, string[]> kvp in GetWalletVariations()
                    .Where(kvp => closingComments.IsTruthy() && closingComments.ContainsAny(kvp.Value)))
            {
                return kvp.Key;
            }

            return null;
        }

        public static string? GetWalletNameFromBenefitDesc(string? wallet)
        {
            foreach (KeyValuePair<string, string[]> kvp in GetWalletVariations()
                    .Where(kvp => wallet.IsTruthy() && wallet.ContainsAny(kvp.Value)))
            {
                return kvp.Key;
            }

            return wallet?.Trim();
        }

        // Create a dictionary to map Z Purse values to BenefitDesc values
        public static readonly Dictionary<string, string> zPurseToBenefitDesc = new()
        {
            { "ZOT", "Over the Counter" },
            { "ZFO", "Healthy Food" },
            { "ZRE", "Rewards" },
            { "ZCS", "Open Spend - Unrestricted Rewards" },
            { "ZUL", "Utilities" },
            { "ZVD", "Vision, Dental, Hearing" },
            { "ZVI", "Vision" },
            { "ZDE", "Dental" },
            { "ZER", "Hearing" },
            { "ZCO", "Combined Food and Over The Counter" },
            { "ZCU", "Combined Food and Utilities" },
            { "ZCF", "Combined Food and Home Modification" },
            { "ZLD", "Other - Health Dollars" },
            { "ZDH", "Other - Vision, Dental, Hearing, Transportation" },
            { "ZUG", "Flex (Grocery, Transportation, Utilities)" },
            { "ZAS", "Assistive Devices" },
            { "ZHG", "Healthy Groceries" },
            { "ZSE", "Service Dog" },
            { "ZAA", "Account Active Fitness" },
            { "ZEF", "Essential Extras Flex Account DVH" },
            { "ZEA", "Embedded Assistive Devices" },
            { "ZED", "Essential Extras Flex Account Dental and Vision" },
            { "ZHD", "Other - Health Dollars Plus" },
            { "ZGT", "Flex (Grocery, OTC, Transportation, Utilities)" },
            { "ZFI", "Flex (Fitness and OTC)" },
            { "ZTR", "Transportation" },
            { "ZUT", "Flex (Utilities, Telecom, Transportation, Education, Child Care, Rental Payments)" },
            { "ZHM", "Flex (Hearing, OTC, Meals, Vision, Transportation)" },
            { "ZBW", "Other- (OTC, Health Education, Fitness Benefit, Home and Bathroom safety devices and modifications, weight management programs, and nutritional/dietary benefits)" },
            { "ZHE", "Other - (Health Education, Fitness Benefits, Home and Bathroom safety devices and modifications, weight management programs, and nutritional/dietary benefits)" },
            { "ZUU", "Gas and Utilities" },
            { "ZLO", "VDH Otc" },
            { "ZUB", "Utilites and Bathroom Safety" },
            { "ZCR", "Community Rent Groceries and Utilities" },
            { "ZPA", "Parking" },
            { "ZLE", "FLEX" },
            { "ZOV", "Other - Vision, Dental, Hearing, Transportation and GAS" },
            { "ZAC", "Dental, Vision, Hearing, Acupuncture, Chiropractor" },
            { "ZCM", "OTC & Grocery" },
            // Add more mappings as needed
        };

        public static readonly Dictionary<string, string> benefitTypeToBenefitDesc = new()
        {
            { "OTC", "Over the Counter" },
            { "FOD", "Healthy Food" },
            { "REW", "Rewards" },
            { "CSH", "Open Spend - Unrestricted Rewards" },
            { "UTL", "Utilities" },
            { "VDH", "Vision, Dental, Hearing" },
            { "VIS", "Vision" },
            { "DEN", "Dental" },
            { "EAR", "Hearing" },
            { "CFO", "Combined Food and Over The Counter" },
            { "CFU", "Combined Food and Utilities" },
            { "CFM", "Combined Food and Home Modification" },
            { "LDD", "Other - Health Dollars" },
            { "DHT", "Other - Vision, Dental, Hearing, Transportation" },
            { "UGT", "Flex (Grocery, Transportation, Utilities)" },
            { "ASD", "Assistive Devices" },
            { "HEG", "Healthy Groceries" },
            { "SED", "Service Dog" },
            { "AAF", "Account Active Fitness" },
            { "EFX", "Essential Extras Flex Account DVH" },
            { "EAD", "Embedded Assistive Devices" },
            { "EDV", "Essential Extras Flex Account Dental and Vision" },
            { "HDD", "Other - Health Dollars Plus" },
            { "GTU", "Flex (Grocery, OTC, Transportation, Utilities)" },
            { "FIO", "Flex (Fitness and OTC)" },
            { "TRN", "Transportation" },
            { "UTT", "Flex (Utilities, Telecom, Transportation, Education, Child Care, Rental Payments)" },
            { "HMV", "Flex (Hearing, OTC, Meals, Vision, Transportation)" },
            { "BWN", "Other- (OTC, Health Education, Fitness Benefit, Home and Bathroom safety devices and modifications, weight management programs, and nutritional/dietary benefits)" },
            { "HEF", "Other - (Health Education, Fitness Benefits, Home and Bathroom safety devices and modifications, weight management programs, and nutritional/dietary benefits)" },
            { "UTG", "Gas and Utilities" },
            { "LEO", "VDH Otc" },
            { "UTB", "Utilites and Bathroom Safety" },
            { "CRG", "Community Rent Groceries and Utilities" },
            { "PAR", "Parking" },
            { "LEX", "FLEX" },
            { "OVD", "Other - Vision, Dental, Hearing, Transportation and GAS" },
            { "ACD", "Dental, Vision, Hearing, Acupuncture, Chiropractor" },
            { "XXE", "Dental & Hearing" }
            // Add more mappings as needed
        };
    }
}
