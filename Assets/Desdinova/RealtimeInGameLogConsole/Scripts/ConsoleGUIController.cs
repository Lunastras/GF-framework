using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;

namespace Desdinova
{
    public class ConsoleGUIController : MonoBehaviour
    {
        public enum ConsoleGUIAnchor
        {
            Top = 1,
            Bottom = 2
        }

        public enum ConsoleGUIHeightDiv
        {
            Full = 1,
            Half = 2,
            Quarter = 4
        }

        private List<string> m_logsList;
        private Vector2 m_scrollPosition;
        private Texture2D m_backgroundTexture;

        private string m_currentCommand = "";

        private int m_currentCharacterCount = 0;

        private bool m_submitReleased = true;

        GUIStyle m_guiStyle;

        GUIContent m_guiContent = new GUIContent("");

        private StringBuilder m_logStringBuilder = null;

        [Header("Show Properties")]
        public bool ShowConsole = true;
        public bool ShowStackTrace = true;
        public bool ShowTitle = true;

        [Header("Key Properties")]
        public KeyCode KeyCode = KeyCode.Backslash;
        public string KeyString = "";

        [Header("GUI Properties")]
        public ConsoleGUIAnchor GUIAnchor = ConsoleGUIAnchor.Bottom;
        public ConsoleGUIHeightDiv GUIHeightDiv = ConsoleGUIHeightDiv.Half;
        public int GUIFontSize = 15;
        public int LogCharacterCapacity = 4096;

        public int HorizontalPaddingConsole = 10;

        public int VerticalPaddingConsole = 0;

        public int HorizontalPaddingText = 10;

        public int VerticalPaddingText = 10;

        public int ScrollbarWidth = 20;

        public int CommandLineHeight = 20;

        public Color GUIColor = Color.black;

        [Header("Behaviours Properties")]
        public bool DoNotDestroyOnLoad = false;

        public GUIStyle DialogBoxStyle { get; private set; }

        const string ERROR_OPEN_TAG = "<color=#FF534A>";
        const string WARN_OPEN_TAG = "<color=#FFC107>";
        const string COLOR_CLOSE_TAG = "</color>";

        void OnEnable()
        {
            if (null == m_logStringBuilder) m_logStringBuilder = new System.Text.StringBuilder(LogCharacterCapacity);

            if (null == m_logsList)
            {
                m_logsList = new(256);
                m_logsList.Add(UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE: \n");
            }

            UnityEngine.Application.logMessageReceived += Log;
        }

        void OnDisable() { UnityEngine.Application.logMessageReceived -= Log; }

        private Texture2D MakeTex(int width, int height, Color col)
        {
            Color[] pix = new Color[width * height];
            for (int i = 0; i < pix.Length; ++i)
            {
                pix[i] = col;
            }
            Texture2D result = new Texture2D(width, height);
            result.SetPixels(pix);
            result.Apply();
            return result;
        }

        private void Start()
        {
            Application.SetStackTraceLogType(LogType.Log, StackTraceLogType.None);

            //Use in different scene
            if (DoNotDestroyOnLoad)
            {
                DontDestroyOnLoad(gameObject);
            }

            //Set static backgorund color (do not do it in the OnGUI method)
            m_backgroundTexture = MakeTex(2, 2, GUIColor);
            UpdateConsoleText();
        }

        private void UpdateConsoleText()
        {
            if (null == m_logStringBuilder || null == m_logsList)
                OnEnable(); //initialize the string builder and logs list

            m_logStringBuilder.Clear();

            m_logStringBuilder.Append("<color=");
            m_logStringBuilder.Append("#FFFFFF");
            m_logStringBuilder.Append('>');
            m_logStringBuilder.Append("<size=");
            m_logStringBuilder.Append(GUIFontSize);
            m_logStringBuilder.Append('>');

            int listCount = m_logsList.Count;
            for (int i = 0; i < listCount; ++i)
                m_logStringBuilder.Append(m_logsList[i]);

            m_logStringBuilder.Append("</size>");
            m_logStringBuilder.Append(COLOR_CLOSE_TAG);

            m_guiContent.text = m_logStringBuilder.ToString();
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode))
            {
                ShowConsole = !ShowConsole;
            }
            else if (KeyString.Length > 0)
            {
                if (Input.GetButtonDown(this.KeyString))
                {
                    ShowConsole = !ShowConsole;
                }
            }

            if (ShowConsole)
            {
                if (Input.GetAxisRaw("Submit") > 0.01f)
                {
                    if (m_submitReleased)
                    {
                        m_submitReleased = false;
                        ProcessCommand(m_currentCommand);
                        m_currentCommand = "";
                    }
                }
                else
                {
                    m_submitReleased = true;
                }
            }
        }

        public void ProcessCommand(string command)
        {
            Debug.Log("Command: " + command);
        }

        public void Log(string logString, string stackTrace, LogType type)
        {
            if (null == m_logStringBuilder || null == m_logsList)
                OnEnable(); //initialize the string builder and logs list

            m_logStringBuilder.Clear();
            m_logStringBuilder.Append('\n');

            switch (type)
            {
                case (LogType.Error): m_logStringBuilder.Append(ERROR_OPEN_TAG); break;
                case (LogType.Exception): m_logStringBuilder.Append(ERROR_OPEN_TAG); break;
                case (LogType.Assert): m_logStringBuilder.Append(WARN_OPEN_TAG); break;
                case (LogType.Warning): m_logStringBuilder.Append(WARN_OPEN_TAG); break;
            }

            //Time appends
            m_logStringBuilder.Append('[');
            m_logStringBuilder.Append(DateTime.Now.ToLongTimeString());
            m_logStringBuilder.Append(']');
            m_logStringBuilder.Append(' ');

            m_logStringBuilder.Append(logString);

            if (this.ShowStackTrace && null != stackTrace && stackTrace.Length > 0)
            {
                m_logStringBuilder.Append("\n<size=");
                m_logStringBuilder.Append(this.GUIFontSize / 1.25);
                m_logStringBuilder.Append('>');
                m_logStringBuilder.Append(stackTrace);
                m_logStringBuilder.Append("</size>");
            }

            if (type != LogType.Log)
                m_logStringBuilder.Append(COLOR_CLOSE_TAG);
            else
                m_logStringBuilder.Append('\n');

            m_logsList.Add(m_logStringBuilder.ToString());
            UpdateConsoleText();
        }

        void OnGUI()
        {
            if (ShowConsole)
            {
                if (null == m_guiStyle)
                    m_guiStyle = new GUIStyle(GUI.skin.box);

                //Style
                m_guiStyle.alignment = TextAnchor.UpperLeft;
                m_guiStyle.richText = true;
                m_guiStyle.normal.background = m_backgroundTexture;
                m_guiStyle.wordWrap = true;

                m_guiStyle.padding = new(HorizontalPaddingText, 4 * HorizontalPaddingText, VerticalPaddingText, VerticalPaddingText);

                //Size
                int screenWidth = Screen.width;
                int screenHeight = Screen.height;
                float height = screenHeight / (int)GUIHeightDiv;
                float width = screenWidth - HorizontalPaddingConsole * 2f;

                //y is set as if the anchor is on top
                Rect newRect = new Rect(HorizontalPaddingConsole, VerticalPaddingConsole, width, height - CommandLineHeight);

                if (GUIAnchor == ConsoleGUIAnchor.Bottom)
                    newRect.y = screenHeight - height - VerticalPaddingConsole;

                //Final height
                float dinamicHeight = Mathf.Max(height - CommandLineHeight, m_guiStyle.CalcHeight(m_guiContent, screenWidth));

                //Begin scroll
                m_scrollPosition = GUI.BeginScrollView(newRect, m_scrollPosition, new Rect(0, 0, 0, dinamicHeight), false, true);

                //Draw box
                GUI.Box(new Rect(0, 0, screenWidth, dinamicHeight), m_guiContent, m_guiStyle);
                //End scroll
                GUI.EndScrollView();

                float textWidth = width;
                Rect textRect = new Rect(HorizontalPaddingConsole, newRect.y + newRect.height, textWidth, CommandLineHeight);

                GUI.Box(textRect, "");
                m_currentCommand = GUI.TextField(textRect, m_currentCommand);

                if ((Event.current.Equals(Event.KeyboardEvent("[enter]"))))
                {
                    ProcessCommand(m_currentCommand);
                    m_currentCommand = "";
                }
            }
        }


    }
}