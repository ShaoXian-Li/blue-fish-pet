using System;

namespace BlueFishPet
{
    public static class ApiKeyUtil
    {
        public static string Trim(string value)
        {
            if (string.IsNullOrEmpty(value)) return "";
            int start = 0;
            int end = value.Length - 1;
            while (start <= end && (char.IsWhiteSpace(value[start]) || value[start] == '\uFEFF' || char.IsControl(value[start]))) start++;
            while (end >= start && (char.IsWhiteSpace(value[end]) || value[end] == '\uFEFF' || char.IsControl(value[end]))) end--;
            if (end < start) return "";
            return value.Substring(start, end - start + 1);
        }

        public static bool IsPlausible(string value)
        {
            string key = Trim(value);
            if (key.Length < 8) return false;
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c < 33 || c > 126) return false;
            }
            return true;
        }

        public static string Problem(string value)
        {
            string key = Trim(value);
            if (key.Length == 0) return "还没有填 API Key。";
            for (int i = 0; i < key.Length; i++)
            {
                char c = key[i];
                if (c > 126) return "Key 里有中文或全角字符。这里要填 DeepSeek 的 sk-... Key，不是问题描述。";
                if (c < 33) return "Key 里有空格或控制字符，请重新粘贴完整的一行 Key。";
            }
            if (key.Length < 8) return "Key 太短了，看起来不像一条完整的 API Key。";
            return "";
        }
    }
}
