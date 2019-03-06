using CreateSpeakerProfile;
using NetCoreAudio;
using SpeechRecEnroll;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using TcpRaspServer.Utility;

namespace SpeakerRecognition
{
    static class VoiceSignature
    {
        public static Player player = new Player();
        public static string name = VarHolder.ProfileName;
        static string profileId = VarHolder.ProfileId;
        static HttpClient client = new HttpClient();

        public static async Task RecVoiceOne(string nameDesc)
        {
            VarHolder.ProfileName = nameDesc.Split(":")[0];
            VarHolder.ProfileDesc = nameDesc.Split(":")[1];
            Console.WriteLine("You need to record two audio notes to enroll your voice");
            Console.WriteLine("recording for only 10secs");
            await player.Record();
            System.Threading.Thread.Sleep(10000);
            await player.StopRecording();
            profileId = await CreateSpeaker.CreateProfile();
            VarHolder.ProfileId = profileId;
            EnrollSpeaker.EnrollVoice(profileId);
        }

        public static async Task RecVoiceTwo()
        {
            Console.WriteLine("recording for only 10secs");
            await player.Record();
            System.Threading.Thread.Sleep(10000);
            await player.StopRecording();
            EnrollSpeaker.EnrollVoice(VarHolder.ProfileId);
        }

        public static void ConfirmEnroll()
        {
            StreamWriter sw = new StreamWriter(@"speaker_recog.txt", true);
            sw.WriteLine(VarHolder.ProfileId + "," + VarHolder.ProfileName + "," + VarHolder.ProfileDesc);
            Console.WriteLine("Profile created!");
            sw.Close();
            VarHolder.ProfileId = "";
            VarHolder.ProfileName = "";
            VarHolder.ProfileDesc = "";
        }

        public static async void CancelEnroll()
        {
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3d0eb2b328f8409f896012b0eb163633"); // Environment.GetEnvironmentVariable("azure_SpkRec_Key", EnvironmentVariableTarget.User));

            var uri = "https://westus.api.cognitive.microsoft.com/spid/v1.0/identificationProfiles/" + VarHolder.ProfileId;

            var response = await client.DeleteAsync(uri);

            Console.WriteLine("profile erased!");
        }

        public static async Task DeleteProfile(string profileId)
        {
            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3d0eb2b328f8409f896012b0eb163633"); // Environment.GetEnvironmentVariable("azure_SpkRec_Key", EnvironmentVariableTarget.User));

            var uri = $"https://westus.api.cognitive.microsoft.com/spid/v1.0/identificationProfiles/{ profileId}?";

            var response = await client.DeleteAsync(uri);

            var responseText = response.Content.ReadAsStringAsync().Result;

            List<string> profiles = new List<string>();

            StreamReader sr = new StreamReader(@"speaker_recog.txt");
            while (!sr.EndOfStream) // extracts info line by line from txt file and puts it in List
            {
                profiles.Add(sr.ReadLine());
            }
            sr.Close();

            for(int i = 0; i < profiles.Count; i++) // removes the deleted profile id info from List
            {
                string[] profile = profiles[i].Split(',');
                if(profile[0] == profileId)
                {
                    profiles.RemoveAt(i);
                    break;
                }
            }

            StreamWriter sw = new StreamWriter(@"speaker_recog.txt", false); // ensures new entry overwrites items in old txt file

            for(int i = 0; i < profiles.Count; i++) // adds the info line by line back into txt file
            {
                sw.WriteLine(profiles[i]);
            }
            sw.Close();
        }
    }
}