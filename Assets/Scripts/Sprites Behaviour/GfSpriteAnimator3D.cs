using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static System.MathF;

public class GfSpriteAnimator3D : MonoBehaviour
{
    [SerializeField]
    private Transform m_objectTransform = null;

    [SerializeField]
    private GfSpriteRenderer m_spriteRenderer = null;

    [SerializeField]
    private AnimationSpriteState[] m_animationStates = null;

    //public int numTi
    [SerializeField]
    public int m_playAnimationOnStart = 0;

    [SerializeField]
    protected Vector3 m_defaultCameraUpvec = Vector3.up;

    public float SpeedMultiplier { get; set; } = 1;

    public float TimeUntilNextFrame { get; protected set; }

    public bool Playing { get; protected set; }
    public Animation CurrentAnimation { get; protected set; }
    public int CurrentFrame { get; protected set; }
    public bool Loop { get; protected set; }


    private AnimationSpriteState m_currentState;
    private int m_currentViewIndex;

    private int m_currentStateIndex = -1;

    private bool m_initialised = false;

    // private Dictionary<string, AnimationSpriteState> statesDictionary;
    // private Dictionary<string, int> stateStartIndex;

    void Start()
    {
        m_initialised = true;

        if (!m_spriteRenderer)
            m_spriteRenderer = GetComponent<GfSpriteRenderer>();

        PlayState(m_playAnimationOnStart);

        if (!m_objectTransform)
            //most likely not something you want because 
            //3d quads will probably follow the camera
            m_objectTransform = transform;
    }

    protected void FixedUpdate()
    {
        UpdateStateIndex();
        TimeUntilNextFrame -= Time.deltaTime * SpeedMultiplier;

        if (TimeUntilNextFrame <= 0)
            NextFrame();
    }



    private void CalculateAnimationIndex()
    {
        var cameraController = CameraController.Instance;

        if (cameraController)
        {
            bool isMirrored = false;

            int numberSides = m_currentState.animations.Length * (m_currentState.rotationMode != RotationModes.NO_REPEAT ? 2 : 1);
            if (m_currentState.hasBackAndFrontSprites) numberSides -= 2;
            // numberSides -= 2 * ()

            float degreesBetweenSteps = 360.0f / (float)numberSides;

            Vector3 upVec = null != cameraController ? cameraController.Upvec : m_defaultCameraUpvec;
            Vector3 mainCameraForward = cameraController.transform.forward;
            Vector3 transForward = m_objectTransform.forward;
            GfTools.RemoveAxis(ref mainCameraForward, upVec);
            GfTools.RemoveAxis(ref transForward, upVec);

            float angleToCamera = -GfTools.SignedAngleDeg(mainCameraForward, transForward, upVec);
            if (angleToCamera < 0) angleToCamera += 360;

            angleToCamera += degreesBetweenSteps / 2 + m_currentState.rotationOffsetDegrees;
            angleToCamera = angleToCamera % 360;


            m_currentViewIndex = (int)(angleToCamera / degreesBetweenSteps);

            float numSidesDiv2 = numberSides / 2;

            switch (m_currentState.rotationMode)
            {
                case (RotationModes.MIRROR_LEFT):
                    isMirrored = m_currentViewIndex > numSidesDiv2;
                    m_currentViewIndex = isMirrored ? (numberSides - m_currentViewIndex) : m_currentViewIndex;
                    break;

                case (RotationModes.MIRROR_RIGHT): //todo
                    isMirrored = m_currentViewIndex > numSidesDiv2;
                    m_currentViewIndex = isMirrored ? (numberSides - m_currentViewIndex) : m_currentViewIndex;
                    isMirrored = !isMirrored;
                    break;

                case (RotationModes.NO_MIRROR)://todo
                    m_currentViewIndex = isMirrored ? (numberSides - m_currentViewIndex) : m_currentViewIndex;
                    break;

                case (RotationModes.NO_REPEAT)://todo
                    break;
            }

            m_currentViewIndex = (int)Min(m_currentViewIndex, m_currentState.animations.Length - 1);
            m_spriteRenderer.SetFlippedX(isMirrored);
            //Debug.Log("The angle is: " + angleToCamera + " with a view index of: " + m_currentViewIndex + " and num sides: " + numberSides);
        }
    }

    protected void OnEnable()
    {
        if (m_initialised) PlayState(m_playAnimationOnStart);
    }

    protected void OnDisable()
    {
        Playing = false;
        CurrentAnimation = null;
    }

    public AnimationSpriteState GetState(int stateId)
    {
        return m_animationStates[stateId];
    }

    protected void UpdateStateIndex()
    {
        CalculateAnimationIndex();

        CurrentAnimation = m_currentState.animations[m_currentViewIndex];
        Playing = true;

        if (CurrentFrame >= CurrentAnimation.frames.Length)
            CurrentFrame = 0; //animations are not compatible, must reset the current frame index

        m_spriteRenderer.SetSprite(CurrentAnimation.frames[CurrentFrame]);
    }

    public void PlayNextState(bool loops = true, int startFrame = 0)
    {
        if (m_currentState.nextState >= 0)
            PlayState(m_currentState.nextState, loops, startFrame, true);
    }

    public void PlayState(int stateIndex, bool loops = true, int startFrame = 0, bool forceState = false)
    {
        if (stateIndex != m_currentStateIndex || forceState)
        {
            m_currentState = m_animationStates[stateIndex];
            m_currentStateIndex = stateIndex;
            InternalForcePlay(m_currentState.animations[stateIndex], loops, startFrame);
        }
    }

    protected void InternalForcePlay(Animation animation, bool loop = true, int startFrame = 0)
    {
        startFrame = Mathf.Min(startFrame, animation.frames.Length - 1);
        this.Loop = loop;
        CurrentAnimation = animation;
        CurrentFrame = startFrame;
        Playing = true;
        m_spriteRenderer.SetSprite(animation.frames[CurrentFrame]);
        TimeUntilNextFrame = CurrentAnimation.timeUntilNextFrame[(int)Min(CurrentAnimation.timeUntilNextFrame.Length - 1, CurrentFrame)];
    }


    protected void NextFrame()
    {
        CurrentFrame++;
        TimeUntilNextFrame = CurrentAnimation.timeUntilNextFrame[(int)Min(CurrentAnimation.timeUntilNextFrame.Length - 1, CurrentFrame)];
        // Debug.Log("The next frame is in " + timeUntilNextFrame);

        if (CurrentFrame >= CurrentAnimation.frames.Length)
        {
            if (Loop)
            {
                CurrentFrame = 0;
            }
            else
            {
                CurrentFrame = CurrentAnimation.frames.Length - 1;
                int nextStateId = m_currentState.nextState;
                if (0 <= nextStateId && m_animationStates.Length > nextStateId)
                {
                    PlayState(m_currentState.nextState, true, 0, true);
                }
                else
                {
                    CurrentFrame--;
                }
            }
        }

        m_spriteRenderer.SetSprite(CurrentAnimation.frames[CurrentFrame]);
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
    public float rotationOffsetDegrees;

    [SerializeField]
    public bool hasBackAndFrontSprites; //whether or not the animation contain frames from behind or the front

    [SerializeField]
    public RotationModes rotationMode;

    [SerializeField]
    public Animation[] animations;
}
