using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace Ogura.ColorNetworkMap
{

    // ノードの配置計算やネットワークの構築
    public class ColorNetworkLogic
    {
        public const float BASE_NODE_DISTANCE = 300f;
        public const int MAX_NODE_LEVEL = 8;

        public List<ColorNode> AllNodes { get; private set; } = new List<ColorNode>();
        public ColorNode RootNode { get; private set; }

        public void InitializeNetwork(float initialBrightness)
        {
            AllNodes.Clear();
            RootNode = new ColorNode(Color.white, Vector2.zero, 0, "Color") { IsExpanded = true };
            AllNodes.Add(RootNode);

            for (int i = 0; i < 6; i++)
            {
                float rad = i * 60f * Mathf.Deg2Rad;
                Vector2 pos = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * BASE_NODE_DISTANCE;
                ColorNode primaryNode = new ColorNode(Color.HSVToRGB(i / 6f, 0.7f, initialBrightness), pos, 1);
                RootNode.Children.Add(primaryNode);
                AllNodes.Add(primaryNode);
            }
        }

        public void ExpandAndMerge(ColorNode node, float brightness)
        {
            if (node.Level >= MAX_NODE_LEVEL)
            {
                Debug.LogWarning($"<color=orange>Limit Reached:</color> これ以上の階層は作成できません。");
                return;
            }

            node.IsExpanded = true;
            float dist = BASE_NODE_DISTANCE * Mathf.Pow(0.5f, node.Level);
            Vector2 dir = node.Position == Vector2.zero ? Vector2.right : node.Position.normalized;

            var parent = AllNodes.FirstOrDefault(n => n.Children.Contains(node));
            if (parent != null) dir = (node.Position - parent.Position).normalized;

            float baseAngle = Mathf.Round(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg / 60f) * 60f;

            for (int i = 1; i <= 5; i++)
            {
                float rad = (baseAngle + 180f + (i * 60f)) * Mathf.Deg2Rad;
                Vector2 targetPos = node.Position + new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * dist;
                ColorNode existing = AllNodes.FirstOrDefault(n => Vector2.Distance(n.Position, targetPos) < 1f);

                if (existing == null)
                {
                    float hue = (Mathf.Atan2(targetPos.y, targetPos.x) * Mathf.Rad2Deg + 360f) % 360f / 360f;
                    var newNode = new ColorNode(Color.HSVToRGB(hue, Mathf.Clamp(targetPos.magnitude / 500f, 0.3f, 1f), brightness), targetPos, node.Level + 1);
                    node.Children.Add(newNode);
                    AllNodes.Add(newNode);
                }
                else if (!node.Children.Contains(existing))
                {
                    node.Children.Add(existing);
                }
            }
        }

        public void UpdateBrightness(float newBrightness)
        {
            foreach (var n in AllNodes)
            {
                if (n.Level == 0) continue;
                Color.RGBToHSV(n.NodeColor, out float h, out float s, out _);
                n.NodeColor = Color.HSVToRGB(h, s, newBrightness);
            }
        }
    }
}