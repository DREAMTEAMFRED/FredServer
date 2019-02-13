using NetCoreAudio;
using System;
using System.Threading.Tasks;

namespace FredServer
{
    static class RecAudio
    {
        static Player player = new Player();

        public static async Task Record()
        {
            Console.WriteLine("recording for only 3secs....");
            await player.Record();
        }

        public static async Task StopRecording()
        {
            await player.StopRecording();
        }
    }
}
