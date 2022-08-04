using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAnimationScript : MonoBehaviour
{
    //1.9 walk speed
    //5.3 run speed
    public float walkSpeed = 1.9f;
    public float runSpeed = 5.3f;

    public bool playDoubleJump = false;


    private Animator animator;
    private MovementAdvanced player;
    // Start is called before the first frame update
    void Start()
    {
        player = FindObjectOfType<MovementAdvanced>();
        animator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void LateUpdate()
    {
        animator.SetBool("isGrounded", player.isGrounded);
        Vector3 velocity = player.GetVelocity();
        float horizontalSpeed = player.targetSpeed;
        // Debug.Log("horizontal spiied iss " + horizontalSpeed);
        animator.SetFloat("HorizontalSpeed", horizontalSpeed);
        animator.SetFloat("runCoef", horizontalSpeed / runSpeed);
        animator.SetFloat("walkCoef", horizontalSpeed / walkSpeed);
        animator.SetFloat("VerticalSpeed", player.GetVelocity().y);
        animator.SetFloat("TurnAmount", Mathf.Abs(player.turnAmount));
        animator.SetBool("TurningClockwise", player.turnAmount >= 0);
        AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

        if (player.isAttachedToWall)
        {
            if (!state.IsName("Climb up wall"))
            {
                animator.Play("Climb up wall");
            }
        }
        else
        {
            if (player.isGrounded)
            {
                playDoubleJump = true;
            }
            else //not grounded
            {

                // Debug.Log("I am not grounded");
                if (!(state.IsName("jumping up") || state.IsName("falling idle")))
                {
                    if (velocity.y <= 0)
                    {
                        animator.Play("falling idle");
                    }
                    else
                    {
                        animator.Play("jumping up");
                    }
                }
                else //if one of them is playing 
                {
                    if (playDoubleJump && !player.canDoubleJump)
                    {
                        animator.SetTrigger("HasToDoubleJump");
                        //animator.Play("Backflip");
                        //    Debug.Log("played something");
                        playDoubleJump = false;
                    }
                }
            }
        }
    }
}
