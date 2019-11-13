using System.Collections;
using System.Collections.Generic;
using Scenes.scripts;
using UnityEngine;

public class SkillStateExit : StateMachineBehaviour
{
    private PlayerMovement playerMovement;
    private static readonly int Skill = Animator.StringToHash("Skill");
    private static readonly int isIdleSword = Animator.StringToHash("IsIdleSword");

    // OnStateEnter is called when a transition starts and the state machine starts to evaluate this state
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerMovement = FindObjectOfType<PlayerMovement>();
        animator.SetBool(isIdleSword, true);
        //currentSkillCode = animator.GetInteger(Skill);
    }

    // OnStateUpdate is called on each Update frame between OnStateEnter and OnStateExit callbacks
    override public void OnStateUpdate(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        playerMovement.isAttacking = true;
        //playerMovement.currentSkillCode = animator.GetInteger(Skill);
        playerMovement.moment = Vector2.zero;
    }

    // OnStateExit is called when a transition ends and the state machine finishes evaluating this state
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        //currentSkillCode = 0;
        playerMovement.currentSkillCode = animator.GetInteger(Skill);
        animator.SetInteger(Skill, 0);
        playerMovement.isAttacking = false;
        //playerMovement.currentSkillCode = 0;
        /*var skillCode = animator.GetInteger(Skill);
        var skillName = /*"trail_" + #1#playerMovement.intSkillToStr(skillCode);
        playerMovement.showOrHideSkillEffect(playerMovement.comboDic[skillName], 0,0);*/
        //Timer.Register(0.01f, () => {  });
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