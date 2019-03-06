using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace UpdatedKB
{
    static class UpdateFredKB
    {
        static string host = "https://westus.api.cognitive.microsoft.com";
        static string service = "/qnamaker/v4.0";
        static string method = "/knowledgebases/";

        static HttpClient client = new HttpClient();

        // NOTE: Replace this with a valid subscription key.
        static string key = "209c1cf5f94f4392b1049da8abc4f59b";// Environment.GetEnvironmentVariable("updateKB_key", EnvironmentVariableTarget.User);

        // NOTE: Replace this with a valid knowledge base ID.
        static string kb = "85e578a7-bf44-4f12-b46e-26efa40b1653";// Environment.GetEnvironmentVariable("kb_Id", EnvironmentVariableTarget.User);

        static string new_kb = "";

        public static void UpdateKB(string[] question, string[] answer)
        {
           new_kb = @"
                    {
                    'add': {
                    'qnaList': [";

            for(int i = 0; i < question.Length; i++)
            {
                if(i == (question.Length - 1))
                {
                    new_kb += @"{
                                'id': 1,
                                'answer': '" + answer[i] + @"',
                                'source': 'Custom Editorial',
                                'questions': [
                                    '" + question[i] + @"'
                                ],
                                'metadata': []
                            }";
                }
                else
                {
                    new_kb += @"{
                                'id': 1,
                                'answer': '" + answer[i] + @"',
                                'source': 'Custom Editorial',
                                'questions': [
                                    '" + question[i] + @"'
                                ],
                                'metadata': []
                            }" + ",";
                }
            }

            new_kb += @"],
                            'delete': {
                                 'ids': [
                                        0
                                        ]
                                      }
                                      }";

            UpdateKBNow(kb, new_kb).Wait();
            PublishKB();
        }

        public struct Response
        {
            public HttpResponseHeaders headers;
            public string response;

            public Response(HttpResponseHeaders headers, string response)
            {
                this.headers = headers;
                this.response = response;
            }
        }

        static string PrettyPrint(string s)
        {
            return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(s), Formatting.Indented);
        }

        async static Task<Response> Patch(string uri, string body)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = new HttpMethod("PATCH");
                request.RequestUri = new Uri(uri);
                request.Content = new StringContent(body, Encoding.UTF8, "application/json");
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return new Response(response.Headers, responseBody);
            }
        }

        async static Task<Response> Get(string uri)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Get;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                var responseBody = await response.Content.ReadAsStringAsync();
                return new Response(response.Headers, responseBody);
            }
        }

        async static Task<Response> PostUpdateKB(string kb, string new_kb)
        {
            string uri = host + service + method + kb;
            Console.WriteLine("Calling " + uri + ".");
            return await Patch(uri, new_kb);
        }

        async static Task<Response> GetStatus(string operation)
        {
            string uri = host + service + operation;
            Console.WriteLine("Calling " + uri + ".");
            return await Get(uri);
        }

        async static Task UpdateKBNow(string kb, string new_kb)
        {
            var response = await PostUpdateKB(kb, new_kb);
            var operation = response.headers.GetValues("Location").First();
            Console.WriteLine(PrettyPrint(response.response));

            var done = false;
            while (true != done)
            {
                response = await GetStatus(operation);
                Console.WriteLine(PrettyPrint(response.response));

                var fields = JsonConvert.DeserializeObject<Dictionary<string, string>>(response.response);

                String state = fields["operationState"];
                if (state.CompareTo("Running") == 0 || state.CompareTo("NotStarted") == 0)
                {
                    var wait = response.headers.GetValues("Retry-After").First();
                    Console.WriteLine("Waiting " + wait + " seconds...");
                    Thread.Sleep(Int32.Parse(wait) * 1000);
                }
                else
                {
                    done = true;
                }
            }
        }

        async static void PublishKB()
        {
            var uri = host + service + method + kb;
            Console.WriteLine("Calling " + uri + ".");
            var response = await Post(uri);
            Console.WriteLine(PrettyPrint(response));
        }

        async static Task<string> Post(string uri)
        {
            using (var request = new HttpRequestMessage())
            {
                request.Method = HttpMethod.Post;
                request.RequestUri = new Uri(uri);
                request.Headers.Add("Ocp-Apim-Subscription-Key", key);

                var response = await client.SendAsync(request);
                if (response.IsSuccessStatusCode)
                {
                    return "{'result' : 'Success.'}";
                }
                else
                {
                    return await response.Content.ReadAsStringAsync();
                }
            }
        }
    }
}