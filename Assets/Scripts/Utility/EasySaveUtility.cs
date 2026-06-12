using System.IO;
using QFramework;
using UnityEngine;

/// <summary>
/// 运行态跨会话存档：优先使用 Easy Save 3，EditMode/初始化失败时回退到 persistentDataPath 文件。
/// 仅保存可 JSON 序列化的纯数据，不保存 UnityEngine.Object 引用。
/// </summary>
public sealed class EasySaveUtility : ISaveUtility
{
    public const string SaveFileName = "table_nine_run.es3";
    private const string FallbackFolderName = "table_nine_save";

    private static bool sEs3Available = true;
    private readonly string mFallbackRoot;

    public EasySaveUtility()
    {
        mFallbackRoot = Path.Combine(Application.persistentDataPath, FallbackFolderName);
        Directory.CreateDirectory(mFallbackRoot);
    }

    public void SaveString(string key, string value)
    {
        if (TryEs3Save(key, value))
        {
            return;
        }

        File.WriteAllText(GetFallbackPath(key), value ?? string.Empty);
    }

    public bool TryLoadString(string key, out string value)
    {
        if (TryEs3Load(key, out value))
        {
            return true;
        }

        var path = GetFallbackPath(key);
        if (!File.Exists(path))
        {
            value = null;
            return false;
        }

        value = File.ReadAllText(path);
        return !string.IsNullOrEmpty(value);
    }

    public void DeleteKey(string key)
    {
        if (sEs3Available)
        {
            try
            {
                if (ES3.KeyExists(key, SaveFileName))
                {
                    ES3.DeleteKey(key, SaveFileName);
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogWarning($"[EasySaveUtility] ES3 delete failed, using fallback. {exception.Message}");
                sEs3Available = false;
            }
        }

        var path = GetFallbackPath(key);
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static bool TryEs3Save(string key, string value)
    {
        if (!sEs3Available)
        {
            return false;
        }

        try
        {
            ES3.Save(key, value, SaveFileName);
            return true;
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[EasySaveUtility] ES3 save failed, using fallback. {exception.Message}");
            sEs3Available = false;
            return false;
        }
    }

    private static bool TryEs3Load(string key, out string value)
    {
        value = null;
        if (!sEs3Available)
        {
            return false;
        }

        try
        {
            if (!ES3.KeyExists(key, SaveFileName))
            {
                return false;
            }

            value = ES3.Load<string>(key, SaveFileName);
            return !string.IsNullOrEmpty(value);
        }
        catch (System.Exception exception)
        {
            Debug.LogWarning($"[EasySaveUtility] ES3 load failed, using fallback. {exception.Message}");
            sEs3Available = false;
            return false;
        }
    }

    private string GetFallbackPath(string key)
    {
        var safeKey = key.Replace(':', '_').Replace('/', '_').Replace('\\', '_');
        return Path.Combine(mFallbackRoot, safeKey + ".txt");
    }
}
