using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class GfCommandConsole : MonoBehaviour
{
    [SerializeField]
    private TMP_InputField m_consoleText = null;

    [SerializeField]
    private TMP_InputField m_commandText = null;

    private Scrollbar m_scrollbar = null;

    [SerializeField] private int m_logCharacterCapacity = 4096;

    [SerializeField] private int m_guiFontSize = 15;

    [SerializeField] private bool m_showStackTrace = true;
    [SerializeField] private bool m_scrollDownOnCommand = true;
    [SerializeField] private bool m_scrollDownOnShowConsole = true;

    [SerializeField] private bool m_showLogs = true;
    [SerializeField] private bool m_showErrors = true;
    [SerializeField] private bool m_showCommands = true;
    [SerializeField] private bool m_showWarnings = true;

    private bool m_mustScrollDown = false;

    private bool m_mustRedoText = true;

    private List<ConsoleLog> m_logsList;

    private StringBuilder m_logStringBuilder = null;

    private int m_currentCharacterCount = 0;

    private bool m_initialised = false;

    private int m_countError = 0;
    private int m_countWarn = 0;
    private int m_countLog = 0;
    private int m_countCommand = 0;

    private char COMMAND_PREFIX = '!';

    const string ERROR_OPEN_TAG = "<color=#FF534A>";
    const string WARN_OPEN_TAG = "<color=#FFC107>";
    const string COMMAND_OPEN_TAG = "<color=#9affd2>";
    const string COLOR_CLOSE_TAG = "</color>";

    private enum GfLogType
    {
        LOG, WARNING, ERROR, COMMAND
    }

    private struct ConsoleLog
    {
        public ConsoleLog(string text, GfLogType type = GfLogType.LOG)
        {
            this.Text = text;
            this.Type = type;
        }

        public string Text;
        public GfLogType Type;

        public int Length
        {
            get
            {
                int length = 0;
                if (null != Text) length = Text.Length;
                return length;
            }
        }

        public override string ToString()
        {
            return Text;
        }
    }

    void OnEnable()
    {
        if (!m_initialised)
        {
            m_initialised = true;
            m_scrollbar = m_consoleText.verticalScrollbar;
            m_logStringBuilder = new System.Text.StringBuilder(m_logCharacterCapacity);

            m_logsList = new(256);
            m_logsList.Add(new(UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE: \n"));
            m_currentCharacterCount += m_logsList[0].Length;

            UnityEngine.Application.logMessageReceived += Log;
        }

        if (gameObject.activeSelf) //this function can be called by the log function without the gameobject being active
        {
            m_commandText.Select();
            m_commandText.ActivateInputField();
        }

        UpdateConsoleText();
    }

    void OnDisable()
    {
        if (0.99f <= m_scrollbar.value) m_mustScrollDown = true;
    }

    void OnDestroy()
    {
        if (m_initialised) UnityEngine.Application.logMessageReceived -= Log;
    }

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && m_commandText.text.Length > 0)
        {
            CommandEntered(m_commandText.text);
            m_commandText.text = "";
            m_commandText.Select();
            m_commandText.ActivateInputField();
        }
    }

    public void CommandEntered(string command)
    {
        m_mustScrollDown = true;
        print(COMMAND_PREFIX + command);

    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        if (null == m_logStringBuilder || null == m_logsList)
            OnEnable(); //initialize the string builder and logs list

        m_logStringBuilder.Clear();
        m_logStringBuilder.Append('\n');

        GfLogType gfLogType = GfLogType.LOG;
        bool showStack = m_showStackTrace;

        switch (type)
        {
            case (LogType.Error):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                m_logStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Exception):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                m_logStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Assert):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                m_logStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Warning):
                ++m_countWarn;
                gfLogType = GfLogType.WARNING;
                m_logStringBuilder.Append(WARN_OPEN_TAG);
                break;

            case (LogType.Log):
                ++m_countCommand;
                gfLogType = GfLogType.LOG;
                if (logString[0] == COMMAND_PREFIX)
                {
                    showStack = false;
                    gfLogType = GfLogType.COMMAND;
                    m_logStringBuilder.Append(COMMAND_OPEN_TAG);
                }
                break;
        }

        //Time appends
        m_logStringBuilder.Append('[');
        m_logStringBuilder.Append(DateTime.Now.ToLongTimeString());
        m_logStringBuilder.Append(']');
        m_logStringBuilder.Append(' ');

        m_logStringBuilder.Append(logString);

        if (showStack && null != stackTrace && stackTrace.Length > 0)
        {
            m_logStringBuilder.Append("\n<size=");
            m_logStringBuilder.Append(this.m_guiFontSize / 1.25f);
            m_logStringBuilder.Append('>');
            m_logStringBuilder.Append(stackTrace);
            m_logStringBuilder.Append("</size>");
        }

        //all logs besides Log have a colour, place the colour tag at the end if it isn't a normal log
        if (GfLogType.LOG != gfLogType)
            m_logStringBuilder.Append(COLOR_CLOSE_TAG);
        // else
        //  m_logStringBuilder.Append('\n');

        m_logsList.Add(new(m_logStringBuilder.ToString(), gfLogType));

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

        m_mustRedoText = true;

        if (gameObject.activeSelf)
        {
            bool scrollToBottom = m_mustScrollDown || 0.99f > m_scrollbar.value || 0.99f < m_scrollbar.size; //currently at the bottom, scroll to bottom after writing new log
            UpdateConsoleText();
            //Don't scroll if the size is still 1
            if (scrollToBottom && 0.99f > m_scrollbar.size)
            {
                m_scrollbar.value = 1;
                m_mustScrollDown = false;
            }
        }
    }

    public void ToggleShowWarnings()
    {
        m_showWarnings = !m_showWarnings;
        m_mustRedoText = true;
        UpdateConsoleText();
    }

    public void ToggleShowErrors()
    {
        m_showErrors = !m_showErrors;
        m_mustRedoText = true;
        UpdateConsoleText();
    }

    public void ToggleShowLogs()
    {
        m_showLogs = !m_showLogs;
        m_mustRedoText = true;
        UpdateConsoleText();
    }

    public void ToggleShowCommands()
    {
        m_showCommands = !m_showCommands;
        m_mustRedoText = true;
        UpdateConsoleText();
    }

    public void ScrollToBottom()
    {
        m_mustScrollDown = true;
    }

    public void ScrollToTop()
    {
        m_scrollbar.value = 0;
    }

    private void UpdateConsoleText()
    {
        if (gameObject.activeSelf && m_mustRedoText)
        {
            m_mustRedoText = false;
            m_logStringBuilder.Clear();

            m_logStringBuilder.Append("<size=");
            m_logStringBuilder.Append(m_guiFontSize);
            m_logStringBuilder.Append('>');
            ConsoleLog log;
            GfLogType type;
            bool writeLog;

            int listCount = m_logsList.Count;
            for (int i = 0; i < listCount; ++i)
            {
                log = m_logsList[i];
                type = log.Type;

                writeLog =
                   (m_showLogs && GfLogType.LOG == type)
                || (m_showWarnings && GfLogType.WARNING == type)
                || (m_showErrors && GfLogType.ERROR == type)
                || (m_showCommands && GfLogType.COMMAND == type); //if it isn't a log or a warning, it's an error/exception/assert

                if (writeLog) m_logStringBuilder.Append(log);
            }


            m_logStringBuilder.Append("</size>");
            m_consoleText.text = m_logStringBuilder.ToString();
        }
    }
}
