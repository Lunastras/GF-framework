using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationManager : MonoBehaviour
{
    [SerializeField]
    private Sprite3DAnimator spriteAnimator;
    [SerializeField]
    private MovementAdvanced playerController;
    [SerializeField]
    private QuadSpriteGraphics sprite;

    [SerializeField]
    private float walkAnimationSpeed;

    private float walkFramesNum = 4;

    private bool playedJumpAnimation;
    private bool isAttachedToWall;
    private bool isSlidingOffWall;
    private bool isGrounded;
    private float currentSpeed;

    // Start is called before the first frame update
    void Start()
    {
        if (spriteAnimator == null)
        {
            spriteAnimator = GetComponent<Sprite3DAnimator>();
        }

        if (playerController == null)
        {
            playerController = GetComponent<MovementAdvanced>();
        }

        if (sprite == null)
        {
            sprite = GetComponent<QuadSpriteGraphics>();
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (playerController == null)
            return;

        isAttachedToWall = playerController.isAttachedToWall;
        isGrounded = playerController.isGrounded;
        Vector2 velocity = new Vector2(playerController.GetVelocity().x, playerController.GetVelocity().z);
        currentSpeed = velocity.magnitude;
        isSlidingOffWall = playerController.isSlidingOffWall;

        UpdateAnimationValues();
    }

    private void UpdateAnimationValues()
    {
        if (spriteAnimator != null)
        {
            spriteAnimator.PlayState(0, true, spriteAnimator.currentFrame);
            // spriteAnimator.Play(sprite.currentRotationIndex, true, frameToPlay);
            //spriteAnimator.SetFlipX(sprite.isMirrored);
            spriteAnimator.speedMultiplier = currentSpeed / walkAnimationSpeed;
        }
    }
}
