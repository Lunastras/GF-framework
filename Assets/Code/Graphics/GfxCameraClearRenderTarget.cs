using UnityEngine;

[RequireComponent(typeof(Camera))]
public class GfxCameraClearRenderTarget : MonoBehaviour { void OnDestroy() { GetComponent<Camera>().targetTexture = null; } }