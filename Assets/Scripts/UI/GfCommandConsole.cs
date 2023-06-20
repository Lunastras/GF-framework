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
    private Scrollbar m_scrollBar = null;

    [SerializeField]
    private RectTransform m_visibleViewport = null;

    private RectTransform m_textViewport = null;

    [SerializeField]
    private float m_scrollSensitivity = 1000;

    [SerializeField]
    private float m_desiredLogHeight = 4000; //the max height of the window used to display the section of the log

    private static int LogCharacterCapacity = 4096;

    private Image m_consoleImage = null;

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

    private ScalableWindow m_scalableWindow;

    private bool m_showLogs = true;
    private bool m_showErrors = true;
    private bool m_showCommands = true;
    private bool m_showWarnings = true;

    private bool m_mustResize = false;

    private bool m_mustScrollDown = false;

    private bool m_mustRedoText = true;

    private List<GfConsoleLog> m_logsList;

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

    private float m_lastScrollValue;

    private const string ERROR_OPEN_TAG = "<color=#FF534A>";
    private const string WARN_OPEN_TAG = "<color=#FFC107>";
    private const string COMMAND_OPEN_TAG = "<color=#9affd2>";

    private const string STACK_ERROR_OPEN_TAG = "<color=#984541>";
    private const string STACK_WARN_OPEN_TAG = "<color=#A59050>";
    private const string STACK_LOG_OPEN_TAG = "<color=#A9A9A9>";


    private const string COLOR_CLOSE_TAG = "</color>";

    private const string NO_PARSE_TAG = "<noparse>";
    private const string NO_PARSE_CLOSE_TAG = "</noparse>";

    private bool m_focusOnCommandLine = false;

    private Vector2 m_currentViewportSize = new();

    private bool m_currentLogIsCommand = false;

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

    private struct GfConsoleLog
    {
        public string Text;

        public string StackTrace;

        public GfLogType LogType;

        public float StartPosY;

        public float Height;

        public GfConsoleLog(string text, string stackTrace, float currentHeight, float height, GfLogType type = GfLogType.LOG)
        {
            Text = text;
            LogType = type;
            StartPosY = currentHeight;
            Height = height;

            StackTrace = stackTrace;
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

        InitializeLog();

        DontDestroyOnLoad(transform.parent);
        RectTransform selfRectTransform = GetComponent<RectTransform>();

        selfRectTransform.sizeDelta = new Vector2(Screen.width * 0.45f, Screen.height * 0.45f);
        selfRectTransform.position = new Vector3(Screen.width * 0.225f, Screen.height * 0.225f, 0);

        m_scalableWindow = GetComponent<ScalableWindow>();

        m_consoleImage = GetComponent<Image>();
        m_consoleImage.raycastTarget = m_console.activeSelf;
        m_consoleText.text = new string(' ', 512);
    }

    private void InitializeLog()
    {
        m_logString = "";

        m_countCommand = 0;
        m_countError = 0;
        m_countLog = 0;
        m_countWarn = 0;
        m_currentViewportSize = new(m_visibleViewport.rect.width, m_visibleViewport.rect.height);

        m_textViewport = m_consoleText.GetComponent<RectTransform>();

        m_mustScrollDown = true;
        UnityEngine.Application.logMessageReceived += Log;
        m_logStringBuilder = new System.Text.StringBuilder(LogCharacterCapacity);

        m_logsList = new(256);
        string initLog = "\n\n" + UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE: \n";
        m_consoleText.text = initLog;

        m_logsList.Add(new(initLog, null, 0, m_consoleText.preferredHeight, GfLogType.LOG));
        Debug.Log("The height of the initial log is: " + m_consoleText.preferredHeight);

        //m_fullHeight = m_consoleText.preferredHeight;
        m_fullHeight = m_consoleText.preferredHeight;

        WriteFromYPosition(0);
        UpdateScrollbar();
    }

    // Start is called before the first frame update
    void Start()
    {
        Application.targetFrameRate = 165;
        m_console.SetActive(false);
    }

    // Update is called once per frame
    void Update()
    {
        if (!m_first && false)
        {
            m_first = true;
            WriteFromBottom();
        }

        if (Input.GetKeyDown(m_consoleKeycode))
        {
            m_console.SetActive(!m_console.activeSelf);
            m_consoleImage.raycastTarget = m_console.activeSelf;

            m_scalableWindow.enabled = m_console.activeSelf;
            if (!m_console.activeSelf)//console just closed
            {
                m_mustScrollDown |= m_currentYScroll >= m_fullHeight;
            }
            else //console just opened
            {
                if (m_mustScrollDown && m_mustRedoText)
                {
                    m_mustRedoText = false;
                    m_mustScrollDown = false;
                    WriteFromBottom();
                }

                ResizeWindow();
                m_focusOnCommandLine = true;
            }
        }

        if (m_console.activeSelf)
        {
            //Cursor.visible = true;
            //Cursor.lockState = CursorLockMode.None;

            m_currentVisibleHeight = m_visibleViewport.rect.height;

            if (Input.GetKeyDown(KeyCode.Return) && m_commandText.text.Length > 0)
            {
                m_mustScrollDown |= m_scrollDownOnCommand;
                CommandEntered(m_commandText.text);
                m_commandText.text = "";
                m_commandText.Select();
                m_commandText.ActivateInputField();
            }

            float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");
            bool scrollValueChanged = m_lastScrollValue != m_scrollBar.value;
            if (scrollValueChanged || (0 != wheelValue))//&& GfUITools.IsMouseOverUICollision(gameObject)))
            {
                float scrollAmmount = wheelValue * m_scrollSensitivity;
                if (scrollValueChanged)
                    scrollAmmount += m_fullHeight * (m_scrollBar.value - m_lastScrollValue);

                Scroll(scrollAmmount);
            }

            UpdateConsoleText();
        }
    }

    private void UpdateConsoleText(bool forceUpdate = false)
    {
        if (!CanvasUpdateRegistry.IsRebuildingLayout() || forceUpdate)
        {
            if (m_mustRedoText && m_mustScrollDown)
            {
                m_toggleCommand.Text.text = m_countCommand > 99 ? PLUS_99_STRING : m_countCommand.ToString();
                m_toggleWarn.Text.text = m_countWarn > 99 ? PLUS_99_STRING : m_countWarn.ToString();
                m_toggleError.Text.text = m_countError > 99 ? PLUS_99_STRING : m_countError.ToString();
                m_toggleLog.Text.text = m_countLog > 99 ? PLUS_99_STRING : m_countLog.ToString();
            }

            if (m_focusOnCommandLine)
            {
                m_focusOnCommandLine = false;

                m_commandText.Select();
                m_commandText.ActivateInputField();
            }
        }
    }

    private bool m_first = false;


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
    }

    protected void ResizeWindow()
    {
        if (m_mustResize && m_console.activeSelf)
        {
            m_mustResize = false;
            if (m_logsList != null)
            {
                Rect visibleViewportRect = m_visibleViewport.rect;
                float width = visibleViewportRect.width;
                if (width != m_currentViewportSize.x)
                {
                    m_currentViewportSize.x = width;
                    UpdateLogHeights();
                    WriteFromBottom();
                }

                float height = visibleViewportRect.height;
                if (height != m_currentViewportSize.y && null != m_scalableWindow)
                {
                    float diff = height - m_currentViewportSize.y;
                    m_currentViewportSize.y = height;

                    bool draggingDown = m_scalableWindow.IsDraggingDown();
                    float yScrollDiff = 0;

                    float desiredYScroll = m_currentYScroll + diff;
                    m_currentYScroll = System.MathF.Max(height, System.MathF.Min(m_fullHeight, desiredYScroll));

                    if (draggingDown)
                    {
                        yScrollDiff = m_currentYScroll - desiredYScroll;
                    }
                    else if (desiredYScroll >= m_fullHeight && m_currentYScroll - height >= 0) //dragging up
                    {
                        yScrollDiff -= diff;
                    }

                    if (VerifyShownLogIntegrity())
                    {
                        var currentPos = m_textViewport.localPosition;
                        currentPos.y += yScrollDiff;
                        m_textViewport.localPosition = currentPos;
                    }

                    UpdateScrollbar();
                }
            }
        }
    }

    private void UpdateLogHeights()
    {
        int length = m_logsList.Count;
        m_fullHeight = 0;
        for (int i = 0; i < length; ++i)
        {
            GfConsoleLog log = m_logsList[i];
            m_consoleText.text = log.Text;
            log.StartPosY = m_fullHeight;
            log.Height = m_consoleText.preferredHeight;

            if (null != log.StackTrace)
            {
                m_consoleText.text = log.StackTrace;
                log.Height += m_consoleText.preferredHeight;
            }

            m_fullHeight += log.Height;
            m_logsList[i] = log;
        }

        m_consoleText.text = m_logString;
    }

    public void CommandEntered(string command)
    {
        m_currentLogIsCommand = true;
        print(command);
        WriteFromBottom();
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
        m_logStringBuilder.Clear();

        GfLogType gfLogType = GfLogType.LOG;
        bool showStack = m_showStackTrace;

        switch (type)
        {
            case (LogType.Error):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case (LogType.Exception):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case (LogType.Assert):
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case (LogType.Warning):
                ++m_countWarn;
                gfLogType = GfLogType.WARNING;
                break;

            case (LogType.Log):
                gfLogType = GfLogType.LOG;
                if (m_currentLogIsCommand)
                {
                    ++m_countCommand;
                    m_currentLogIsCommand = false;
                    showStack = false;
                    gfLogType = GfLogType.COMMAND;
                }
                else
                {
                    ++m_countLog;
                }

                break;
        }

        if (m_showTime && false) //todo
        {
            //Time appends
            m_logStringBuilder.Append('[');
            m_logStringBuilder.Append(DateTime.Now.ToLongTimeString());
            m_logStringBuilder.Append(']');
            m_logStringBuilder.Append(' ');
        }

        if (!showStack || null == stackTrace || stackTrace.Length == 0)
            stackTrace = null;

        int listCount = m_logsList.Count;
        string currentString = m_consoleText.text;

        m_consoleText.text = logString;
        float height = m_consoleText.preferredHeight;

        if (null != stackTrace)
        {
            m_consoleText.text = stackTrace;
            height += m_consoleText.preferredHeight;
        }

        m_consoleText.text = currentString;

        m_logsList.Add(new(logString, stackTrace, m_fullHeight, height, gfLogType));

        float newFullHeight = m_fullHeight + height;
        m_mustRedoText = true;
        m_mustScrollDown |= m_currentYScroll >= m_fullHeight;
        if (m_console.activeSelf)
        {
            m_currentYScroll = System.MathF.Max(m_currentYScroll, m_visibleViewport.rect.height);
            if (m_currentYScroll >= m_fullHeight) //must scroll down if we can see the bottom
            {
                m_fullHeight = newFullHeight;
                WriteFromBottom();
                m_mustRedoText = false;
            }
            else
            {
                UpdateScrollbar();
            }
        }

        m_fullHeight = newFullHeight;
    }

    protected void WriteLogToStringBuilder(int logIndex)
    {
        bool mustCloseColorTag = false;
        GfConsoleLog log = m_logsList[logIndex];


        switch (log.LogType)
        {
            case (GfLogType.ERROR):
                mustCloseColorTag = true;
                m_logStringBuilder.Append(ERROR_OPEN_TAG);
                break;

            case (GfLogType.WARNING):
                mustCloseColorTag = true;
                m_logStringBuilder.Append(WARN_OPEN_TAG);
                break;

            case (GfLogType.COMMAND):
                mustCloseColorTag = true;
                m_logStringBuilder.Append(COMMAND_OPEN_TAG);

                break;
        }

        //m_logStringBuilder.Append('>');
        m_logStringBuilder.Append(NO_PARSE_TAG);
        m_logStringBuilder.Append(log.Text);
        m_logStringBuilder.Append(NO_PARSE_CLOSE_TAG);
        if (mustCloseColorTag)
            m_logStringBuilder.Append(COLOR_CLOSE_TAG);

        if (null != log.StackTrace && log.StackTrace.Length > 0)
        {
            switch (log.LogType)
            {
                case (GfLogType.ERROR):
                    m_logStringBuilder.Append(STACK_ERROR_OPEN_TAG);
                    break;

                case (GfLogType.WARNING):
                    m_logStringBuilder.Append(STACK_WARN_OPEN_TAG);
                    break;

                case (GfLogType.LOG):
                    m_logStringBuilder.Append(STACK_LOG_OPEN_TAG);
                    break;
            }

            m_logStringBuilder.Append('\n');
            m_logStringBuilder.Append(log.StackTrace);
            m_logStringBuilder.Append(COLOR_CLOSE_TAG);
        }





        /////// unsafe pointers test

        unsafe
        {
            fixed (char* p = m_consoleText.text)
            {
                // do some work
            }
        }
    }

    void WriteFromBottom()
    {
        float consoleHeight = m_visibleViewport.rect.height;
        if (m_logsList.Count > 0)
        {
            m_shownLogsHeight = 0;
            m_logStringBuilder.Clear();
            int logIndex = m_logsList.Count - 1;
            m_currentYScroll = System.MathF.Max(m_fullHeight, consoleHeight);

            while (logIndex > -1 && m_shownLogsHeight < m_desiredLogHeight)
            {
                m_shownLogsHeight += m_logsList[logIndex].Height;
                logIndex--;
            }

            while (++logIndex < m_logsList.Count)
            {
                WriteLogToStringBuilder(logIndex);
                if (logIndex < m_logsList.Count)
                    m_logStringBuilder.Append('\n');
            }

            m_shownLogEndY = m_fullHeight;
            m_shownLogStartY = System.MathF.Max(0, m_shownLogEndY - m_shownLogsHeight);

            var currentPos = m_textViewport.localPosition;
            currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f;
            currentPos.y -= m_shownLogEndY - m_currentYScroll;
            m_textViewport.localPosition = currentPos;

            // m_consoleText.text = m_logStringBuilder.ToString();

            UpdateScrollbar();
        }
        else
        {
            m_consoleText.text = "";
            m_shownLogsHeight = 0;
            m_shownLogStartY = 0;
            m_shownLogEndY = consoleHeight;
        }
    }

    protected void OnRectTransformDimensionsChange()
    {
        m_mustResize = true;
        ResizeWindow();
    }

    private const string PLUS_99_STRING = "+99";

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

    float GetLogHeight(int logIndex)
    {
        //  float height = m_logsList[logIndex].StartPosY;
        return m_logsList[logIndex + 1].StartPosY - m_logsList[logIndex].StartPosY;
    }

    void WriteDownFromIndex(int logIndex)
    {
        float consoleHeight = m_visibleViewport.rect.height;
        if (m_logsList.Count > 0)
        {
            m_shownLogsHeight = 0;
            m_logStringBuilder.Clear();
            int initialIndex = logIndex;
            m_shownLogStartY = m_logsList[logIndex].StartPosY;

            while (logIndex < m_logsList.Count && m_shownLogsHeight < m_desiredLogHeight)
            {
                //do not add \n on the first log
                if (initialIndex != logIndex) m_logStringBuilder.Append('\n');
                m_shownLogsHeight += m_logsList[logIndex].Height;
                WriteLogToStringBuilder(logIndex);
                logIndex++;
            }

            m_shownLogEndY = m_shownLogStartY + m_shownLogsHeight;

            var currentPos = m_textViewport.localPosition;
            currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f; //set at bottom
            currentPos.y -= m_shownLogEndY - m_currentYScroll;
            m_textViewport.localPosition = currentPos;

            m_consoleText.text = m_logStringBuilder.ToString();

            UpdateScrollbar();
        }
        else
        {
            m_consoleText.text = "";
            m_shownLogsHeight = 0;
            m_shownLogStartY = 0;
            m_shownLogEndY = consoleHeight;
        }
    }

    private void Scroll(float scrollValue)
    {
        float visibleHeight = m_visibleViewport.rect.height;
        float desiredYScroll = System.MathF.Max(visibleHeight, System.MathF.Min(m_fullHeight, m_currentYScroll + scrollValue));

        scrollValue = desiredYScroll - m_currentYScroll;
        m_currentYScroll = desiredYScroll;
        //Debug.Log("The REAL scroll input is: " + scrollValue);

        if (scrollValue != 0)
        {
            consoleWindowStartY = System.MathF.Max(0, m_currentYScroll - visibleHeight);

            if (VerifyShownLogIntegrity()) //move the window only if the log is valid
            {
                var currentPos = m_textViewport.position;
                currentPos.y += scrollValue;
                m_textViewport.position = currentPos;
            }
        }

        UpdateScrollbar();
    }

    /*
    Make sure the shown log isn't overshooting the written log and that the console doesn't show blank spots
    @return Return true if the log was recalculated, 
    */
    protected bool VerifyShownLogIntegrity()
    {
        bool validLog = true;
        float visibleHeight = m_visibleViewport.rect.height;
        consoleWindowStartY = System.MathF.Max(0, m_currentYScroll - visibleHeight);

        if (m_currentYScroll > m_shownLogEndY || consoleWindowStartY < m_shownLogStartY)
        {
            validLog = false;
            float newTop = m_currentYScroll - 0.5f * (m_desiredLogHeight + visibleHeight);
            WriteFromYPosition(newTop);
        }

        return validLog;
    }

    private void UpdateScrollbar()
    {
        float visibleHeight = m_visibleViewport.rect.height;
        float estimatedBottomConsoleTopPos = m_fullHeight - visibleHeight; //the top position of the console when we are at the bottom of the log
        float consoleTopYPos = System.MathF.Max(0, m_currentYScroll - visibleHeight);

        if (0 < estimatedBottomConsoleTopPos)
        {
            m_scrollBar.value = System.MathF.Max(0, System.MathF.Min(1, consoleTopYPos / estimatedBottomConsoleTopPos));
        }
        else
        {
            m_scrollBar.value = 1;
        }

        m_scrollBar.size = System.MathF.Min(1.0f, visibleHeight / m_fullHeight);
        m_lastScrollValue = m_scrollBar.value;
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
        m_scrollBar.value = 0;
    }
}
