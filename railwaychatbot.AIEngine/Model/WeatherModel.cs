using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine.Model
{
    internal class WeatherModel
    {
        public WeatherModel() { }

        public string? City { get; set; }
        public int Temperature { get; set; }
        public string? Weather { get; set; }
    }
}
