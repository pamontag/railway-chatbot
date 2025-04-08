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
    internal class MotoreOrarioTrainScheduleExpertMockPlugin
    {
        private readonly List<TrainModel> _trainModels = new()
        {
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Roma", Id = 2, Name = "Roma Termini" } }, Id = 1, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,08,30,00), StazioneArrivo = new StationModel { City = "Roma", Id = 2, Name = "Roma Termini" }, StazionePartenza = new StationModel { City = "Milano", Id = 3885, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" }, new StationModel { City = "Roma", Id = 8, Name = "Roma Tiburtina" } }, Id = 1, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,08,30,00), StazioneArrivo = new StationModel { City = "Roma", Id = 8, Name = "Roma Tiburtina" }, StazionePartenza = new StationModel { City = "Milano", Id = 4672, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" } }, Id = 2, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,14,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,10,30,00), StazioneArrivo = new StationModel { City = "Firenze", Id = 3, Name = "Firenze Santa Maria Novella" }, StazionePartenza = new StationModel { City = "Milano", Id = 4999, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie = new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Napoli", Id = 4, Name = "Napoli Centrale" } }, Id = 3, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,18,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,14,30,00), StazioneArrivo = new StationModel { City = "Napoli", Id = 4, Name = "Napoli Centrale" }, StazionePartenza = new StationModel { City = "Milano", Id = 3215, Name = "Milano Centrale" } },
            new TrainModel { ElencoStazioniIntermedie= new List<StationModel> { new StationModel { City = "Milano", Id = 1, Name = "Milano Centrale" }, new StationModel { City = "Torino", Id = 5, Name = "Torino Porta Nuova" } }, Id = 4, OrarioArrivo = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,16,30,00), OrarioPartenza = new DateTime(DateTime.Now.Year,DateTime.Now.Month,DateTime.Now.Day,12,30,00), StazioneArrivo = new StationModel { City = "Torino", Id = 5, Name = "Torino Porta Nuova" }, StazionePartenza = new StationModel { City = "Milano", Id = 3674, Name = "Milano Centrale" } }
        };

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
    }
}
