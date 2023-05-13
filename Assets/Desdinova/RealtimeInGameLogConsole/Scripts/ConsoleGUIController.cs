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
        private Texture2D m_whiteTexture;

        private string m_currentCommand = "";

        private const string COMMAND_LINE_FOCUS_GROUP = "Command line focus group";

        private int m_currentCharacterCount = 0;

        private bool m_consoleJustOpened = false;

        private bool m_hasToRedoLog = true;

        private float m_lastDynamicHeight = 0;

        bool m_isAtBottomOfTheList = false;

        GUIStyle m_guiStyle;

        GUIContent m_guiContent = new GUIContent("");

        private StringBuilder m_logStringBuilder = null;

        [Header("Show Properties")]
        public bool ShowConsole = true;
        public bool ShowStackTrace = true;

        public bool ScrollDownOnCommand = true;
        public bool ScrollDownOnShowConsole = true;

        [Header("Key Properties")]
        public KeyCode ConsoleKeyCode = KeyCode.Backslash;
        public string KeyString = "";

        [Header("GUI Properties")]
        public ConsoleGUIAnchor GUIAnchor = ConsoleGUIAnchor.Bottom;
        public ConsoleGUIHeightDiv GUIHeightDiv = ConsoleGUIHeightDiv.Half;
        public int GUIFontSize = 15;
        public int LogCharacterCapacity = 4096;

        public int MaxCommandLength = 256;

        public int HorizontalPaddingConsole = 10;

        public int VerticalPaddingConsole = 0;

        public int HorizontalPaddingText = 10;

        public int VerticalPaddingText = 10;

        public int ScrollbarWidth = 20;

        public int CommandLineVerticalPadding = 4;

        public Color ColorConsoleBackground = new Color(0, 0, 0, 0.6f);



        public Color ColorSlider = new Color(1, 1, 1, 0.6f);

        public Color ColorText = new Color(1, 1, 1, 1);

        public Color ColorCommandLineUnfocused = new Color(0, 0, 0, 0.75f);
        public Color ColorCommandTextUnfocused = new Color(1, 1, 1, 0.6f);
        public Color ColorCommandLineFocused = new Color(0, 0, 0, 0.95f);
        public Color ColorCommandTextFocused = new Color(1, 1, 1, 1);

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
                m_currentCharacterCount += m_logsList[0].Length;
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
            m_whiteTexture = MakeTex(2, 2, Color.white);
            m_hasToRedoLog = true;
        }

        private void UpdateConsoleText()
        {
            if (m_hasToRedoLog)
            {
                m_hasToRedoLog = false;

                if (null == m_logStringBuilder || null == m_logsList)
                    OnEnable(); //initialize the string builder and logs list

                m_logStringBuilder.Clear();

                /*
                m_logStringBuilder.Append("<color=");
                m_logStringBuilder.Append("#FFFFFF");
                m_logStringBuilder.Append('>');*/

                m_logStringBuilder.Append("<size=");
                m_logStringBuilder.Append(GUIFontSize);
                m_logStringBuilder.Append('>');

                int listCount = m_logsList.Count;
                for (int i = 0; i < listCount; ++i)
                    m_logStringBuilder.Append(m_logsList[i]);

                m_logStringBuilder.Append("</size>");
                //  m_logStringBuilder.Append(COLOR_CLOSE_TAG);

                m_guiContent.text = m_logStringBuilder.ToString();
            }
        }

        void Update()
        {
            //this line is weird because only using "Input.GetKeyDown(ConsoleKeyCode)" didn't work properly, and 
            //and "Input.GetKeyUp(ConsoleKeyCode) || Input.GetKeyDown(ConsoleKeyCode)" worked for a bit then bugged out as well
            //could be a unity bug, but adding "!ShowConsole && " to the second part seemed to fix it
            if (Input.GetKeyUp(ConsoleKeyCode) || (!ShowConsole && Input.GetKeyDown(ConsoleKeyCode)))
            {
                Debug.Log("back slash was hit");
                ShowConsole = !ShowConsole;
                m_consoleJustOpened = ShowConsole;
            }
            else if (KeyString.Length > 0)
            {
                if (Input.GetButtonDown(this.KeyString))
                {
                    ShowConsole = !ShowConsole;
                }
            }
        }

        public void ProcessCommand(string command)
        {
            print("Command: " + command);
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

            int listCount = m_logsList.Count;
            m_currentCharacterCount += m_logsList[listCount - 1].Length;

            /*
            int elementsToRemove = 0;
            
            while (m_currentCharacterCount > LogCharacterCapacity)
                m_currentCharacterCount -= m_logsList[elementsToRemove++].Length;

            if (elementsToRemove > 0)
            {
                for (int i = 0; i < listCount - elementsToRemove; ++i)
                    m_logsList[i] = m_logsList[elementsToRemove + i];

                while (0 < elementsToRemove--)
                    m_logsList.RemoveAt(--listCount);
            }*/

            m_hasToRedoLog = true;
        }

        void OnGUI()
        {
            if (ShowConsole)
            {
                UpdateConsoleText();
                if (null == m_guiStyle)
                    m_guiStyle = new GUIStyle(GUI.skin.box);


                m_guiStyle = GUI.skin.box;
                GUI.backgroundColor = ColorConsoleBackground;
                m_guiStyle.alignment = TextAnchor.UpperLeft;
                m_guiStyle.richText = true;
                m_guiStyle.normal.background = m_whiteTexture;
                m_guiStyle.wordWrap = true;

                m_guiStyle.padding = new(HorizontalPaddingText, 4 * HorizontalPaddingText, VerticalPaddingText, VerticalPaddingText);

                //Size
                float startY = VerticalPaddingConsole;
                float startX = HorizontalPaddingConsole;

                float height = System.MathF.Max(0, Screen.height / (int)GUIHeightDiv);
                float width = System.MathF.Max(0, Screen.width - HorizontalPaddingConsole * 2f);

                if (GUIAnchor == ConsoleGUIAnchor.Bottom)
                    startY = Screen.height - height - VerticalPaddingConsole;


                float commandLineHeight = CommandLineVerticalPadding * 3 + GUIFontSize;
                Rect consoleLogRect = new Rect(startX, startY, width, height - commandLineHeight);

                //Final height
                float dynamicHeight = Mathf.Max(height - commandLineHeight, m_guiStyle.CalcHeight(m_guiContent, width));
                bool heightChanged = m_lastDynamicHeight != dynamicHeight;
                if (heightChanged && ((m_consoleJustOpened && ScrollDownOnShowConsole) || m_isAtBottomOfTheList)) m_scrollPosition.y = dynamicHeight;

                GUIStyle verticalScrollbarStyle = GUI.skin.verticalScrollbar;

                verticalScrollbarStyle.normal.background = m_whiteTexture;
                verticalScrollbarStyle.margin = new(0, 0, 0, 0);
                verticalScrollbarStyle.fixedWidth = ScrollbarWidth;
                // verticalScrollbarStyle.imagePosition
                // verticalScrollbarStyle.

                GUIStyle sliderStyle = GUI.skin.verticalScrollbarThumb;
                GUI.backgroundColor = ColorSlider;
                sliderStyle.normal.background = m_whiteTexture;

                //GUI.backgroundColor = ColorConsoleBackground;

                //Begin scroll
                m_scrollPosition = GUI.BeginScrollView(consoleLogRect, m_scrollPosition, new Rect(0, 0, 0, dynamicHeight), false, true
                                                , GUIStyle.none, verticalScrollbarStyle);

                GUI.contentColor = ColorText;
                GUI.backgroundColor = ColorConsoleBackground;

                //Draw box
                GUI.Box(new Rect(0, 0, width, dynamicHeight), m_guiContent, m_guiStyle);
                //End scroll
                GUI.EndScrollView();

                float textWidth = width;
                Rect commandLineRect = new Rect(startX, consoleLogRect.y + consoleLogRect.height, textWidth, commandLineHeight);

                //thanks to Charles-Van-Norman from the unity forums
                if (m_currentCommand.Length > 0 && Event.current.keyCode == KeyCode.Return)
                {
                    ProcessCommand(m_currentCommand);
                    m_currentCommand = "";
                    if (ScrollDownOnCommand) m_scrollPosition.y = dynamicHeight;
                }

                m_guiStyle = GUI.skin.box;

                m_guiStyle.normal.background = m_whiteTexture;
                m_guiStyle.fontSize = GUIFontSize;
                m_guiStyle.padding = new(HorizontalPaddingText, 0, CommandLineVerticalPadding, CommandLineVerticalPadding);


                if (GUI.GetNameOfFocusedControl().Equals(COMMAND_LINE_FOCUS_GROUP))
                {
                    GUI.contentColor = ColorCommandTextFocused;
                    GUI.backgroundColor = ColorCommandLineFocused;
                }
                else
                {
                    GUI.contentColor = ColorCommandTextUnfocused;
                    GUI.backgroundColor = ColorCommandLineUnfocused;
                }
                //thanks col000r from the unity forums
                if (m_consoleJustOpened) GUI.SetNextControlName(COMMAND_LINE_FOCUS_GROUP);
                m_currentCommand = GUI.TextField(commandLineRect, m_currentCommand, MaxCommandLength, m_guiStyle);
                if (m_consoleJustOpened) GUI.FocusControl(COMMAND_LINE_FOCUS_GROUP);

                m_consoleJustOpened = false;

                m_isAtBottomOfTheList = 0 == (int)(dynamicHeight - m_scrollPosition.y + commandLineHeight - height);
                m_lastDynamicHeight = dynamicHeight;
            } // if (ShowConsole)
        }


    }
}