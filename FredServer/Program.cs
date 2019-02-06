using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using CamTheGeek.GpioDotNet;
using ManagedBass;

namespace FredServer
{
    class Program
    {
        private List<string> IPs = new List<string>();

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
                Byte[] bytes = new Byte[256];
                String data = null;

                // Enter the listening loop.
                while (true)
                {
                    Console.Write("Waiting for a connection... ");

                    TcpClient client = server.AcceptTcpClient();
                    Console.WriteLine("Connected!");

                    data = null;
                    
                    NetworkStream stream = client.GetStream();

                    int i;

                    // Loop to receive all the data sent by the client.
                    while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                    {
                        // Translate data bytes to a ASCII string.
                        data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                        Console.WriteLine("Received: {0}", data);
                                               
                        // translate data into commands
                        string[] cmd = data.Split('-');
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
                                    await TTS.Speak(cmd[1]);
                                    Console.WriteLine("FRED says " + cmd[1]);
                                    FredSays();                                                                         
                                    break;
                                }
                            case "vision":
                                {
                                    await FredVision.GetVision("describe");
                                    await TTS.Speak(FredVision.FredSees());
                                    Console.WriteLine("FRED sees " + FredVision.FredSees());
                                    FredSays();                                    
                                    break;
                                }
                            case "auto":
                                {
                                    

                                    break;
                                }
                        }                       
       
                        // Process the data sent by the client.
                        //data = data.ToUpper();

                        //byte[] msg = System.Text.Encoding.ASCII.GetBytes(data);

                        // Send back a response.
                        //stream.Write(msg, 0, msg.Length);
                        //Console.WriteLine("Sent: {0}", data);

                    } // while

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

        public static void FredSays()
        {                      
            var stream = Bass.CreateStream("fredSays.wav", 0, 0, BassFlags.AutoFree);
                
            if (stream != 0)
                Bass.ChannelPlay(stream); // Play the stream

            // Error creating the stream
            else Console.WriteLine("Error: {0}!", Bass.LastError);                            
        }

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
