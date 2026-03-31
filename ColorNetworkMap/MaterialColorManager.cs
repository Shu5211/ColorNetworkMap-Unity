using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

namespace Ogura.ColorNetworkMap
{
    // マテリアルプロパティ取得と、色のプレビュー
    public class MaterialColorManager
    {
        public List<string> ColorProperties { get; private set; } = new List<string>();
        public int PropertyIndex { get; set; }
        public bool HasBackup { get; private set; }
        public bool IsApplied { get; set; }

        private Color _originalColor;

        public void RefreshProperties()
        {
            ColorProperties.Clear();
            var renderer = Selection.activeGameObject?.GetComponent<Renderer>();
            if (renderer?.sharedMaterial == null) return;

            var shader = renderer.sharedMaterial.shader;
            for (int i = 0; i < shader.GetPropertyCount(); i++)
            {
                if (shader.GetPropertyType(i) == UnityEngine.Rendering.ShaderPropertyType.Color)
                {
                    ColorProperties.Add(shader.GetPropertyName(i));
                }
            }
        }

        public void PreviewColor(Color color)
        {
            if (Selection.activeGameObject == null || ColorProperties.Count == 0) return;
            var mat = Selection.activeGameObject.GetComponent<Renderer>()?.sharedMaterial;
            if (mat == null) return;

            if (!HasBackup)
            {
                _originalColor = mat.GetColor(ColorProperties[PropertyIndex]);
                HasBackup = true;
            }
            mat.SetColor(ColorProperties[PropertyIndex], color);
            IsApplied = false;
            SceneView.RepaintAll();
        }

        public void RevertColor()
        {
            if (!HasBackup || Selection.activeGameObject == null || ColorProperties.Count == 0 || PropertyIndex >= ColorProperties.Count) return;

            Selection.activeGameObject
                .GetComponent<Renderer>()?
                .sharedMaterial
                .SetColor(ColorProperties[PropertyIndex], _originalColor);

            HasBackup = false;
            SceneView.RepaintAll();
        }

        public void FinalizeColor(Color color)
        {
            var mat = Selection.activeGameObject?.GetComponent<Renderer>()?.sharedMaterial;
            if (mat == null) return;

            Undo.RecordObject(mat, "Apply Color");
            mat.SetColor(ColorProperties[PropertyIndex], color);
            EditorUtility.SetDirty(mat);
            HasBackup = false;
            IsApplied = true;
        }
    }
}