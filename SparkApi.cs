using Newtonsoft.Json.Linq;
using System.Net.WebSockets;
using System.Security.Cryptography;
using System.Text;

namespace QQBot
{
    public class SparkApi
    {
        private string _appId;
        private string _apiSecret;
        private string _apiKey;
        private SparkVersions _version;
        private ClientWebSocket? _webSocketClient;
        private string url = "";
        private string domain = "";

        public SparkApi(string appId, string apiSecret, string apiKey, SparkVersions version = SparkVersions.general)
        {
            this._apiKey = apiKey;
            this._apiSecret = apiSecret;
            this._appId = appId;
            this._version = version;

            switch (this._version)
            {
                case SparkVersions.general:
                    this.url = "ws://spark-api.xf-yun.com/v1.1/chat";
                    this.domain = "general";
                    break;
                case SparkVersions.V1_5:
                    this.url = "ws://spark-api.xf-yun.com/v1.1/chat";
                    this.domain = "general";
                    break;
                case SparkVersions.V2_0:
                    this.url = "ws://spark-api.xf-yun.com/v2.1/chat";
                    this.domain = "generalv2";
                    break;
                case SparkVersions.V3_0:
                    this.url = "ws://spark-api.xf-yun.com/v3.1/chat";
                    this.domain = "generalv3";
                    break;
            }
        }
        private string GetAuthUrl(string baseUrl, string apiSecret, string apiKey)
        {
            string date = DateTime.UtcNow.ToString("r");
            Uri uri = new Uri(baseUrl);
            var str = $"host: {uri.Host}\ndate: {date}\nGET {uri.LocalPath} HTTP/1.1";

            //使用apisecret,HMACSHA256算法加密str
            var sha256Bytes = new HMACSHA256(Encoding.UTF8.GetBytes(apiSecret)).ComputeHash(Encoding.UTF8.GetBytes(str));
            var sha256Str = Convert.ToBase64String(sha256Bytes);
            var authorization = $"api_key=\"{apiKey}\",algorithm=\"hmac-sha256\",headers=\"host date request-line\",signature=\"{sha256Str}\"";

            //date要做url处理
            date = Uri.EscapeDataString(date);
            string newUrl = $"ws://{uri.Host}{uri.LocalPath}?authorization={Convert.ToBase64String(Encoding.UTF8.GetBytes(authorization))}&date={date}&host={uri.Host}";
            return newUrl;
        }
        /// <summary>
        /// 询问问题，流式调用response
        /// 返回结果表示调用成功还是失败，如果调用失败，则返回失败原因
        /// </summary>
        /// <param name="question"></param>
        /// <param name="response"></param>
        /// <returns></returns>
        public async Task<(bool, string)> Ask(List<JObject> questions, CancellationToken token)
        {
            try
            {
                var newUrl = GetAuthUrl(url, this._apiSecret, this._apiKey);
                this._webSocketClient = new ClientWebSocket();
                await this._webSocketClient.ConnectAsync(new Uri(newUrl), token);

                var request = new
                {
                    header = new
                    {
                        app_id = this._appId,
                        uid = "123"
                    },
                    parameter = new
                    {
                        chat = new
                        {
                            this.domain,
                            temperature = 0.8,
                            max_tokens = 2048,
                            top_k = 5,

                            auditing = "default"
                        }
                    },
                    payload = new
                    {
                        message = new
                        {
                            text = questions
                        }
                    }
                };
                var jsonStr = Newtonsoft.Json.JsonConvert.SerializeObject(request);

                await this._webSocketClient.SendAsync(new ArraySegment<byte>(Encoding.UTF8.GetBytes(jsonStr)), WebSocketMessageType.Text, true, token);

                var recvBuffer = new byte[1024];

                string answer = "";
                while (true)
                {
                    WebSocketReceiveResult result = await this._webSocketClient.ReceiveAsync(new ArraySegment<byte>(recvBuffer), token);
                    if (result.CloseStatus.HasValue) return (true, "");
                    if (result.MessageType == WebSocketMessageType.Text)
                    {
                        string recvMsg = Encoding.UTF8.GetString(recvBuffer, 0, result.Count);
                        var response = JObject.Parse(recvMsg);

                        if ((int)response["header"]["code"] != 0)
                        {
                            return (false, (string)response["header"]["message"]);
                        }

                        answer += (string)response["payload"]["choices"]["text"][0]["content"];

                        if ((int)response["payload"]["choices"]["status"] == 2) // 最后一个消息
                        {
                            return (true, answer);
                        }

                    }
                    else if (result.MessageType == WebSocketMessageType.Close)
                    {
                        return (false, "连接关闭");
                    }
                }
            }
            catch (Exception e)
            {
                return (false, e.Message);
            }
            finally
            {
                await this._webSocketClient?.CloseAsync(WebSocketCloseStatus.NormalClosure, "client raise close request", token);
            }
        }
        public async void Close()
        {
            if (_webSocketClient != null)
            {
                await _webSocketClient.CloseAsync(WebSocketCloseStatus.NormalClosure, "正常关闭", new CancellationToken());
            }
        }
    }
    public enum SparkVersions
    {
        general,
        V1_5,
        V2_0,
        V3_0,

    }
}