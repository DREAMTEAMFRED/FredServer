using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace FredServer
{
    class FredQnA
    {
        public static HttpClient client = new HttpClient();
        static string appKey = Environment.GetEnvironmentVariable("Wolfram_App_Key", EnvironmentVariableTarget.User);
        static string wolframText = "";
        //public static ProgramRestSTT speech = new ProgramRestSTT();
        //static ProgramTTS tts = new ProgramTTS();
        public static string word = "";
        public static string cmd = "";
        public static bool test = false;
        private static string voice;

        public static async Task AskFred()
        {            
            TTS.Speak("I am listening...").Wait();

            await STT.Hears();
            voice = STT.FredHears().ToLower();                   

            if (voice == "nothing recorded")
            {
                cmd = "";
            }
            else
            {
                cmd = FredKB.Query(voice); // checks the KB for stored answers
            }

            cmd = cmd.Replace("\"", "");
            switch (cmd)
            {
                case "Fred Sees":
                    // run Fred Sees code
                    
                    break;
                case "Fred Reads":
                    // run Fred Reads code
                    break;
                case "Light On":
                    // run Light On code
                    break;
                case "Light Off":
                    // run Light Off code
                    break;
                default:
                    await FredKnows();
                    break;
            }
        }

        public static async Task FredKnows()
        {
            //string question = ProgramRestSTT.text;
            if (voice.Equals("nothing recorded"))
            {
                await TTS.Speak("I didn't hear a question...");
            }
            else
            {
                wolframText = FredKB.Query(voice);

                if (wolframText == "")
                {
                    await GetAnswer(voice);
                    TTS.Speak(wolframText).Wait();
                }
                else
                {
                    TTS.Speak(wolframText).Wait();
                }
            }
        }

        public static async Task GetAnswer(string search)
        {
            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue
                ("applicationException/json"));

            // grab 20 vids
            HttpResponseMessage response = await client.GetAsync($"https://api.wolframalpha.com/v1/result?i= {search}&appid={appKey}");

            if (response.IsSuccessStatusCode)
            {
                string Data = await response.Content.ReadAsStringAsync();
                wolframText = Data.Split(". ")[0];
                //Console.WriteLine(wolframText);
                if (wolframText.Length < 10)
                {
                    AskQuestion(search).Wait();
                }
                else
                {
                    Console.WriteLine(wolframText);
                }
                //Console.ReadLine();
            }
            else
            {
                await TTS.Speak("please rephrase your question");
            }
        }

        private static async Task AskQuestion(string question)
        {
            string newText = "";

            client.DefaultRequestHeaders.Accept.Clear();
            client.DefaultRequestHeaders.Accept.Add(new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue
                ("applicationException/json"));

            HttpResponseMessage response = await client.GetAsync($"http://api.duckduckgo.com/?q= {question} &format=json");

            if (response.IsSuccessStatusCode)
            {
                string Data = await response.Content.ReadAsStringAsync();
                JsonNinja ninja = new JsonNinja(Data);
                List<string> answer = ninja.GetInfoList("\"Abstract\"");
                List<string> rTopics = ninja.GetInfoList("\"RelatedTopics\"");
                ninja = new JsonNinja(rTopics[0]);
                List<string> texts = ninja.GetInfoList("\"Text\"");
                //Console.WriteLine("Answer: \n");
                if (answer[0] != "")
                {
                    string addStr = answer[0].Split('.')[0];
                    wolframText += "\n" + addStr;
                    Console.WriteLine(wolframText);
                }
                else
                {
                    if (texts.Count == 0)
                    {
                        //string data = ""; //this is so that if the search field is empty it does not show what was last searched
                        Console.WriteLine(wolframText);
                    }
                    else
                    {
                        int count = 1;
                        wolframText += "\nFound " + texts.Count + " other result(s)";
                        //Console.WriteLine("Found " + texts.Count + " results");
                        foreach (string text in texts)
                        {
                            newText += count + ": " + text.Split('.')[0].Replace("\\", "") + "\n";
                            //Console.WriteLine(count + ": " + newText + "\n");
                            count++;
                        }
                        wolframText += "\n" + newText;
                        Console.WriteLine(wolframText);
                    }
                }
            }
            else
            {
                string Data = ""; //this is so that if the search field is empty it does not show what was last searched
                Console.WriteLine(Data);
            }
        }
    }
}
