using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;

public class GfCommandConsole : MonoBehaviour
{
    public static GfCommandConsole Instance { get; private set; } = null;

    [SerializeField]
    private bool m_bottomOnLog = true;

    [SerializeField]
    private TMP_InputField m_consoleText = null;

    [SerializeField]
    private TMP_InputField m_commandText = null;

    [SerializeField]
    private Scrollbar m_scrollbar = null;

    [SerializeField]
    private RectTransform m_visibleViewport = null;

    private RectTransform m_textViewport = null;

    [SerializeField]
    private float m_scrollSensitivity = 1000;

    [SerializeField]
    private float m_writtenLogHeight = 2000; //the max height of the window used to display the section of the log

    private static int LogCharacterCapacity = 4096;

    [SerializeField] private int m_guiFontSize = 15;

    [SerializeField] private bool m_showTime = true;
    [SerializeField] private bool m_showStackTrace = true;
    [SerializeField] private bool m_scrollDownOnCommand = true;

    [SerializeField] private LogTypeToggle m_toggleLog = default;
    [SerializeField] private LogTypeToggle m_toggleWarn = default;
    [SerializeField] private LogTypeToggle m_toggleError = default;
    [SerializeField] private LogTypeToggle m_toggleCommand = default;

    private float m_currentYScroll = 0;

    private float m_fullHeight = 0;

    private static bool ShowLogs = true;
    private static bool ShowErrors = true;
    private static bool ShowCommands = true;
    private static bool ShowWarnings = true;

    private static bool MustScrollDown = false;

    private static bool MustRedoText = true;

    //Last console to receive the updated string
    private static GfCommandConsole LastUpdatedConsole;

    private static List<ConsoleLog> LogsList;

    private static string LogString = "";

    private static StringBuilder LogStringBuilder = null;

    private static int CountError = 0;
    private static int CountWarn = 0;
    private static int CountLog = 0;
    private static int CountCommand = 0;

    private float m_shownLogStartY = 0; //the top y of the full log shown

    private float m_shownLogEndY = 0; //the bottom y of the full log shown

    private float m_shownLogsHeight = 0;

    private const char COMMAND_PREFIX = '!';

    private const string ERROR_OPEN_TAG = "<color=#FF534A>";
    private const string WARN_OPEN_TAG = "<color=#FFC107>";
    private const string COMMAND_OPEN_TAG = "<color=#9affd2>";
    private const string COLOR_CLOSE_TAG = "</color>";

    private static int LastGuiFontSize = 15;

    private static bool LastShowStack = true;

    private static bool LastShowTime = true;

    private bool m_focusOnCommandLine = false;

    private float m_currentViewportWidth = 0;

    [System.Serializable]
    private struct LogTypeToggle
    {
        public LogTypeToggle(Toggle toggle = null, TMP_Text text = null)
        {
            Toggle = toggle;
            Text = text;
        }

        public Toggle Toggle;
        public TMP_Text Text;
    }

    private enum GfLogType
    {
        LOG, WARNING, ERROR, COMMAND
    }

    private struct ConsoleLog
    {
        public string Text;
        public GfLogType Type;
        public float Height;

        public float StartPosY;

        public ConsoleLog(string text, GfLogType type = GfLogType.LOG, float height = 0, float currentHeight = 0)
        {
            Text = text;
            Type = type;
            Height = height;
            StartPosY = currentHeight;
        }

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

    private void InitializeLog()
    {
        LogString = "";

        CountCommand = 0;
        CountError = 0;
        CountLog = 0;
        CountWarn = 0;
        m_currentViewportWidth = m_visibleViewport.rect.width;

        m_textViewport = m_consoleText.GetComponent<RectTransform>();

        MustScrollDown = true;
        LastUpdatedConsole = null;
        UnityEngine.Application.logMessageReceived += Instance.Log;
        LogStringBuilder = new System.Text.StringBuilder(LogCharacterCapacity);

        LogsList = new(256);
        string initLog = "\n\n" + UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE: \n";
        m_consoleText.text = initLog;

        LogsList.Add(new(initLog, GfLogType.LOG, m_consoleText.preferredHeight, 0));


        Debug.Log("The height of the initial log is: " + m_consoleText.preferredHeight);

        //m_fullHeight = m_consoleText.preferredHeight;
        m_fullHeight = LogsList[0].Height;

        m_shownLogStartY = 0;

        m_shownLogsHeight = m_fullHeight;
        m_shownLogEndY = m_fullHeight;
        WriteFromBottom();
        UpdateScrollbar();
    }


    private void DeinitLog()
    {
        // if (Instance) Destroy(Instance);
        //Instance = null;
        UnityEngine.Application.logMessageReceived -= Instance.Log;
    }

    private void OnDestroy()
    {
        DeinitLog();
    }

    void OnEnable()
    {
        if (this != Instance)
        {
            Destroy(Instance);
            Instance = this;
            UpdateShowErrors();
            UpdateShowLogs();
            UpdateShowWarnings();
            UpdateShowCommands();
        }

        m_focusOnCommandLine = true;
    }

    void OnDisable()
    {
        MustScrollDown |= 0.95f <= m_scrollbar.value || 0.99f < m_scrollbar.size; //currently at the bottom, scroll to bottom after writing new log
    }

    // Start is called before the first frame update
    void Start()
    {
        InitializeLog();
    }

    private void UpdateLogHeights()
    {
        int length = LogsList.Count;
        m_fullHeight = 0;
        for (int i = 0; i < length; ++i)
        {
            ConsoleLog log = LogsList[i];
            m_consoleText.text = log.Text;
            log.Height = m_consoleText.preferredHeight;
            log.StartPosY = m_fullHeight;
            m_fullHeight += log.Height;
            LogsList[i] = log;
        }

        m_consoleText.text = LogString;
    }

    public void CommandEntered(string command)
    {
        print(COMMAND_PREFIX + command);
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        LogStringBuilder.Clear();

        GfLogType gfLogType = GfLogType.LOG;
        if (Instance) LastShowStack = Instance.m_showStackTrace;
        bool showStack = LastShowStack;

        switch (type)
        {
            case (LogType.Error):
                ++CountError;
                gfLogType = GfLogType.ERROR;
                LogStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Exception):
                ++CountError;
                gfLogType = GfLogType.ERROR;
                LogStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Assert):
                ++CountError;
                gfLogType = GfLogType.ERROR;
                LogStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (LogType.Warning):
                ++CountWarn;
                gfLogType = GfLogType.WARNING;
                LogStringBuilder.Append(WARN_OPEN_TAG);
                break;

            case (LogType.Log):
                gfLogType = GfLogType.LOG;
                if (logString[0] == COMMAND_PREFIX)
                {
                    ++CountCommand;
                    showStack = false;
                    gfLogType = GfLogType.COMMAND;
                    LogStringBuilder.Append(COMMAND_OPEN_TAG);
                }
                else
                {
                    ++CountLog;
                }

                break;
        }

        if (Instance) LastShowTime = Instance.m_showTime;
        if (LastShowTime)
        {
            //Time appends
            LogStringBuilder.Append('[');
            LogStringBuilder.Append(DateTime.Now.ToLongTimeString());
            LogStringBuilder.Append(']');
            LogStringBuilder.Append(' ');
        }
        else
        {
            LogStringBuilder.Append('>');
        }

        LogStringBuilder.Append(logString);

        if (showStack && null != stackTrace && stackTrace.Length > 0)
        {
            if (Instance) LastGuiFontSize = Instance.m_guiFontSize;
            LogStringBuilder.Append("\n<size=");
            LogStringBuilder.Append(LastGuiFontSize / 1.25f);
            LogStringBuilder.Append('>');
            LogStringBuilder.Append(stackTrace);
            LogStringBuilder.Append("</size>");
        }

        //all logs besides Log have a colour, place the colour tag at the end if it isn't a normal log
        if (GfLogType.LOG != gfLogType)
            LogStringBuilder.Append(COLOR_CLOSE_TAG);

        int listCount = LogsList.Count;
        string fullLog = LogStringBuilder.ToString();
        Instance.m_consoleText.text = fullLog;
        float height = Instance.m_consoleText.preferredHeight;
        LogsList.Add(new(fullLog, gfLogType, height, m_fullHeight));

        m_fullHeight += height;

        MustRedoText = true;
        MustScrollDown |= Instance && (0.95f <= Instance.m_scrollbar.value || 0.99f < Instance.m_scrollbar.size); //currently at the bottom, scroll to bottom after writing new log

        //  WriteFromBottom();
    }

    void WriteFromHeight(float shownLogStartY)
    {
        int firstIndex = 0;
        int secondIndex = LogsList.Count;
        int middleIndex;

        int count = 0;
        while (1 != (secondIndex - firstIndex) && 30 > ++count)
        {
            middleIndex = (firstIndex + secondIndex) / 2; //divide by two bitwise
            if (LogsList[middleIndex].StartPosY > shownLogStartY)
                secondIndex = middleIndex;
            else
                firstIndex = middleIndex;
        }

        if (count == 30)
        {
            Debug.LogError("UHHHHHHH NOTHING WORKED, first is: " + firstIndex + " second is: " + secondIndex);
        }

        if (m_bottomOnLog)
            WriteDownFromIndex(firstIndex);
    }

    void WriteDownFromIndex(int logIndex)
    {
        m_shownLogsHeight = 0;
        LogStringBuilder.Clear();

        float consoleHeight = m_visibleViewport.rect.height;

        m_shownLogStartY = LogsList[logIndex].StartPosY;
        m_shownLogEndY = System.MathF.Min(m_fullHeight, m_shownLogStartY + m_shownLogsHeight);

        while (logIndex < LogsList.Count && m_shownLogsHeight < m_writtenLogHeight)
        {
            LogStringBuilder.Append('\n');
            m_shownLogsHeight += LogsList[logIndex].Height;
            LogStringBuilder.Append(LogsList[logIndex]);
            logIndex++;
        }

        var currentPos = m_textViewport.localPosition;
        currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f;
        m_textViewport.localPosition = currentPos;

        m_consoleText.text = LogStringBuilder.ToString();
        // Debug.Log("The estimated height is: " + m_shownLogsHeight + " the real height is " + m_consoleText.preferredHeight + " full height of the console is: " + consoleHeight + " full log height " + consoleHeight + " written log count is: " + acount + " estimated count is: " + count);

        UpdateScrollbar();
    }

    void WriteFromBottom()
    {
        int logIndex = LogsList.Count - 1;
        m_shownLogsHeight = 0;
        m_currentYScroll = m_fullHeight;

        float consoleHeight = m_visibleViewport.rect.height;

        int count = 0;
        while (logIndex > -1 && m_shownLogsHeight < m_writtenLogHeight)
        {
            count++;
            m_shownLogsHeight += LogsList[logIndex].Height;
            // Debug.Log("current height of thing is: " + LogsList[logIndex].Height + " current count is: " + count);
            logIndex--;
        }

        LogStringBuilder.Clear();
        //Debug.Log("current logIndex is: " + logIndex + " current logs count is: " + LogsList.Count);
        int acount = 0;
        while (++logIndex < LogsList.Count)
        {
            ++acount;
            LogStringBuilder.Append(LogsList[logIndex]);
            if (logIndex < LogsList.Count)
                LogStringBuilder.Append('\n');
        }

        var currentPos = m_textViewport.localPosition;
        currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f;
        m_textViewport.localPosition = currentPos;

        m_shownLogEndY = m_fullHeight;
        m_shownLogStartY = System.MathF.Max(0, m_shownLogEndY - m_shownLogsHeight);

        m_consoleText.text = LogStringBuilder.ToString();
        // Debug.Log("The estimated height is: " + m_shownLogsHeight + " the real height is " + m_consoleText.preferredHeight + " full height of the console is: " + consoleHeight + " full log height " + consoleHeight + " written log count is: " + acount + " estimated count is: " + count);

        UpdateScrollbar();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return) && m_commandText.text.Length > 0)
        {
            MustScrollDown |= m_scrollDownOnCommand;
            CommandEntered(m_commandText.text);
            m_commandText.text = "";
            m_commandText.Select();
            m_commandText.ActivateInputField();
        }

        if (m_consoleText.isFocused || true)
        {
            float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");
            Scroll(wheelValue);
        }

        float width = m_visibleViewport.rect.width;
        if (width != m_currentViewportWidth)
        {
            m_currentViewportWidth = width;
            UpdateLogHeights();
            WriteFromBottom();
        }

        if (false)
            UpdateConsoleText();
    }

    private const string PLUS_99_STRING = "+99";

    private void UpdateConsoleText(bool forceUpdate = false)
    {
        if (!CanvasUpdateRegistry.IsRebuildingLayout() || forceUpdate)
        {
            if (this != LastUpdatedConsole)
            {
                m_toggleCommand.Text.text = CountCommand > 99 ? PLUS_99_STRING : CountCommand.ToString();
                m_toggleWarn.Text.text = CountWarn > 99 ? PLUS_99_STRING : CountWarn.ToString();
                m_toggleError.Text.text = CountError > 99 ? PLUS_99_STRING : CountError.ToString();
                m_toggleLog.Text.text = CountLog > 99 ? PLUS_99_STRING : CountLog.ToString();

                m_consoleText.text = LogString;
                LastUpdatedConsole = this;

                if (MustScrollDown && 0.99f > m_scrollbar.size) //Don't scroll if the size is still 1
                {
                    m_scrollbar.value = 1;
                    MustScrollDown = false;
                }

                m_scrollbar.value = System.MathF.Min(1.0f, m_scrollbar.value);
            }

            if (m_focusOnCommandLine)
            {
                m_focusOnCommandLine = false;

                m_commandText.Select();
                m_commandText.ActivateInputField();
            }
        }
    }

    private void Scroll(float scrollValue)
    {
        if (scrollValue != 0)
        {
            //m_fullHeight = m_consoleText.preferredHeight - m_visibleViewport.rect.height;
            scrollValue *= m_scrollSensitivity;
            float desiredYScroll = System.MathF.Max(0, System.MathF.Min(m_fullHeight, m_currentYScroll + scrollValue));

            scrollValue = desiredYScroll - m_currentYScroll;
            m_currentYScroll = desiredYScroll;
            //Debug.Log("The REAL scroll input is: " + scrollValue);

            if (scrollValue != 0)
            {
                float visibleHeightHalf = m_visibleViewport.rect.height;
                float consoleWindowStartY = System.MathF.Max(0, m_currentYScroll - visibleHeightHalf);
                float consoleWindowEndY = System.MathF.Min(m_fullHeight, m_currentYScroll + visibleHeightHalf);

                bool move = true;
                if (true)
                {
                    if (consoleWindowEndY > m_shownLogEndY)
                    {

                        Debug.Log("Oh no i reached the end of the bottom.");
                        move = false;
                        float newTop = m_currentYScroll + 0.5f * m_shownLogsHeight;
                        WriteFromHeight(newTop);
                    }
                    else if (consoleWindowStartY < m_shownLogStartY)
                    {
                        Debug.Log("Oh no i reached the end of the start.");
                        move = false;
                        float newTop = m_currentYScroll + 0.5f * m_shownLogsHeight;
                        WriteFromHeight(newTop);
                    }
                }

                if (move)
                {
                    var currentPos = m_textViewport.position;
                    currentPos.y += scrollValue;
                    m_textViewport.position = currentPos;
                }
            }
        }

        UpdateScrollbar();
    }

    private void UpdateScrollbar()
    {
        float visibleHeight = m_visibleViewport.rect.height;
        float effectiveConsoleLogHeight = System.MathF.Max(visibleHeight, m_fullHeight - visibleHeight);
        effectiveConsoleLogHeight = System.MathF.Max(visibleHeight, m_fullHeight);
        m_scrollbar.value = System.MathF.Max(0, System.MathF.Min(1, m_currentYScroll / effectiveConsoleLogHeight));
        m_scrollbar.size = System.MathF.Min(1.0f, visibleHeight / effectiveConsoleLogHeight);
    }

    public void UpdateShowWarnings()
    {
        ShowWarnings = m_toggleWarn.Toggle.isOn;
        MustRedoText = true;
    }

    public void UpdateShowErrors()
    {
        ShowErrors = m_toggleError.Toggle.isOn;
        MustRedoText = true;
    }

    public void UpdateShowLogs()
    {
        ShowLogs = m_toggleLog.Toggle.isOn;
        MustRedoText = true;
    }

    public void UpdateShowCommands()
    {
        ShowCommands = m_toggleCommand.Toggle.isOn;
        MustRedoText = true;
    }

    public void ScrollToBottom()
    {
        MustScrollDown = true;
    }

    public void ScrollToTop()
    {
        m_scrollbar.value = 0;
    }
}
