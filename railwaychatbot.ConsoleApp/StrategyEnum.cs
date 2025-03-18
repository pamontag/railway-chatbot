using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.ConsoleApp
{

    public enum Strategy
    {
        MotoreOrarioAgent = 1,
        MotoreOrarioStreamingAgent = 2,
        MotoreOrarioGroupAgent = 3,
        MotoreOrarioGroupStreamingAgent = 4,
        MotoreOrarioStreamingAgentFunction = 5,
        MotoreOrarioGroupStreamingAgentFunction = 6,
        MotoreOrarioAudioToTextAgent = 7,
        MotoreOrarioAudioToTextStreamingAgent = 8,
        MotoreOrarioGroupAgentRealTimeAudio = 9
    }

}
