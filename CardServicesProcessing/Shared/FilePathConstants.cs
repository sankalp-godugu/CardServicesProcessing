namespace CardServicesProcessor.Shared
{
    public class FilePathConstants
    {
        public static readonly string ActiveFitnessTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\ActiveFitness.txt";
        public static readonly string DVHTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\DVH.txt";
        public static readonly string AssistiveDevicesTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\AssistiveDevices.txt";

        public static readonly string HealthyGroceriesTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\HealthyGroceries.txt";
        public static readonly string MoreFoodsTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\MoreFoods.txt";


        public static readonly string OtcTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\OTC.txt";
        public static readonly string MoreOtcTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\MoreOTC.txt";

        public static readonly string ServiceDogTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\ServiceDog.txt";
        public static readonly string UtilitiesTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\Utilities.txt";
        public static readonly string NotAvailableTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\NotAvailable.txt";
        public static readonly string UnknownTextFilePath = @"C:\Users\Sankalp.Godugu\source\repos\CardServicesProcessing\CardServicesProcessing\Shared\ReimbursementItems\Unknown.txt";
        

        // Preload text file contents
        public static readonly Dictionary<string, string> ReimbursementItemFilePaths = new()
        {
            { Wallet.HealthyGroceries, HealthyGroceriesTextFilePath },
            { Wallet.OTC, OtcTextFilePath },
            { Wallet.DVH, DVHTextFilePath },
            { Wallet.Utilities, UtilitiesTextFilePath },
            { Wallet.ActiveFitness, ActiveFitnessTextFilePath },
            { Wallet.AssistiveDevices, AssistiveDevicesTextFilePath },
            { Wallet.ServiceDog, ServiceDogTextFilePath },
            //{ Wallet.OTC, MoreOtcTextFilePath },
            //{ Wallet.HealthyGroceries, MoreFoodsTextFilePath },
            { Wallet.NA, NotAvailableTextFilePath },
            { Wallet.Unknown, UnknownTextFilePath }
        };
    }
}
