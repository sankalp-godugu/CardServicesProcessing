using CardServicesProcessor.Utilities.Constants;
using System.Linq;

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
        public const string ServiceDog = "Service Dog";
        public const string Transportation = "Transportation";
        public const string Utilities = "Utilities";
        public const string Unknown = "Unknown";
        // wallets from z purse mapping
        public const string Rewards = "Rewards";

        public static readonly string[] AssistiveDevicesVariations = ["assistive devices"];
        public static readonly string[] ActiveFitnessVariations = ["fitness", "AAF"];
        public static readonly string[] DvhVariations = ["dvh", "dhv", "vdh", "vhd", "hvd", "hdv", "dental", "vision", "hearing", "ZVD2300"];
        public static readonly string[] HgVariations = ["hg", "grocer", "heg", "healthy food", "FOD2312"];
        public static readonly string[] OtcVariations = ["otc", "over the counter", "ESM"];
        public static readonly string[] PersVariations = [" pers "];
        public static readonly string[] RewardsVariations = ["Wellness your way", "Open Spend - Unrestricted Rewards"];
        public static readonly string[] ServiceVariations = ["service dog", " dog "];
        public static readonly string[] UtilityVariations = ["utility", "utilities", "utl", "parking"];
        public static readonly string[] OtherBenefitTypes = ["y", "yes", "n", "no", "denied", "ptc", "none"];

        public static Dictionary<string, string[]> GetCategoryVariations()
        {
            return new Dictionary<string, string[]>
            {
                { ActiveFitness, ActiveFitnessVariations },
                { AssistiveDevices, AssistiveDevicesVariations },
                { DVH, DvhVariations },
                { HealthyGroceries, HgVariations },
                { OTC, OtcVariations },
                { PERS, PersVariations },
                { Rewards, RewardsVariations },
                { ServiceDog, ServiceVariations },
                { Utilities, UtilityVariations }
            };
        }

        public static string GetWalletFromCommentsOrWalletCol(this string wallet, string? closingComments)
        {
            foreach (var kvp in from KeyValuePair<string, string[]> kvp in GetCategoryVariations()
                                where (closingComments.IsTruthy() && closingComments.ContainsAny(kvp.Value))
                                || wallet.ContainsAny(kvp.Value)
                                select kvp)
            {
                return kvp.Key;
            }

            // If no specific category found, return trimmed input
            return wallet.Trim();
        }

        public static readonly Dictionary<string, string> BenefitDescToWalletName = new()
        {
            // active fitness
            { "Active Fitness", ActiveFitness },
            { "Account Active Fitness", ActiveFitness },

            // assistive devices
            { "Assistive Devices", AssistiveDevices },
            { "Embedded Assistive Devices", AssistiveDevices },

            // dental, vision, hearing
            { "Vision, Dental, Hearing", DVH },
            { "Dental, Vision, Hearing", DVH },
            { "Vision", DVH },
            { "Dental", DVH },
            { "Hearing", DVH },
            { "DVH", DVH },
            { "Flex-DVH", DVH },
            { "Flex DVH", DVH },
            { "Flex-Vision only", DVH },
            { "Flex-Dental", DVH },
            { "DHV", DVH },
            { "VDH", DVH },
            { "VHD", DVH },
            { "HVD", DVH },
            { "HDV", DVH },
            { "ZVD", DVH },
            { "Essential Extras Flex Account DVH", DVH },
            { "Everyday Options Allowance for Dental, Vision, and Hearing", DVH },

            // healthy food
            { "Healthy Food", HealthyGroceries },
            { "HG", HealthyGroceries },
            { "FOD", HealthyGroceries },
            { "Healthy Groceries [FOD]", HealthyGroceries },
            { "Groceries", HealthyGroceries },

            // over the counter
            { "OTC", OTC },
            { "Over the Counter", OTC },
            { "ESM", OTC },

            { "PERS", PERS },

            // rewards
            { "Rewards", Rewards },
            { "Open Spend - Unrestricted Rewards", Rewards },

            { "Service Dog", ServiceDog },

            // transportation
 

            // utilities
            { "Gas and Utilities", Utilities },
            { "Utilities", Utilities },
            { "Utilites and Bathroom Safety", Utilities },
            { "Parking", Utilities },
            
            // Add more mappings as needed
        };

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
            { "ZAC", "Dental, Vision, Hearing, Acupuncture, Chiropractor" }
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
            { "ACD", "Dental, Vision, Hearing, Acupuncture, Chiropractor" }
            // Add more mappings as needed
        };
    }
}
