using Azure.AI.OpenAI.Assistants;
using Microsoft.SemanticKernel;
using OpenAI.RealtimeConversation;
using railwaychatbot.AIEngine.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

# pragma warning disable OPENAI002

namespace railwaychatbot.AIEngine.Plugins
{
    internal class MotoreOrarioFunctions
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
       

        public MotoreOrarioFunctions()
        {
            getStations = new ConversationFunctionTool
            {
                Name = "get_stations",
                Description = "Ottieni la lista delle stazioni ferroviarie e le loro città dove sono presenti"
            };
        }

        public ConversationFunctionTool getStations { get; set; }

        string GetStations()
        {
            return JsonSerializer.Serialize(this._stations);
        } 


        ToolOutput GetResolvedToolOutput(RequiredToolCall toolCall)
        {
            if (toolCall is RequiredFunctionToolCall functionToolCall)
            {
                if (functionToolCall.Name == getStations.Name)
                {
                    return new ToolOutput(toolCall, GetStations());
                }
            }
            return null;
        }
    }
}
