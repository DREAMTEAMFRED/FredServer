using System;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Web;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Threading;

namespace IdentVoice
{
    public static class VoiceIdentification
    {
        public static async Task<string> IdentVoice()
        {
            //bool ident = true;
            string test = "";
            int count = 0;
            string value = "";
            string profileIds = "";
            string speaker = "";
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3d0eb2b328f8409f896012b0eb163633"); // Environment.GetEnvironmentVariable("azure_SpkRec_Key", EnvironmentVariableTarget.User));

            // get all profiles call
            var uri = "https://westus.api.cognitive.microsoft.com/spid/v1.0/identificationProfiles?";

            var response = await client.GetAsync(uri);

            var responseText = response.Content.ReadAsStringAsync().Result;

            var deserializedProduct = JsonConvert.DeserializeObject(responseText);

            string deserResponse = deserializedProduct.ToString().Replace("\r\n", "").Replace("{", "").Replace("}", "").Replace("[", "").Replace("]", "").Replace("\"", "").Replace(" ", "").Replace("\n", "");

            string[] tester = deserResponse.Split(',');
            foreach(string tested in tester)
            {
                string[] testing = tested.Split(':');
                if(testing[0] == "identificationProfileId")
                {
                    profileIds += testing[1] + ",";
                }
            }

            try
            {
                profileIds = profileIds.Remove(profileIds.Length - 1); // remove the ',' from the end.
            }
            catch
            {
                Console.WriteLine("Too many request");
            }
         
            // Request parameters
            queryString["shortAudio"] = "true";
            uri = "https://westus.api.cognitive.microsoft.com/spid/v1.0/identify?identificationProfileIds=" + profileIds + "&" + queryString;

            //HttpResponseMessage response;

            //string path = Directory.GetCurrentDirectory() + "\\record.wav";

            // Request body
            byte[] byteData = File.ReadAllBytes(@"record.wav");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);

                //string responseStr = await response.Content.ReadAsStringAsync();

                try
                {
                    IEnumerable<string> responseStr = response.Headers.GetValues("Operation-Location");

                    string uriPath = responseStr.ElementAt<string>(0);

                    while (count < 3)
                    {
                        Thread.Sleep(1000); // timed delay to allow for response from API endpoint
                        count++;
                        response = await client.GetAsync(uriPath);
                        var jsonResponse = response.Content.ReadAsStringAsync().Result;
                        JObject jsonDoc = JObject.Parse(jsonResponse);
                        test = jsonDoc.ToString();
                        if (test.Contains("succeeded"))
                        {
                            value = jsonDoc["processingResult"].ToString();
                            jsonDoc = JObject.Parse(value);
                            value = jsonDoc["identifiedProfileId"].ToString();
                        }
                    }
                }
                catch
                {
                    Console.WriteLine("Too many request");
                }

                Console.WriteLine(value);

                var profiles = new Dictionary<string, string>();

                // retrieve stored 
                StreamReader sr = new StreamReader(@"speaker_recog.txt");
                while (!sr.EndOfStream)
                {
                    string[] lineInfo = sr.ReadLine().Split(',');
                    profiles.Add(lineInfo[0], lineInfo[1]);
                }

                sr.Close();

                try // unidentifiable speaker
                {
                    speaker = "Hello " + profiles[value];
                    Console.WriteLine("Welcome, " + profiles[value] + "!");
                }
                catch
                {
                    speaker = "Hello";
                }
                return speaker;
            }
        }
    }
}