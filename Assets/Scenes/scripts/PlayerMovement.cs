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
        private AudioSource audioSource;
        public PlayerInputActions inputActions;
        private LoadAudioManager loadAudioManager;
        private Dictionary<string, AudioClip> playerClips;
        public Vector2 moment;

        public bool idleSword;
        private bool isPress;
        public bool isAttacking { get; set; }
        public int currentSkillCode { get; set; }
        private float moveSpeed = 5f;
        private float rotateSpeed = 30f;
        private Vector3 moveDirection;
        private Animator animator;
        private const string PlayerParaName = "AniIndex";
        private const string Skill = "Skill";
        private const string IsIdleSword = "IsIdleSword";
        private Timer runTimer;
        private Timer skillTimer;
        private Timer effectTimer;
        private Transform comboEffect;
        private List<Transform> combos;
        private const string Combo = "combo";
        private const string Trail = "trail";
        public Dictionary<string, Transform> comboDic;
        public Dictionary<string, float> animatorClipTimeDic;

        private readonly List<int> skillList = new List<int>();
        private float latestTime;
        public Queue<int> skillQueue;
        private int skillTimes;
        private Camera mainCamera;
        private int runTime;


        //这个是shader里面colorName
        private static readonly int TintColor = Shader.PropertyToID("_TintColor");


        private void Awake()
        {
            mainCamera = Camera.main;
            player = GetComponent<Rigidbody>();
            animator = GetComponent<Animator>();
            audioSource = GetComponent<AudioSource>();
            inputActions = new PlayerInputActions();
            skillQueue = new Queue<int>();
            inputActions.Player.Move.performed += ctx => moment = ctx.ReadValue<Vector2>();
            inputActions.Player.LeftButtonDown.performed += ctx => pressKey(1); //x
            inputActions.Player.RightButtonDown.performed += ctx => pressKey(2); //o
            initComobs();
            initAnimatorClipTime();
        }

        private void Start()
        {
            loadAudioManager = FindObjectOfType<LoadAudioManager>();
            playerClips = loadAudioManager.playerClips;
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
            //需求:
            //1,按下第一个攻击键直接攻击,按下第二个攻击键后,判断是否在按下上一个攻击键的0.1秒内
            //2.如果在0.1秒内就把第一个攻击键+第二个攻击键组合,播放组合的动画,组合动画必须得要在上一个动画播放完之后才能播放
            //3.播放玩连招动作后要回到idleSword状态
            //计时器出现是如果0.1秒内按下了第二个键
            if (Time.time - latestTime < 0.3f)
            {
                //如果上一个动作还没有播放完,就把skillCode放进队列中,等到上一个技能播放完毕后,再去播放队列里的第一个
                latestTime = Time.time;
                skillList.Add(skill);
                var str = string.Join("", skillList);
                if (str.Length > 6)
                {
                    str = str.Substring(0, 4);
                }

                var index = int.Parse(str);
                if (!comboDic.ContainsKey(intSkillToStr(index))) return;
                //等到上一个动画播放完毕之后,播放index动画
                StartCoroutine(playerSkillWhenNoAttacking(index));
            }
            else
            {
                skillList.Clear();
                latestTime = Time.time;
                //skillQueue.Enqueue(skill);
                StartCoroutine(playerSkillWhenNoAttacking(skill));
                skillList.Add(skill);
            }
        }

        IEnumerator playerSkillWhenNoAttacking(int skill)
        {
            //第三个技能进来的时候得判断第二个技能执行完毕没有,如果第二个技能没有执行完毕就等待第二个技能执行完毕
            var skillCode = (skill + "");
            if (skillCode.Length > 1)
            {
                if (skillTimes > 5)
                {
                    yield break;
                }
                skillTimes++;
                skillCode = skillCode.Substring(0, skillCode.Length - 1);
                yield return new WaitUntil(() => skillCode.Equals(currentSkillCode + ""));
                playAnimator(Skill, skill);
                StartCoroutine(playEffect(skill));
                skillToMoveCamera(skill);
            }
            else
            {
                Debug.Log(skill);
                skillTimes = 0;
                playAnimator(Skill, skill);
                StartCoroutine(playEffect(skill));
            }

            //todo 排队播放的技能只能有5个,多的播不了
        }

        private bool waitUtilLatestDone()
        {
            //return animator.GetInteger(Skill) == 0 && !isAttacking;
            return !isAttacking;
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
            if (!animatorClipTimeDic.ContainsKey(intSkillToStr(skillCode)))
            {
                yield break;
            }

            effectTimer = Timer.Register(animatorClipTimeDic[intSkillToStr(skillCode)] - 0.3f,
                () => showOrHideSkillEffect(trans, 0, 0.1f));
            playOneShot(AudioName.attack, 0.5f);
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
                if (runTime == 0)
                {
                    playOneShot(AudioName.step);
                }

                runTime++;
                //根据帧数,来判断是否run还是walk
                if (runTime >= getFrames())
                {
                    runTime = 0;
                }
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

        private async void playOneShot(AudioName clipName, float volume = 1f)
        {
            await Task.Delay(TimeSpan.FromSeconds(.25f));
            audioSource.PlayOneShot(getAudioClip(clipName), volume);
        }

        private int getFrames()
        {
            if (moveSpeed == 8)
            {
                return 15;
            }

            return 20;
        }

        private AudioClip getAudioClip(AudioName clipName)
        {
            if (playerClips.ContainsKey(clipName.ToString()))
            {
                return playerClips[clipName.ToString()];
            }

            Debug.LogError("can not find " + clipName + "clip");
            return null;
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

public enum AudioName
{
    attack,
    injory,
    kotoul,
    step,
}