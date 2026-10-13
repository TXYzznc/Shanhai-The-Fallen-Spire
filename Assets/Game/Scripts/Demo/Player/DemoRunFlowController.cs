using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.Player
{
    /// <summary>处理二次死亡后的灰盒重试：回到起点并恢复一局初始状态。</summary>
    public sealed class DemoRunFlowController : MonoBehaviour
    {
        private Game.State.GamePauseController _pause;
        public void BindPause(Game.State.GamePauseController pause) => _pause = pause;
        private bool GameplayBlocked => _pause != null && _pause.BlocksGameplay;
        [SerializeField] private DemoPlayerHealthController playerHealth;
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private DemoEncounterController[] encounters = new DemoEncounterController[0];
        [SerializeField] private DemoWaveSpawner[] waves = new DemoWaveSpawner[0];
        [SerializeField] private DemoCheckpointController[] checkpoints = new DemoCheckpointController[0];
        [SerializeField] private DemoRewardChoiceController rewardChoice;
        private IDemoRewardPresenter rewardView;
        [SerializeField] private DemoMarkLoadout markLoadout;
        [SerializeField] private DemoBuffLoadout buffLoadout;
        [SerializeField] private DemoPlayerCombatController playerCombat;
        [SerializeField] private DemoPlayerController playerMovement;
        [SerializeField] private Vector2 startPosition;
        [SerializeField, Min(0f)] private float retryDelay = 0.5f;

        public event System.Action WorldReset;

        public bool IsRetrying { get; private set; }
        public int RetryCount { get; private set; }
        public int CurrentCurrency { get; private set; }
        public DemoCheckpointSnapshot CheckpointSnapshot { get; private set; }
        public int CheckpointSaveRevision { get; private set; }
        private float _retryAt;

        public void BindViews(DemoRewardChoiceController choice, IDemoRewardPresenter view)
        {
            rewardChoice = choice; rewardView = view;
        }

        public void BindWorld(DemoEncounterController[] encounterList, DemoWaveSpawner[] waveList, DemoCheckpointController[] checkpointList)
        {
            encounters = encounterList ?? System.Array.Empty<DemoEncounterController>();
            waves = waveList ?? System.Array.Empty<DemoWaveSpawner>();
            checkpoints = checkpointList ?? System.Array.Empty<DemoCheckpointController>();
        }
        public void ResetForNewSession()
        {
            startPosition = playerBody.position; RetryCount = 0; IsRetrying = false; _retryAt = 0f;
            ResetWorldState();
        }

        public void AddCurrency(int amount)
        {
            if (amount <= 0) return;
            CurrentCurrency += Mathf.Min(amount, int.MaxValue - CurrentCurrency);
        }

        private void Awake()
        {
            if (playerHealth == null) playerHealth = GetComponent<DemoPlayerHealthController>();
            if (playerBody == null) playerBody = GetComponent<Rigidbody2D>();
            startPosition = playerBody != null ? playerBody.position : transform.position;
        }

        private void Update()
        {
            if (GameplayBlocked) return;
            if (playerHealth == null || !playerHealth.IsRetryRequired) return;
            if (!IsRetrying)
            {
                IsRetrying = true;
                _retryAt = Time.time + retryDelay;
            }

            if (Time.time < _retryAt) return;
            ResetWorldState();
            RetryCount++;
            IsRetrying = false;
        }

        public void RestartRun()
        {
            if (IsRetrying) return;
            ResetWorldState();
            RetryCount++;
        }

        internal void CaptureCheckpoint(DemoCheckpointController checkpoint, bool upgraded, string upgradeId)
        {
            if (checkpoint == null || !checkpoint.IsActivated || IsRetrying || playerHealth == null || playerHealth.IsRetryRequired) return;
            var marks = new string[markLoadout != null ? markLoadout.SlotCount : 0];
            for (int i = 0; i < marks.Length; i++) marks[i] = markLoadout.GetMarkId(i);
            var buffs = new string[buffLoadout != null ? buffLoadout.ActiveCount : 0];
            for (int i = 0; i < buffs.Length; i++) buffs[i] = buffLoadout.GetBuffId(i);
            var waveProgress = new DemoCheckpointProgress[waves.Length];
            for (int i = 0; i < waves.Length; i++)
                waveProgress[i] = waves[i] != null ? new DemoCheckpointProgress(waves[i].name, waves[i].HasStarted, waves[i].IsCompleted, waves[i].SpawnedCount) : null;
            var encounterProgress = new DemoCheckpointProgress[encounters.Length];
            for (int i = 0; i < encounters.Length; i++)
                encounterProgress[i] = encounters[i] != null ? new DemoCheckpointProgress(encounters[i].name, encounters[i].HasStarted, encounters[i].IsCompleted, encounters[i].HasStarted ? 1 : 0) : null;
            CheckpointSnapshot = new DemoCheckpointSnapshot(checkpoint.name, RetryCount,
                playerBody != null ? playerBody.position : (Vector2)transform.position, playerHealth.CurrentHealth, playerHealth.MaxHealth,
                playerHealth.RevivesRemaining, playerCombat != null ? playerCombat.CurrentEnergy : 0f, CurrentCurrency,
                upgraded, upgradeId, marks, buffs, waveProgress, encounterProgress);
            CheckpointSaveRevision++;
        }

        private void ResetWorldState()
        {
            CheckpointSnapshot = null;
            CheckpointSaveRevision = 0;
            CurrentCurrency = 0;
            if (playerBody != null) playerBody.position = startPosition;
            else transform.position = startPosition;
            // 先清空局内构筑，再按初始属性恢复生命，避免上一局生命加成残留。
            if (markLoadout != null) markLoadout.ResetForRetry();
            if (buffLoadout != null) buffLoadout.ResetForRetry();
            if (playerCombat != null) playerCombat.ResetForRetry();
            if (playerMovement != null) playerMovement.ResetForRetry();
            if (playerHealth != null) playerHealth.ResetForRetry();
            for (int i = 0; i < encounters.Length; i++)
            {
                if (encounters[i] != null) encounters[i].ResetForRetry();
            }
            for (int i = 0; i < waves.Length; i++)
            {
                if (waves[i] != null) waves[i].ResetForRetry();
            }
            for (int i = 0; i < checkpoints.Length; i++)
            {
                if (checkpoints[i] != null) checkpoints[i].ResetForRetry();
            }
            if (rewardChoice != null) rewardChoice.ResetForRetry();
            if (rewardView != null) rewardView.Hide();
            var feedback = GetComponent<Shanhai.Demo.Presentation.DemoSpriteFeedback>();
            if (feedback != null) feedback.ResetPresentation();
            WorldReset?.Invoke();
        }
    }
}
