using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Parser
{
    public class District
    {
        [Key]
        public string DistrictName { get; set; }
        public List<Event> Events { get; set; }
        public List<Address> Addresses { get; set; }
        public District()
        {
            Addresses = new List<Address>();
        }

        public District(string name)
        {
            DistrictName  = name;
            Addresses = new List<Address>();
        }

       

    }
}
