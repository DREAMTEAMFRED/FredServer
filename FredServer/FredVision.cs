using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Json;
using MovieMarvel;
using TextToSPeechApp;

namespace FredServer
{
    public class FredVision
    {
        private static HttpClient client = new HttpClient();
        private static string data = null;
        private static JsonNinja jNinja;
        static TextToSpeech tts = new TextToSpeech();
        private static void SetData(string _data) { data = _data; }
        public string GetData() { return data; }

        // GetPerson()
        private static List<string> faces = new List<string>();
        private static List<string> names = new List<string>();
        //public void ClearNames() { this.names.Clear(); }
        public List<string> GetNames() { return names; }

        private string name;
        private void SetName(string name) { this.name = name; }
        public string GetName() { return name; }

        public static async Task GetVision(string type)
        {
            client.DefaultRequestHeaders.Clear();
            string serverIP = Program.GetIpAddress();
            var uri = "";
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers and parameters
            switch (type)
            {
                case "describe":
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "266478d1a0f44c8abb164af3c768a48d");
                        queryString["maxCandidates"] = "1";
                        uri = "https://eastus.api.cognitive.microsoft.com/vision/v1.0/describe?" + queryString;
                        break;
                    }
                case "read":
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "266478d1a0f44c8abb164af3c768a48d");
                        queryString["language"] = "unk";
                        queryString["detectOrientation "] = "true";
                        uri = "https://eastus.api.cognitive.microsoft.com/vision/v1.0/ocr?" + queryString;
                        break;
                    }                
                case "detect":
                    {
                        client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "e97b9e9e76314914b139b73a8a2148cb");
                        queryString["returnFaceId"] = "true";
                        queryString["returnFaceLandmarks"] = "false";
                        //queryString["returnFaceAttributes"] = "false";
                        uri = "https://eastus.api.cognitive.microsoft.com/face/v1.0/detect?" + queryString;
                        break;
                    }                
            }

            // download image from web cam  
            using (WebClient webClient = new WebClient())
            {
                webClient.DownloadFile("http://" + serverIP + ":8080/?action=snapshot", "fredSees.jpg");
            }
            byte[] img = File.ReadAllBytes("fredSees.jpg");

            HttpResponseMessage response;

            // Request body
            using (var content = new ByteArrayContent(img))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
                response = await client.PostAsync(uri, content);
            }

            if (response.IsSuccessStatusCode)
            {
                data = await response.Content.ReadAsStringAsync();
                SetData(data);
            }
            else
            {
                
            }

        }

        public static string FredSees()
        {
            string fredSees = "";
            JsonObject jsonDoc = (JsonObject)JsonValue.Parse(data);
            JsonArray jsonArray = (JsonArray)jsonDoc["description"]["captions"];

            foreach (JsonObject obj in jsonArray)
            {
                JsonValue text;
                obj.TryGetValue("text", out text);
                fredSees = text.ToString();
            }
            return fredSees;
        }

        public static string FredReads()
        {
            List<string> tempWords = new List<string>();
            List<string> words = new List<string>();

            jNinja = new JsonNinja(data);
            string filterCollection = jNinja.GetDetails("\"regions\"")[0]; // jNinja.GetInfo("\"regions\"");
            jNinja = new JsonNinja(filterCollection);
            filterCollection = jNinja.GetDetails("\"lines\"")[0];  // jNinja.GetInfo("\"lines\"");
            jNinja = new JsonNinja(filterCollection);
            List<string> filterCollections = jNinja.GetDetails("\"words\"");  // jNinja.GetInfoList("\"words\"");

            for (int i = 0; i < filterCollections.Count; i++)
            {
                jNinja = new JsonNinja(filterCollections[i]);
                tempWords = jNinja.GetDetails("\"text\""); // jNinja.GetInfoList("\"text\"");
                foreach (string word in tempWords)
                {
                    words.Add(word);
                }
            }

            string fredReads = string.Join(" ", words);

            return fredReads;
        }

        public static async Task DetectFace()
        {
            client.DefaultRequestHeaders.Clear();
            List<string> faceIDs = new List<string>();

            JsonArray jsonArray = (JsonArray)JsonValue.Parse(data);
            foreach (JsonObject obj in jsonArray)
            {
                JsonValue id;
                obj.TryGetValue("faceId", out id);
                faceIDs.Add(id.ToString());
            }

            var queryString = HttpUtility.ParseQueryString(string.Empty);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "e97b9e9e76314914b139b73a8a2148cb");

            var uri = "https://eastus.api.cognitive.microsoft.com/face/v1.0/identify?" + queryString;

            HttpResponseMessage response;

            // Request body
            if (faceIDs.Count > 0)
            {
                string faceID = "";
                if (faceIDs.Count != 1)
                {
                    foreach (string face in faceIDs)
                    {
                        faceID += face + ",";
                    }
                }
                else
                {
                    faceID = faceIDs[0];
                }

                byte[] byteData = Encoding.UTF8.GetBytes("{\"PersonGroupId\": \"1111\", \"faceIds\": [" +
                                                        faceID + "]," +
                                                        "\"maxNumOfCandidatesReturned\": 1," +
                                                        "\"confidenceThreshold\": 0.5}");

                using (var content = new ByteArrayContent(byteData))
                {
                    content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                    response = await client.PostAsync(uri, content);
                }

                if (response.IsSuccessStatusCode)
                {
                    data = await response.Content.ReadAsStringAsync();
                }
                else
                {
                    // do nothing yet
                }

                jNinja = new JsonNinja(data);
                List<string> filterCollections = jNinja.GetDetails("\"candidates\""); // jNinja.GetInfoList("\"candidates\"");
                for (int i = 0; i < filterCollections.Count; i++)
                {
                    jNinja = new JsonNinja(filterCollections[i]);
                    if (filterCollections[i] == "")
                        names.Add("dont recognize");
                    else
                        faces.Add(jNinja.GetDetails("\"personId\"")[0]); // jNinja.GetInfo("\"personId\"")
                }


                foreach (string face in faces)
                {
                    await GetPerson(face);
                }
                faces.Clear();
            }
            else
            {
                faces.Clear();
                names.Add("no face");
            }

        }//DectectFace

        public static async Task GetPerson(string personID)
        {
            client.DefaultRequestHeaders.Clear();
            string personsName;
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "e97b9e9e76314914b139b73a8a2148cb");

            if (personID != "")
            {
                personID = personID.Substring(1, 36);

                var uri = "https://eastus.api.cognitive.microsoft.com/face/v1.0/persongroups/1111/persons/" +
                    personID + "?" + queryString;

                var response = await client.GetAsync(uri);

                if (response.IsSuccessStatusCode)
                {
                    data = await response.Content.ReadAsStringAsync();
                    JsonObject jsonDoc = (JsonObject)JsonValue.Parse(data);
                    JsonValue name;
                    jsonDoc.TryGetValue("name", out name);
                    personsName = name.ToString();
                    personsName = personsName.Substring(1, personsName.Length - 2);
                    names.Add(personsName);
                }
                else
                {
                    // do nothing yet
                }

            }
            else
            {

                names.Add("dont recgonize");
            }

        }// GetPerson()

        public static async void GreetPerson()
        {
            await GetVision("detect");
            await DetectFace();

            if (names.Count > 1)
            {
                string sayNames = "";
                foreach (string name in names)
                {
                    sayNames += name + " and ";
                }
                sayNames = sayNames.Substring(0, sayNames.Length - 5);
                tts.TextToWords("TTS-Hello " + sayNames + "! How are you today?").Wait();
            }
            else
            {
                switch (names[0])
                {
                    case "no face":
                        {
                            tts.TextToWords("TTS-I dont see any faces to detect").Wait();
                            break;
                        }
                    case "dont recgonize":
                        {
                            tts.TextToWords("TTS-I dont recgonize any faces?").Wait();
                            break;
                        }
                    default:
                        {
                            string[] greeting = { "How are you today?", "whats up?", "What, you never heard a toy car talk before?" };
                            Random randNum = new Random();
                            randNum.Next(3);
                            tts.TextToWords("TTS-Hello " + names[0] + "! How are you today?").Wait();
                            break;
                        }
                }
            }
            names.Clear();
        }
    }
}
