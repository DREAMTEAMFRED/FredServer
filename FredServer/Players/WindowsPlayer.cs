using NAudio.Wave;
using NetCoreAudio.Interfaces;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Timers;

namespace NetCoreAudio.Players
{
    internal class WindowsPlayer : IPlayer
    {
        [DllImport("winmm.dll")]
        private static extern long mciSendString(string command, StringBuilder stringReturn, int returnLength, IntPtr hwndCallback);

        private Timer _playbackTimer;
        private Stopwatch _playStopwatch;
        private string myFilename = "";

        public event EventHandler PlaybackFinished;

        public bool Playing { get; private set; }
        public bool Paused { get; private set; }

        public Task Play(string fileName)
        {
            myFilename = fileName;
            _playbackTimer = new Timer();
            _playbackTimer.AutoReset = false;
            _playStopwatch = new Stopwatch();
            ExecuteMsiCommand("Close All");
            ExecuteMsiCommand($"Open " + fileName);
            ExecuteMsiCommand("Status " + fileName + " Length");
            ExecuteMsiCommand("Play " + fileName);
            Paused = false;
            Playing = true;
            _playbackTimer.Elapsed += HandlePlaybackFinished;
            _playbackTimer.Start();
            _playStopwatch.Start();

            return Task.CompletedTask;
        }

        public Task Record()
        {
            //bool hmmm = guess;
            var sb = new StringBuilder();
            mciSendString("open new Type waveaudio Alias recsound", sb, 0, IntPtr.Zero);
            mciSendString("set recsound time format ms bitspersample 16 channels 1 samplespersec 16000 bytespersec 128000 alignment 4", sb, 0, IntPtr.Zero);
            mciSendString("record recsound", sb, 0, IntPtr.Zero);

            return Task.CompletedTask;
        }

        public Task StopRecording()
        {
            string path = Directory.GetCurrentDirectory();
            string fullPath = path + "\\record.wav";
            var sb = new StringBuilder();
            mciSendString("save recsound " + fullPath, sb, 0, IntPtr.Zero);
            mciSendString("close recsound ", sb, 0, IntPtr.Zero);

            byte[] audio = File.ReadAllBytes(@"record.wav");
            ConvertWaveFormat(audio);

            return Task.CompletedTask;
        }

        public static void ConvertWaveFormat(byte[] inArray)
        {
            using (var mem = new MemoryStream(inArray))
            using (var reader = new WaveFileReader(mem))
            using (var converter = WaveFormatConversionStream.CreatePcmStream(reader))
            using (var upsampler = new WaveFormatConversionStream(new WaveFormat(16000, 16, 1), converter))
            {
                // todo: without saving to file using MemoryStream or similar
                WaveFileWriter.CreateWaveFile(@"record.wav", upsampler);
            }
        }

        public Task Pause()
        {
            if (Playing && !Paused)
            {
                ExecuteMsiCommand("Pause myDevice");
                Paused = true;
                _playbackTimer.Stop();
                _playStopwatch.Stop();
                _playbackTimer.Interval -= _playStopwatch.ElapsedMilliseconds;
            }

            return Task.CompletedTask;
        }

        public Task Resume()
        {
            if (Playing && Paused)
            {
                ExecuteMsiCommand("Resume myDevice");
                Paused = false;
                _playbackTimer.Start();
                _playStopwatch.Reset();
                _playStopwatch.Start();
            }
            return Task.CompletedTask;
        }

        public Task Stop()
        {
            if (Playing)
            {
                ExecuteMsiCommand("Stop All");
                ExecuteMsiCommand("Close All");
                ExecuteMsiCommand("Stop " + myFilename);
                Playing = false;
                Paused = false;
                _playbackTimer.Stop();
                _playStopwatch.Stop();
            }
            else
            {
                ExecuteMsiCommand("Stop All");
                ExecuteMsiCommand("Close All");
            }
            return Task.CompletedTask;
        }

        private void HandlePlaybackFinished(object sender, ElapsedEventArgs e)
        {
            Stop();
            ExecuteMsiCommand("Close All");
            Playing = false;
            PlaybackFinished?.Invoke(this, e);
            _playbackTimer.Dispose();
            _playbackTimer = null;
        }

        private Task ExecuteMsiCommand(string commandString)
        {
            var sb = new StringBuilder();

            var result = mciSendString(commandString, sb, 1024 * 1024, IntPtr.Zero);

            if (result != 0)
            {
                throw new Exception($"Error executing MCI command. Error code: {result}");
            }

            if (commandString.ToLower().StartsWith("status") && int.TryParse(sb.ToString(), out var length))
                _playbackTimer.Interval = length;

            return Task.CompletedTask;
        }
    }
}