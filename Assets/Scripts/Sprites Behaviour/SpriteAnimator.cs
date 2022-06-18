using UnityEngine;
using System.Collections;

public class SpriteAnimator : MonoBehaviour
{
    [System.Serializable]
    public class Animation
    {
        public string name;
        public float[] timeUntilNextFrame = { 0.1f };
        public Sprite[] frames;
    }

    public float speedMultiplier { get; set; }
    public Animation[] animations;
    public SpriteRenderer spriteRenderer;

    public float timeOfFrameUpdate { get; protected set; }

    public bool playing { get; protected set; }
    public Animation currentAnimation { get; protected set; }
    public int currentFrame { get; protected set; }
    public bool loop { get; protected set; }
    //public int numTi

    public string playAnimationOnStart;

    protected void Awake()
    {
        if (!spriteRenderer)
            spriteRenderer = GetComponent<SpriteRenderer>();
    }

    protected void OnEnable()
    {
        if (playAnimationOnStart != "")
            Play(playAnimationOnStart);
    }

    protected void OnDisable()
    {
        playing = false;
        currentAnimation = null;
    }

    public void ForcePlay(int animationIndex, bool loop = true, int startFrame = 0)
    {
        InternalForcePlay(animations[animationIndex], loop, startFrame);
    }

    public void ForcePlay(string name, bool loop = true, int startFrame = 0)
    {
        InternalPlay(GetAnimation(name), loop, startFrame);
    }

    public void Play(int animationIndex, bool loop = true, int startFrame = 0)
    {
        InternalPlay(animations[animationIndex], loop, startFrame);
    }

    public void Play(string name, bool loop = true, int startFrame = 0)
    {
        InternalPlay(GetAnimation(name), loop, startFrame);
    }

    protected void InternalPlay(Animation animation, bool loop = true, int startFrame = 0)
    {
        startFrame = startFrame % animation.frames.Length;
        if (animation != null)
        {
            if (animation != currentAnimation)
            {
                InternalForcePlay(animation, loop, startFrame);
            }
        }
        else
        {
            Debug.LogWarning("could not find animation: " + name);
        }
    }

    protected void InternalForcePlay(Animation animation, bool loop = true, int startFrame = 0)
    {
        startFrame = startFrame % animation.frames.Length;
        if (animation != null)
        {
            this.loop = loop;
            currentAnimation = animation;
            playing = true;
            timeOfFrameUpdate = Time.time;
            currentFrame = startFrame;
            spriteRenderer.sprite = animation.frames[currentFrame];
        }
    }

    public bool IsPlaying(string name)
    {
        return (currentAnimation != null && currentAnimation.name == name);
    }

    public Animation GetAnimation(string name)
    {
        foreach (Animation animation in animations)
        {
            if (animation.name == name)
            {
                return animation;
            }
        }
        return null;
    }

    protected void Update()
    {
        if (currentAnimation == null)
            return;

        float timeSinceLastFrameUpdate = Time.time - timeOfFrameUpdate;
        float delay = currentAnimation.timeUntilNextFrame[currentFrame % currentAnimation.timeUntilNextFrame.Length];
        delay = (speedMultiplier == 0 ? 0 : delay / speedMultiplier);

        if (delay == 0)
            return;

        float delayRemaining = delay - timeSinceLastFrameUpdate;

        if (delayRemaining < 0)
        {
            NextFrame(currentAnimation, delayRemaining);
        }
    }


    protected virtual void NextFrame(Animation animation, float timeError)
    {
        currentFrame++;
        // timeOfFrameUpdate = Time.time + timeError;
        timeOfFrameUpdate = Time.time;


        if (currentFrame >= animation.frames.Length)
        {
            if (loop)
                currentFrame = 0;
            else
            {
                currentFrame = animation.frames.Length - 1;
                currentAnimation = null;
            }
        }

        spriteRenderer.sprite = animation.frames[currentFrame];
    }

    public int GetFacing()
    {
        return (int)Mathf.Sign(spriteRenderer.transform.localScale.x);
    }

    public void FlipTo(float dir)
    {
        if (dir < 0f)
            spriteRenderer.transform.localScale = new Vector3(-1f, 1f, 1f);
        else
            spriteRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public void FlipTo(Vector3 position)
    {
        float diff = position.x - transform.position.x;
        if (diff < 0f)
            spriteRenderer.transform.localScale = new Vector3(-1f, 1f, 1f);
        else
            spriteRenderer.transform.localScale = new Vector3(1f, 1f, 1f);
    }

    public void SetFlipX(bool isMirrored)
    {
        spriteRenderer.flipX = isMirrored;
    }

    public void SetFlipY(bool isMirrored)
    {
        spriteRenderer.flipY = isMirrored;
    }
}