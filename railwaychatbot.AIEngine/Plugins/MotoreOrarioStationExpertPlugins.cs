using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using railwaychatbot.AIEngine.Model;

namespace railwaychatbot.AIEngine.Plugins
{
    internal class MotoreOrarioStationExpertPlugins
    {
        private readonly List<StationModel> _stations = new()
        {
            new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" },
            new StationModel { City = "Roma", Id = 2, Name = "Roma Termini" },
            new StationModel { City = "Roma", Id = 8, Name = "Roma Tiburtina" },
            new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" },
            new StationModel { City = "Napoli", Id = 4, Name = "Napoli Centrale" },
            new StationModel { City = "Torino", Id = 5, Name = "Torino Porta Nuova" },
            new StationModel { City = "Venezia", Id = 6, Name = "Venezia Santa Lucia" },
            new StationModel { City = "Bologna", Id = 7, Name = "Bologna Centrale" }
        };

        [KernelFunction("get_stations")]
        [Description("Ottieni la lista delle stazioni ferroviarie e le loro città dove sono presenti")]
        [return: Description("Una lista di stazioni ferroviarie")]
        public async Task<List<StationModel>> GetStations()
        {
            await Task.CompletedTask;  // This line removes the warning
            return this._stations;
        }
    }
}
