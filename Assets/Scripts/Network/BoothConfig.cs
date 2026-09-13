using System;
using System.IO;
using UnityEngine;

namespace BoothNetwork
{
    [SerializeField]
    public class BoothConfig
    {
        public string serverIp = "127.0.0.1";
        public int serverPort = 8080;
        public string roomId = "booth-1";

        private const string ConfigFileName = "config.json";

        /// <summary>
        /// 組み立て済みのWebSocket接続URLを取得
        /// </summary>
        public string GetWebSocketUrl()
        {
            return $"ws://{serverIp}:{serverPort}/ws?room={roomId}";
        }

        /// <summary>
        /// StreamingAssets/config.json を読み込む。存在しない場合は自動生成する。
        /// </summary>
        public static BoothConfig LoadOrCreate()
        {
            string dirPath = Application.streamingAssetsPath;
            string filePath = Path.Combine(dirPath, ConfigFileName);

            try
            {
                if (File.Exists(filePath))
                {
                    string json = File.ReadAllText(filePath);
                    BoothConfig config = JsonUtility.FromJson<BoothConfig>(json);
                    if (config != null)
                    {
                        Debug.Log($"[BoothNetwork] 設定をロードしました: {config.GetWebSocketUrl()}");
                        return config;
                    }
                }

                // ファイルが存在しない、またはパース失敗時はデフォルト値で新規生成
                BoothConfig defaultConfig = new BoothConfig();
                if (!Directory.Exists(dirPath))
                {
                    Directory.CreateDirectory(dirPath);
                }

                string defaultJson = JsonUtility.ToJson(defaultConfig, true);
                File.WriteAllText(filePath, defaultJson);
                Debug.LogWarning($"[BoothNetwork] {ConfigFileName} が見つからないため新規生成しました: {filePath}");
                return defaultConfig;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[BoothNetwork] 設定ファイルの読み込みに失敗しました: {ex.Message}");
                return new BoothConfig();
            }
        }
    }
}
