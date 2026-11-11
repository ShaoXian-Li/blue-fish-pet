using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Script.Serialization;

namespace BlueFishPet
{
    public class StoredMessage
    {
        public string Role { get; set; }
        public string Content { get; set; }
        public string Time { get; set; }
    }

    public class AppConfig
    {
        public string ApiBase { get; set; }
        public string Model { get; set; }
        public string ApiKeyCipher { get; set; }
        public bool AlwaysOnTop { get; set; }
        public bool ShowOnStart { get; set; }
        public bool ThoughtBubble { get; set; }
        public bool AutoStart { get; set; }
        public bool WanderEnabled { get; set; }
        public bool FirstRun { get; set; }
        public double WindowLeft { get; set; }
        public double WindowTop { get; set; }
        public double ChatLeft { get; set; }
        public double ChatTop { get; set; }
        public double Volume { get; set; }

        public AppConfig()
        {
            ApiBase = "https://api.deepseek.com";
            Model = "deepseek-chat";
            AlwaysOnTop = true;
            ShowOnStart = true;
            ThoughtBubble = true;
            WanderEnabled = true;
            FirstRun = true;
            WindowLeft = double.NaN;
            WindowTop = double.NaN;
            ChatLeft = double.NaN;
            ChatTop = double.NaN;
            Volume = 0.5;
        }
    }

    public static class AppPaths
    {
        private static string _dataDir;

        public static string DataDir
        {
            get
            {
                if (_dataDir == null) Init();
                return _dataDir;
            }
        }

        public static string ConfigFile { get { return Path.Combine(DataDir, "config.json"); } }
        public static string HistoryFile { get { return Path.Combine(DataDir, "history.json"); } }
        public static string LogFile { get { return Path.Combine(DataDir, "error.log"); } }

        private static void Init()
        {
            string custom = Environment.GetEnvironmentVariable("BLUEFISHPET_DATA");
            if (!string.IsNullOrWhiteSpace(custom))
            {
                _dataDir = custom;
                Directory.CreateDirectory(_dataDir);
                return;
            }
            string roaming = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "BlueFishPet");
            try
            {
                Directory.CreateDirectory(roaming);
                _dataDir = roaming;
                return;
            }
            catch { }
            string local = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "data");
            try
            {
                Directory.CreateDirectory(local);
                _dataDir = local;
                return;
            }
            catch { }
            _dataDir = Path.Combine(Path.GetTempPath(), "BlueFishPet");
            Directory.CreateDirectory(_dataDir);
        }

        public static void EnsureDir()
        {
            if (!Directory.Exists(DataDir)) Directory.CreateDirectory(DataDir);
        }

        public static void Log(string text)
        {
            try
            {
                EnsureDir();
                File.AppendAllText(LogFile,
                    "[" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + "] " + text + Environment.NewLine,
                    Encoding.UTF8);
            }
            catch { }
        }
    }

    public static class ConfigStore
    {
        private static readonly byte[] Entropy = Encoding.UTF8.GetBytes("BlueFishPet::DeepSeek::2026");
        private static readonly JavaScriptSerializer Json = new JavaScriptSerializer();

        public static AppConfig Load()
        {
            try
            {
                AppPaths.EnsureDir();
                if (!File.Exists(AppPaths.ConfigFile)) return new AppConfig();
                var text = File.ReadAllText(AppPaths.ConfigFile, Encoding.UTF8);
                var cfg = Json.Deserialize<AppConfig>(text);
                if (cfg == null) return new AppConfig();
                if (string.IsNullOrWhiteSpace(cfg.ApiBase)) cfg.ApiBase = "https://api.deepseek.com";
                if (string.IsNullOrWhiteSpace(cfg.Model)) cfg.Model = "deepseek-chat";
                return cfg;
            }
            catch (Exception ex)
            {
                AppPaths.Log("读取配置失败: " + ex);
                return new AppConfig();
            }
        }

        public static void Save(AppConfig cfg)
        {
            AppPaths.EnsureDir();
            if (double.IsNaN(cfg.WindowLeft) || double.IsInfinity(cfg.WindowLeft)) cfg.WindowLeft = -1;
            if (double.IsNaN(cfg.WindowTop) || double.IsInfinity(cfg.WindowTop)) cfg.WindowTop = -1;
            if (double.IsNaN(cfg.ChatLeft) || double.IsInfinity(cfg.ChatLeft)) cfg.ChatLeft = -1;
            if (double.IsNaN(cfg.ChatTop) || double.IsInfinity(cfg.ChatTop)) cfg.ChatTop = -1;
            var text = Json.Serialize(cfg);
            var tmp = AppPaths.ConfigFile + ".tmp";
            File.WriteAllText(tmp, text, Encoding.UTF8);
            if (File.Exists(AppPaths.ConfigFile))
            {
                try { File.Replace(tmp, AppPaths.ConfigFile, null); }
                catch { File.Copy(tmp, AppPaths.ConfigFile, true); File.Delete(tmp); }
            }
            else
            {
                File.Move(tmp, AppPaths.ConfigFile);
            }
        }

        public static void SaveHistory(List<StoredMessage> messages)
        {
            try
            {
                AppPaths.EnsureDir();
                if (messages.Count > 60) messages.RemoveRange(0, messages.Count - 60);
                File.WriteAllText(AppPaths.HistoryFile, Json.Serialize(messages), Encoding.UTF8);
            }
            catch (Exception ex) { AppPaths.Log("保存对话失败: " + ex.Message); }
        }

        public static List<StoredMessage> LoadHistory()
        {
            try
            {
                if (!File.Exists(AppPaths.HistoryFile)) return new List<StoredMessage>();
                var list = Json.Deserialize<List<StoredMessage>>(File.ReadAllText(AppPaths.HistoryFile, Encoding.UTF8));
                return list ?? new List<StoredMessage>();
            }
            catch { return new List<StoredMessage>(); }
        }

        public static string ProtectKey(string key)
        {
            if (string.IsNullOrEmpty(key)) return "";
            var raw = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), Entropy, DataProtectionScope.CurrentUser);
            return Convert.ToBase64String(raw);
        }

        public static string UnprotectKey(string cipher)
        {
            if (string.IsNullOrWhiteSpace(cipher)) return "";
            try
            {
                var raw = ProtectedData.Unprotect(Convert.FromBase64String(cipher), Entropy, DataProtectionScope.CurrentUser);
                return Encoding.UTF8.GetString(raw);
            }
            catch (Exception ex)
            {
                AppPaths.Log("解密 API Key 失败: " + ex.Message);
                return "";
            }
        }
    }
}

