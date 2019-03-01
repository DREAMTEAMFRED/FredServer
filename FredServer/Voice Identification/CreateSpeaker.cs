using System;
using System.Net.Http.Headers;
using System.Text;
using System.Net.Http;
using System.Web;
using MovieMarvel;
using System.Threading.Tasks;

namespace CreateSpeakerProfile
{
    class CreateSpeaker
    {
        public static async Task<string> CreateProfile()
        {
            var client = new HttpClient();
            var queryString = HttpUtility.ParseQueryString(string.Empty);

            // Request headers
            client.DefaultRequestHeaders.Add("Ocp-Apim-Subscription-Key", "3d0eb2b328f8409f896012b0eb163633"); // Environment.GetEnvironmentVariable("azure_SpkRec_Key", EnvironmentVariableTarget.User));

            var uri = "https://westus.api.cognitive.microsoft.com/spid/v1.0/identificationProfiles?" + queryString;

            HttpResponseMessage response;

            // Request body
            byte[] byteData = Encoding.UTF8.GetBytes("{\"locale\":\"en-us\",}");

            using (var content = new ByteArrayContent(byteData))
            {
                content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
                response = await client.PostAsync(uri, content);
                string responseText = response.Content.ReadAsStringAsync().Result;

                JsonNinja jNinja = new JsonNinja(responseText);

                string profileId = jNinja.GetVals()[0].ToString().Replace("\r\n", "").Replace("\"", "").Replace(" ", "");

                Console.WriteLine(profileId);

                return profileId;

                //string checkLocation = result.Headers.GetValues("operation-location").FirstOrDefault();
            }
        }
    }
}