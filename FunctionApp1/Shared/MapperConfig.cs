using AutoMapper;

namespace ReimbursementReporting.Shared
{
    public class MapperConfig
    {
        public static Mapper InitializeAutomapper()
        {
            //Provide all the Mapping Configuration
            MapperConfiguration config = new(cfg =>
            {

            });

            //Create an Instance of Mapper and return that Instance
            Mapper mapper = new(config);
            return mapper;
        }
    }
}
