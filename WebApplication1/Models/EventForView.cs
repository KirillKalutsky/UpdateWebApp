using Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace WebApplication.Models
{
    public class EventForView
    {
        public Event Event { get; set; }
        public bool IsChecked { get; set; }

        public bool IsViewed { get; set; }
    }
}
