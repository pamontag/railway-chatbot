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
    internal class MotoreOrarioConversationalPlugins
    {
        // Mock Data
        private readonly DateTime today = DateTime.Now;
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

        [KernelFunction("get_today")]
        [Description("Ottieni la data di oggi")]
        [return: Description("La data di oggi")]
        public async Task<DateTime> GetTodayAsync()
        {
            await Task.CompletedTask;  // This line removes the warning
            return this.today;
        }

        [KernelFunction("get_weather_by_city")]
        [Description("Ottieni le previsioni meteo per una città specifica")]
        [return: Description("Le previsioni meteo per la città specificata")]
        public async Task<List<WeatherModel>> GetWeatherByCityAsync(string city)
        {
            return this._weather.Where(weather => weather.City == city || String.IsNullOrWhiteSpace(city)).ToList();
        }
       
    }
}
