using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Sprite3DAnimator : MonoBehaviour
{
    [SerializeField]
    public Transform objectTransform;

    [SerializeField]
    private SpriteRenderer spriteRenderer;

    [SerializeField]
    public AnimationSpriteState[] animationStates;

    public float speedMultiplier { get; set; }

    public float timeUntilNextFrame { get; protected set; }

    public bool playing { get; protected set; }
    public Animation currentAnimation { get; protected set; }
    public int currentFrame { get; protected set; }
    public bool loop { get; protected set; }
    //public int numTi

    public int playAnimationOnStart = 0;

    private AnimationSpriteState currentState;
    private int currentViewIndex;

    private int currentStateIndex = -1;



    // private Dictionary<string, AnimationSpriteState> statesDictionary;
    // private Dictionary<string, int> stateStartIndex;

    void Awake()
    {
        PlayState(playAnimationOnStart);

        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (!objectTransform)
            //most likely not something you want because 
            //3d quads will most likely follow the camera
            objectTransform = transform;
    }

    protected void FixedUpdate()
    {
        UpdateStateIndex();
        timeUntilNextFrame = Mathf.Max(-1, timeUntilNextFrame - Time.deltaTime * speedMultiplier); //
        //Debug.Log("The until next frame " + timeUntilNextFrame);

        if (timeUntilNextFrame <= 0)
            NextFrame();
    }



    private void CalculateAnimationIndex()
    {
        bool isMirrored = false;

        int numberSides = currentState.animations.Length * (currentState.rotationMode != RotationModes.NO_REPEAT ? 2 : 1);
        // numberSides -= 2 * ()

        float degreesBetweenSteps = 360.0f / (float)numberSides;

        Vector3 mainCameraForward = Camera.main.transform.forward;
        Vector2 transForward = new Vector2(objectTransform.forward.x, objectTransform.forward.z);
        Vector2 cameraForward = new Vector2(mainCameraForward.x, mainCameraForward.z);

        float dot = Vector2.Dot(transForward, cameraForward);
        float det = transForward.x * cameraForward.y - transForward.y * cameraForward.x;
        float angleToCamera = Mathf.Atan2(det, dot) * Mathf.Rad2Deg;

        angleToCamera += degreesBetweenSteps / 2 + currentState.rotationOffsetCounter;

        angleToCamera += (angleToCamera < 0) ? 360f : 0f;
        currentViewIndex = (int)(angleToCamera / degreesBetweenSteps);

        float numSidesDiv2 = numberSides / 2 + 1;

        switch (currentState.rotationMode)
        {
            case (RotationModes.MIRROR_LEFT):
                isMirrored = currentViewIndex > numSidesDiv2;
                currentViewIndex = isMirrored ? (numberSides - currentViewIndex) : currentViewIndex;
                break;

            case (RotationModes.MIRROR_RIGHT):
                break;

            case (RotationModes.NO_MIRROR):
                break;

            case (RotationModes.NO_REPEAT):
                break;
        }

        currentViewIndex = Mathf.Min(currentViewIndex, currentState.animations.Length - 1);
        spriteRenderer.flipX = isMirrored;
    }

    protected void OnEnable()
    {
        PlayState(playAnimationOnStart);
    }

    protected void OnDisable()
    {
        playing = false;
        currentAnimation = null;
    }

    public AnimationSpriteState GetState(int stateId)
    {
        return animationStates[stateId];
    }

    protected void UpdateStateIndex()
    {
        CalculateAnimationIndex();

        currentAnimation = currentState.animations[currentViewIndex];
        playing = true;
        spriteRenderer.sprite = currentAnimation.frames[currentFrame];
    }

    public void PlayState(int stateIndex, bool loops = true, int startFrame = 0)
    {
        if (stateIndex != currentStateIndex)
        {
            currentState = animationStates[stateIndex];
            currentStateIndex = stateIndex;
            InternalForcePlay(currentState.animations[stateIndex], loop, startFrame);
        }
    }

    public void ForcePlayState(int stateIndex, bool loops = true, int startFrame = 0)
    {
        currentState = animationStates[stateIndex];
        currentStateIndex = stateIndex;
        InternalForcePlay(currentState.animations[currentViewIndex], loops, startFrame);
    }

    protected void InternalForcePlay(Animation animation, bool loop = true, int startFrame = 0)
    {
        startFrame = Mathf.Min(startFrame, animation.frames.Length - 1);
        this.loop = loop;
        currentAnimation = animation;
        currentFrame = startFrame;
        playing = true;
        spriteRenderer.sprite = animation.frames[currentFrame];
        timeUntilNextFrame = currentAnimation.timeUntilNextFrame[Mathf.Min(currentAnimation.timeUntilNextFrame.Length - 1, currentFrame)];
    }


    protected void NextFrame()
    {
        currentFrame++;
        timeUntilNextFrame = currentAnimation.timeUntilNextFrame[Mathf.Min(currentAnimation.timeUntilNextFrame.Length - 1, currentFrame)];
       // Debug.Log("The next frame is in " + timeUntilNextFrame);

        if (currentFrame >= currentAnimation.frames.Length)
        {
            if (loop)
            {
                currentFrame = 0;
            }
            else
            {
                currentFrame = currentAnimation.frames.Length - 1;
                int nextStateId = currentState.nextState;
                if (0 <= nextStateId && animationStates.Length > nextStateId)
                {
                    ForcePlayState(currentState.nextState, true, 0);
                } else
                {
                    currentFrame--;
                }
            }
        }

        spriteRenderer.sprite = currentAnimation.frames[currentFrame];
    }
}

[System.Serializable]
public class Animation
{
    public float[] timeUntilNextFrame = { 0.1f };
    public Sprite[] frames;
}
/// <summary>
/// The mirroring mode of the 3d animation
/// MIRROR_LEFT mirrors the sprite on the animations seen from the object's left
/// MIRROR_RIGHT mirrors the sprite on the animations seen from the object's right
/// NO_MIRROR reuses the same animations on the left and right without mirroring them
/// NO_REPEAT will not repeat any sprite index, and will treat the given animation states as a full rotation
/// </summary>

[System.Serializable]
public enum RotationModes
{
    MIRROR_LEFT,
    MIRROR_RIGHT,
    NO_MIRROR,
    NO_REPEAT
}

[System.Serializable]
public class AnimationSpriteState
{
    // [SerializeField]
    //public string stateName;
    [SerializeField]
    public int nextState = -1;
    [SerializeField]
    public float rotationOffsetCounter;

    [SerializeField]
    public RotationModes rotationMode;

    [SerializeField]
    public Animation[] animations;
}
