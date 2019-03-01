using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Json;
using System.Threading;

namespace SpeechRecEnroll
{
    static class EnrollSpeaker
    {
        public static HttpClient client = new HttpClient();
        static HttpResponseMessage response;

        public static async void EnrollVoice(string profileId)
        {

            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3d0eb2b328f8409f896012b0eb163633"); // Environment.GetEnvironmentVariable("azure_SpkRec_Key", EnvironmentVariableTarget.User));

            // Request parameters
            queryString["shortAudio"] = "true";
            var uri = "https://westus.api.cognitive.microsoft.com/spid/v1.0/identificationProfiles/" + profileId + "/enroll?" + queryString;

            //string path = Directory.GetCurrentDirectory() + "\\record.wav";
            byte[] byteData = File.ReadAllBytes(@"record.wav");

            // Request body
            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                Thread.Sleep(2000);

                try
                {
                    IEnumerable<string> responseStr = response.Headers.GetValues("Operation-Location");
                    string uriPath = responseStr.ElementAt<string>(0);

                    bool enrol = true;
                    string test = "";
                    while (enrol)
                    {
                        response = await client.GetAsync(uriPath);
                        var jsonResponse = response.Content.ReadAsStringAsync().Result;
                        JsonObject jsonDoc = (JsonObject)JsonValue.Parse(jsonResponse);


                        test = jsonDoc.ToString();
                        if (test.Contains("Enrolled"))
                        {
                            enrol = false;
                        }
                    }
                    Console.WriteLine(test);
                }
                catch
                {
                    Console.WriteLine("Too many requests");
                }
            }
        }

        /*private static byte[] ReadFully(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using (MemoryStream ms = new MemoryStream())
            {
                int read;
                while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
                {
                    ms.Write(buffer, 0, read);
                }
                return ms.ToArray();
            }
        }*/
    }
}