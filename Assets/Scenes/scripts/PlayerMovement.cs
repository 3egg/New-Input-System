using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Scenes.scripts
{
    public class PlayerMovement : MonoBehaviour
    {
        private Rigidbody player;
        public PlayerInputActions inputActions;

        public Vector2 moment;

        public bool idleSword;
        private bool isPress;
        public bool isAttacking { get; set; }
        public bool isIdleSword;
        public int currentSkillCode { get; set; }
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
        private Transform comboEffect;
        private List<Transform> combos;
        private const string Combo = "combo";
        private const string Trail = "trail";
        public Dictionary<string, Transform> comboDic;
        public Dictionary<string, float> animatorClipTimeDic;

        private Queue<int> skillQueue;
        private Sequence dotweenSequence;
        private Camera mainCamera;

        public int testInt = 123;
        public string testStr = 123 + "";

        //这个是shader里面colorName
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");


        private void Awake()
        {
            mainCamera = Camera.main;
            player = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            inputActions = new PlayerInputActions();
            skillQueue = new Queue<int>();
            inputActions.Player.Move.performed += ctx => moment = ctx.ReadValue<Vector2>();
            inputActions.Player.LeftButtonDown.performed += ctx => pressKey(1); //x
            inputActions.Player.RightButtonDown.performed += ctx => pressKey(2); //o
            initComobs();
            initAnimatorClipTime();
        }

        private void initAnimatorClipTime()
        {
            var clips = animator.runtimeAnimatorController.animationClips.Where(t => t.name.Contains("attack"))
                .ToArray();
            animatorClipTimeDic = new Dictionary<string, float>(clips.Length);
            foreach (var clip in clips)
            {
                animatorClipTimeDic[clip.name.Replace("attack", "")] = clip.length;
            }
        }

        private void FixedUpdate()
        {
            movePlayer();
            walkToRun(isPress);
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
                hideAllSKillEffect(combo);
            }
        }

        private void pressKey(int skill)
        {
            //播放完技能之后进入idle_sword状态,没有attack之后一秒后进入默认idle状态,在状态机脚本里实现了
            skillTimer = Timer.Register(.3f, () =>
            {
                onComplete(true, skill);
                skillList.Clear();
            });

            onComplete(false, skill);
        }

        void onComplete(bool isTimer, int skill)
        {
            //bug? 如果只按了一个键,0.3秒后才能触发
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
                    var skillToStr = intSkillToStr(skillCode);
                    if (skillToStr.ToCharArray().Length > 5)
                    {
                        skillCode = strSkillToInt(skillToStr.Substring(0, 4));
                    }

                    //todo skillCode不在出招表里面,播放基本动作,可以写一个while循环一直找连招表里面的动作
                    var code = !comboDic.ContainsKey(intSkillToStr(skillCode)) ? 1 : skillCode;
                    if (skillQueue.Count < 2)
                    {
                        skillQueue.Enqueue(code);
                    }

                    if (!isAttacking)
                    {
                        if (skillQueue.Count <= 0) return;
                        var index = skillQueue.Dequeue();
                        playAnimator(Skill, index);
                        StartCoroutine(playEffect(index));
                        skillToMoveCamera(index);
                        isAttacking = true;
                        //currentSkillCode = index;
                    }
                }
            }
        }

        private async void skillToMoveCamera(int index)
        {
            var delay = animatorClipTimeDic[intSkillToStr(index)] / 2.0f;
            await Task.Delay(TimeSpan.FromSeconds(delay));
            if ((index + "").Length >= 3)
            {
                mainCamera.DOShakePosition(0.2f, 0.5f, 20);
            }
        }

        IEnumerator playEffect(int skillCode)
        {
            yield return new WaitForSeconds(0.1f);
            var skill = intSkillToStr(skillCode);
            var trans = comboDic[skill];
            showOrHideSkillEffect(trans, 1, 0);
            effectTimer = Timer.Register(animatorClipTimeDic[intSkillToStr(skillCode)] - 0.3f,
                () => showOrHideSkillEffect(trans, 0, 0f));
        }

        private void hideAllSKillEffect(Transform trans)
        {
            var meshRenderers = trans.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                var color = meshRenderer.material.GetColor(TintColor);
                color.a = 0;
                meshRenderer.material.SetColor(TintColor, color);
            }
        }

        private void showOrHideSkillEffect(Transform trans, int endVal, float delay)
        {
            var meshRenderers = trans.GetComponentsInChildren<MeshRenderer>();
            foreach (var meshRenderer in meshRenderers)
            {
                var material = meshRenderer.material;
                material.DOKill();
                material.DOFade(endVal, TintColor, 0.1f).SetDelay(delay);
            }

            var ani = transform.GetComponentInChildren<Animation>();
            if (ani != null)
            {
                ani.Play();
            }
        }


        //通过movePosition移动对象
        //通过quaternion旋转对象
        private void movePlayer()
        {
            if (isAttacking)
            {
                return;
            }

            float h = moment.x;
            float v = moment.y;
            if (Mathf.Abs(h) > 0 || Mathf.Abs(v) > 0)
            {
                moveDirection = new Vector3(h, 0.0f, v);
                player.MovePosition(player.position + moveSpeed * Time.deltaTime * moveDirection);
                Quaternion newRotation = Quaternion.LookRotation(moveDirection, Vector3.up);
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


        public string intSkillToStr(int skillCode)
        {
            string returnStr = "";
            foreach (var c in (skillCode + "").ToCharArray())
            {
                if (c == '1') returnStr += "X";
                else returnStr += "O";
            }

            return returnStr;
        }

        public int strSkillToInt(string skillCode)
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