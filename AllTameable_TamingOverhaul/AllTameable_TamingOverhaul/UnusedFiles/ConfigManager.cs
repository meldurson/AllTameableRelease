using System.Reflection;
using UnityEngine;

namespace AllTameable
{
    public class ConfigManager : MonoBehaviour
    {
        public string CfgName;

        private GUIStyle button;

        private GUIStyle scroll;

        private GUIStyle debugscroll;

        private GUIStyle normalText;

        private GUIStyle debugText;

        private GUIStyle placeholder;

        private Rect SetupWindowRect;

        public object obj;

        private Vector2 scrollPosition = Vector2.zero;

        private Vector2 scrollPosition2 = Vector2.zero;

        public static Transform Root;

        private float width = 600f;

        private float height = 1000f;

        public int noramalFontSize = 20;

        public int topicFontSzie = 25;

        public string title;

        public string debugInfo;

        private BindingFlags bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.GetProperty;

        private void Awake()
        {
            Root = base.gameObject.transform;
            SetupWindowRect = new Rect(0f, 0f, width, height);
        }

        private void OnGUI()
        {
            SetupGUI();
            GUI.backgroundColor = Color.black;
            //SetupWindowRect = GUI.Window(9107, SetupWindowRect, (GUI.WindowFunction)SetupWindow, title);
        }

        private void ConstrutGUI()
        {
            PropertyInfo[] properties = obj.GetType().GetProperties(bindingFlags);
            PropertyInfo[] array = properties;
            foreach (PropertyInfo propertyInfo in array)
            {
                if (propertyInfo.PropertyType == typeof(bool))
                {
                    DrawBoolToogle(propertyInfo);
                }
                if (propertyInfo.PropertyType == typeof(int))
                {
                    DrawIntField(propertyInfo);
                }
                if (propertyInfo.PropertyType == typeof(float))
                {
                    DrawFloatField(propertyInfo);
                }
                if (propertyInfo.PropertyType == typeof(string))
                {
                    DrawStringField(propertyInfo);
                }
            }
        }

        private void DrawStringField(PropertyInfo fieldInfo)
        {
            string text = fieldInfo.GetValue(obj).ToString();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label(fieldInfo.Name, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            text = GUILayout.TextField(text, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 2f / 3f) });
            GUILayout.EndHorizontal();
            fieldInfo.SetValue(obj, text);
        }

        private void DrawIntField(PropertyInfo fieldInfo)
        {
            string text = fieldInfo.GetValue(obj).ToString();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label(fieldInfo.Name, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            text = GUILayout.TextField(text, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 2f / 3f) });
            GUILayout.EndHorizontal();
            text = float.Parse(text).ToString();
            if (int.TryParse(text, out var result))
            {
                fieldInfo.SetValue(obj, result);
            }
        }

        private void DrawFloatField(PropertyInfo fieldInfo)
        {
            string text = fieldInfo.GetValue(obj).ToString();
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label(fieldInfo.Name, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            text = ((!(fieldInfo.Name == "pregnancyChance")) ? GUILayout.TextField(text, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 2f / 3f) }) : GUILayout.TextField(float.Parse(text).ToString("0.00"), new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 2f / 3f) }));
            GUILayout.EndHorizontal();
            if (float.TryParse(text, out var result))
            {
                fieldInfo.SetValue(obj, result);
            }
        }

        private void DrawBoolToogle(PropertyInfo fieldInfo)
        {
            bool flag = (bool)fieldInfo.GetValue(obj);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label(fieldInfo.Name, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            flag = GUILayout.Toggle(flag, flag ? "on" : "off", new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            GUILayout.EndHorizontal();
            fieldInfo.SetValue(obj, flag);
        }

        private void SetupWindow(int windowid)
        {
            GUI.DragWindow(new Rect(0f, 0f, width, 30f));
            GUILayout.BeginArea(new Rect(20f, 80f, width, height - 30f));
            GUILayout.Label("", new GUILayoutOption[1] { GUILayout.Height(30f) });
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("", new GUILayoutOption[1] { GUILayout.Width(30f) });
            if (GUILayout.Button("Add", button, new GUILayoutOption[0]))
            {
                Add();
            }
            if (GUILayout.Button("Remove", button, new GUILayoutOption[0]))
            {
                Remove();
            }
            if (GUILayout.Button("Replace", button, new GUILayoutOption[0]))
            {
                Replace();
            }
            if (GUILayout.Button("Get", button, new GUILayoutOption[0]))
            {
                Get();
            }
            GUILayout.EndHorizontal();
            scrollPosition = GUILayout.BeginScrollView(scrollPosition, debugscroll);
            GUILayout.TextArea(debugInfo, debugText, new GUILayoutOption[1] { GUILayout.Height(300f) });
            GUILayout.EndScrollView();
            scrollPosition2 = GUILayout.BeginScrollView(scrollPosition2, scroll);
            GUILayout.BeginHorizontal(new GUILayoutOption[0]);
            GUILayout.Label("name", new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 1f / 3f) });
            CfgName = GUILayout.TextField(CfgName, new GUILayoutOption[1] { GUILayout.Width((width - 50f) * 2f / 3f) });
            GUILayout.EndHorizontal();
            ConstrutGUI();
            GUI.EndScrollView();
            GUILayout.EndArea();
        }

        private void SetupGUI()
        {
            button = new GUIStyle(GUI.skin.button);
            button.fontSize = noramalFontSize;
            button.fixedWidth = ((width - 100f) / 4f);
            button.fixedHeight = (30f);
            normalText = new GUIStyle(GUI.skin.label);
            normalText.fontSize = (noramalFontSize);
            debugText = new GUIStyle(GUI.skin.label);
            debugText.fontSize = (noramalFontSize);
            debugText.normal.textColor = Color.yellow;
            debugText.focused.textColor = Color.yellow;
            debugText.richText = (true);
            scroll = new GUIStyle(GUI.skin.scrollView);
            scroll.fontSize = (noramalFontSize);
            scroll.fixedWidth = (width - 20f);
            scroll.fixedHeight = (height - 20f);
            debugscroll = new GUIStyle(GUI.skin.scrollView);
            debugscroll.fontSize = (noramalFontSize);
            debugscroll.fixedWidth = (width - 20f);
            debugscroll.fixedHeight = (120f);
            placeholder = new GUIStyle(GUI.skin.box);
        }

        /*
		private void SetupGUI()
		{
			button = new GUIStyle(GUI.skin.button);
			button.set_fontSize(noramalFontSize);
			button.set_fixedWidth((width - 100f) / 4f);
			button.set_fixedHeight(30f);
			normalText = new GUIStyle(GUI.skin.label);
			normalText.set_fontSize(noramalFontSize);
			debugText = new GUIStyle(GUI.skin.label);
			debugText.set_fontSize(noramalFontSize);
			debugText.normal.textColor = Color.yellow;
			debugText.get_focused().textColor = Color.yellow;
			debugText.set_richText(true);
			scroll = new GUIStyle(GUI.skin.scrollView);
			scroll.set_fontSize(noramalFontSize);
			scroll.set_fixedWidth(width - 20f);
			scroll.set_fixedHeight(height - 20f);
			debugscroll = new GUIStyle(GUI.skin.scrollView);
			debugscroll.set_fontSize(noramalFontSize);
			debugscroll.set_fixedWidth(width - 20f);
			debugscroll.set_fixedHeight(120f);
			placeholder = new GUIStyle(GUI.skin.box);
		}
		*/

        private void Add()
        {
            Plugin.CfgMangerAdd();
        }

        private void Replace()
        {
            Plugin.cfgMangerReplace();
        }

        private void Remove()
        {
            Plugin.CfgMangerRemove();
        }

        private void Get()
        {
            Plugin.CfgMangerGet();
        }
    }
}
