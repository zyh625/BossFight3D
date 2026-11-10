using BossFight.Boss;
using BossFight.CameraRig;
using BossFight.Combat;
using BossFight.Player;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BossFight.Game
{
    public enum FightResult
    {
        Ongoing,
        Victory,
        Defeat
    }

    /// <summary>一局 Arena 对战的胜负与重开。训练模式沿用木桩的独立流程。</summary>
    [DefaultExecutionOrder(100)]
    public sealed class FightDirector : MonoBehaviour
    {
        [SerializeField] private PlayerController player;
        [SerializeField] private BossController boss;
        [SerializeField] private CursorLocker cursorLocker;
        [SerializeField] private FinisherCamera finisher;

        private bool pending;
        private LockOnTargeting targeting;

        public Health PlayerHealth { get; private set; }
        public Health BossHealth { get; private set; }
        public Stamina PlayerStamina { get; private set; }
        public BossController Boss => boss;
        public LockOnTargeting Targeting => targeting;
        public float ChargeRatio => player.VisibleChargeRatio;
        public bool IsBossFight => boss.isActiveAndEnabled;
        public FightResult Result { get; private set; }
        public bool ResultReady => Result != FightResult.Ongoing && (finisher == null || !finisher.IsPlaying);
        public bool IsRestarting { get; private set; }

        private void Awake()
        {
            PlayerHealth = player.GetComponent<Health>();
            BossHealth = boss.GetComponent<Health>();
            PlayerStamina = player.GetComponent<Stamina>();
            targeting = player.GetComponent<LockOnTargeting>();
        }

        private void OnEnable()
        {
            PlayerHealth.Died += RequestResult;
            BossHealth.Died += RequestResult;
        }

        private void OnDisable()
        {
            PlayerHealth.Died -= RequestResult;
            BossHealth.Died -= RequestResult;
        }

        private void RequestResult() => pending = true;

        private void LateUpdate()
        {
            if (!pending)
            {
                return;
            }
            pending = false;
            if (!IsBossFight || Result != FightResult.Ongoing || (!PlayerHealth.IsDead && !BossHealth.IsDead))
            {
                return;
            }

            Result = PlayerHealth.IsDead ? FightResult.Defeat : FightResult.Victory;
            player.enabled = false;
            targeting.enabled = false;
            PlayerStamina.RegenMode = StaminaRegenMode.None;
            cursorLocker.enabled = false;
        }

        public void Restart()
        {
            if (!ResultReady || IsRestarting)
            {
                return;
            }

            int buildIndex = gameObject.scene.buildIndex;
            if (buildIndex < 0)
            {
                Debug.LogError("The Arena scene must be in the build scene list to restart.", this);
                return;
            }
            IsRestarting = SceneManager.LoadSceneAsync(buildIndex, LoadSceneMode.Single) != null;
        }
    }
}
