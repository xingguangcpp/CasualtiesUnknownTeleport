using BepInEx;
using System.IO;
using UnityEngine;

namespace KrokoshaTeleport
{
    [BepInPlugin(
        "com.krokosha.teleport",
        "Krokosha Teleport",
        "1.2.0")]
    public sealed class TeleportPlugin : BaseUnityPlugin
    {
        private TeleportService service;
        private TeleportChatHandler chat;
        private TeleportGui gui;

        private void Awake()
        {
            string configPath = Path.Combine(
                Paths.PluginPath,
                "Teleport.json");

            service = new TeleportService(
                configPath,
                message => Logger.LogInfo(message),
                message => Logger.LogWarning(message));

            chat = new TeleportChatHandler(
                service,
                message => Logger.LogInfo(message));

            chat.Subscribe();

            gui = gameObject.AddComponent<TeleportGui>();
            gui.Initialize(service);

            Logger.LogInfo(
                "Krokosha Teleport 单端主机版已加载。\n" +
                "仅主机安装此插件。\n" +
                "客机聊天命令：\\\\tp list 或 \\\\tp 锚点名称。\n" +
                "按 F5 打开或关闭管理面板。 ");
        }

        private void Update()
        {
            if (service != null)
                service.UpdateAutomaticTeleports();
        }

        private void OnDestroy()
        {
            if (chat != null)
            {
                chat.Unsubscribe();
                chat = null;
            }

            if (gui != null)
            {
                Destroy(gui);
                gui = null;
            }

            service = null;
        }
    }
}