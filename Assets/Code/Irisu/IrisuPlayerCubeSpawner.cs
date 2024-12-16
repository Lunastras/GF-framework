using Unity.Mathematics;
using UnityEngine;

public class IrisuPlayerCubeSpawner : MonoBehaviour
{
    public Vector3 CubePositionOffset = new(0, 1, 0);
    public GameObject CubePrefab;
    public float CubeLength = 0.3f;
    public GfcInputType CubeSpawnInput = GfcInputType.SUBMIT;

    GfcInputTracker m_spawnTracker;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        m_spawnTracker = new(CubeSpawnInput);
        m_spawnTracker.DisplayPromptString = new("Spawn Cube");
        GfcPooling.Pool(CubePrefab, 8);
    }

    // Update is called once per frame
    void Update()
    {
        if (m_spawnTracker.PressedSinceLastCheck())
        {
            IrisuBlock block = GfcPooling.Instantiate(CubePrefab).GetComponent<IrisuBlock>();
            block.SetIsPlayerCube(true);
            Vector3 position = GfcCamera.MainCamera.ScreenToWorldPoint(GfcCursor.MousePositionWithDepth(0));
            position.z = 0;
            block.transform.SetParent(IrisuManagerGame.GetBlocksParent());
            GfcTools.Add(ref position, CubePositionOffset);
            block.transform.localPosition = position;
            block.transform.localScale = new(CubeLength, CubeLength, CubeLength);
            block.transform.rotation = Quaternion.identity;
        }
    }
}
