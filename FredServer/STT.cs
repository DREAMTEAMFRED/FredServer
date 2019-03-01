using System;
using System.Collections.Generic;
using System.Text;
using System.Json;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace FredServer
{
    class STT
    {
        private static string fredHears = "";

        public async Task Hears()
        {
            await RecordAudio.RecordAudio.Record();
            Thread.Sleep(3000);
            await RecordAudio.RecordAudio.StopRecording();

            string requestUri = "https://eastus.stt.speech.microsoft.com/speech/recognition/conversation/cognitiveservices/v1?language=en-US";
            string contentType = @"audio/wav; codec=""audio/pcm""; samplerate=16000";

            /*
             * Input your own audio file or use read from a microphone stream directly.
             */
            string audioFile = @"record.wav";
            string responseString;
            FileStream fs = null;

            try
            {
                HttpWebRequest request = null;
                request = (HttpWebRequest)HttpWebRequest.Create(requestUri);
                request.SendChunked = true;
                request.Accept = @"application/json";
                request.Method = "POST";
                request.ProtocolVersion = HttpVersion.Version11;
                request.ContentType = contentType;
                request.Headers["Ocp-Apim-Subscription-Key"] = "9423ed79bbda4d3b8e0687363b7d9de2";

                using (fs = new FileStream(audioFile, FileMode.Open, FileAccess.Read))
                {
                    /*
                     * Open a request stream and write 1024 byte chunks in the stream one at a time.
                     */
                    byte[] buffer = null;
                    int bytesRead = 0;
                    using (Stream requestStream = request.GetRequestStream())
                    {
                        /*
                         * Read 1024 raw bytes from the input audio file.
                         */
                        buffer = new Byte[checked((uint)Math.Min(1024, (int)fs.Length))];
                        while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) != 0)
                        {
                            requestStream.Write(buffer, 0, bytesRead);
                        }

                        requestStream.Flush();
                    }

                    using (WebResponse response = request.GetResponse())
                    {
                        using (StreamReader sr = new StreamReader(response.GetResponseStream()))
                        {
                            responseString = sr.ReadToEnd();
                        }
                    }                                        
                }

                JsonObject jsonDoc = (JsonObject)JsonValue.Parse(responseString);
                jsonDoc.TryGetValue("DisplayText", out JsonValue text);
                fredHears = text.ToString();
                fredHears = fredHears.Substring(1, fredHears.Length - 2);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                Console.ReadLine();
            }
                        
        }// Hear

        public static string FredHears()
        {
            return fredHears;
        }


    }// class
    
}
