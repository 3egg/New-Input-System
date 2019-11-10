using System.Collections;
using System.Collections.Generic;
using Scenes.scripts;
using UnityEngine;

public class IdleSwordState : StateMachineBehaviour
{
    private static readonly int isIdleSword = Animator.StringToHash("IsIdleSword");
    private PlayerMovement playerMovement;


    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        Timer.Register(1f, () => animator.SetBool(isIdleSword, false));
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        /*if (playerMovement.moment.x > 0 || playerMovement.moment.y > 0)
        {
        }*/
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //playerMovement.moment = Vector2.zero;
    }

    // OnStateMove is called right after Animator.OnAnimatorMove()
    //override public void OnStateMove(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that processes and affects root motion
    //}

    // OnStateIK is called right after Animator.OnAnimatorIK()
    //override public void OnStateIK(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    //{
    //    // Implement code that sets up animation IK (inverse kinematics)
    //}
}