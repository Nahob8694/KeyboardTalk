using Controllers.InterceptKeys;
using System.Runtime.InteropServices;
using System.Text;

namespace Controllers.UserSettings
{
    public enum EUserSettingsSection
    {
        Basic,
        Sounds
    }

    public static class UserSettings
    {
        private const int maxLength = 32767;
        private const string fileName = "UserSetting.ini";
        private static readonly string _filePath = Path.GetFullPath(fileName);

        public static bool IsExists
        {
            get
            {
                return File.Exists(_filePath);
            }
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern long WritePrivateProfileString(string section, string key, string? val, string filePath);

        public static void Write(EUserSettingsSection section, string key, string value)
        {
            WritePrivateProfileString(section.ToString(), key, value, _filePath);
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key, string def, StringBuilder retVal, int size, string filePath);

        public static string GetValue(EUserSettingsSection section, string key)
        {
            StringBuilder temp = new StringBuilder(4096);
            _ = GetPrivateProfileString(section.ToString(), key, string.Empty, temp, 4096, _filePath);
            return temp.ToString();
        }

        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileSection(string section, byte[] buffer, int size, string filePath);

        public static Dictionary<string, string> GetSection(EUserSettingsSection section)
        {
            byte[] buffer = new byte[maxLength];
            GetPrivateProfileSection(section.ToString(), buffer, maxLength, _filePath);

            Dictionary<string, string> keyValuePairs = new();

            string sectionData = Encoding.Unicode.GetString(buffer).Trim('\0');
            string[] keyValueArray = sectionData.Split('\0');

            foreach (string keyValueString in keyValueArray)
            {
                string[] keyValue = keyValueString.Split('=');
                if (keyValue.Length >= 2)
                {
                    string key = keyValue[0].Trim();
                    string value = keyValue[1].Trim();
                    keyValuePairs[key] = value;
                }
            }

            return keyValuePairs;
        }

        public static void Delete(EUserSettingsSection section, string key)
        {
            WritePrivateProfileString(section.ToString(), key, null, _filePath);
        }
    }
}
