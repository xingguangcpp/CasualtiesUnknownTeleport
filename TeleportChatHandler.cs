using System;
using KrokoshaCasualtiesMP;

namespace KrokoshaTeleport
{
    public sealed class TeleportChatHandler
    {
        private readonly TeleportService service;
        private readonly Action<string> info;

        public TeleportChatHandler(
            TeleportService teleportService,
            Action<string> infoLogger)
        {
            service = teleportService;
            info = infoLogger;
        }

        public void Subscribe()
        {
            Chat.OnPlayerChatMessage +=
                OnPlayerChatMessage;

            info?.Invoke(
                "传送聊天命令监听已注册。");
        }

        public void Unsubscribe()
        {
            Chat.OnPlayerChatMessage -=
                OnPlayerChatMessage;
        }

        private void OnPlayerChatMessage(
            NetPlayer player,
            string message)
        {
            if (!Net.is_server ||
                player == null ||
                service == null ||
                service.Config == null ||
                string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            string[] args =
                TeleportService.Parse(message);

            if (args == null || args.Length == 0)
                return;

            string command =
                args[0].Trim();

            /*
             * 注意：
             * "\\tp" 在 C# 中代表实际的 \tp
             * 不能写成 "\\\\tp"，那代表实际的 \\tp。
             */
            bool isTeleportCommand =
                string.Equals(
                    command,
                    "\\tp",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    command,
                    "\\teleport",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    command,
                    "/tp",
                    StringComparison.OrdinalIgnoreCase) ||
                string.Equals(
                    command,
                    "/teleport",
                    StringComparison.OrdinalIgnoreCase);

            if (!isTeleportCommand)
                return;

            info?.Invoke(
                "收到传送命令：" +
                message +
                "，发送者：" +
                player.playername);

            if (args.Length < 2)
            {
                SendResult(
                    player,
                    "用法：\\tp list 或 \\tp 锚点名称");

                return;
            }

            if (!service.Config.Enabled)
            {
                SendResult(
                    player,
                    "主机已关闭传送功能。");

                return;
            }

            if (string.Equals(
                    args[1],
                    "list",
                    StringComparison.OrdinalIgnoreCase))
            {
                SendResult(
                    player,
                    service.GetAnchorList());

                return;
            }

            if (!service.IsAllowed(player))
            {
                SendResult(
                    player,
                    "你没有使用传送功能的权限。");

                return;
            }

            string anchorName =
                string.Join(
                    " ",
                    args,
                    1,
                    args.Length - 1);

            AnchorPoint anchor;

            if (!service.TryGetAnchor(
                    anchorName,
                    out anchor))
            {
                SendResult(
                    player,
                    "找不到传送锚点：" +
                    anchorName);

                return;
            }

            bool success =
                service.Teleport(
                    player,
                    anchor.Target);

            if (success)
            {
                SendResult(
                    player,
                    "已传送到：" +
                    anchor.Name);
            }
            else
            {
                SendResult(
                    player,
                    "传送失败。");
            }
        }

        private void SendResult(
            NetPlayer player,
            string message)
        {
            if (player == null ||
                string.IsNullOrWhiteSpace(message))
            {
                return;
            }

            if (service.Config.ShowTeleportResults)
            {
                player.Server_DoAlertSingle(
                    message,
                    false);
            }
            else
            {
                info?.Invoke(
                    "传送结果：" +
                    message);
            }
        }
    }
}