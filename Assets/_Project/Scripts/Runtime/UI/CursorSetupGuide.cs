using UnityEngine;

namespace Antventure.UI
{
    /// <summary>
    /// 光标设置指南 - 添加到CursorManager同一个GameObject上
    /// </summary>
    [System.Serializable]
    public class CursorSetupGuide : MonoBehaviour
    {
        [Header("📋 光标设置指南")]
        [TextArea(10, 20)]
        [SerializeField] private string setupInstructions = 
@"🎯 光标设置步骤：

1. 设置光标纹理：
   • Hand Cursor: 拖拽 hand.png 文件
   • Default Cursor: 拖拽 cursor.png 文件

2. 可用的光标文件位置：
   • Assets/_Project/Art/UI/Cursor/hand.png
   • Assets/_Project/Art/UI/Cursor/cursor.png
   • Assets/Thoth/Cartoon UI Pack/Material/Cursor_Hand1.png

3. 光标纹理设置：
   • 选中PNG文件
   • Texture Type = Cursor
   • 勾选 Read/Write Enabled
   • 点击 Apply

4. 热点位置调整：
   • Hand Hotspot: (10, 2) - 手指尖位置
   • Default Hotspot: (0, 0) - 箭头尖端

5. 测试：
   • 运行游戏
   • 悬停按钮查看光标变化
   • 查看Console日志确认光标切换

🔧 如果不工作：
   • 确保 Use Custom Cursors 已勾选
   • 检查光标纹理是否正确设置
   • 调整热点位置";

        [Header("🔧 快速设置按钮")]
        [SerializeField] private bool showQuickSetup = true;

        private void OnValidate()
        {
            // 在Inspector中显示设置指南
        }

        [ContextMenu("自动设置光标纹理")]
        public void AutoSetupCursors()
        {
            CursorManager cursorManager = GetComponent<CursorManager>();
            if (cursorManager == null)
            {
                Debug.LogError("请先添加CursorManager组件！");
                return;
            }

            #if UNITY_EDITOR
            // 尝试自动加载光标纹理
            Texture2D handTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/hand.png");
            Texture2D defaultTexture = UnityEditor.AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/_Project/Art/UI/Cursor/cursor.png");

            if (handTexture != null)
            {
                Debug.Log("✅ 找到手型光标: " + handTexture.name);
                // 通过反射设置私有字段（仅在编辑器中）
                var handField = typeof(CursorManager).GetField("handCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                handField?.SetValue(cursorManager, handTexture);
            }
            else
            {
                Debug.LogWarning("❌ 未找到手型光标文件");
            }

            if (defaultTexture != null)
            {
                Debug.Log("✅ 找到默认光标: " + defaultTexture.name);
                var defaultField = typeof(CursorManager).GetField("defaultCursor", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                defaultField?.SetValue(cursorManager, defaultTexture);
            }
            else
            {
                Debug.LogWarning("❌ 未找到默认光标文件");
            }

            UnityEditor.EditorUtility.SetDirty(cursorManager);
            #endif
        }
    }
}
