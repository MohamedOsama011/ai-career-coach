using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs
{
    public class FawaterakDto
    {
        public decimal cartTotal { get; set; }
        public string currency { get; set; }
        public string first_name { get; set; }
        public string last_name { get; set; }
        public string email { get; set; }
        public string? phone { get; set; }
        public string? address { get; set; }
        public string cartitems_name { get; set; }
        public decimal cartitems_price { get; set; }
        public int cartitems_quantity { get; set; }

        public string invoice { get; set; }
    }
}
