using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ManagedBass;

namespace FredServer
{
    class TTS
    {        
        public async Task Speak(string text)
        {
            string accessToken;

            Authentication auth = new Authentication("https://eastus.api.cognitive.microsoft.com/sts/v1.0/issueToken", "9423ed79bbda4d3b8e0687363b7d9de2");
            try
            {
                accessToken = await auth.FetchTokenAsync().ConfigureAwait(false);
                Console.WriteLine("Successfully obtained an access token. \n");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Failed to obtain an access token.");
                Console.WriteLine(ex.ToString());
                Console.WriteLine(ex.Message);
                return;
            }

            string host = "https://eastus.tts.speech.microsoft.com/cognitiveservices/v1";

            string body = @"<speak version='1.0' xmlns='http://www.w3.org/2001/10/synthesis' xml:lang='en-US'>
              <voice name='Microsoft Server Speech Text to Speech Voice (en-US, BenjaminRUS)'><prosody rate='-10.00%'><break time='100ms' />" +
              text + "</prosody></voice></speak>";

            using (var client = new HttpClient())
            {
                using (var request = new HttpRequestMessage())
                {
                    // Set the HTTP method
                    request.Method = HttpMethod.Post;
                    // Construct the URI
                    request.RequestUri = new Uri(host);
                    // Set the content type header
                    request.Content = new StringContent(body, Encoding.UTF8, "application/ssml+xml");
                    // Set additional header, such as Authorization and User-Agent
                    request.Headers.Add("Authorization", "Bearer " + accessToken);
                    request.Headers.Add("Connection", "Keep-Alive");
                    // Update your resource name
                    request.Headers.Add("User-Agent", "TTS");
                    request.Headers.Add("X-Microsoft-OutputFormat", "riff-24khz-16bit-mono-pcm");
                    // Create a request
                    Console.WriteLine("Calling the TTS service. Please wait... \n");
                    using (var response = await client.SendAsync(request).ConfigureAwait(false))
                    {
                        response.EnsureSuccessStatusCode();
                        // Asynchronously read the response
                        using (var dataStream = await response.Content.ReadAsStreamAsync().ConfigureAwait(false))
                        {
                            //Console.WriteLine("Your speech file is being written to file...");
                            using (var fileStream = new FileStream(@"fredSays.wav", FileMode.Create, FileAccess.Write, FileShare.Write))
                            {
                                await dataStream.CopyToAsync(fileStream).ConfigureAwait(false);
                                fileStream.Close();
                            }
                        }
                    }
                }
            }
            FredSays();
        }// speak

        public static void FredSays()
        {            
            var stream = Bass.CreateStream("fredSays.wav", 0, 0, BassFlags.AutoFree);

            if (stream != 0)
                Bass.ChannelPlay(stream); // Play the stream

            // Error creating the stream
            else Console.WriteLine("Error: {0}!", Bass.LastError);
        }

    }
}
