using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static railwaychatbot.AIEngine.Impl.AIRealTimeAudioEngine;

namespace railwaychatbot.AIEngine
{
    public interface IAIRealTimeAudioEngine
    {
        IAsyncEnumerable<Stream> GetSingleResponseFromAudio(Stream audio);
        Task InitSession();

        Task SendAudioAsync(Stream audio);

        bool IsProcessing();

        event StreamingDeltaResponseHandler? OnStreamingDeltaResponse;
    }
}
