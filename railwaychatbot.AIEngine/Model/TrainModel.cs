using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine.Model
{
    public class TrainModel
    {
        [JsonPropertyName("id")]
        public int Id { get; set; }

        [JsonPropertyName("stazione_partenza")]
        public StationModel? StazionePartenza { get; set; }

        [JsonPropertyName("stazione_arrivo")]
        public StationModel? StazioneArrivo { get; set; }

        [JsonPropertyName("orario_partenza")]
        public DateTime OrarioPartenza { get; set; }

        [JsonPropertyName("orario_arrivo")]
        public DateTime OrarioArrivo { get; set; }

        [JsonPropertyName("elenco_stazioni_intermedie")]
        public List<StationModel>? ElencoStazioniIntermedie { get; set; } 
    }
}
