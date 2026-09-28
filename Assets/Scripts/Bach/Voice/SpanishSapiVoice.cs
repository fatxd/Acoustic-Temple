#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
#endif

internal static class SpanishSapiVoice
{
    public static bool TryFind(out string languageId, out string voiceName)
    {
        languageId = null;
        voiceName = null;

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        return TryFindInHive(CurrentUser, out languageId, out voiceName)
            || TryFindInHive(LocalMachine, out languageId, out voiceName);
#else
        return false;
#endif
    }

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
    private static readonly IntPtr CurrentUser = new IntPtr(unchecked((int)0x80000001));
    private static readonly IntPtr LocalMachine = new IntPtr(unchecked((int)0x80000002));
    private const int KeyRead = 0x20019;
    private const int NoMoreItems = 259;
    private const string VoiceTokens = @"SOFTWARE\Microsoft\Speech\Voices\Tokens";

    private static bool TryFindInHive(IntPtr hive, out string languageId, out string voiceName)
    {
        languageId = null;
        voiceName = null;

        if (RegOpenKeyEx(hive, VoiceTokens, 0, KeyRead, out IntPtr voices) != 0)
        {
            return false;
        }

        try
        {
            for (uint index = 0; ; index++)
            {
                StringBuilder token = new StringBuilder(256);
                uint tokenLength = (uint)token.Capacity;
                int result = RegEnumKeyEx(voices, index, token, ref tokenLength,
                    IntPtr.Zero, IntPtr.Zero, IntPtr.Zero, IntPtr.Zero);

                if (result == NoMoreItems)
                {
                    break;
                }

                if (result != 0 || RegOpenKeyEx(voices, token + @"\Attributes", 0, KeyRead, out IntPtr attributes) != 0)
                {
                    continue;
                }

                try
                {
                    string languages = ReadString(attributes, "Language");
                    if (languages == null)
                    {
                        continue;
                    }

                    foreach (string value in languages.Split(';'))
                    {
                        if (int.TryParse(value.Trim(), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int id)
                            && (id & 0x3FF) == 0x0A)
                        {
                            languageId = id.ToString("X", CultureInfo.InvariantCulture);
                            voiceName = ReadString(attributes, "Name") ?? token.ToString();
                            return true;
                        }
                    }
                }
                finally
                {
                    RegCloseKey(attributes);
                }
            }
        }
        finally
        {
            RegCloseKey(voices);
        }

        return false;
    }

    private static string ReadString(IntPtr key, string name)
    {
        uint size = 0;
        if (RegQueryValueEx(key, name, IntPtr.Zero, out uint type, null, ref size) != 0
            || (type != 1 && type != 2) || size == 0 || size > 16384)
        {
            return null;
        }

        byte[] data = new byte[(int)size];
        if (RegQueryValueEx(key, name, IntPtr.Zero, out type, data, ref size) != 0)
        {
            return null;
        }

        return Encoding.Unicode.GetString(data, 0, (int)size).TrimEnd('\0');
    }

    [DllImport("advapi32.dll", EntryPoint = "RegOpenKeyExW", CharSet = CharSet.Unicode)]
    private static extern int RegOpenKeyEx(IntPtr key, string subKey, int options, int desiredAccess, out IntPtr result);

    [DllImport("advapi32.dll", EntryPoint = "RegEnumKeyExW", CharSet = CharSet.Unicode)]
    private static extern int RegEnumKeyEx(IntPtr key, uint index, StringBuilder name, ref uint nameLength,
        IntPtr reserved, IntPtr className, IntPtr classLength, IntPtr lastWriteTime);

    [DllImport("advapi32.dll", EntryPoint = "RegQueryValueExW", CharSet = CharSet.Unicode)]
    private static extern int RegQueryValueEx(IntPtr key, string name, IntPtr reserved, out uint type, byte[] data, ref uint size);

    [DllImport("advapi32.dll")]
    private static extern int RegCloseKey(IntPtr key);
#endif
}
