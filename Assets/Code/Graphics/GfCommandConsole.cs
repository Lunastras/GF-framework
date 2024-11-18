using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.UI;
using System.Text;
using TMPro;
using UnityEngine.Assertions;

public class GfCommandConsole : MonoBehaviour
{
    public static GfCommandConsole Instance { get; private set; } = null;
    [SerializeField] private GameObject m_console = null;
    [SerializeField] private KeyCode m_consoleKeycode = KeyCode.Backslash;
    [SerializeField] private RectTransform m_visibleViewport = null;
    [SerializeField] private TMP_InputField m_consoleText = null;
    [SerializeField] private TMP_InputField m_commandText = null;
    [SerializeField] private Scrollbar m_scrollBar = null;
    [SerializeField] private float m_scrollSensitivity = 1000;
    [SerializeField] private float m_scrollSmoothTime = 0.05f;
    [SerializeField] private bool m_showLogTime = false;
    [SerializeField] private bool m_showStackTrace = true;
    [SerializeField] private bool m_scrollDownOnCommand = true;
    [SerializeField] private LogTypeToggle m_toggleLog = default;
    [SerializeField] private LogTypeToggle m_toggleWarn = default;
    [SerializeField] private LogTypeToggle m_toggleError = default;
    [SerializeField] private LogTypeToggle m_toggleCommand = default;

    private TMP_InputField m_consoleTextCopyForHeight;
    private Image m_consoleImage = null;
    private RectTransform m_textViewport = null;
    private int m_lastUpdatedIndexHeight = 0;
    private ScalableWindow m_scalableWindow;
    private GfcStringBuffer m_consoleStringBuffer = new(1024);
    private GfcStringBuffer m_auxStringBuffer = new(64);
    private GfcStringBuffer m_timeStampStringBuffer = new(64);
    private bool m_showLogs = true;
    private bool m_showErrors = true;
    private bool m_showCommands = true;
    private bool m_showWarnings = true;
    private bool m_mustResize = false;
    private bool m_mustScrollDown = false;
    private bool m_mustRedoText = true;
    private bool m_ignoreCallStackForNextLog = false;
    private bool m_currentLogIsCommand = false;
    private bool m_focusOnCommandLine = false;
    private Dictionary<string, Action> m_commands = new(16);
    private Vector2 m_currentViewportSize = new();
    private List<GfConsoleLog> m_logsList;
    private int m_countError = 0;
    private int m_countWarn = 0;
    private int m_countLog = 0;
    private int m_countCommand = 0;

    private float m_yScrollBottomSmoothRef = 0;
    private float m_desiredYScrollBottom = 0;
    [SerializeField] private float m_desiredLogHeight = 4000; //the max height of the window used to display the section of the log, todo should me automated
    [SerializeField] private float m_currentYScrollBottom = 0;
    [SerializeField] private float m_fullHeight = 0;
    [SerializeField] private float m_shownLogStartY = 0; //the top y of the full log shown
    [SerializeField] private float m_shownLogEndY = 0; //the bottom y of the full log shown
    [SerializeField] private float m_shownLogsHeight = 0;
    [SerializeField] float m_consoleWindowStartY = 0;
    [SerializeField] private float m_lastScrollValue;

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

    public static void IgnoreCallStackForNextLog() { Instance.m_ignoreCallStackForNextLog = true; }

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

    void Start()
    {
        m_console.SetActive(false);
        m_consoleImage.raycastTarget = m_console.activeSelf;
    }

    void FixedUpdate()
    {

    }

    void Update()
    {
        Vector2 mousePosLocal = GfcCursor.MousePosition - m_console.transform.position.xy();
        //Debug.Log("Mouse Pos relative to console: " + mousePosLocal);

        if (Input.GetKeyDown(m_consoleKeycode))
        {
            m_console.SetActive(!m_console.activeSelf);
            m_consoleImage.raycastTarget = m_console.activeSelf;

            m_scalableWindow.enabled = m_console.activeSelf;
            if (!m_console.activeSelf)//console just closed
            {
                m_mustScrollDown |= m_currentYScrollBottom >= m_fullHeight;
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

                m_currentYScrollBottom = MathF.Max(m_visibleViewport.rect.height, MathF.Min(m_fullHeight, m_currentYScrollBottom + scrollAmmount));
                UpdateScrollbar();

                if (VerifyShownLogIntegrity()) //move the window only if the log is valid
                    AdjustTextBoxPositionY();
            }

            if (m_focusOnCommandLine)
            {
                m_focusOnCommandLine = false;

                m_commandText.Select();
                m_commandText.ActivateInputField();
            }
        }
    }

    void OnEnable()
    {
        if (this != Instance)
        {
            Destroy(Instance);
            Instance = this;
            UpdateLogHeights();
            UpdateShowErrors();
            UpdateShowLogs();
            UpdateShowWarnings();
            UpdateShowCommands();
        }
    }

    private void OnDestroy()
    {
        DeinitLog();
    }

    private void InitializeLog()
    {
        RegisterCommand("Vera", VeraCommand);

        m_countCommand = 0;
        m_countError = 0;
        m_countLog = 0;
        m_countWarn = 0;
        float consoleVisibleHeight = m_visibleViewport.rect.height;
        m_currentViewportSize = new(m_visibleViewport.rect.width, consoleVisibleHeight);

        m_currentYScrollBottom = consoleVisibleHeight;
        m_desiredLogHeight = consoleVisibleHeight * 1.5f;
        m_consoleText.scrollSensitivity = 0;

        Instantiate(m_consoleText.gameObject, m_consoleText.transform.parent, false).GetComponent(ref m_consoleTextCopyForHeight);
        m_consoleTextCopyForHeight.gameObject.SetActive(false);

        m_textViewport = m_consoleText.GetComponent<RectTransform>();
        m_consoleText.text = m_consoleStringBuffer;

        m_mustScrollDown = true;
        Application.logMessageReceived += Log;

        m_logsList = new(256);
        string initLog = "\n\n" + Application.productName + " " + Application.version + " LOG CONSOLE BETA:\n";
        Log(initLog, null, LogType.Log);

        WriteDownFromIndex(0);
        UpdateScrollbar();
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
        Application.logMessageReceived -= Instance.Log;
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
                    m_lastUpdatedIndexHeight = -1;
                    m_fullHeight = 0;

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

                    float desiredYScroll = m_currentYScrollBottom + diff;
                    m_currentYScrollBottom = MathF.Max(height, MathF.Min(m_fullHeight, desiredYScroll));

                    if (draggingDown)
                    {
                        yScrollDiff = m_currentYScrollBottom - desiredYScroll;
                    }
                    else if (desiredYScroll >= m_fullHeight && m_currentYScrollBottom - height >= 0) //dragging up
                    {
                        yScrollDiff -= diff;
                    }

                    if (VerifyShownLogIntegrity())
                    {
                        Debug.Log("NOOOOOOOOOO I GOT HERE");
                        // var currentPos = m_textViewport.localPosition;
                        //currentPos.y += yScrollDiff;
                        //m_textViewport.localPosition = currentPos;
                    }

                    UpdateScrollbar();
                }
            }
        }
    }

    private float CalculateLogHeight(ref GfConsoleLog aLog)
    {
        m_consoleTextCopyForHeight.text = aLog.Text + '.';
        aLog.StartPosY = m_fullHeight;
        aLog.Height = m_consoleTextCopyForHeight.preferredHeight;

        if (null != aLog.StackTrace)
        {
            m_consoleTextCopyForHeight.text = aLog.StackTrace + '.';
            aLog.Height += m_consoleTextCopyForHeight.preferredHeight;
        }

        return aLog.Height;
    }

    private void UpdateLogHeights()
    {
        Profiler.BeginSample("GfCommandConsole.UpdateLogHeights()");

        int length = m_logsList.Count;
        for (int i = m_lastUpdatedIndexHeight + 1; i < length; ++i)
        {
            GfConsoleLog log = m_logsList[i];
            m_fullHeight += CalculateLogHeight(ref log);

            m_logsList[i] = log;
        }

        m_lastUpdatedIndexHeight = length - 1;

        Profiler.EndSample();

        UpdateScrollbar();
    }

    public void CommandEntered(string aCommand)
    {
        var stringBufferCommand = GetAuxStringBuffer();
        stringBufferCommand.Append(aCommand);
        stringBufferCommand.StrLwr();

        m_currentLogIsCommand = true;
        Debug.Log(stringBufferCommand);

        if (m_commands.TryGetValue(stringBufferCommand, out Action action))
            action.Invoke();
        else
        {
            IgnoreCallStackForNextLog();
            Debug.LogWarning("Command '" + COMMAND_OPEN_TAG + stringBufferCommand + COLOR_CLOSE_TAG + "' not found.");
        }

        stringBufferCommand.Clear();
        if (m_scrollDownOnCommand)
            WriteFromBottom();
    }

    public void Log(string aLogString, string aStackTrace, LogType aType)
    {
        Profiler.BeginSample("GfCommandConsole.Log()");

        GfLogType gfLogType = GfLogType.LOG;
        bool showStack = m_showStackTrace;

        switch (aType)
        {
            case LogType.Error:
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case LogType.Exception:
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case LogType.Assert:
                return; //delme
                ++m_countError;
                gfLogType = GfLogType.ERROR;
                break;

            case LogType.Warning:
                ++m_countWarn;
                gfLogType = GfLogType.WARNING;
                break;

            case LogType.Log:
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

        if (m_showLogTime)
        {
            var stringBuffer = GetAuxStringBuffer();

            //Time appends
            m_timeStampStringBuffer.Append('[');
            m_timeStampStringBuffer.Append(DateTime.Now.ToLongTimeString());
            m_timeStampStringBuffer.Append(']');
            m_timeStampStringBuffer.Append(' ');
            m_timeStampStringBuffer.Append(aLogString);
            aLogString = m_timeStampStringBuffer.GetStringCopy();
            m_timeStampStringBuffer.Clear();
        }

        if (m_ignoreCallStackForNextLog || !showStack || null == aStackTrace || aStackTrace.Length == 0)
            aStackTrace = null;

        m_ignoreCallStackForNextLog = false;

        GfConsoleLog log = new(aLogString, aStackTrace, m_fullHeight, gfLogType);
        m_logsList.Add(log);

        m_mustRedoText = true;
        m_mustScrollDown |= m_currentYScrollBottom >= m_fullHeight - 20;
        if (m_console && m_console.activeSelf)
        {
            UpdateLogHeights();
            UpdateLogCounters();
            if (m_mustScrollDown) //must scroll down if we can see the bottom
            {
                WriteFromBottom();
                m_mustRedoText = false;
                m_mustScrollDown = false;
            }
        }

        Profiler.EndSample();
    }

    protected unsafe void WriteLog(int aLogIndex)
    {
        bool mustCloseColorTag = false;
        GfConsoleLog log = m_logsList[aLogIndex];

        switch (log.LogType)
        {
            case GfLogType.ERROR:
                mustCloseColorTag = true;
                m_consoleStringBuffer.Append(ERROR_OPEN_TAG);
                break;

            case GfLogType.WARNING:
                mustCloseColorTag = true;
                m_consoleStringBuffer.Append(WARN_OPEN_TAG);
                break;

            case GfLogType.COMMAND:
                mustCloseColorTag = true;
                m_consoleStringBuffer.Append(COMMAND_OPEN_TAG);
                break;
        }

        m_consoleStringBuffer.Append(log.Text);

        if (mustCloseColorTag)
            m_consoleStringBuffer.Append(COLOR_CLOSE_TAG);

        if (null != log.StackTrace && log.StackTrace.Length > 0)
        {
            switch (log.LogType)
            {
                case GfLogType.ERROR:
                    m_consoleStringBuffer.Append(STACK_ERROR_OPEN_TAG);
                    break;

                case GfLogType.WARNING:
                    m_consoleStringBuffer.Append(STACK_WARN_OPEN_TAG);
                    break;

                case GfLogType.LOG:
                    m_consoleStringBuffer.Append(STACK_LOG_OPEN_TAG);
                    break;
            }

            m_consoleStringBuffer.Append('\n');
            m_consoleStringBuffer.Append(log.StackTrace);
            m_consoleStringBuffer.Append(COLOR_CLOSE_TAG);
        }
    }

    void WriteFromBottom()
    {
        float shownLogsHeight = 0;
        int logIndex = m_logsList.Count - 1;
        m_currentYScrollBottom = MathF.Max(m_fullHeight, m_visibleViewport.rect.height);

        while (logIndex > -1 && shownLogsHeight < m_desiredLogHeight)
            shownLogsHeight += m_logsList[logIndex--].Height;

        WriteDownFromIndex(++logIndex, true);
    }

    protected void OnRectTransformDimensionsChange()
    {
        m_mustResize = true;
        ResizeWindow();
    }

    public int lastCount = -1;
    public int lastIndex = -1;

    int GetLogIndexAtY(float aPosY)
    {
        aPosY.ClampSelf(0, m_fullHeight);

        int low = 0, high = m_logsList.Count - 1;
        while (low <= high) //perform a binary seach to find log closest to desired height
        {
            int mid = (low + high) >> 1;
            float startPointMid = m_logsList[mid].StartPosY;

            if (startPointMid <= aPosY && aPosY <= startPointMid + m_logsList[mid].Height)
                return mid;

            if (startPointMid < aPosY)
                low = mid + 1;
            else
                high = mid - 1;
        }

        return low;
    }

    void WriteFromYPosition(float aShownLogStartY) { WriteDownFromIndex(GetLogIndexAtY(aShownLogStartY)); }

    void UpdateConsoleText()
    {
        Profiler.BeginSample("GfCommandConsole.UpdateConsoleText()");
        m_consoleText.text = m_consoleStringBuffer;
        m_consoleText.ForceLabelUpdate();
        Profiler.EndSample();
    }

    public float visibleHeight;

    void AdjustTextBoxPositionY()
    {
        visibleHeight = m_visibleViewport.rect.height;
        m_textViewport.SetPosY(m_currentYScrollBottom - m_visibleViewport.rect.height - m_shownLogStartY);
    }

    void WriteDownFromIndex(int aLogIndex, bool aDrawUntilLast = false)
    {
        Profiler.BeginSample("GfCommandConsole.WriteDownFromIndex()");

        m_consoleStringBuffer.Clear();

        if (m_logsList.Count > aLogIndex && aLogIndex >= 0)
        {
            int initialIndex = aLogIndex;
            m_shownLogsHeight = 0;
            m_shownLogStartY = m_logsList[aLogIndex].StartPosY;

            while (aLogIndex < m_logsList.Count && (aDrawUntilLast || m_shownLogsHeight < m_desiredLogHeight))
            {
                if (initialIndex != aLogIndex)
                    m_consoleStringBuffer.Append('\n');

                m_shownLogsHeight += m_logsList[aLogIndex].Height;
                WriteLog(aLogIndex);
                aLogIndex++;
            }

            m_shownLogEndY = m_shownLogStartY + m_shownLogsHeight;

            UpdateConsoleText();
            AdjustTextBoxPositionY();
            UpdateScrollbar();
        }
        else
        {
            if (m_logsList.Count == 0)
            {
                m_consoleStringBuffer.Clear();
                UpdateConsoleText();
                m_shownLogsHeight = m_shownLogStartY = 0;
                m_shownLogEndY = m_visibleViewport.rect.height;
            }

            if (aLogIndex < 0 || m_logsList.Count >= m_logsList.Count)
                Debug.LogError("Bad index, count of " + m_logsList.Count + ", index " + aLogIndex);
        }

        Profiler.EndSample();
    }

    /*
    Make sure the shown log isn't overshooting the written log and that the console doesn't show blank spots
    @return Return true if the log was recalculated, 
    */
    protected bool VerifyShownLogIntegrity()
    {
        float visibleHeight = m_visibleViewport.rect.height;
        m_consoleWindowStartY = MathF.Max(0, m_currentYScrollBottom - visibleHeight);
        bool validLog = m_currentYScrollBottom <= m_shownLogEndY && m_consoleWindowStartY >= m_shownLogStartY;

        if (!validLog)
        {
            float newTop = m_currentYScrollBottom - 0.5f * (visibleHeight + m_desiredLogHeight);
            WriteFromYPosition(newTop);
        }

        return validLog;
    }

    private void UpdateScrollbar()
    {
        Profiler.BeginSample("GfCommandConsole.UpdateScrollbar()");
        float visibleHeight = m_visibleViewport.rect.height;
        float estimatedBottomConsoleTopPos = m_fullHeight - visibleHeight; //the top position of the console when we are at the bottom of the log
        float consoleTopYPos = MathF.Max(0, m_currentYScrollBottom - visibleHeight);

        if (0 < estimatedBottomConsoleTopPos)
            m_scrollBar.value = MathF.Max(0, MathF.Min(1, consoleTopYPos / estimatedBottomConsoleTopPos));
        else
            m_scrollBar.value = 1;

        m_scrollBar.size = MathF.Max(MathF.Min(1.0f, visibleHeight / m_fullHeight), 0.1f);
        m_lastScrollValue = m_scrollBar.value;
        Profiler.EndSample();
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

    public void ScrollToTop() { m_scrollBar.value = 0; }

    public static GfcStringBuffer GetAuxStringBuffer()
    {
        Debug.Assert(Instance.m_auxStringBuffer.Length == 0, "The string buffer is used at the same time somewhere or it wasn't cleared after use.");
        Instance.m_auxStringBuffer.Clear();
        return Instance.m_auxStringBuffer;
    }

    public static void RegisterCommand(string aCommand, Action anAction)
    {
        Debug.Assert(!aCommand.IsEmpty());
        Debug.Assert(anAction != null);

        var stringBufferCommand = GetAuxStringBuffer();
        stringBufferCommand.Append(aCommand);
        stringBufferCommand.StrLwr();

        Instance.m_commands.Add(stringBufferCommand, anAction);
        stringBufferCommand.Clear();
    }

    private static void VeraCommand()
    {
        Debug.Log("I love Vera so much, best doggo <3");
    }
}

[Serializable]
internal struct LogTypeToggle
{
    public LogTypeToggle(Toggle toggle = null, TMP_Text text = null)
    {
        Toggle = toggle;
        Text = text;
    }

    public Toggle Toggle;
    public TMP_Text Text;
}

internal enum GfLogType
{
    LOG, WARNING, ERROR, COMMAND
}

internal struct GfConsoleLog
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
    public readonly override string ToString() { return Text; }
}