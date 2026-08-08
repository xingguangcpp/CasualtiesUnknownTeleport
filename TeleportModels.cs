using System;
using System.Collections.Generic;
using UnityEngine;

namespace KrokoshaTeleport
{
    [Serializable]
    public sealed class TeleportConfig
    {
        public bool Enabled = true;
        public bool ShowTeleportResults = true;

        public Dictionary<string, bool> PlayerPermissions =
            new Dictionary<string, bool>();

        public List<OneToOnePoint> OneToOne =
            new List<OneToOnePoint>();

        public List<AnchorPoint> Anchors =
            new List<AnchorPoint>();
    }

    [Serializable]
    public sealed class OneToOnePoint
    {
        public string Name = "被动传送点";
        public float TriggerX;
        public float TriggerY;
        public float TargetX;
        public float TargetY;
        public float Radius = 1f;
        public bool Enabled = true;

        public Vector2 Trigger
        {
            get { return new Vector2(TriggerX, TriggerY); }
        }

        public Vector2 Target
        {
            get { return new Vector2(TargetX, TargetY); }
        }
    }

    [Serializable]
    public sealed class AnchorPoint
    {
        public string Name = "传送锚点";
        public float TargetX;
        public float TargetY;
        public bool Enabled = true;

        public Vector2 Target
        {
            get { return new Vector2(TargetX, TargetY); }
        }
    }
}