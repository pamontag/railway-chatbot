using Microsoft.SemanticKernel;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using railwaychatbot.AIEngine.Model;

namespace railwaychatbot.AIEngine.Plugins.Impl
{
    public class MotoreOrarioMockPlugins 
    {

        // Mock Data
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

        private readonly List<TrainModel> _trainModels = new()
        {
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Roma", Id = 2, Name = "Roma Termini" } }, Id = 1, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,08,30,00), StazioneArrivo = new StationModel { City = "Roma", Id = 2, Name = "Roma Termini" }, StazionePartenza = new StationModel { City = "Milano", Id = 3885, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" }, new StationModel { City = "Roma", Id = 8, Name = "Roma Tiburtina" } }, Id = 1, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,08,30,00), StazioneArrivo = new StationModel { City = "Roma", Id = 8, Name = "Roma Tiburtina" }, StazionePartenza = new StationModel { City = "Milano", Id = 4672, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" } }, Id = 2, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,14,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,10,30,00), StazioneArrivo = new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" }, StazionePartenza = new StationModel { City = "Milano", Id = 4999, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Napoli", Id = 4, Name = "Napoli Centrale" } }, Id = 3, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,18,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,14,30,00), StazioneArrivo = new StationModel { City = "Napoli", Id = 4, Name = "Napoli Centrale" }, StazionePartenza = new StationModel { City = "Milano", Id = 3215, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie= new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Torino", Id = 5, Name = "Torino Porta Nuova" } }, Id = 4, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,16,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), StazioneArrivo = new StationModel { City = "Torino", Id = 5, Name = "Torino Porta Nuova" }, StazionePartenza = new StationModel { City = "Milano", Id = 3674, Name = "Milano Centrale" } }
        };

        private readonly List<WeatherModel> _weather = new()
        {
            new WeatherModel { City = "Milano", Temperature = 20, Weather = "Soleggiato" },
            new WeatherModel { City = "Roma", Temperature = 25, Weather = "Soleggiato" },
            new WeatherModel { City = "Firenze", Temperature = 22, Weather = "Soleggiato" },
            new WeatherModel { City = "Napoli", Temperature = 28, Weather = "Soleggiato" },
            new WeatherModel { City = "Torino", Temperature = 18, Weather = "Soleggiato" },
            new WeatherModel { City = "Venezia", Temperature = 21, Weather = "Soleggiato" },
            new WeatherModel { City = "Bologna", Temperature = 23, Weather = "Soleggiato" }
        };


        [KernelFunction("get_stations")]
        [Description("Ottieni la lista delle stazioni ferroviarie e le loro città dove sono presenti")]
        [return: Description("Una lista di stazioni ferroviarie")]
        public async Task<List<StationModel>> GetStations()
        {
            await Task.CompletedTask;  // This line removes the warning
            return _stations;
        }

        [KernelFunction("get_trains_by_date_city")]
        [Description("Fornisce un elenco di treni che soddisfano le condizioni di data di partenza, città di partenza e città di arrivo")]
        [return: Description("La lista di treni che soddisfa le condizioni")]
        public async Task<List<TrainModel>> GetTrainsByDateCityStartCityArrival(DateTime date, string cityStart, string cityArrival)
        {
            // refactor this code, get the trains that match the date, cityStart and cityArrival. Date must be equal for year, month and day
            var trains = _trainModels.Where(train => train.OrarioPartenza.Date.Year == date.Date.Year && train.OrarioPartenza.Date.Month == date.Date.Month && train.OrarioPartenza.Day == date.Date.Day
            && train.StazionePartenza.City == cityStart && train.StazioneArrivo.City == cityArrival).ToList();

            if (trains == null)
            {
                return null;
            }
            await Task.CompletedTask;  // This line removes the warning
            return trains;
        }

        [KernelFunction("get_weather_by_city")]
        [Description("Ottieni le previsioni meteo per una città specifica")]
        [return: Description("Le previsioni meteo per la città specificata")]
        public async Task<List<WeatherModel>> GetWeatherByCityAsync(string city)
        {
            await Task.CompletedTask; // This line removes the warning
            return _weather.Where(weather => weather.City == city || string.IsNullOrWhiteSpace(city)).ToList();
        }
    }
}
