using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CamTheGeek.GpioDotNet;
using FredQnA;
using ManagedBass;
using NetCoreAudio;
using SpeakerRecognition;
using TcpRaspServer.Utility;
using UpdatedKB;

namespace FredServer
{
    class Program
    {
        private List<string> IPs = new List<string>();
        static Player player = new Player();

        public static async Task Main()
        {
            TcpListener server = null;
            GpioPin light = new GpioPin(21, Direction.Out); // initalize pin 21 for light
            Bass.Init(); // initalize sound card

            try
            {
                // Set the TcpListener on port 13000.
                Int32 port = 13000;
                string localIP = GetIpAddress();
                IPAddress localAddr = IPAddress.Parse(localIP);

                server = new TcpListener(localAddr, port);
                server.Start();

                // Buffer for reading data
                Byte[] bytes = new Byte[1024];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");
                    
                    NetworkStream stream = client.GetStream();

                    int i = stream.Read(bytes, 0, bytes.Length);

                    // Loop to receive all the data sent by the client.

                    // Translate data bytes to a ASCII string.
                    data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    Console.WriteLine("Received: {0}", data);
                                               
                    // translate data into commands
                    string[] cmd = data.Split('*');
                    switch (cmd[0])
                    {
                        case "light":
                            {                                    
                                if (cmd[1] == "on")
                                {
                                    light.Value = PinValue.High;
                                    Console.WriteLine("Light On");
                                }
                                if (cmd[1] == "off")
                                {
                                    light.Value = PinValue.Low;
                                    Console.WriteLine("Light Off");                                                                                                                        
                                }
                                break;
                            }
                        case "TTS":
                            {
                                await AskFred.tts.TextToWords(cmd[1]);
                                Console.WriteLine("FRED says " + cmd[1]);
                                //FredSays();                                                                         
                                break;
                            }
                        case "vision":
                            {
                                await FredVision.GetVision("describe");
                                await AskFred.tts.TextToWords(FredVision.FredSees());
                                Console.WriteLine("FRED sees " + FredVision.FredSees());
                                //FredSays();                                    
                                break;
                            }
                        case "Record":
                            {
                                VarHolder.LinuxRecTime = "";
                                Console.WriteLine(cmd[0]);
                                player.Record().Wait();
                                break;
                            }
                        case "PlayB":
                            {
                                string path = Directory.GetCurrentDirectory();
                                if (path.Contains("\\"))
                                {
                                    path += "\\record.wav";
                                }
                                else
                                {
                                    path += "/record.wav";
                                }
                                Console.WriteLine(cmd[0]);
                                player.Play(path).Wait();
                                break;
                            }
                        case "StopR":
                            {
                                VarHolder.LinuxRecTime = "";
                                player.StopRecording().Wait();
                                break;
                            }
                        case "FredSpy":
                            {
                                string path = Directory.GetCurrentDirectory();
                                if (path.Contains("\\"))
                                {
                                    path += "\\record.wav";
                                }
                                else
                                {
                                    path += "/record.wav";
                                }
                                Byte[] audio = File.ReadAllBytes(path);
                                stream.WriteAsync(audio, 0, audio.Length).Wait();
                                break;
                            }
                        case "Reco1":
                            {
                                VarHolder.LinuxRecTime = "-d 10";
                                VoiceSignature.RecVoiceOne(cmd[1]).Wait();
                                break;
                            }
                        case "Reco2":
                            {
                                VarHolder.LinuxRecTime = "-d 10";
                                VoiceSignature.RecVoiceTwo().Wait();
                                break;
                            }
                        case "ConfEnroll":
                            {
                                VoiceSignature.ConfirmEnroll();
                                break;
                            }
                        case "CancEnroll":
                            {
                                VoiceSignature.CancelEnroll();
                                break;
                            }
                        case "GetEnroll":
                            {
                                string path = Directory.GetCurrentDirectory();
                                if (path.Contains("\\"))
                                {
                                    path += "\\speaker_recog.txt";
                                }
                                else
                                {
                                    path += "/speaker_recog.txt";
                                }
                                Byte[] text = File.ReadAllBytes(path);
                                stream.WriteAsync(text, 0, text.Length).Wait();
                                break;
                            }
                        case "DelProfile":
                            {
                                string profileId = cmd[1];
                                VoiceSignature.DeleteProfile(profileId).Wait();
                                break;
                            }
                        case "AskFred": // Ask Fred
                            {
                                //TTS.Speak("I am listening...").Wait();

                                VarHolder.LinuxRecTime = "-d 5";
                                AskFred.Inquiry().Wait();
                                break;
                            }
                        case "UpdateKB": // Update KB
                            {
                                string trial = cmd[1];
                                string[] splitTrial = trial.Split(";");
                                string[] question = new string[splitTrial.Length];
                                string[] answer = new string[splitTrial.Length];
                                for (int j = 0; j < splitTrial.Length; j++)
                                {
                                    question[j] = splitTrial[j].Split(":")[0].Replace("'", "");
                                    answer[j] = splitTrial[j].Split(":")[1].Replace("'", "");
                                }
                                UpdateFredKB.UpdateKB(question, answer);
                                break;
                            }
                        case "UpdateProfileInfo":
                            {
                                VoiceSignature.UpdateProfile(cmd[1]);
                                break;
                            }
                    }                       
                } // listening loop
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
            }
            finally
            {
                // Stop listening for new clients.
                server.Stop();
            }

        } // main   

        public static string GetIpAddress()
        {
            string localIP;
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                localIP = endPoint.Address.ToString();
            }
            return localIP;
        }

    }
}
