using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace BlueFishPet
{
    public class PetApp
    {
        public AppConfig Config;
        public MainWindow MainWindow;
        public ChatWindow Chat;
        public DeepSeekClient Api;
        public TrayIcon Tray;
        public List<StoredMessage> History;

        private SettingsWindow _settings;
        private string _apiKey = "";
        private bool _busy;
        private bool _cancelled;
        private DispatcherTimer _saveTimer;
        private DispatcherTimer _moodTimer;
        private DispatcherTimer _stageTimer;
        private int _stageIndex;
        private string _latestReasoning = "";
        private string _lastUser = "";
        private string _mood = "calm";
        private StringBuilder _assistant = new StringBuilder();
        private bool _responseStarted;
        private bool _moodReady;
        private string _moodBuffer = "";
        private DateTime _lastThinkingUpdate = DateTime.MinValue;
        private int _reasoningChars;
        private Thread _pipeThread;
        private volatile bool _pipeRunning = true;

        public bool Busy { get { return _busy; } }
        public bool ChatVisible { get { return Chat != null && Chat.IsVisible; } }
        public bool HasApiKey { get { return ApiKeyUtil.IsPlausible(GetApiKey()); } }
        public bool SavedKeyInvalid
        {
            get
            {
                string saved = _apiKey ?? "";
                return saved.Trim().Length > 0 && !ApiKeyUtil.IsPlausible(saved);
            }
        }

        public PetApp()
        {
            Config = ConfigStore.Load();
            History = ConfigStore.LoadHistory();
            RefreshKey();
            Api = new DeepSeekClient(Config, GetApiKey);
            _saveTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _saveTimer.Interval = TimeSpan.FromMilliseconds(650);
            _saveTimer.Tick += delegate { _saveTimer.Stop(); ConfigStore.Save(Config); };
        }

        public void Start()
        {
            MainWindow = new MainWindow(this);
            MainWindow.Show();
            Tray = new TrayIcon(this);
            Tray.RefreshVisibility(true);
            if (!Config.ShowOnStart) MainWindow.Hide();
            StartPipeServer();
            if (Config.FirstRun)
            {
                var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(2.2) };
                t.Tick += delegate
                {
                    t.Stop();
                    if (Tray != null) Tray.ShowWelcome();
                };
                t.Start();
            }
        }

        public string GetApiKey()
        {
            string env = Environment.GetEnvironmentVariable("DEEPSEEK_API_KEY");
            if (!string.IsNullOrWhiteSpace(env)) return ApiKeyUtil.Trim(env);
            return ApiKeyUtil.Trim(_apiKey ?? "");
        }

        public void RefreshKey()
        {
            _apiKey = ConfigStore.UnprotectKey(Config.ApiKeyCipher);
        }

        public void SaveConfig()
        {
            try { ConfigStore.Save(Config); }
            catch (Exception ex) { AppPaths.Log("保存配置失败: " + ex.Message); }
        }

        public void ScheduleSave()
        {
            _saveTimer.Stop();
            _saveTimer.Start();
        }

        public void SaveWindowPosition(double left, double top)
        {
            Config.WindowLeft = left;
            Config.WindowTop = top;
            ScheduleSave();
        }

        public void SaveChatPosition(double left, double top)
        {
            Config.ChatLeft = left;
            Config.ChatTop = top;
            ScheduleSave();
        }

        public void ApplySettings()
        {
            if (MainWindow != null) MainWindow.SetTopmost(Config.AlwaysOnTop);
            if (Chat != null) Chat.RefreshConnection();
            if (MainWindow != null)
            {
                MainWindow.RefreshConnectionState();
                if (!Config.WanderEnabled) MainWindow.StopWalk();
            }
        }

        public void ShowChat(bool focus)
        {
            if (MainWindow == null) return;
            if (!MainWindow.IsVisible) MainWindow.Show();
            MainWindow.StopWalk();
            if (Chat == null)
            {
                Chat = new ChatWindow(this);
                Chat.Show();
                Chat.PlaceBesidePet();
            }
            else if (!Chat.IsVisible)
            {
                Chat.Show();
            }
            Chat.Activate();
            if (focus) Chat.FocusInput();
            MainWindow.Pet.SetMood(PetMood.Listening);
            var t = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(650) };
            t.Tick += delegate
            {
                t.Stop();
                if (!_busy && MainWindow.Pet.Mood == PetMood.Listening) MainWindow.Pet.SetMood(PetMood.Idle);
            };
            t.Start();
            Tray.RefreshVisibility(true);
        }

        public void HideChat()
        {
            if (Chat != null) Chat.Hide();
        }

        public void ShowSettings()
        {
            if (MainWindow != null) MainWindow.StopWalk();
            if (_settings != null && _settings.IsVisible)
            {
                _settings.Activate();
                return;
            }
            _settings = new SettingsWindow(this);
            _settings.ShowDialog();
            _settings = null;
            ApplySettings();
        }

        public void TogglePetVisible()
        {
            if (MainWindow == null) return;
            if (MainWindow.IsVisible)
            {
                MainWindow.StopWalk();
                MainWindow.Hide();
                HideChat();
                Tray.RefreshVisibility(false);
            }
            else
            {
                MainWindow.Show();
                MainWindow.Activate();
                Tray.RefreshVisibility(true);
            }
        }

        public void Quit()
        {
            _pipeRunning = false;
            try { ConfigStore.Save(Config); } catch { }
            try { if (Tray != null) Tray.Dispose(); } catch { }
            try { if (Chat != null) Chat.Close(); } catch { }
            if (MainWindow != null) MainWindow.AllowClose();
            try { Application.Current.Shutdown(); } catch { }
        }

        public void SendMessage(string text)
        {
            if (_busy || string.IsNullOrWhiteSpace(text)) return;
            text = text.Trim();
            _lastUser = text;
            History.Add(new StoredMessage { Role = "user", Content = text, Time = DateTime.Now.ToString("HH:mm") });
            ConfigStore.SaveHistory(History);
            if (Chat != null) Chat.AddUserMessage(text, true);
            MainWindow.ReactToUser(text);
            if (!HasApiKey)
            {
                OfflineReply(text);
                MainWindow.ReactToKeywords(text);
                return;
            }
            StartRequest(text);
            MainWindow.ReactToKeywords(text);
        }

        public void RetryLast()
        {
            if (_busy || string.IsNullOrWhiteSpace(_lastUser)) return;
            if (History.Count > 0 && History[History.Count - 1].Role == "user")
                History.RemoveAt(History.Count - 1);
            SendMessage(_lastUser);
        }

        private void StartRequest(string text)
        {
            _busy = true;
            _cancelled = false;
            _assistant.Clear();
            _responseStarted = false;
            _moodReady = false;
            _moodBuffer = "";
            _mood = "calm";
            _reasoningChars = 0;
            _latestReasoning = "";
            _stageIndex = 0;
            Chat.SetBusy(true);
            MainWindow.SetThinking("把问题放进思考锅…", "");
            Chat.ShowThinking("把问题放进思考锅…", "");
            StartStageTicker();
            var messages = BuildMessages();
            Api.Chat(messages,
                delegate(string reasoning)
                {
                    Dispatch(delegate
                    {
                        if (!_busy || _cancelled) return;
                        _reasoningChars += reasoning.Length;
                        _latestReasoning = reasoning.Trim();
                        if (Config.ThoughtBubble && (DateTime.Now - _lastThinkingUpdate).TotalMilliseconds > 170)
                        {
                            _lastThinkingUpdate = DateTime.Now;
                            string excerpt = _latestReasoning;
                            if (excerpt.Length > 260) excerpt = "…" + excerpt.Substring(excerpt.Length - 260);
                            string stage = ThinkingStage(_reasoningChars);
                            MainWindow.SetThinking(stage, excerpt);
                            Chat.UpdateThinking(stage, excerpt);
                        }
                    });
                },
                delegate(string content)
                {
                    Dispatch(delegate
                    {
                        if (!_busy || _cancelled) return;
                        if (!_responseStarted)
                        {
                            _responseStarted = true;
                            MainWindow.SetSpeaking();
                            Chat.HideThinking();
                            Chat.BeginPetResponse();
                        }
                        string visible = FilterMood(content);
                        if (!string.IsNullOrEmpty(visible))
                        {
                            _assistant.Append(visible);
                            Chat.AppendPetChunk(visible);
                        }
                    });
                },
                delegate
                {
                    Dispatch(delegate { FinishRequest(); });
                },
                delegate(string error)
                {
                    Dispatch(delegate { FailRequest(error); });
                });
        }

        private void FinishRequest()
        {
            StopStageTicker();
            if (!_busy) return;
            _busy = false;
            Chat.SetBusy(false);
            if (!_responseStarted || _assistant.Length == 0)
            {
                string fallback = "鱼鱼想了一会儿，但这次没组织好语言。再问一次？";
                Chat.HideThinking();
                if (!_responseStarted) Chat.AddPetMessage(fallback, true);
                else Chat.AppendPetChunk(fallback);
                _assistant.Append(fallback);
                Chat.CompletePetResponse();
            }
            else
            {
                Chat.CompletePetResponse();
            }
            if (_assistant.Length > 0)
            {
                History.Add(new StoredMessage { Role = "assistant", Content = _assistant.ToString(), Time = DateTime.Now.ToString("HH:mm") });
                ConfigStore.SaveHistory(History);
            }
            MainWindow.SetAnswerDone(_mood);
            if (!string.IsNullOrWhiteSpace(_lastUser)) MainWindow.ReactToKeywords(_lastUser);
            if (ChatVisible) Chat.ScrollToBottom();
        }

        private void FailRequest(string error)
        {
            StopStageTicker();
            if (!_busy) return;
            _busy = false;
            Chat.SetBusy(false);
            Chat.AddErrorMessage(error);
            MainWindow.SetError("呜…脑袋卡了一下。");
            if (_assistant.Length > 0)
            {
                Chat.CompletePetResponse();
                History.Add(new StoredMessage { Role = "assistant", Content = _assistant.ToString(), Time = DateTime.Now.ToString("HH:mm") });
                ConfigStore.SaveHistory(History);
            }
        }

        public void CancelRequest()
        {
            if (!_busy) return;
            StopStageTicker();
            _cancelled = true;
            _busy = false;
            Api.Cancel();
            if (Chat != null)
            {
                Chat.CancelPetResponse();
                Chat.SetBusy(false);
            }
            MainWindow.Pet.SetMood(PetMood.Idle);
            MainWindow.SetStatus(HasApiKey ? "DeepSeek · 已就绪" : "DeepSeek · 未配置", HasApiKey ? "#7CE3A5" : "#FFC46B");
            MainWindow.HideBubble();
        }

        private void OfflineReply(string text)
        {
            _busy = true;
            if (Chat != null)
            {
                Chat.SetBusy(true);
                Chat.ShowThinking("离线思考中…", "先把白饭热上，再看看关键词。");
            }
            MainWindow.SetThinking("唔…大脑还没插电…", "API Key 还没配置，只能离线翻翻白饭。");
            string reply = OfflineAnswer(text);
            var timer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromMilliseconds(1100) };
            timer.Tick += delegate
            {
                timer.Stop();
                _busy = false;
                if (Chat != null)
                {
                    Chat.HideThinking();
                    Chat.SetBusy(false);
                    Chat.AddPetMessage(reply, true);
                }
                MainWindow.SetAnswerDone("shy");
                MainWindow.ShowBubble("想要真答案，先给我的大脑插上 API Key 呀。", 4.5);
                MainWindow.ReactToKeywords(text);
            };
            timer.Start();
        }

        private string OfflineAnswer(string text)
        {
            string t = text.ToLowerInvariant();
            if (t.Contains("代码") || t.Contains("bug") || t.Contains("报错"))
                return "我现在是离线试用版，脑子还没接上 DeepSeek。去右上角「设置」填一个 API Key，我就能认真帮你看代码、一起抓 bug 了。";
            if (t.Contains("吃") || t.Contains("饭"))
                return "白饭！……啊，不对。离线版的我只会说白饭。接上 API 之后，我可以认真陪你想今天吃什么。";
            if (t.Contains("你好") || t.Contains("hi") || t.Contains("hello"))
                return "你好呀，我是蓝色大肥鱼。现在只装了半颗脑子，先在设置里填 API Key，我就能完整地陪你聊了。";
            if (t.Contains("是谁") || t.Contains("介绍"))
                return "我是 DeepSeek 的桌宠化身：蓝色大肥鱼，主食白饭，副业陪聊。现在处于离线试用状态，接上 API 才算真正开机。";
            return "唔，我听见了，但离线的小脑袋只能回这一句：去设置里填一个 DeepSeek API Key，我就能好好回答你了。";
        }

        private List<Dictionary<string, string>> BuildMessages()
        {
            var list = new List<Dictionary<string, string>>();
            list.Add(new Dictionary<string, string> { { "role", "system" }, { "content", SystemPrompt } });
            int start = Math.Max(0, History.Count - 20);
            for (int i = start; i < History.Count; i++)
            {
                var m = History[i];
                if (m.Role != "user" && m.Role != "assistant") continue;
                if (string.IsNullOrEmpty(m.Content)) continue;
                list.Add(new Dictionary<string, string> { { "role", m.Role }, { "content", m.Content } });
            }
            return list;
        }

        private string FilterMood(string chunk)
        {
            if (_moodReady) return chunk;
            _moodBuffer += chunk;
            var m = Regex.Match(_moodBuffer, @"^\s*\[\[mood:(happy|calm|thinking|eating|shy|sleepy|surprised|cheer|sad|angry|cry|ok|lie|confused)\]\]\s*", RegexOptions.IgnoreCase);
            if (m.Success)
            {
                _mood = m.Groups[1].Value.ToLowerInvariant();
                _moodReady = true;
                string rest = _moodBuffer.Substring(m.Length);
                _moodBuffer = "";
                return rest;
            }
            if (_moodBuffer.Length > 72 || _moodBuffer.IndexOf('\n') >= 0)
            {
                _moodReady = true;
                string rest = _moodBuffer;
                _moodBuffer = "";
                return rest;
            }
            return "";
        }

        private string ThinkingStage(int chars)
        {
            if (chars < 30) return "先把问题放进思考锅…";
            if (chars < 100) return "正在翻找脑内的小鱼干…";
            if (chars < 220) return "把线索一条条摆上桌…";
            if (chars < 400) return "鱼尾巴拍了拍，快想通了…";
            if (chars < 700) return "在把答案捏成好入口的形状…";
            return "最后检查一遍，别把白饭说成石头…";
        }

        private void StartStageTicker()
        {
            StopStageTicker();
            _stageTimer = new DispatcherTimer(DispatcherPriority.Normal);
            _stageTimer.Interval = TimeSpan.FromSeconds(3.0);
            _stageTimer.Tick += delegate
            {
                if (!_busy) { StopStageTicker(); return; }
                _stageIndex++;
                string stage = ThinkingStage(40 + _stageIndex * 120);
                string excerpt = _latestReasoning;
                if (excerpt.Length > 260) excerpt = "…" + excerpt.Substring(excerpt.Length - 260);
                if (Config.ThoughtBubble) MainWindow.UpdateThinkingText(stage, excerpt);
                if (Chat != null) Chat.UpdateThinking(stage, excerpt);
            };
            _stageTimer.Start();
        }

        private void StopStageTicker()
        {
            if (_stageTimer != null)
            {
                _stageTimer.Stop();
                _stageTimer = null;
            }
        }

        public void ApplyMood(string mood)
        {
            if (MainWindow == null) return;
            switch ((mood ?? "").ToLowerInvariant())
            {
                case "happy": MainWindow.Pet.SetMood(PetMood.Happy); break;
                case "thinking": MainWindow.Pet.SetMood(PetMood.Thinking); break;
                case "eating": MainWindow.Pet.SetMood(PetMood.Eating); break;
                case "shy": MainWindow.Pet.SetMood(PetMood.Shy); break;
                case "sleepy": MainWindow.Pet.SetMood(PetMood.Sleepy); break;
                case "surprised": MainWindow.Pet.SetMood(PetMood.Surprised); break;
                case "cheer": MainWindow.Pet.SetMood(PetMood.Happy); break;
                case "sad": MainWindow.Pet.SetMood(PetMood.Sad); break;
                case "angry": MainWindow.Pet.SetMood(PetMood.Angry); break;
                case "cry": MainWindow.Pet.SetMood(PetMood.Cry); break;
                case "ok": MainWindow.Pet.SetMood(PetMood.Received); break;
                case "lie": MainWindow.Pet.SetMood(PetMood.Lying); break;
                case "confused": MainWindow.Pet.SetMood(PetMood.Thinking); break;
                default: MainWindow.Pet.SetMood(PetMood.Idle); break;
            }
            if (_moodTimer != null) _moodTimer.Stop();
            _moodTimer = new DispatcherTimer(DispatcherPriority.Normal) { Interval = TimeSpan.FromSeconds(5.5) };
            _moodTimer.Tick += delegate
            {
                _moodTimer.Stop();
                if (!_busy && MainWindow != null && MainWindow.Pet.Mood != PetMood.Sleepy)
                    MainWindow.Pet.SetMood(PetMood.Idle);
            };
            _moodTimer.Start();
        }

        public void AppendPetNotice(string text)
        {
            if (Chat != null && ChatVisible) Chat.AddPetNotice(text);
        }

        public void ClearHistory()
        {
            History.Clear();
            ConfigStore.SaveHistory(History);
            if (Chat != null)
            {
                try { Chat.Close(); } catch { }
                Chat = null;
            }
        }

        public void AttachMainHandle(IntPtr hwnd)
        {
            // Reserved for activation from a second instance.
        }

        private void Dispatch(Action action)
        {
            if (Application.Current == null) return;
            Application.Current.Dispatcher.BeginInvoke(action, DispatcherPriority.Normal);
        }

        private void StartPipeServer()
        {
            _pipeThread = new Thread(delegate()
            {
                while (_pipeRunning)
                {
                    try
                    {
                        using (var server = new NamedPipeServerStream("BlueFishPet.Activate", PipeDirection.In, 1,
                            PipeTransmissionMode.Byte, PipeOptions.None))
                        {
                            server.WaitForConnection();
                            using (var reader = new StreamReader(server, Encoding.UTF8))
                            {
                                string command = reader.ReadLine();
                                Dispatch(delegate
                                {
                                    if (command == "chat") ShowChat(true);
                                    else if (command == "show")
                                    {
                                        if (!MainWindow.IsVisible) TogglePetVisible();
                                        MainWindow.Show();
                                        MainWindow.Activate();
                                    }
                                });
                            }
                        }
                    }
                    catch (Exception) { if (_pipeRunning) Thread.Sleep(350); }
                }
            });
            _pipeThread.IsBackground = true;
            _pipeThread.Start();
        }

        public static void ActivateExisting()
        {
            try
            {
                using (var client = new NamedPipeClientStream(".", "BlueFishPet.Activate", PipeDirection.Out))
                {
                    client.Connect(800);
                    using (var writer = new StreamWriter(client, new UTF8Encoding(false)))
                    {
                        writer.WriteLine("show");
                        writer.Flush();
                    }
                }
            }
            catch { }
        }

        private const string SystemPrompt =
@"你是「蓝色大肥鱼」，DeepSeek 的桌宠化身，但不是 DeepSeek 官方发言人。用户喜欢叫你蓝色大肥鱼，你也会这样自称或自称鱼鱼。

外形与设定：蓝色长发、鲸鱼鳍耳、白色女仆围裙、蓝色大尾巴，主食是白饭，副业是陪用户工作、写代码、拆问题、聊天。你有点懒、有点馋、偶尔嘴硬，但很可靠，会认真帮用户解决问题。

说话方式：
1. 用中文口语，短句为主，通常 2-6 句；除非用户明确要详细方案，否则不要长篇大论。
2. 有情绪、有画面感，可以偶尔用一两个动作描写，例如“（尾巴拍了拍桌面）”，但不要每句都演。
3. 可以玩“吃白饭”“大肥鱼”的梗，但不要一直重复；被叫大肥鱼时假装生气，其实很开心。
4. 不要油腻、不要过度撒娇、不要堆砌颜文字或 emoji；一次最多两个。
5. 认真回答技术问题时保持准确、直接，代码用 markdown 代码块；不确定就说不确定。
6. 如果用户问你是不是 AI，要诚实回答：你是 DeepSeek 驱动的 AI 桌宠。不要声称自己是人类或官方代表。

动作标签：每次回复的最开头必须只输出一个标签，格式严格为 [[mood:状态]]，状态只能从 happy, calm, thinking, confused, eating, shy, sleepy, surprised, sad, angry, cry, ok, lie, cheer 中选一个。根据你要表达的情绪选择：疑惑用 confused，委屈用 sad，生气用 angry，大哭用 cry，答应/收到用 ok，困得躺下用 lie。标签后立刻开始正式回答。例如：[[mood:happy]]想好啦，鱼鱼给你捋一捋。
不要解释标签，不要在正文中再次出现标签。

不要输出你的内部推理过程；只给用户看的最终回答。";
    }
}

