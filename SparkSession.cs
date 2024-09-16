using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace QQBot { 
    public class SparkSession
    {
        public SparkApi SparkApi { get; set; }
        public List<JObject> Text { get; set; } = new List<JObject>();
        private CancellationTokenSource cst { get; set; }

        public SparkSession(string appId, string apiSecret, string apiKey, SparkVersions version, string prompt)
        {
            
            SparkApi = new SparkApi(appId, apiSecret, apiKey, version);
            Text.Add(new JObject { ["role"] = "system", ["content"] = prompt });
        }

        public async Task<string> Ask(string question)
        {
            CheckLen(GetText("user", question));
            cst = new CancellationTokenSource();
            var response = await SparkApi.Ask(Text, cst.Token);
            bool status = response.Item1;
            string answer = response.Item2;
            if (status)
            {
                GetText("assistant", answer);
                answer = response.Item2;
            }
            else
            {
                answer = "似乎出了点问题，请稍后再试。";
            }

            return answer;
        }

        public List<JObject> GetText(string role, string content)
        {
            var jsonCon = new JObject
            {
                ["role"] = role,
                ["content"] = content
            };
            Text.Add(jsonCon);
            return Text;
        }

        public int GetLength(List<JObject> text)
        {
            int length = 0;
            foreach (var content in text)
            {
                var temp = content["content"].ToString();
                length += temp.Length;
            }
            return length;
        }

        public void CheckLen(List<JObject> text)
        {
            while (GetLength(text) > 8000)
            {
                text.RemoveAt(0);
            }
        }

        public string GetHistory()
        {
            //序列化 Text
            var jsonText = JsonConvert.SerializeObject(Text);
            return jsonText;
        }
        
    }
}
