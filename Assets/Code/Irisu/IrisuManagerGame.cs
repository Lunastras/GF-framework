using System.Collections.Generic;
using UnityEngine;
using MEC;
using UnityEngine.UI;
using TMPro;

public class IrisuManagerGame : MonoBehaviour
{
    private static IrisuManagerGame Instance;

    [SerializeField] private GameObject m_rainbowClearPrefab;
    [SerializeField] private float m_rainbowClearPrefabSpawnRate = 0.1f;

    [SerializeField] private Image m_hpImage;

    [SerializeField] private TextMeshProUGUI m_textDifficultyLevel;
    [SerializeField] private TextMeshProUGUI m_textScore;

    [SerializeField] private Transform m_blocksParent;
    [SerializeField] private Transform m_spawnSquare;
    [SerializeField] private EnumSingletons<GameObject, IrisuShapeType> m_blocks;

    [SerializeField] private int m_pointsPerBlock = 100;
    [SerializeField] private float m_hyperMultiplier = 3;

    [SerializeField] private AnimationCurve m_difficultyCurve;
    [SerializeField] private AnimationCurve m_colorsDifficultyCurve;

    [SerializeField] private Vector2 m_spawnDelayEasyHard = new(3, 1);
    [SerializeField] private Vector2 m_spawnCountEasyHard = new(4, 10);
    [SerializeField] private Vector2 m_fallSpeedEasyHard = new(0.5f, 3.0f);
    [SerializeField] private Vector2 m_countColorsEasyHard = new(3, (int)IrisuColorType.COUNT);
    [SerializeField] private Vector2 m_hpLostPerSecondEasyHard = new(0.01f, 0.03f);

    [SerializeField] private float m_maxDifficultyScore = 60000;
    [SerializeField] private float m_hpLostOnStaticBlock = 0.1f;
    [SerializeField] private float m_pointsHpCoefGain = 0.001f;

    public static System.Action OnGameFinished;

    private int m_points = 0;
    private float m_hp = 1;

    CoroutineHandle m_gameHandle;

    public static CoroutineHandle GetGameHandle() { return Instance.m_gameHandle; }
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        this.SetSingleton(ref Instance);
        OnGameFinished = null;
    }

    void Start()
    {
        m_blocks.Validate(IrisuShapeType.COUNT);
        for (int i = 0; i < m_blocks.Length; i++)
            GfcPooling.Pool(m_blocks[i], 8);

        GfcPooling.Pool(m_rainbowClearPrefab, 1);

        UpdateHud();
        m_gameHandle = Timing.RunCoroutine(_ExecuteGame().CancelWith(gameObject));
    }

    void FixedUpdate()
    {
        m_hp -= Time.deltaTime * GetValueForDifficulty(m_hpLostPerSecondEasyHard);
        m_hp.ClampSelf(0, 1);
        UpdateHpBar();
    }

    private void InitializeBlock(Transform aBlock, bool aSpawnAsStatic = false, float aYOffset = 0)
    {
        Vector3 position = m_spawnSquare.position;
        Vector3 spawnScale = m_spawnSquare.localScale;
        position.x += Random.Range(-spawnScale.x * 0.5f, spawnScale.x * 0.5f);
        position.y += Random.Range(-spawnScale.y * 0.5f, spawnScale.y * 0.5f) + aYOffset;
        position.z = 0;
        aBlock.SetParent(m_blocksParent);
        aBlock.localPosition = position;
        IrisuBlock irisuBlock = aBlock.GetComponent<IrisuBlock>();
        irisuBlock.SetIsPlayerCube(false);
        if (aSpawnAsStatic)
            irisuBlock.SetState(IrisuBlockState.STATIC, false, false);
    }

    private void SpawnBlocks(int aCountBlocks, bool aSpawnAsStatic = false, float aYOffset = 0)
    {
        for (int i = 0; i < aCountBlocks; i++)
        {
            int shapeIndex = Random.Range(0, (int)IrisuShapeType.COUNT);
            InitializeBlock(GfcPooling.Instantiate(m_blocks[shapeIndex]).transform, aSpawnAsStatic, aYOffset);
        }
    }

    private IEnumerator<float> _ExecuteGame()
    {
        SpawnBlocks(6, true, -7);

        //spawn initial shapes
        while (m_hp > 0)
        {
            float difficulty = GetDifficulty();
            SpawnBlocks((int)GetValueForDifficulty(m_spawnCountEasyHard, difficulty));
            yield return Timing.WaitForSeconds(GetValueForDifficulty(m_spawnDelayEasyHard, difficulty));

            if (Random.Range(0.0f, 1.0f) <= m_rainbowClearPrefabSpawnRate)
                InitializeBlock(GfcPooling.Instantiate(m_rainbowClearPrefab).transform);
        }

        Debug.Log("Game over!");

        OnGameFinished?.Invoke();
    }

    public static void ClearAllBlocksOfColor(IrisuColorType aColor)
    {
        Transform parent = Instance.m_blocksParent;
        for (int i = 0; i < parent.childCount; i++)
        {
            IrisuBlock block = parent.GetChild(i).GetComponent<IrisuBlock>();
            if (block.GetColor() == aColor)
            {
                ClearBlock(block);
                i--;
            }
        }
    }

    public static void ClearBlock(IrisuBlock aBlock)
    {
        float points = Instance.m_pointsPerBlock;
        if (aBlock.GetState() == IrisuBlockState.HYPER)
            points *= Instance.m_hyperMultiplier;

        points *= aBlock.transform.localScale.x;
        AddPoints((int)points);
        GfcPooling.Destroy(aBlock.gameObject);
    }

    public static void AddPoints(int points)
    {
        Instance.m_hp += Instance.m_pointsHpCoefGain * points;
        Instance.m_hp.ClampSelf(0, 1);
        Instance.m_points += points;
        Instance.UpdateHud();
    }

    public static void ShapeWentStatic(IrisuBlock aBlock)
    {
        Instance.m_hp -= Instance.m_hpLostOnStaticBlock;
        Instance.UpdateHud();
    }

    private void UpdateHud()
    {
        UpdateHpBar();
        m_textScore.text = Instance.m_points.ToString();
        m_textDifficultyLevel.text = GetDifficultyLevel().Max(1).ToString();
    }

    public int GetDifficultyLevel() { return (int)(GetDifficulty() * 100.0f); }

    private void UpdateHpBar()
    {
        m_hpImage.fillAmount = m_hp;
    }

    public static Transform GetBlocksParent() { return Instance.m_blocksParent; }
    public static float GetFallingSpeed() { return Instance.GetValueForDifficulty(Instance.m_fallSpeedEasyHard); }
    public static int GetColorsCount() { return (int)Instance.GetValueForDifficulty(Instance.m_countColorsEasyHard, Instance.GetColorDifficulty()).Round(); }
    public static float GetDifficulty() { return Instance.m_difficultyCurve.Evaluate((Instance.m_points / Instance.m_maxDifficultyScore).Min(1)); }
    private float GetValueForDifficulty(Vector2 aValuesRangeEasyHard) { return GetValueForDifficulty(aValuesRangeEasyHard, GetDifficulty()); }
    private float GetValueForDifficulty(Vector2 aValuesRangeEasyHard, float aDifficulty) { return aValuesRangeEasyHard.x.Lerp(aValuesRangeEasyHard.y, aDifficulty); }
    private float GetColorDifficulty() { return m_colorsDifficultyCurve.Evaluate((m_points / m_maxDifficultyScore).Min(1)); }
}

public enum IrisuShapeType
{
    SQUARE,
    TRIANGLE,
    COUNT
}
