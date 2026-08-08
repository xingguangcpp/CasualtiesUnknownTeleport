using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using UnityEngine;
using KrokoshaCasualtiesMP;

namespace KrokoshaTeleport
{
    public sealed class TeleportService
    {
        private readonly string configPath;
        private readonly Action<string> info;
        private readonly Action<string> warning;

        private readonly HashSet<string> insideTriggers =
            new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        public TeleportConfig Config { get; private set; }

        public bool IsHost
        {
            get { return Net.is_server && Net.is_host; }
        }

        public TeleportService(
            string path,
            Action<string> infoLogger,
            Action<string> warningLogger)
        {
            configPath = path;
            info = infoLogger;
            warning = warningLogger;
            Load();
        }

        public void Load()
        {
            try
            {
                if (!File.Exists(configPath))
                {
                    Config = new TeleportConfig();
                    EnsureCollections(Config);
                    SaveLocalOnly();
                    info?.Invoke("已创建 Teleport.json。");
                    return;
                }

                Config = JsonConvert.DeserializeObject<TeleportConfig>(
                    File.ReadAllText(configPath));

                if (Config == null)
                    Config = new TeleportConfig();

                EnsureCollections(Config);
                info?.Invoke("已加载 Teleport.json。");
            }
            catch (Exception ex)
            {
                Config = new TeleportConfig();
                EnsureCollections(Config);
                warning?.Invoke(
                    "加载 Teleport.json 失败：" + ex.Message);
            }
        }

        public void Save()
        {
            SaveLocalOnly();
        }

        private void SaveLocalOnly()
        {
            if (Config == null)
                Config = new TeleportConfig();

            EnsureCollections(Config);

            try
            {
                File.WriteAllText(
                    configPath,
                    JsonConvert.SerializeObject(
                        Config,
                        Formatting.Indented));
            }
            catch (Exception ex)
            {
                warning?.Invoke(
                    "保存 Teleport.json 失败：" + ex.Message);
            }
        }

        private static void EnsureCollections(
            TeleportConfig config)
        {
            if (config.PlayerPermissions == null)
                config.PlayerPermissions =
                    new Dictionary<string, bool>();

            if (config.OneToOne == null)
                config.OneToOne =
                    new List<OneToOnePoint>();

            if (config.Anchors == null)
                config.Anchors =
                    new List<AnchorPoint>();
        }

        public bool IsAllowed(NetPlayer player)
        {
            if (player == null ||
                Config == null ||
                !Config.Enabled)
            {
                return false;
            }

            if (player.is_host ||
                (player.is_local && player.clientId == 0))
            {
                return true;
            }

            bool allowed;

            return Config.PlayerPermissions.TryGetValue(
                player.clientId.ToString(),
                out allowed) && allowed;
        }

        public bool GetPlayerPermission(NetPlayer player)
        {
            if (player == null || Config == null)
                return false;

            if (player.is_host ||
                (player.is_local && player.clientId == 0))
            {
                return true;
            }

            bool allowed;

            return Config.PlayerPermissions.TryGetValue(
                player.clientId.ToString(),
                out allowed) && allowed;
        }

        public void SetPlayerPermission(
            NetPlayer player,
            bool allowed)
        {
            if (!IsHost ||
                player == null ||
                player.is_host)
            {
                return;
            }

            Config.PlayerPermissions[
                player.clientId.ToString()] = allowed;

            Save();
        }

        public bool TryGetAnchor(
            string name,
            out AnchorPoint anchor)
        {
            anchor = null;

            if (Config == null ||
                Config.Anchors == null ||
                string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            anchor = Config.Anchors.FirstOrDefault(
                x => x != null &&
                     x.Enabled &&
                     string.Equals(
                         x.Name,
                         name,
                         StringComparison.OrdinalIgnoreCase));

            return anchor != null;
        }

        public bool Teleport(
            NetPlayer player,
            Vector2 target)
        {
            if (!Net.is_server ||
                player == null ||
                Config == null ||
                !Config.Enabled ||
                !IsAllowed(player))
            {
                return false;
            }

            try
            {
                player.Server_TeleportCharacter(target);
                return true;
            }
            catch (Exception ex)
            {
                warning?.Invoke(
                    "传送失败：" + ex.Message);
                return false;
            }
        }

        public void UpdateAutomaticTeleports()
        {
            if (!Net.is_server ||
                Config == null ||
                !Config.Enabled ||
                NetPlayer.ClientIdToPlayerDict == null)
            {
                insideTriggers.Clear();
                return;
            }

            foreach (NetPlayer player in
                     NetPlayer.ClientIdToPlayerDict.Values)
            {
                if (player == null ||
                    player.body == null ||
                    !IsAllowed(player))
                {
                    continue;
                }

                string playerId =
                    player.clientId.ToString();

                bool currentlyInside = false;

                foreach (OneToOnePoint point in Config.OneToOne)
                {
                    if (point == null || !point.Enabled)
                        continue;

                    float radius = Mathf.Max(
                        0.01f,
                        point.Radius);

                    if (Vector2.Distance(
                            player.pos,
                            point.Trigger) > radius)
                    {
                        continue;
                    }

                    currentlyInside = true;

                    string key = playerId + "|" + point.Name;

                    if (insideTriggers.Contains(key))
                        continue;

                    insideTriggers.Add(key);
                    Teleport(player, point.Target);
                    break;
                }

                if (!currentlyInside)
                {
                    string prefix = playerId + "|";

                    insideTriggers.RemoveWhere(
                        x => x.StartsWith(
                            prefix,
                            StringComparison.OrdinalIgnoreCase));
                }
            }
        }

        public string GetAnchorList()
        {
            if (Config == null || Config.Anchors == null)
                return "没有可用的传送锚点。";

            string[] names = Config.Anchors
                .Where(x => x != null &&
                            x.Enabled &&
                            !string.IsNullOrWhiteSpace(x.Name))
                .Select(x => x.Name)
                .ToArray();

            return names.Length == 0
                ? "没有启用的传送锚点。"
                : string.Join("、", names);
        }

        public static string[] Parse(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
                return new string[0];

            List<string> result = new List<string>();
            bool quoted = false;
            char quote = '\0';
            string current = "";

            for (int i = 0; i < message.Length; i++)
            {
                char c = message[i];

                if (c == '"' || c == '\'')
                {
                    if (!quoted)
                    {
                        quoted = true;
                        quote = c;
                        continue;
                    }

                    if (c == quote)
                    {
                        quoted = false;
                        quote = '\0';
                        continue;
                    }
                }

                if (char.IsWhiteSpace(c) && !quoted)
                {
                    if (current.Length > 0)
                    {
                        result.Add(current);
                        current = "";
                    }

                    continue;
                }

                current += c;
            }

            if (current.Length > 0)
                result.Add(current);

            return result.ToArray();
        }
    }
}