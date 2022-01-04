using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parser
{
    public class Event
    {
        [Key]
        public string Link { get; set; }
        public Source Source { get; set; }
        public string Title { get; set; }
        public string IncidentCategory {get;set;}
        private string body;
        public string Body 
        {
            get { return body; }
            set
            {
                if (value.Length > 1000)
                    body = value.Substring(0, 1000);
                else
                    body = value;
            }

        }
        public string Date { get; set; }
        public DateTime DateOfDownload { get; set; }
        public District District{ get; set; }

        [Column(TypeName = "jsonb")]
        public HashSet<string> Hash { get; }

        public override int GetHashCode()
        {
            return Link.GetHashCode();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is Event))
                return false;
            
            return ((Event)obj).Link.Equals(Link);
        }

        public override string ToString()
        {
            return $"{Link}\n{Body}";
        }
    }
}
