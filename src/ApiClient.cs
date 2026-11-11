using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;

namespace BlueFishPet
{
    public class DeepSeekClient
    {
        private readonly AppConfig _config;
        private readonly Func<string> _keyProvider;
        private readonly JavaScriptSerializer _json = new JavaScriptSerializer();
        private HttpWebRequest _request;
        private volatile bool _cancelled;
        private readonly object _gate = new object();

        public DeepSeekClient(AppConfig config, Func<string> keyProvider)
        {
            _config = config;
            _keyProvider = keyProvider;
            _json.MaxJsonLength = 16 * 1024 * 1024;
        }

        public string Endpoint
        {
            get
            {
                string b = (_config.ApiBase ?? "").Trim().TrimEnd('/');
                if (b.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return b;
                return b + "/chat/completions";
            }
        }

        public void Cancel()
        {
            _cancelled = true;
            lock (_gate)
            {
                try { if (_request != null) _request.Abort(); } catch { }
            }
        }

        public void Chat(List<Dictionary<string, string>> messages,
            Action<string> onReasoning, Action<string> onContent, Action onDone, Action<string> onError)
        {
            _cancelled = false;
            Task.Factory.StartNew(delegate
            {
                try
                {
                    string body = BuildBody(messages, false);
                    string text;
                    using (var response = (HttpWebResponse)CreateRequest(body).GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        text = reader.ReadToEnd();
                    }
                    if (_cancelled) return;
                    var root = _json.Deserialize<Dictionary<string, object>>(text);
                    if (root == null) throw new ApiException("接口返回了空内容。");
                    object errObj;
                    if (root.TryGetValue("error", out errObj))
                        throw new ApiException(ExtractError(errObj));

                    string reasoning = ExtractField(root, "reasoning_content");
                    string content = ExtractField(root, "content");
                    if (!string.IsNullOrEmpty(reasoning) && onReasoning != null) onReasoning(reasoning);
                    if (!string.IsNullOrEmpty(content) && onContent != null) onContent(content);
                    if (!_cancelled && onDone != null) onDone();
                }
                catch (Exception ex)
                {
                    if (_cancelled) return;
                    if (onError != null) onError(Describe(ex));
                }
                finally
                {
                    lock (_gate) { _request = null; }
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        public void Test(string apiKey, string apiBase, string model, Action<string> onResult)
        {
            Task.Factory.StartNew(delegate
            {
                try
                {
                    _cancelled = false;
                    string body = BuildBody(new List<Dictionary<string, string>>
                    {
                        new Dictionary<string, string> { { "role", "user" }, { "content", "只回复两个字：鱼在" } }
                    }, false, apiKey, 24, model);
                    using (var response = (HttpWebResponse)CreateRequest(body, apiKey, apiBase).GetResponse())
                    using (var stream = response.GetResponseStream())
                    using (var reader = new StreamReader(stream, Encoding.UTF8))
                    {
                        string text = reader.ReadToEnd();
                        var root = _json.Deserialize<Dictionary<string, object>>(text);
                        if (root == null) throw new ApiException("接口返回了空内容。");
                        object errObj;
                        if (root.TryGetValue("error", out errObj)) throw new ApiException(ExtractError(errObj));
                        string content = ExtractField(root, "content");
                        onResult("连接成功 · " + (string.IsNullOrEmpty(content) ? "模型已应答" : content.Trim()));
                    }
                }
                catch (Exception ex)
                {
                    onResult("连接失败 · " + Describe(ex));
                }
            }, CancellationToken.None, TaskCreationOptions.LongRunning, TaskScheduler.Default);
        }

        private HttpWebRequest CreateRequest(string body, string overrideKey = null, string baseOverride = null)
        {
            string key = ApiKeyUtil.Trim(overrideKey ?? _keyProvider());
            if (string.IsNullOrWhiteSpace(key)) throw new ApiException("还没有填 API Key。");
            string keyProblem = ApiKeyUtil.Problem(key);
            if (keyProblem != "")
                throw new ApiException(keyProblem + " 请打开设置重新粘贴 DeepSeek 的 Key。");
            string endpoint = Endpoint;
            if (!string.IsNullOrWhiteSpace(baseOverride))
            {
                string b = baseOverride.Trim().TrimEnd('/');
                endpoint = b.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase) ? b : b + "/chat/completions";
            }
            var req = (HttpWebRequest)WebRequest.Create(endpoint);
            req.Method = "POST";
            req.ContentType = "application/json; charset=utf-8";
            req.Accept = "application/json";
            req.Headers[HttpRequestHeader.Authorization] = "Bearer " + key;
            req.UserAgent = "BlueFishPet/1.0 (Windows; WPF)";
            req.Timeout = 30000;
            req.ReadWriteTimeout = 180000;
            req.KeepAlive = false;
            req.AllowAutoRedirect = true;
            req.AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate;
            try
            {
                req.Proxy = WebRequest.DefaultWebProxy;
                if (req.Proxy != null) req.Proxy.Credentials = CredentialCache.DefaultCredentials;
            }
            catch { }
            byte[] bytes = Encoding.UTF8.GetBytes(body);
            req.ContentLength = bytes.Length;
            lock (_gate) { _request = req; }
            using (var s = req.GetRequestStream()) s.Write(bytes, 0, bytes.Length);
            return req;
        }

        private string BuildBody(List<Dictionary<string, string>> messages, bool stream, string overrideKey = null, int maxTokens = 2048, string modelOverride = null)
        {
            var msgList = new List<object>();
            foreach (var m in messages) msgList.Add(new Dictionary<string, object> { { "role", m["role"] }, { "content", m["content"] } });
            var payload = new Dictionary<string, object>();
            string model = string.IsNullOrWhiteSpace(modelOverride) ? _config.Model : modelOverride;
            payload["model"] = string.IsNullOrWhiteSpace(model) ? "deepseek-chat" : model.Trim();
            payload["messages"] = msgList;
            payload["stream"] = stream;
            if (payload["model"].ToString().IndexOf("reasoner", StringComparison.OrdinalIgnoreCase) < 0)
                payload["temperature"] = 1.0;
            payload["max_tokens"] = maxTokens;
            return _json.Serialize(payload);
        }

        private static string ExtractField(Dictionary<string, object> root, string field)
        {
            if (root == null) return "";
            object choicesObj;
            if (!root.TryGetValue("choices", out choicesObj)) return "";
            var choices = choicesObj as IList;
            if (choices == null || choices.Count == 0) return "";
            var choice = choices[0] as Dictionary<string, object>;
            if (choice == null) return "";
            object msgObj;
            if (!choice.TryGetValue("message", out msgObj)) return "";
            var msg = msgObj as Dictionary<string, object>;
            if (msg == null) return "";
            object value;
            return msg.TryGetValue(field, out value) && value != null ? Convert.ToString(value) : "";
        }

        private static string ExtractError(object errObj)
        {
            var d = errObj as Dictionary<string, object>;
            if (d != null)
            {
                object msg;
                if (d.TryGetValue("message", out msg) && msg != null) return Convert.ToString(msg);
            }
            return Convert.ToString(errObj);
        }

        private string Describe(Exception ex)
        {
            var ae = ex as ApiException;
            if (ae != null) return ae.Message;
            var we = ex as WebException;
            if (we != null)
            {
                string detail = "";
                try
                {
                    var resp = we.Response as HttpWebResponse;
                    if (resp != null)
                    {
                        using (var reader = new StreamReader(resp.GetResponseStream(), Encoding.UTF8))
                            detail = reader.ReadToEnd();
                        if (!string.IsNullOrEmpty(detail))
                        {
                            try
                            {
                                var root = _json.Deserialize<Dictionary<string, object>>(detail);
                                object err;
                                if (root != null && root.TryGetValue("error", out err)) detail = ExtractError(err);
                            }
                            catch { }
                        }
                        int code = (int)resp.StatusCode;
                        if (code == 401) return "API Key 不对或已经失效，去设置里换一个吧。";
                        if (code == 402) return "账户余额不够啦，鱼鱼吃不起这顿 API 了。";
                        if (code == 429) return "请求太快被限流了，等几秒再试。";
                        if (code >= 500) return "DeepSeek 服务暂时卡住了，稍后再试。";
                    }
                }
                catch { }
                if (we.Status == WebExceptionStatus.Timeout) return "等太久啦，网络可能有点慢。";
                if (!string.IsNullOrEmpty(detail)) return "接口返回：" + Shorten(detail, 180);
                return "连不上 DeepSeek，检查一下网络或代理。";
            }
            if (ex is UriFormatException) return "API 地址格式不对。";
            return Shorten(ex.Message, 220);
        }

        private static string Shorten(string text, int max)
        {
            if (text == null) return "";
            text = text.Replace("\r", " ").Replace("\n", " ").Trim();
            return text.Length <= max ? text : text.Substring(0, max) + "…";
        }
    }

    public class ApiException : Exception
    {
        public ApiException(string message) : base(message) { }
    }
}
