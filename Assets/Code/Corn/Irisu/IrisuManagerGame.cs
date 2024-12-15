using System.Collections.Generic;
using UnityEngine;
using MEC;
using UnityEngine.UIElements;

public class IrisuManagerGame : MonoBehaviour
{
    private static IrisuManagerGame Instance;

    [SerializeField] private Transform m_blocksParent;
    [SerializeField] private Transform m_spawnSquare;
    [SerializeField] private EnumSingletons<GameObject, IrisuShapeType> m_blocks;

    [SerializeField] private int m_pointsPerBlock = 100;
    [SerializeField] private float m_hyperMultiplier = 3;

    [SerializeField] private AnimationCurve m_difficultyCurve;
    [SerializeField] private Vector2 m_spawnDelayEasyHard = new(3, 1);
    [SerializeField] private Vector2 m_spawnCountEasyHard = new(4, 10);
    [SerializeField] private Vector2 m_fallSpeedEasyHard = new(0.5f, 3.0f);
    [SerializeField] private Vector2 m_countColorsEasyHard = new(3, (int)IrisuColorType.COUNT);
    [SerializeField] private Vector2 m_hpLostPerSecondEasyHard = new(0.01f, 0.03f);

    [SerializeField] private float m_maxDifficultyScore = 60000;
    [SerializeField] private float m_hpLostOnStaticBlock = 0.1f;
    [SerializeField] private float m_pointsHpCoefGain = 0.001f;


    private int m_points = 0;
    private float m_hp = 1;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake() { this.SetSingleton(ref Instance); }

    void Start()
    {
        m_blocks.Validate(IrisuShapeType.COUNT);
        for (int i = 0; i < m_blocks.Length; i++)
            GfcPooling.Pool(m_blocks[i], 8);

        Timing.RunCoroutine(_ExecuteGame().CancelWith(gameObject));
    }

    private IEnumerator<float> _ExecuteGame()
    {
        //spawn initial shapes
        while (m_hp > 0)
        {
            float difficulty = GetDifficulty();
            int spawnCount = (int)GetValueForDifficulty(m_spawnCountEasyHard, difficulty);

            for (int i = 0; i < spawnCount; i++)
            {
                int shapeIndex = Random.Range(0, (int)IrisuShapeType.COUNT);
                Transform newShape = GfcPooling.Instantiate(m_blocks[shapeIndex]).transform;
                Vector3 position = m_spawnSquare.position;
                Vector3 spawnScale = m_spawnSquare.localScale;
                position.x += Random.Range(-spawnScale.x * 0.5f, spawnScale.x * 0.5f);
                position.y += Random.Range(-spawnScale.y * 0.5f, spawnScale.y * 0.5f);
                position.z = 0;
                newShape.SetParent(m_blocksParent);
                newShape.localPosition = position;
                newShape.GetComponent<IrisuBlock>().SetIsPlayerCube(false);
            }

            yield return Timing.WaitForSeconds(GetValueForDifficulty(m_spawnDelayEasyHard, difficulty));
        }

        Debug.Log("Game over!");
    }

    public static void ClearBlock(IrisuBlock aBlock)
    {
        float points = Instance.m_pointsPerBlock;
        if (aBlock.GetState() == IrisuBlockState.HYPER)
            points *= Instance.m_hyperMultiplier;

        points *= aBlock.transform.localScale.x;
        Instance.m_points += (int)points;
        GfcPooling.Destroy(aBlock.gameObject);
    }

    public static void AddPoints(int points)
    {
        Instance.m_hp += Instance.m_pointsHpCoefGain * points;
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

    }

    public static Transform GetBlocksParent() { return Instance.m_blocksParent; }
    public static float GetFallingSpeed() { return Instance.GetValueForDifficulty(Instance.m_fallSpeedEasyHard); }
    public static int GetColorsCount() { return (int)Instance.GetValueForDifficulty(Instance.m_countColorsEasyHard); }
    private float GetValueForDifficulty(Vector2 aValuesRangeEasyHard) { return GetValueForDifficulty(aValuesRangeEasyHard, GetDifficulty()); }
    private float GetValueForDifficulty(Vector2 aValuesRangeEasyHard, float aDifficulty) { return aValuesRangeEasyHard.x.Lerp(aValuesRangeEasyHard.y, aDifficulty); }
    private float GetDifficulty() { return m_difficultyCurve.Evaluate((m_points / m_maxDifficultyScore).Min(1)); }
}

public enum IrisuShapeType
{
    SQUARE,
    TRIANGLE,
    COUNT
}
