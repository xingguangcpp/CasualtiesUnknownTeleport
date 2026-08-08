using System;
using System.Globalization;
using UnityEngine;
using KrokoshaCasualtiesMP;

namespace KrokoshaTeleport
{
    public sealed class TeleportGui : MonoBehaviour
    {
        private enum Page
        {
            Home,
            Anchors,
            PassivePoints,
            Players
        }

        private enum PickMode
        {
            None,
            AnchorTarget,
            PointTrigger,
            PointTarget
        }

        private TeleportService service;
        private Rect windowRect = new Rect(120f, 80f, 720f, 560f);
        private Page currentPage = Page.Home;
        private PickMode pickMode = PickMode.None;
        private bool visible;
        private int windowId;
        private Vector2 scrollPosition;
        private string status = "就绪";

        private string selectedAnchor = "";
        private string anchorName = "";
        private string anchorX = "0";
        private string anchorY = "0";

        private string selectedPoint = "";
        private string pointName = "";
        private string triggerX = "0";
        private string triggerY = "0";
        private string targetX = "0";
        private string targetY = "0";
        private string pointRadius = "1";

        private GUIStyle windowStyle;
        private GUIStyle titleStyle;
        private GUIStyle labelStyle;
        private GUIStyle buttonStyle;
        private GUIStyle textFieldStyle;

        public void Initialize(TeleportService teleportService)
        {
            service = teleportService;
            windowId = GetInstanceID();
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.F5))
            {
                visible = !visible;

                if (!visible)
                    pickMode = PickMode.None;
            }
        }

        private void OnGUI()
        {
            if (service == null || service.Config == null)
                return;

            CreateStyles();

            if (visible)
            {
                windowRect = GUI.Window(
                    windowId,
                    windowRect,
                    DrawWindow,
                    "传送管理",
                    windowStyle);
            }

            HandleWorldCoordinatePicking();
        }

        private void CreateStyles()
        {
            if (windowStyle != null)
                return;

            windowStyle = new GUIStyle(GUI.skin.window);
            windowStyle.fontSize = 18;
            windowStyle.padding = new RectOffset(14, 14, 38, 14);

            titleStyle = new GUIStyle(GUI.skin.label);
            titleStyle.fontSize = 20;
            titleStyle.fontStyle = FontStyle.Bold;

            labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = 15;
            labelStyle.normal.textColor = Color.white;

            buttonStyle = new GUIStyle(GUI.skin.button);
            buttonStyle.fontSize = 14;
            buttonStyle.fixedHeight = 32f;

            textFieldStyle = new GUIStyle(GUI.skin.textField);
            textFieldStyle.fontSize = 14;
            textFieldStyle.fixedHeight = 28f;
        }

        private void HandleWorldCoordinatePicking()
        {
            if (pickMode == PickMode.None ||
                !service.IsHost ||
                Event.current == null ||
                Event.current.type != EventType.MouseDown ||
                Event.current.button != 0)
            {
                return;
            }

            if (visible && windowRect.Contains(Event.current.mousePosition))
                return;

            Camera camera = Camera.main;

            if (camera == null)
            {
                status = "找不到主摄像机，无法选择坐标。";
                pickMode = PickMode.None;
                Event.current.Use();
                return;
            }

            float distance = Mathf.Abs(
                camera.transform.position.z);

            Vector3 screenPosition = new Vector3(
                Event.current.mousePosition.x,
                Screen.height - Event.current.mousePosition.y,
                distance);

            Vector3 worldPosition =
                camera.ScreenToWorldPoint(screenPosition);

            string x = worldPosition.x.ToString(
                CultureInfo.InvariantCulture);

            string y = worldPosition.y.ToString(
                CultureInfo.InvariantCulture);

            if (pickMode == PickMode.AnchorTarget)
            {
                anchorX = x;
                anchorY = y;
                status = "已选择锚点位置：(" + x + ", " + y + ")";
            }
            else if (pickMode == PickMode.PointTrigger)
            {
                triggerX = x;
                triggerY = y;
                status = "已选择触发位置：(" + x + ", " + y + ")";
            }
            else if (pickMode == PickMode.PointTarget)
            {
                targetX = x;
                targetY = y;
                status = "已选择目标位置：(" + x + ", " + y + ")";
            }

            pickMode = PickMode.None;
            Event.current.Use();
        }

        private void DrawWindow(int id)
        {
            GUILayout.BeginVertical();

            GUILayout.Label("传送管理", titleStyle);

            if (!service.IsHost)
            {
                GUILayout.Label("当前不是主机，只有主机可以管理传送。", labelStyle);
                GUILayout.EndVertical();
                GUI.DragWindow();
                return;
            }

            DrawSettings();
            DrawTabs();

            GUILayout.Space(8f);

            scrollPosition = GUILayout.BeginScrollView(scrollPosition);

            if (currentPage == Page.Home)
                DrawHomePage();
            else if (currentPage == Page.Anchors)
                DrawAnchorPage();
            else if (currentPage == Page.PassivePoints)
                DrawPassivePage();
            else
                DrawPlayersPage();

            GUILayout.EndScrollView();

            GUILayout.Space(6f);
            GUILayout.Label("状态：" + status, labelStyle);
            GUILayout.Label("按 F5 打开或关闭窗口", labelStyle);

            GUILayout.EndVertical();
            GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 34f));
        }

        private void DrawSettings()
        {
            GUILayout.BeginHorizontal();

            GUILayout.Label("传送功能：", labelStyle, GUILayout.Width(100f));

            bool enabled = GUILayout.Toggle(
                service.Config.Enabled,
                service.Config.Enabled ? "已开启" : "已关闭",
                labelStyle);

            if (enabled != service.Config.Enabled)
            {
                service.Config.Enabled = enabled;
                service.Save();
                status = enabled ? "已开启传送功能。" : "已关闭传送功能。";
            }

            GUILayout.Space(16f);

            bool showResults = GUILayout.Toggle(
                service.Config.ShowTeleportResults,
                "显示传送结果提示",
                labelStyle);

            if (showResults != service.Config.ShowTeleportResults)
            {
                service.Config.ShowTeleportResults = showResults;
                service.Save();
                status = showResults
                    ? "已显示传送结果提示。"
                    : "已隐藏传送结果提示。";
            }

            GUILayout.FlexibleSpace();

            if (GUILayout.Button("保存", buttonStyle, GUILayout.Width(90f)))
            {
                service.Save();
                status = "配置已保存。";
            }

            GUILayout.EndHorizontal();
        }

        private void DrawTabs()
        {
            GUILayout.BeginHorizontal();

            if (GUILayout.Button("主页", buttonStyle))
                currentPage = Page.Home;

            if (GUILayout.Button("锚点", buttonStyle))
                currentPage = Page.Anchors;

            if (GUILayout.Button("单对单", buttonStyle))
                currentPage = Page.PassivePoints;

            if (GUILayout.Button("玩家", buttonStyle))
                currentPage = Page.Players;

            GUILayout.EndHorizontal();
        }

        private void DrawHomePage()
        {
            GUILayout.Label("使用方式", titleStyle);
            GUILayout.Label("客机聊天输入：\\tp list", labelStyle);
            GUILayout.Label("客机聊天输入：\\tp 锚点名称", labelStyle);
            GUILayout.Label("主机验证权限后执行传送。", labelStyle);
            GUILayout.Label("单对单传送点会在玩家进入区域时自动触发。", labelStyle);
            GUILayout.Label(
                "传送结果提示：" +
                (service.Config.ShowTeleportResults ? "显示" : "隐藏"),
                labelStyle);
        }

        private void DrawAnchorPage()
        {
            GUILayout.Label("多对单传送锚点", titleStyle);

            foreach (AnchorPoint anchor in service.Config.Anchors)
            {
                if (anchor == null)
                    continue;

                GUILayout.BeginHorizontal();

                if (GUILayout.Button(
                        selectedAnchor == anchor.Name
                            ? "[已选择] " + anchor.Name
                            : anchor.Name,
                        buttonStyle))
                {
                    selectedAnchor = anchor.Name;
                    anchorName = anchor.Name;
                    anchorX = anchor.TargetX.ToString(
                        CultureInfo.InvariantCulture);
                    anchorY = anchor.TargetY.ToString(
                        CultureInfo.InvariantCulture);
                    status = "已选择锚点：" + anchor.Name;
                }

                GUILayout.Label(
                    "(" +
                    anchor.TargetX.ToString(CultureInfo.InvariantCulture) +
                    ", " +
                    anchor.TargetY.ToString(CultureInfo.InvariantCulture) +
                    ")",
                    labelStyle,
                    GUILayout.Width(220f));

                GUILayout.EndHorizontal();
            }

            GUILayout.Space(8f);
            GUILayout.Label("锚点名称", labelStyle);
            anchorName = GUILayout.TextField(anchorName, textFieldStyle);
            GUILayout.Label("目标 X", labelStyle);
            anchorX = GUILayout.TextField(anchorX, textFieldStyle);
            GUILayout.Label("目标 Y", labelStyle);
            anchorY = GUILayout.TextField(anchorY, textFieldStyle);

            if (GUILayout.Button(
                    pickMode == PickMode.AnchorTarget
                        ? "取消选择锚点位置"
                        : "鼠标选择锚点位置",
                    buttonStyle))
            {
                TogglePickMode(PickMode.AnchorTarget);
            }

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("保存锚点", buttonStyle))
                SaveAnchor();

            if (GUILayout.Button("删除选中锚点", buttonStyle))
                DeleteAnchor();

            GUILayout.EndHorizontal();
        }

        private void DrawPassivePage()
        {
            GUILayout.Label("单对单传送点", titleStyle);

            foreach (OneToOnePoint point in service.Config.OneToOne)
            {
                if (point == null)
                    continue;

                if (GUILayout.Button(
                        selectedPoint == point.Name
                            ? "[已选择] " + point.Name
                            : point.Name,
                        buttonStyle))
                {
                    selectedPoint = point.Name;
                    pointName = point.Name;
                    triggerX = point.TriggerX.ToString(
                        CultureInfo.InvariantCulture);
                    triggerY = point.TriggerY.ToString(
                        CultureInfo.InvariantCulture);
                    targetX = point.TargetX.ToString(
                        CultureInfo.InvariantCulture);
                    targetY = point.TargetY.ToString(
                        CultureInfo.InvariantCulture);
                    pointRadius = point.Radius.ToString(
                        CultureInfo.InvariantCulture);
                    status = "已选择传送点：" + point.Name;
                }
            }

            GUILayout.Space(8f);
            GUILayout.Label("传送点名称", labelStyle);
            pointName = GUILayout.TextField(pointName, textFieldStyle);

            GUILayout.Label("触发位置 X", labelStyle);
            triggerX = GUILayout.TextField(triggerX, textFieldStyle);
            GUILayout.Label("触发位置 Y", labelStyle);
            triggerY = GUILayout.TextField(triggerY, textFieldStyle);

            if (GUILayout.Button(
                    pickMode == PickMode.PointTrigger
                        ? "取消选择触发位置"
                        : "鼠标选择触发位置",
                    buttonStyle))
            {
                TogglePickMode(PickMode.PointTrigger);
            }

            GUILayout.Label("目标位置 X", labelStyle);
            targetX = GUILayout.TextField(targetX, textFieldStyle);
            GUILayout.Label("目标位置 Y", labelStyle);
            targetY = GUILayout.TextField(targetY, textFieldStyle);

            if (GUILayout.Button(
                    pickMode == PickMode.PointTarget
                        ? "取消选择目标位置"
                        : "鼠标选择目标位置",
                    buttonStyle))
            {
                TogglePickMode(PickMode.PointTarget);
            }

            GUILayout.Label("触发半径", labelStyle);
            pointRadius = GUILayout.TextField(pointRadius, textFieldStyle);

            GUILayout.BeginHorizontal();

            if (GUILayout.Button("保存传送点", buttonStyle))
                SavePassivePoint();

            if (GUILayout.Button("删除选中传送点", buttonStyle))
                DeletePassivePoint();

            GUILayout.EndHorizontal();
        }

        private void DrawPlayersPage()
        {
            GUILayout.Label("玩家权限管理", titleStyle);
            GUILayout.Label("只有允许的玩家可以使用聊天传送命令。", labelStyle);

            if (NetPlayer.ClientIdToPlayerDict == null)
            {
                GUILayout.Label("当前没有玩家信息。", labelStyle);
                return;
            }

            foreach (NetPlayer player in
                     NetPlayer.ClientIdToPlayerDict.Values)
            {
                if (player == null)
                    continue;

                bool allowed = service.GetPlayerPermission(player);

                GUILayout.BeginHorizontal();

                GUILayout.Label(
                    player.clientId + ":" + player.playername,
                    labelStyle,
                    GUILayout.Width(330f));

                GUILayout.Label(
                    allowed ? "允许使用" : "禁止使用",
                    labelStyle,
                    GUILayout.Width(100f));

                if (!player.is_host &&
                    GUILayout.Button(
                        allowed ? "禁止" : "允许",
                        buttonStyle,
                        GUILayout.Width(90f)))
                {
                    service.SetPlayerPermission(player, !allowed);
                    status = allowed
                        ? "已禁止该玩家使用传送。"
                        : "已允许该玩家使用传送。";
                }

                GUILayout.EndHorizontal();
            }
        }

        private void TogglePickMode(PickMode mode)
        {
            if (pickMode == mode)
            {
                pickMode = PickMode.None;
                status = "已取消坐标选择。";
            }
            else
            {
                pickMode = mode;
                status = "请将鼠标移到游戏世界，并左键确认坐标。";
            }
        }

        private void SaveAnchor()
        {
            float x;
            float y;

            if (string.IsNullOrWhiteSpace(anchorName) ||
                !TryParseFloat(anchorX, out x) ||
                !TryParseFloat(anchorY, out y))
            {
                status = "锚点名称或坐标格式不正确。";
                return;
            }

            AnchorPoint anchor = service.Config.Anchors.Find(
                item => item != null &&
                        string.Equals(
                            item.Name,
                            anchorName,
                            StringComparison.OrdinalIgnoreCase));

            if (anchor == null)
            {
                anchor = new AnchorPoint();
                service.Config.Anchors.Add(anchor);
            }

            anchor.Name = anchorName;
            anchor.TargetX = x;
            anchor.TargetY = y;
            anchor.Enabled = true;

            selectedAnchor = anchor.Name;
            service.Save();
            status = "锚点已保存。";
        }

        private void DeleteAnchor()
        {
            if (string.IsNullOrWhiteSpace(selectedAnchor))
            {
                status = "请先选择要删除的锚点。";
                return;
            }

            service.Config.Anchors.RemoveAll(
                item => item != null &&
                        string.Equals(
                            item.Name,
                            selectedAnchor,
                            StringComparison.OrdinalIgnoreCase));

            selectedAnchor = "";
            service.Save();
            status = "锚点已删除。";
        }

        private void SavePassivePoint()
        {
            float a;
            float b;
            float c;
            float d;
            float r;

            if (string.IsNullOrWhiteSpace(pointName) ||
                !TryParseFloat(triggerX, out a) ||
                !TryParseFloat(triggerY, out b) ||
                !TryParseFloat(targetX, out c) ||
                !TryParseFloat(targetY, out d) ||
                !TryParseFloat(pointRadius, out r))
            {
                status = "传送点名称、坐标或半径格式不正确。";
                return;
            }

            OneToOnePoint point = service.Config.OneToOne.Find(
                item => item != null &&
                        string.Equals(
                            item.Name,
                            pointName,
                            StringComparison.OrdinalIgnoreCase));

            if (point == null)
            {
                point = new OneToOnePoint();
                service.Config.OneToOne.Add(point);
            }

            point.Name = pointName;
            point.TriggerX = a;
            point.TriggerY = b;
            point.TargetX = c;
            point.TargetY = d;
            point.Radius = Mathf.Max(0.01f, r);
            point.Enabled = true;

            selectedPoint = point.Name;
            service.Save();
            status = "单对单传送点已保存。";
        }

        private void DeletePassivePoint()
        {
            if (string.IsNullOrWhiteSpace(selectedPoint))
            {
                status = "请先选择要删除的传送点。";
                return;
            }

            service.Config.OneToOne.RemoveAll(
                item => item != null &&
                        string.Equals(
                            item.Name,
                            selectedPoint,
                            StringComparison.OrdinalIgnoreCase));

            selectedPoint = "";
            service.Save();
            status = "传送点已删除。";
        }

        private static bool TryParseFloat(
            string text,
            out float value)
        {
            return float.TryParse(
                text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out value);
        }
    }
}