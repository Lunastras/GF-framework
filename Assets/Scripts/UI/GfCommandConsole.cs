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
    private GameObject m_console = null;

    [SerializeField] private KeyCode m_consoleKeycode = KeyCode.Backslash;


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
    private float m_writtenLogHeight = 4000; //the max height of the window used to display the section of the log

    private static int LogCharacterCapacity = 4096;

    private RectTransform m_consoleRectTransform = null;

    [SerializeField] private int m_guiFontSize = 15;

    [SerializeField] private bool m_showTime = true;
    [SerializeField] private bool m_showStackTrace = true;
    [SerializeField] private bool m_scrollDownOnCommand = true;

    [SerializeField] private LogTypeToggle m_toggleLog = default;
    [SerializeField] private LogTypeToggle m_toggleWarn = default;
    [SerializeField] private LogTypeToggle m_toggleError = default;
    [SerializeField] private LogTypeToggle m_toggleCommand = default;

    [SerializeField]
    private float m_currentYScroll = 0;

    [SerializeField]
    private float m_fullHeight = 0;

    private bool m_showLogs = true;
    private bool m_showErrors = true;
    private bool m_showCommands = true;
    private bool m_showWarnings = true;

    private bool m_mustScrollDown = false;

    private bool m_mustRedoText = true;

    //Last console to receive the updated string
    private GfCommandConsole m_lastUpdatedConsole;

    private List<ConsoleLog> m_logsList;

    private string m_logString = "";

    private StringBuilder m_logStringBuilder = null;

    private int m_countError = 0;
    private int m_countWarn = 0;
    private int m_countLog = 0;
    private int m_countCommand = 0;

    [SerializeField]
    private float m_shownLogStartY = 0; //the top y of the full log shown

    [SerializeField]
    private float m_shownLogEndY = 0; //the bottom y of the full log shown

    [SerializeField]
    private float m_shownLogsHeight = 0;

    [SerializeField]
    private float m_currentVisibleHeight = 0;


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

    [SerializeField]
    float consoleWindowStartY = 0;

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

    void Awake()
    {
        if (Instance) Destroy(Instance);
        Instance = this;

        DontDestroyOnLoad(transform.parent);
        RectTransform selfRectTransform = GetComponent<RectTransform>();

        selfRectTransform.sizeDelta = new Vector2(Screen.width * 0.45f, Screen.height * 0.45f);
        selfRectTransform.position = new Vector3(Screen.width * 0.225f, Screen.height * 0.225f, 0);

        InitializeLog();
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 30;
    }

    private void InitializeLog()
    {
        m_logString = "";

        m_countCommand = 0;
        m_countError = 0;
        m_countLog = 0;
        m_countWarn = 0;
        m_currentViewportWidth = m_visibleViewport.rect.width;

        m_textViewport = m_consoleText.GetComponent<RectTransform>();

        m_mustScrollDown = true;
        m_lastUpdatedConsole = null;
        UnityEngine.Application.logMessageReceived += Log;
        m_logStringBuilder = new System.Text.StringBuilder(LogCharacterCapacity);

        m_logsList = new(256);
        string initLog = "\n\n" + UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE: \n";
        m_consoleText.text = initLog;

        m_logsList.Add(new(initLog, GfLogType.LOG, m_consoleText.preferredHeight, 0));
        Debug.Log("The height of the initial log is: " + m_consoleText.preferredHeight);

        //m_fullHeight = m_consoleText.preferredHeight;
        m_fullHeight = m_logsList[0].Height;

        m_shownLogStartY = 0;

        m_shownLogsHeight = m_fullHeight;
        m_shownLogEndY = m_fullHeight;
        WriteFromBottom();
        UpdateScrollbar();
    }


    private void DeinitLog()
    {
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
        m_mustScrollDown |= 0.95f <= m_scrollbar.value || 0.99f < m_scrollbar.size; //currently at the bottom, scroll to bottom after writing new log
    }


    private void UpdateLogHeights()
    {
        int length = m_logsList.Count;
        m_fullHeight = 0;
        for (int i = 0; i < length; ++i)
        {
            ConsoleLog log = m_logsList[i];
            m_consoleText.text = log.Text;
            log.Height = m_consoleText.preferredHeight;
            log.StartPosY = m_fullHeight;
            m_fullHeight += log.Height;
            m_logsList[i] = log;
        }

        m_consoleText.text = m_logString;
    }

    public void CommandEntered(string command)
    {
        print(COMMAND_PREFIX + command);
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        m_logStringBuilder.Clear();

        GfLogType gfLogType = GfLogType.LOG;
        if (Instance) LastShowStack = Instance.m_showStackTrace;
        bool showStack = LastShowStack;

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
                gfLogType = GfLogType.LOG;
                if (logString[0] == COMMAND_PREFIX)
                {
                    ++m_countCommand;
                    showStack = false;
                    gfLogType = GfLogType.COMMAND;
                    m_logStringBuilder.Append(COMMAND_OPEN_TAG);
                }
                else
                {
                    ++m_countLog;
                }

                break;
        }

        if (Instance) LastShowTime = Instance.m_showTime;
        if (LastShowTime)
        {
            //Time appends
            m_logStringBuilder.Append('[');
            m_logStringBuilder.Append(DateTime.Now.ToLongTimeString());
            m_logStringBuilder.Append(']');
            m_logStringBuilder.Append(' ');
        }
        else
        {
            m_logStringBuilder.Append('>');
        }

        m_logStringBuilder.Append(logString);

        if (showStack && null != stackTrace && stackTrace.Length > 0)
        {
            if (Instance) LastGuiFontSize = Instance.m_guiFontSize;
            m_logStringBuilder.Append("\n<size=");
            m_logStringBuilder.Append(LastGuiFontSize / 1.25f);
            m_logStringBuilder.Append('>');
            m_logStringBuilder.Append(stackTrace);
            m_logStringBuilder.Append("</size>");
        }

        //all logs besides Log have a colour, place the colour tag at the end if it isn't a normal log
        if (GfLogType.LOG != gfLogType)
            m_logStringBuilder.Append(COLOR_CLOSE_TAG);

        int listCount = m_logsList.Count;
        string fullLog = m_logStringBuilder.ToString();
        string currentString = m_consoleText.text;
        m_consoleText.text = fullLog;
        float height = Instance.m_consoleText.preferredHeight;
        m_consoleText.text = currentString;
        m_logsList.Add(new(fullLog, gfLogType, height, m_fullHeight));

        m_fullHeight += height;

        m_mustRedoText = true;
        m_mustScrollDown |= Instance && (0.95f <= Instance.m_scrollbar.value || 0.99f < Instance.m_scrollbar.size); //currently at the bottom, scroll to bottom after writing new log

        WriteFromBottom();
    }

    void WriteFromBottom()
    {
        float consoleHeight = m_visibleViewport.rect.height;
        int logIndex = m_logsList.Count - 1;
        m_shownLogsHeight = 0;

        m_currentYScroll = System.MathF.Max(m_fullHeight, consoleHeight);

        int count = 0;
        while (logIndex > -1 && m_shownLogsHeight < m_writtenLogHeight)
        {
            count++;
            m_shownLogsHeight += m_logsList[logIndex].Height;
            // Debug.Log("current height of thing is: " + LogsList[logIndex].Height + " current count is: " + count);
            logIndex--;
        }

        m_logStringBuilder.Clear();
        //Debug.Log("current logIndex is: " + logIndex + " current logs count is: " + LogsList.Count);
        int acount = 0;
        while (++logIndex < m_logsList.Count)
        {
            ++acount;
            m_logStringBuilder.Append(m_logsList[logIndex]);
            if (logIndex < m_logsList.Count)
                m_logStringBuilder.Append('\n');
        }

        m_shownLogEndY = m_fullHeight;
        m_shownLogStartY = System.MathF.Max(0, m_shownLogEndY - m_shownLogsHeight);

        var currentPos = m_textViewport.localPosition;
        currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f;
        currentPos.y -= m_shownLogEndY - m_currentYScroll;
        m_textViewport.localPosition = currentPos;

        m_consoleText.text = m_logStringBuilder.ToString();
        // Debug.Log("The estimated height is: " + m_shownLogsHeight + " the real height is " + m_consoleText.preferredHeight + " full height of the console is: " + consoleHeight + " full log height " + consoleHeight + " written log count is: " + acount + " estimated count is: " + count);

        UpdateScrollbar();
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(m_consoleKeycode))
        {
            m_console.SetActive(!m_console.activeSelf);
        }

        if (m_console.activeSelf)
        {
            m_currentVisibleHeight = m_visibleViewport.rect.height;

            if (Input.GetKeyDown(KeyCode.Return) && m_commandText.text.Length > 0)
            {
                m_mustScrollDown |= m_scrollDownOnCommand;
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

            if (false) UpdateConsoleText();
        }
    }

    private const string PLUS_99_STRING = "+99";

    private void UpdateConsoleText(bool forceUpdate = false)
    {
        if (!CanvasUpdateRegistry.IsRebuildingLayout() || forceUpdate)
        {
            if (this != m_lastUpdatedConsole)
            {
                m_toggleCommand.Text.text = m_countCommand > 99 ? PLUS_99_STRING : m_countCommand.ToString();
                m_toggleWarn.Text.text = m_countWarn > 99 ? PLUS_99_STRING : m_countWarn.ToString();
                m_toggleError.Text.text = m_countError > 99 ? PLUS_99_STRING : m_countError.ToString();
                m_toggleLog.Text.text = m_countLog > 99 ? PLUS_99_STRING : m_countLog.ToString();

                m_consoleText.text = m_logString;
                m_lastUpdatedConsole = this;

                if (m_mustScrollDown && 0.99f > m_scrollbar.size) //Don't scroll if the size is still 1
                {
                    m_scrollbar.value = 1;
                    m_mustScrollDown = false;
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

    void WriteFromYPosition(float shownLogStartY)
    {
        int firstIndex = 0;
        int secondIndex = m_logsList.Count;
        int middleIndex;

        int count = 0;
        while (1 != (secondIndex - firstIndex) && 30 > ++count) //perform a binary seach to find log closest to ddesired height
        {
            middleIndex = (firstIndex + secondIndex) / 2; //divide by two bitwise
            if (m_logsList[middleIndex].StartPosY > shownLogStartY)
                secondIndex = middleIndex;
            else
                firstIndex = middleIndex;
        }

        float firstDistanceFromDesiredY = System.MathF.Abs(shownLogStartY);

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
        m_logStringBuilder.Clear();
        float consoleHeight = m_visibleViewport.rect.height;
        m_shownLogStartY = m_logsList[logIndex].StartPosY;
        int initialIndex = logIndex;

        while (logIndex < m_logsList.Count && m_shownLogsHeight < m_writtenLogHeight)
        {
            //do not add \n on the first log
            if (initialIndex != logIndex) m_logStringBuilder.Append('\n');
            m_shownLogsHeight += m_logsList[logIndex].Height;
            m_logStringBuilder.Append(m_logsList[logIndex]);
            logIndex++;
        }

        m_shownLogEndY = m_shownLogStartY + m_shownLogsHeight;

        var currentPos = m_textViewport.localPosition;
        currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f; //set at bottom
        currentPos.y -= m_shownLogEndY - m_currentYScroll;
        m_textViewport.localPosition = currentPos;

        m_consoleText.text = m_logStringBuilder.ToString();
        // Debug.Log("The estimated height is: " + m_shownLogsHeight + " the real height is " + m_consoleText.preferredHeight + " full height of the console is: " + consoleHeight + " full log height " + consoleHeight + " written log count is: " + acount + " estimated count is: " + count);

        UpdateScrollbar();
    }

    private void Scroll(float scrollValue)
    {
        if (scrollValue != 0)
        {
            float visibleHeigh = m_visibleViewport.rect.height;
            scrollValue *= m_scrollSensitivity;
            float desiredYScroll = System.MathF.Max(visibleHeigh, System.MathF.Min(m_fullHeight, m_currentYScroll + scrollValue));

            scrollValue = desiredYScroll - m_currentYScroll;
            m_currentYScroll = desiredYScroll;
            //Debug.Log("The REAL scroll input is: " + scrollValue);

            if (scrollValue != 0)
            {
                consoleWindowStartY = System.MathF.Max(0, m_currentYScroll - visibleHeigh);

                bool move = true;
                if (true)
                {
                    if (m_currentYScroll > m_shownLogEndY)
                    {
                        //  Debug.Log("Oh no i reached the end of the bottom.");
                        move = false;
                        float newTop = m_currentYScroll - 0.5f * (m_writtenLogHeight + visibleHeigh);
                        WriteFromYPosition(newTop);
                    }
                    else if (consoleWindowStartY < m_shownLogStartY)
                    {
                        // Debug.Log("Oh no i reached the end of the start.");
                        move = false;
                        float newTop = m_currentYScroll - 0.5f * (m_writtenLogHeight + visibleHeigh);
                        WriteFromYPosition(newTop);
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

        float efefctiveScrollPos = System.MathF.Max(0, m_currentYScroll - visibleHeight);

        //effectiveConsoleLogHeight = System.MathF.Max(visibleHeight, m_fullHeight);
        m_scrollbar.value = System.MathF.Max(0, System.MathF.Min(1, efefctiveScrollPos / effectiveConsoleLogHeight));
        m_scrollbar.size = System.MathF.Min(1.0f, visibleHeight / effectiveConsoleLogHeight);
    }

    public void UpdateShowWarnings()
    {
        m_showWarnings = m_toggleWarn.Toggle.isOn;
        m_mustRedoText = true;
    }

    public void UpdateShowErrors()
    {
        m_showErrors = m_toggleError.Toggle.isOn;
        m_mustRedoText = true;
    }

    public void UpdateShowLogs()
    {
        m_showLogs = m_toggleLog.Toggle.isOn;
        m_mustRedoText = true;
    }

    public void UpdateShowCommands()
    {
        m_showCommands = m_toggleCommand.Toggle.isOn;
        m_mustRedoText = true;
    }

    public void ScrollToBottom()
    {
        m_mustScrollDown = true;
    }

    public void ScrollToTop()
    {
        m_scrollbar.value = 0;
    }
}
