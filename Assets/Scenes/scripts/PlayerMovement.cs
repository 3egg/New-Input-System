using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scenes.scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        private Rigidbody player;
        private PlayerInputActions inputActions;

        private Vector2 moment;

        private bool idleSword;
        private bool isPress;
        private float moveSpeed = 5f;
        private float rotateSpeed = 30f;
        private Vector3 moveDirection;
        private Animator animator;
        private const string PlayerParaName = "AniIndex";
        private const string Skill = "Skill";
        private const string IsIdleSword = "IsIdleSword";
        private readonly List<int> skillList = new List<int>();
        private Timer runTimer;
        private Timer skillTimer;
        private Timer effectTimer;
        private bool isAttacking;
        private Transform comboEffect;
        private List<Transform> combos;
        private const string Combo = "combo";
        private const string Trail = "trail";
        private Dictionary<string, Transform> comboDic;

        private void Awake()
        {
            initComobs();
            player = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            inputActions = new PlayerInputActions();
            inputActions.Player.Move.performed += ctx => moment = ctx.ReadValue<Vector2>();
            inputActions.Player.LeftButtonDown.performed += ctx => pressKey(1); //x
            inputActions.Player.RightButtonDown.performed += ctx => pressKey(2); //o
        }

        private void initComobs()
        {
            comboEffect = GetComponentsInChildren<Transform>().First(t => t.name.Equals(Combo));
            combos = new List<Transform>(comboEffect.GetComponentsInChildren<Transform>(true)
                .Where(t => t.name.Contains(Trail)).ToList());
            comboDic = new Dictionary<string, Transform>(combos.Count);
            foreach (var combo in combos)
            {
                comboDic[combo.name.Replace(Trail + "_", "")] = combo;
            }
        }

        private void FixedUpdate()
        {
            movePlayer();
            walkToRun(isPress);
        }

        private void pressKey(int skill)
        {
            //todo 播放完技能之后进入idle_sword状态,没有attack之后一秒后进入默认idle状态
            isAttacking = true;
            skillTimer = Timer.Register(0.3f, () =>
            {
                onComplete(true, skill);
                skillList.Clear();
                isAttacking = false;
            });

            onComplete(false, skill);
        }

        void onComplete(bool isTimer, int skill)
        {
            //bug? 如果只按了一个键,0.5秒后才能触发
            //如果是的话,做成dmc样的连续连招表
            if (!isTimer)
            {
                skillList.Add(skill);
            }
            else
            {
                var skillCode = skillList.Count > 0 ? int.Parse(string.Join("", skillList)) : 0;
                if (skillCode != 0)
                {
                    //todo skillCode 太长了就去截取
                    var skillToStr = intSkillToStr(skillCode);
                    if (skillToStr.ToCharArray().Length > 5)
                    {
                        skillCode = strSkillToInt(skillToStr.Substring(0, 4));
                    }

                    //todo skillCode不在出招表里面,播放基本动作,可以写一个while循环一直找连招表里面的动作
                    var code = !comboDic.ContainsKey(intSkillToStr(skillCode)) ? 1 : skillCode;
                    //bug 通过队列,一次播放的动画只能有两个排队
                    //animator.Play("attack" + intSkillToStr(code));
                    playAnimator(Skill, code);
                    StartCoroutine(playEffect(code));
                }
            }
        }

        IEnumerator playEffect(int skillCode)
        {
            yield return new WaitForSeconds(0.1f);
            var skill = intSkillToStr(skillCode);
            comboDic[skill].gameObject.SetActive(true);
            effectTimer = Timer.Register(0.4f, () => comboDic[skill].gameObject.SetActive(false));
        }

        private void movePlayer()
        {
            if (isAttacking) return;
            float h = moment.x;
            float v = moment.y;
            if (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0)
            {
                moveDirection = new Vector3(h, 0.0f, v);
                player.MovePosition(player.position + moveSpeed * Time.deltaTime * moveDirection);
                Quaternion newRotation = Quaternion.LookRotation(moveDirection, Vector3.up); //创建旋转
                transform.rotation = Quaternion.Lerp(transform.rotation, newRotation, rotateSpeed * Time.deltaTime);
                playAnimator(PlayerParaName, 2);
                isPress = true;
            }
            else
            {
                isPress = false;
                playAnimator(PlayerParaName, 0);
            }
        }

        private void walkToRun(bool hold)
        {
            if (hold)
            {
                runTimer.Resume();
                if (runTimer.isCompleted)
                {
                    playAnimator(PlayerParaName, 1);
                    moveSpeed = 8f;
                }
            }
            else
            {
                moveSpeed = 5f;
                runTimer = Timer.Register(1, null);
                runTimer.Pause();
                playAnimator(PlayerParaName, 0);
            }
        }

        private void setAnimatorBool(string aniName, bool isActive)
        {
            animator.SetBool(aniName, isActive);
        }

        private void playAnimator(string aniName, int index)
        {
            animator.SetInteger(aniName, index);
        }


        private void OnEnable()
        {
            inputActions.Enable();
        }

        private void OnDisable()
        {
            inputActions.Disable();
        }


        private string intSkillToStr(int skillCode)
        {
            string returnStr = "";
            foreach (var c in (skillCode + "").ToCharArray())
            {
                if (c == '1') returnStr += "X";
                else returnStr += "O";
            }

            return returnStr;
        }

        private int strSkillToInt(string skillCode)
        {
            string returnStr = "";
            foreach (var c in skillCode.ToCharArray())
            {
                if (c == 'X') returnStr += "1";
                else returnStr += "2";
            }

            return int.Parse(returnStr);
        }
    }
}