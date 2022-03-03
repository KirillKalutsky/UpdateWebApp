using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Parser
{
    public class Event
    {
        [Key]
        public string Link { get; set; }//сылка на источник
        public Source Source { get; set; }//представление источника(для БД связь использовалась)
        public string Title { get; set; }//заголовок статьи
        public string IncidentCategory {get;set;}//одна из категорий инцидента(дтп, пожар, ...)
        
        public DateTime DateOfDownload { get; set; }//дата распаршевания статьи(использую ее так как
                                                    //подразумевается что будем парсить в реальном времени
                                                    //и разница с датой написанной в статье не критична)
        public District District{ get; set; }//представление района


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

    }
}
