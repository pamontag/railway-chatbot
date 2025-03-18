using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.AIEngine
{
    public interface IAIRealTimeAudioEngine
    {
        IAsyncEnumerable<Stream> GetResponseFromAudio(Stream audio);
    }
}
