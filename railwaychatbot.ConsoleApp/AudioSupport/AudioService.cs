using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace railwaychatbot.ConsoleApp.AudioSupport
{

    public class AudioService
    {
        private const int RATE = 16000;
        private const int CHANNELS = 1;

        private readonly MemoryStream m_MicrophoneStream = new();

        private WaveInEvent? m_Windows_WaveInEvent;

        public void PlayAudio(byte[] audioData)
        {
            if (OperatingSystem.IsWindows())
            {
                using (var waveOut = new WaveOutEvent())
                {
                    using (var memoryStream = new MemoryStream(audioData))
                    {
                        // Convert the byte array to a stream and play it
                        memoryStream.Position = 0;
                        using (var audioFileReader = new Mp3FileReader(memoryStream)) { 
                            waveOut.Init(audioFileReader);
                            waveOut.Play();
                            while (waveOut.PlaybackState == PlaybackState.Playing)
                            {
                                System.Threading.Thread.Sleep(100);
                            }
                        }
                    }
                }
            }
            else
            {
                //Linux stuff for later
            }
        }

        public void StartRecording()
        {
            if (OperatingSystem.IsWindows())
            {
                if (m_Windows_WaveInEvent == null)
                {
                    m_Windows_WaveInEvent = new WaveInEvent()
                    {
                        DeviceNumber = 0,
                        WaveFormat = new WaveFormat(RATE, 16, CHANNELS),
                        BufferMilliseconds = 20
                    };

                    //use event to copy recording into memory until stopped
                    m_Windows_WaveInEvent.DataAvailable += (sender, e) =>
                    {
                        m_MicrophoneStream.Write(e.Buffer, 0, e.BytesRecorded);
                    };
                }

                try
                {
                    m_Windows_WaveInEvent.StartRecording();
                }
                catch { }   //No microphone
            }
            else
            {
                //Linux stuff for later
            }
        }

        public byte[] StopRecording()
        {
            byte[] result = Array.Empty<byte>();

            if (OperatingSystem.IsWindows())
            {
                if (m_Windows_WaveInEvent != null)
                {
                    try
                    {
                        m_Windows_WaveInEvent.StopRecording();
                    }
                    catch { }
                }

                if ((m_MicrophoneStream != null) && (m_MicrophoneStream.Length > 0))
                {
                    byte[] rawMicrophoneData = m_MicrophoneStream.ToArray();
                    m_MicrophoneStream.SetLength(0);

                    //convert raw microphone data to in-memory .wav file
                    using (MemoryStream memoryStream = new())
                    {
                        using (WaveFileWriter writer = new(memoryStream, new WaveFormat(RATE, 16, CHANNELS)))
                        {
                            writer.Write(rawMicrophoneData, 0, rawMicrophoneData.Length);
                            result = memoryStream.ToArray();
                        }
                    }
                }
            }
            else
            {
                //Linux stuff for later
            }

            return result;
        }
    }
}
