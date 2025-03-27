using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine.Model
{
    public class ChatMessage
    {
        public string id { get; set; }
        public string sessionid { get; set; }
        public string message { get; set; }
        public string role { get; set; } // "User" or "Assistant"
        public DateTime Timestamp { get; set; }
    }
}
