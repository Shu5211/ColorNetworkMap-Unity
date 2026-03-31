using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Linq;

namespace Ogura.ColorNetworkMap
{
    // エディタウィンドウ 
    public class ColorMapWindow : EditorWindow
    {
        private const float ANIM_DURATION = 0.6f;
        private const float SLIDER_AREA_WIDTH = 45f;
        private const float TOP_MARGIN = 21f;

        [MenuItem("Tool/Color Network Map")]
        public static void Open() => GetWindow<ColorMapWindow>("Color Network Map");

        
        private ColorNetworkLogic _networkLogic = new ColorNetworkLogic();
        private MaterialColorManager _materialManager = new MaterialColorManager();

        private ColorNode _selectedNode;
        private List<ColorNode> _sortedNodes = new List<ColorNode>();
        
        // アニメーション用変数
        private Vector2 _viewOffset, _targetOffset, _startOffset;
        private float _zoomScale = 1f, _targetZoom = 1f, _startZoom = 1f;
        private float _animTime = 1f; 
        private double _lastTime;
        private float _brightness = 0.9f;

        private void OnEnable() 
        { 
            if (_networkLogic.AllNodes.Count == 0) InitializeNetwork(); 
            _materialManager.RefreshProperties(); 
        }
        
        private void OnDisable() => _materialManager.RevertColor();
        
        private void OnSelectionChange() 
        { 
            _materialManager.RevertColor(); 
            _selectedNode = null;
            _materialManager.RefreshProperties(); 
            Repaint(); 
        }

        private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);

        private void InitializeNetwork()
        {
            _brightness = 0.9f;
            _selectedNode = null;
            _networkLogic.InitializeNetwork(_brightness);
            UpdateSortedNodes();

            _targetOffset = _viewOffset = Vector2.zero; 
            _targetZoom = _zoomScale = 1f;
            _animTime = 1f;
        }

        private void UpdateSortedNodes()
        {
            _sortedNodes = _networkLogic.AllNodes.OrderByDescending(n => n.Level).ToList();
        }

        private void UpdateAnimation()
        {
            if (_animTime >= 1f) return;

            double deltaTime = EditorApplication.timeSinceStartup - _lastTime;
            _lastTime = EditorApplication.timeSinceStartup;
            
            _animTime = Mathf.Min(1f, _animTime + (float)deltaTime / ANIM_DURATION);
            float t = EaseOutCubic(_animTime);

            _viewOffset = Vector2.LerpUnclamped(_startOffset, _targetOffset, t);
            _zoomScale = Mathf.LerpUnclamped(_startZoom, _targetZoom, t);
        }

        private void OnGUI()
        {
            UpdateAnimation();
            Vector2 center = position.size * 0.5f;
            HandleInput();

            DrawGrid(center);
            DrawConnections(center);

            foreach (var node in _sortedNodes) 
            {
                DrawNode(node, center);
            }

            DrawToolbar();
            DrawBrightnessSlider();
            Repaint();
        }

        private void DrawGrid(Vector2 center)
        {
            float width = position.width;
            float height = position.height;
            DrawGridLines(45f * _zoomScale, 0.07f);

            void DrawGridLines(float spacing, float opacity)
            {
                if (spacing < 5f) return; 
                Handles.color = new Color(1, 1, 1, opacity);
                float offsetX = (_viewOffset.x * _zoomScale + center.x) % spacing;
                float offsetY = (_viewOffset.y * _zoomScale + center.y) % spacing;

                for (float x = offsetX; x < width; x += spacing)
                    Handles.DrawLine(new Vector2(x, 0), new Vector2(x, height));

                for (float y = offsetY; y < height; y += spacing)
                    Handles.DrawLine(new Vector2(0, y), new Vector2(width, y));
            }
        }

        private void DrawToolbar()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                EditorGUI.BeginChangeCheck();
                var props = _materialManager.ColorProperties.ToArray();
                _materialManager.PropertyIndex = EditorGUILayout.Popup(_materialManager.PropertyIndex, props, GUILayout.Width(120));
                if (EditorGUI.EndChangeCheck()) _materialManager.RevertColor();

                GUI.backgroundColor = new Color(0.4f, 0.83f, 1.0f);
                if (GUILayout.Button("Ref.", EditorStyles.toolbarButton, GUILayout.Width(40))) _materialManager.RefreshProperties();
                
                GUILayout.Space(10);
                GUI.backgroundColor = (_selectedNode != null && !_materialManager.IsApplied) ? Color.green : Color.gray;
                
                if (GUILayout.Button("Apply", EditorStyles.toolbarButton, GUILayout.Width(70)) && _selectedNode != null) 
                {
                    _materialManager.FinalizeColor(_selectedNode.NodeColor);
                }

                GUI.backgroundColor = _materialManager.HasBackup ? new Color(1, 0.2f, 0.2f) : Color.gray;
                if (GUILayout.Button("Cancel", EditorStyles.toolbarButton, GUILayout.Width(70))) 
                {
                    _materialManager.RevertColor();
                    _selectedNode = null;
                }
                
                GUILayout.FlexibleSpace();

                GUI.backgroundColor = Color.white;
                if (GUILayout.Button("Reset View", EditorStyles.toolbarButton, GUILayout.Width(80))) StartTransition(Vector2.zero, 1f);

                GUI.backgroundColor = new Color(1f, 0.3f, 0.3f);
                if (GUILayout.Button("Clear All", EditorStyles.toolbarButton, GUILayout.Width(80)) && EditorUtility.DisplayDialog("Clear", "Reset network?", "Yes", "No")) 
                {
                    InitializeNetwork();
                }
                GUI.backgroundColor = Color.white;
            }
        }

        private void DrawBrightnessSlider()
        {
            Rect areaRect = new Rect(position.width - SLIDER_AREA_WIDTH, TOP_MARGIN, SLIDER_AREA_WIDTH, position.height - TOP_MARGIN);
            EditorGUI.DrawRect(areaRect, new Color(0, 0, 0, 1f));
            
            GUIStyle labelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleCenter, normal = { textColor = Color.white } };
            GUI.Label(new Rect(position.width - SLIDER_AREA_WIDTH, 30, SLIDER_AREA_WIDTH, 20), "VAL", labelStyle);

            Rect sliderRect = new Rect(position.width - 28, 55, 10, position.height - 100);
            
            EditorGUI.BeginChangeCheck();
            float newBrightness = GUI.VerticalSlider(sliderRect, _brightness, 1.0f, 0.01f);
            if (EditorGUI.EndChangeCheck())
            {
                _brightness = newBrightness;
                _networkLogic.UpdateBrightness(_brightness);
                
                if (_selectedNode != null) _materialManager.PreviewColor(_selectedNode.NodeColor);
            }
        }

        private void DrawConnections(Vector2 center)
        {
            foreach (var node in _networkLogic.AllNodes)
            {
                Vector2 start = center + (_viewOffset + node.Position) * _zoomScale;
                foreach (var child in node.Children)
                {
                    Handles.color = new Color(0.7f, 0.7f, 0.7f, 0.7f);
                    Handles.DrawLine(start, center + (_viewOffset + child.Position) * _zoomScale, 0.7f);
                }
            }
        }

        private void DrawNode(ColorNode node, Vector2 center)
        {
            Vector2 screenPos = center + (_viewOffset + node.Position) * _zoomScale;
            float nodeScale = (node.Level == 0) ? 1.0f : Mathf.Pow(0.5f, node.Level - 1);
            float size = Mathf.Max(85f * nodeScale * _zoomScale, 4f);
            float radius = size * 0.5f;

            if (node == _selectedNode)
            {
                Handles.color = Color.white;
                Handles.DrawWireDisc(screenPos, Vector3.forward, radius * 1.15f);
                Handles.color = new Color(1, 1, 1, 0.45f);
                Handles.DrawWireDisc(screenPos, Vector3.forward, radius * 1.23f);
            }

            Handles.color = new Color(0, 0, 0, 0.4f);
            Handles.DrawWireDisc(screenPos, Vector3.forward, radius);
            Handles.color = node.NodeColor;
            Handles.DrawSolidDisc(screenPos, Vector3.forward, radius);

            bool isTarget = (node.Level == 0) || (_zoomScale > 0.7f / nodeScale);

            if (isTarget && size > 15f)
            {
                string displayTag = !string.IsNullOrEmpty(node.Label) ? node.Label : $"#{ColorUtility.ToHtmlStringRGB(node.NodeColor)}";
                int dynamicFontSize = Mathf.Max(6, Mathf.RoundToInt(10 * nodeScale * _zoomScale));
                var style = new GUIStyle(EditorStyles.label) 
                { 
                    alignment = TextAnchor.MiddleCenter, 
                    fontSize = Mathf.Min(dynamicFontSize, 24),
                    fontStyle = (node.Level == 0) ? FontStyle.Bold : FontStyle.Normal,
                    clipping = TextClipping.Overflow
                };
                style.normal.textColor = node.NodeColor.grayscale > 0.5f ? new Color(0.1f, 0.1f, 0.1f) : Color.white;
                Rect labelRect = new Rect(screenPos.x - radius * 1.5f, screenPos.y - radius, size * 1.5f, size);
                GUI.Label(labelRect, displayTag, style);
            }

            if (Event.current.type == EventType.MouseDown && 
                Event.current.mousePosition.y > TOP_MARGIN && 
                Event.current.mousePosition.x < position.width - SLIDER_AREA_WIDTH && 
                Vector2.Distance(Event.current.mousePosition, screenPos) < radius)
            {
                if (_selectedNode != node) _materialManager.IsApplied = false;
                _selectedNode = node;

                StartTransition(-node.Position, Mathf.Clamp(1.75f / nodeScale, 1f, 500f));

                if (!node.IsExpanded) 
                {
                    _networkLogic.ExpandAndMerge(node, _brightness);
                    UpdateSortedNodes();
                }
                
                _materialManager.PreviewColor(node.NodeColor);
                Event.current.Use();
            }
        }

        private void StartTransition(Vector2 targetPos, float targetZoom)
        {
            _startOffset = _viewOffset;
            _startZoom = _zoomScale;
            _targetOffset = targetPos;
            _targetZoom = targetZoom;
            _animTime = 0f;
            _lastTime = EditorApplication.timeSinceStartup;
        }

        private void HandleInput()
        {
            if (Event.current.mousePosition.y <= TOP_MARGIN || Event.current.mousePosition.x > position.width - SLIDER_AREA_WIDTH) return;
            
            if (Event.current.type == EventType.MouseDrag && Event.current.button == 1) 
            { 
                _targetOffset += Event.current.delta / _zoomScale; 
                _viewOffset = _targetOffset; 
                _animTime = 1f; 
                Event.current.Use(); 
            }
            
            if (Event.current.type == EventType.ScrollWheel) 
            { 
                _targetZoom = Mathf.Clamp(_targetZoom - Event.current.delta.y * 0.05f, 0.1f, 128f);
                _zoomScale = _targetZoom;
                _animTime = 1f;
                Event.current.Use(); 
            }
        }
    }
}