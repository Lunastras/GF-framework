using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.Assertions;

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

    // [SerializeField] private float m_scrollSmoothTime = 0.1f; //smoothing of the scroll

    //  private float m_scrollSmoothRef = 0;

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

    private int m_consoleStringBufferLength = 1023;

    private string m_consoleStringBuffer = new('F', 1023);

    private bool m_showLogs = true;
    private bool m_showErrors = true;
    private bool m_showCommands = true;
    private bool m_showWarnings = true;

    private bool m_mustResize = false;

    private bool m_mustScrollDown = false;

    private bool m_mustRedoText = true;

    private List<GfConsoleLog> m_logsList;

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

    private const string PLUS_99_STRING = "+99";

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

        public GfConsoleLog(string text, string stackTrace, float startPosY, GfLogType type = GfLogType.LOG)
        {
            Text = text;
            LogType = type;
            StartPosY = startPosY;
            Height = 0;

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

        RectTransform selfRectTransform = GetComponent<RectTransform>();

        selfRectTransform.sizeDelta = new Vector2(Screen.width * 0.45f, Screen.height * 0.45f);
        selfRectTransform.position = new Vector3(Screen.width * 0.225f, Screen.height * 0.225f, 0);

        m_scalableWindow = GetComponent<ScalableWindow>();

        m_consoleImage = GetComponent<Image>();
        m_consoleImage.raycastTarget = m_console.activeSelf;
    }

    private void InitializeLog()
    {
        m_countCommand = 0;
        m_countError = 0;
        m_countLog = 0;
        m_countWarn = 0;
        m_currentViewportSize = new(m_visibleViewport.rect.width, m_visibleViewport.rect.height);

        m_textViewport = m_consoleText.GetComponent<RectTransform>();

        m_mustScrollDown = true;
        UnityEngine.Application.logMessageReceived += Log;

        m_logsList = new(256);
        string initLog = "\n\n" + UnityEngine.Application.productName + " " + UnityEngine.Application.version + " LOG CONSOLE ALPHA: \n";
        m_consoleText.text = initLog;

        GfConsoleLog initialLog = new(initLog, null, 0, GfLogType.LOG);
        initialLog.Height = m_consoleText.preferredHeight;
        m_fullHeight = initialLog.Height;
        m_logsList.Add(initialLog);

        m_consoleText.text = m_consoleStringBuffer;

        WriteFromYPosition(0);
        UpdateScrollbar();
    }

    // Start is called before the first frame update
    void Start()
    {
        m_console.SetActive(false);
        m_consoleImage.raycastTarget = m_console.activeSelf;
    }

    // Update is called once per frame
    void Update()
    {
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
                UpdateLogCounters();
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

            float scrollAmmount = 0;
            float wheelValue = Input.GetAxisRaw("Mouse ScrollWheel");
            float visibleHeight = m_visibleViewport.rect.height;
            bool scrollValueChanged = m_lastScrollValue != m_scrollBar.value;
            if (scrollValueChanged || (0 != wheelValue))//&& GfUITools.IsMouseOverUICollision(gameObject)))
            {
                scrollAmmount = wheelValue * m_scrollSensitivity;
                if (scrollValueChanged)
                    scrollAmmount += m_fullHeight * (m_scrollBar.value - m_lastScrollValue);

                float desiredYScroll = System.MathF.Max(visibleHeight, System.MathF.Min(m_fullHeight, m_currentYScroll + scrollAmmount));

                scrollAmmount = desiredYScroll - m_currentYScroll;
                m_currentYScroll = desiredYScroll;

                /* Planning on adding smooth scroll at some point
                if (scrollValueChanged)
                {
                    m_currentYScroll = desiredYScroll;
                }
                else
                {
                    m_currentYScroll = desiredYScroll;
                }*/
                //Debug.Log("The REAL scroll input is: " + scrollValue);

                UpdateScrollbar();
            }

            if (scrollAmmount != 0)
            {
                consoleWindowStartY = System.MathF.Max(0, m_currentYScroll - visibleHeight);

                if (VerifyShownLogIntegrity()) //move the window only if the log is valid
                {
                    var currentPos = m_textViewport.position;
                    currentPos.y += scrollAmmount;
                    m_textViewport.position = currentPos;
                }
            }

            if (m_focusOnCommandLine)
            {
                m_focusOnCommandLine = false;

                m_commandText.Select();
                m_commandText.ActivateInputField();
            }
        }
    }

    protected void UpdateLogCounters()
    {
        m_toggleCommand.Text.text = m_countCommand > 99 ? PLUS_99_STRING : m_countCommand.ToString();
        m_toggleWarn.Text.text = m_countWarn > 99 ? PLUS_99_STRING : m_countWarn.ToString();
        m_toggleError.Text.text = m_countError > 99 ? PLUS_99_STRING : m_countError.ToString();
        m_toggleLog.Text.text = m_countLog > 99 ? PLUS_99_STRING : m_countLog.ToString();
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
        string logString = m_consoleText.text;
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

        m_consoleText.text = logString;
    }

    public void CommandEntered(string command)
    {
        m_currentLogIsCommand = true;
        print(command);
        WriteFromBottom();
    }

    public void Log(string logString, string stackTrace, LogType type)
    {
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
            //m_logStringBuilder.Append('[');
            // m_logStringBuilder.Append(DateTime.Now.ToLongTimeString());
            //m_logStringBuilder.Append(']');
            //m_logStringBuilder.Append(' ');
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

        GfConsoleLog log = new(logString, stackTrace, m_fullHeight, gfLogType);
        log.Height = height;
        m_logsList.Add(log);


        float newFullHeight = m_fullHeight + height;
        m_mustRedoText = true;
        m_mustScrollDown |= m_currentYScroll >= m_fullHeight;
        if (m_console && m_console.activeSelf)
        {
            UpdateLogCounters();
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

    /**
    Makes sure the string used by the console text has enough space to insert the 'str' string
    */
    protected unsafe void ResizeConsoleStringForConcatenation(int aSourcePosition, int aConcatenateLength)
    {
        int requiredLength = aConcatenateLength + aSourcePosition;
        if (requiredLength >= m_consoleStringBufferLength)
        {
            // make sure the length isn't 0, otherwise it will be stuck on the following loop
            if (m_consoleStringBufferLength == 0)
                m_consoleStringBufferLength = 1023;//not a typo, the string will have the null termination character at the end, which will make the real length 1024

            while (requiredLength >= m_consoleStringBufferLength)
                m_consoleStringBufferLength = ((m_consoleStringBufferLength + 1) << 1) - 1;

            aSourcePosition = 0;
            string newString = new('F', m_consoleStringBufferLength);
            FastStringConcatenate(newString, m_consoleStringBuffer, ref aSourcePosition);

            m_consoleStringBuffer = newString;
            m_consoleText.text = newString;
        }

        fixed (char* bufferPtr = m_consoleStringBuffer)
        {
            int* intBufferPtr = (int*)bufferPtr;
            intBufferPtr[-1] = requiredLength;
            bufferPtr[requiredLength] = '\0';
        }
    }

    protected unsafe static void FastStringConcatenate(string sourceString, string insertString, ref int sourcePosition)
    {
        fixed (char* source = sourceString)
        {
            int strLen = insertString.Length;
            for (int i = 0; i < strLen; ++i)
                source[sourcePosition + i] = insertString[i];

            sourcePosition += strLen;
        }
    }

    protected unsafe static void FastStringConcatenate(string sourceString, char insertChar, ref int sourcePosition)
    {
        fixed (char* source = sourceString)
        {
            source[sourcePosition++] = insertChar;
        }
    }

    protected unsafe void ConsoleStringConcatenate(ref int aSourcePosition, string insertString)
    {
        ResizeConsoleStringForConcatenation(aSourcePosition, insertString.Length);
        FastStringConcatenate(m_consoleStringBuffer, insertString, ref aSourcePosition);
    }

    protected unsafe void ConsoleStringConcatenate(ref int aSourcePosition, char aInsertChar)
    {
        ResizeConsoleStringForConcatenation(aSourcePosition, 1);
        FastStringConcatenate(m_consoleStringBuffer, aInsertChar, ref aSourcePosition);
    }

    protected unsafe void WriteLog(int aLogIndex, ref int aCurrentStringIndex)
    {
        bool mustCloseColorTag = false;
        GfConsoleLog log = m_logsList[aLogIndex];

        switch (log.LogType)
        {
            case (GfLogType.ERROR):
                mustCloseColorTag = true;
                ConsoleStringConcatenate(ref aCurrentStringIndex, ERROR_OPEN_TAG);
                break;

            case (GfLogType.WARNING):
                mustCloseColorTag = true;
                ConsoleStringConcatenate(ref aCurrentStringIndex, WARN_OPEN_TAG);
                break;

            case (GfLogType.COMMAND):
                mustCloseColorTag = true;
                ConsoleStringConcatenate(ref aCurrentStringIndex, COMMAND_OPEN_TAG);
                break;
        }

        // ConsoleStringInsert(ref consoleString, ref currentStringIndex, NO_PARSE_TAG);
        ConsoleStringConcatenate(ref aCurrentStringIndex, log.Text);
        // ConsoleStringInsert(ref consoleString, ref currentStringIndex, NO_PARSE_CLOSE_TAG);

        if (mustCloseColorTag)
            ConsoleStringConcatenate(ref aCurrentStringIndex, COLOR_CLOSE_TAG);

        if (null != log.StackTrace && log.StackTrace.Length > 0)
        {
            switch (log.LogType)
            {
                case (GfLogType.ERROR):
                    ConsoleStringConcatenate(ref aCurrentStringIndex, STACK_ERROR_OPEN_TAG);
                    break;

                case (GfLogType.WARNING):
                    ConsoleStringConcatenate(ref aCurrentStringIndex, STACK_WARN_OPEN_TAG);
                    break;

                case (GfLogType.LOG):
                    ConsoleStringConcatenate(ref aCurrentStringIndex, STACK_LOG_OPEN_TAG);
                    break;
            }

            ConsoleStringConcatenate(ref aCurrentStringIndex, '\n');
            ConsoleStringConcatenate(ref aCurrentStringIndex, log.StackTrace);
            ConsoleStringConcatenate(ref aCurrentStringIndex, COLOR_CLOSE_TAG);
        }
    }

    void WriteFromBottom()
    {
        float consoleHeight = m_visibleViewport.rect.height;
        if (m_logsList.Count > 0)
        {
            m_shownLogsHeight = 0;
            int logIndex = m_logsList.Count - 1;
            m_currentYScroll = System.MathF.Max(m_fullHeight, consoleHeight);

            while (logIndex > -1 && m_shownLogsHeight < m_desiredLogHeight)
            {
                m_shownLogsHeight += m_logsList[logIndex].Height;
                logIndex--;
            }

            int currentStringIndex = 0;
            while (++logIndex < m_logsList.Count)
            {
                WriteLog(logIndex, ref currentStringIndex);
                if (logIndex < m_logsList.Count)
                    ConsoleStringConcatenate(ref currentStringIndex, '\n');
            }

            m_shownLogEndY = m_fullHeight;
            m_shownLogStartY = System.MathF.Max(0, m_shownLogEndY - m_shownLogsHeight);

            var currentPos = m_textViewport.localPosition;
            currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f;
            currentPos.y -= m_shownLogEndY - m_currentYScroll;
            m_textViewport.localPosition = currentPos;
            m_textViewport.sizeDelta = new Vector2(m_textViewport.sizeDelta.x, m_shownLogsHeight);

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

    void WriteFromYPosition(float shownLogStartY)
    {
        int firstIndex = 0;
        int secondIndex = m_logsList.Count;
        int middleIndex;

        while (1 > (secondIndex - firstIndex)) //perform a binary seach to find log closest to desired height
        {
            middleIndex = (firstIndex + secondIndex) >> 1;
            if (m_logsList[middleIndex].StartPosY > shownLogStartY)
                secondIndex = middleIndex;
            else
                firstIndex = middleIndex;
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
            int initialIndex = logIndex;
            m_shownLogStartY = m_logsList[logIndex].StartPosY;

            int currentStringIndex = 0;
            string consoleString = m_consoleText.text;
            while (logIndex < m_logsList.Count && m_shownLogsHeight < m_desiredLogHeight)
            {
                //do not add \n on the first log
                if (initialIndex != logIndex)
                    ConsoleStringConcatenate(ref currentStringIndex, '\n');

                m_shownLogsHeight += m_logsList[logIndex].Height;
                WriteLog(logIndex, ref currentStringIndex);
                logIndex++;
            }

            m_shownLogEndY = m_shownLogStartY + m_shownLogsHeight;

            var currentPos = m_textViewport.localPosition;
            currentPos.y = m_shownLogsHeight - consoleHeight * 0.5f; //set at bottom
            currentPos.y -= m_shownLogEndY - m_currentYScroll;
            m_textViewport.localPosition = currentPos;
            m_textViewport.sizeDelta = new Vector2(m_textViewport.sizeDelta.x, m_shownLogsHeight);

            unsafe
            {
                int textLength = consoleString.Length;
                fixed (char* p = consoleString)
                    for (; currentStringIndex < textLength; ++currentStringIndex)
                        p[currentStringIndex] = ' ';
            }

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

        m_scrollBar.size = System.MathF.Max(System.MathF.Min(1.0f, visibleHeight / m_fullHeight), 0.1f);
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
