using UnityEngine;
using System.Collections.Generic;

namespace Ogura.ColorNetworkMap
{

    // 各ノードのデータ情報を保持
    public class ColorNode
    {
        public Color NodeColor { get; set; }
        public Vector2 Position { get; private set; }
        public List<ColorNode> Children { get; private set; } = new List<ColorNode>();
        public bool IsExpanded { get; set; } = false;
        public int Level { get; private set; }
        public string Label { get; private set; }

        public ColorNode(Color color, Vector2 position, int level, string label = "")
        {
            NodeColor = color;
            Position = position;
            Level = level;
            Label = label;
        }
    }
}