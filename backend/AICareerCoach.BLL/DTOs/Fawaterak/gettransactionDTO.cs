using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace AICareerCoach.BLL.DTOs.Fawaterak
{
    public class gettransactionDTO
    {
        [JsonPropertyName("intent_key")]
        public string Intentkey { get; set; }
    }
}
