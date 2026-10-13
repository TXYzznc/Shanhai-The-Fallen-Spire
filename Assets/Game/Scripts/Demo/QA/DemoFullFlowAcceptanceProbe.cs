using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoFullFlowAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private Rigidbody2D playerBody;
        [SerializeField] private DemoWaveSpawner commonWave;
        [SerializeField] private DemoDamageReceiver[] commonEnemies;
        [SerializeField] private DemoWaveSpawner secondCommonWave;
        [SerializeField] private DemoDamageReceiver[] secondCommonEnemies;
        [SerializeField] private DemoCheckpointController checkpoint;
        [SerializeField] private DemoEncounterController eliteEncounter;
        [SerializeField] private DemoDamageReceiver eliteEnemy;
        [SerializeField] private DemoEncounterController bossEncounter;
        [SerializeField] private DemoDamageReceiver bossEnemy;
        [SerializeField] private DemoRewardChoiceController rewardChoice;
        [SerializeField] private DemoCompletionView completionView;

        public bool StartedAtMapStart { get; private set; }
        public bool StartsWithoutEnemies { get; private set; }
        public bool CommonWaveTriggered { get; private set; }
        public bool CommonWaveCompleted { get; private set; }
        public bool SecondCommonWaveTriggered { get; private set; }
        public bool SecondCommonWaveCompleted { get; private set; }
        public bool BothPointsSameWave { get; private set; }
        public bool NoRespawnOnBacktrack { get; private set; }
        public bool BothPointsRetryReset { get; private set; }
        public string RetryDetails { get; private set; }
        public bool BothPointsRestarted { get; private set; }
        public bool CheckpointUpgradeConfirmed { get; private set; }
        public bool EliteRewardConfirmed { get; private set; }
        public bool BossRewardShown { get; private set; }
        public bool BossCompletionShown { get; private set; }
        public bool Completed { get; private set; }

        private IEnumerator Start()
        {
            yield return null;
            if (playerBody == null || commonWave == null || commonEnemies == null || checkpoint == null || eliteEncounter == null || eliteEnemy == null || bossEncounter == null || bossEnemy == null || rewardChoice == null || completionView == null)
                yield break;

            StartedAtMapStart = playerBody.position.sqrMagnitude < 0.05f;
            if (secondCommonWave == null || secondCommonEnemies == null) yield break;
            StartsWithoutEnemies = !commonWave.HasStarted && !secondCommonWave.HasStarted;
            for (int i = 0; i < commonEnemies.Length; i++) StartsWithoutEnemies &= !commonEnemies[i].gameObject.activeSelf;
            for (int i = 0; i < secondCommonEnemies.Length; i++) StartsWithoutEnemies &= !secondCommonEnemies[i].gameObject.activeSelf;
            // 本探针验证刷怪/奖励/重试编排；传送期间关闭AI攻击以避免死亡流程干扰。
            // 实际攻击与死亡行为由战斗探针另行验收。
            for (int i = 0; i < commonEnemies.Length; i++) commonEnemies[i].GetComponent<DemoEnemyController>().enabled = false;
            for (int i = 0; i < secondCommonEnemies.Length; i++) secondCommonEnemies[i].GetComponent<DemoEnemyController>().enabled = false;
            eliteEnemy.GetComponent<DemoEnemyController>().enabled = false;
            bossEnemy.GetComponent<DemoEnemyController>().enabled = false;
            BothPointsSameWave = commonWave.Wave == 1 && secondCommonWave.Wave == 1;
            MovePlayer(8f);
            yield return new WaitForSeconds(0.2f);
            CommonWaveTriggered = commonWave.HasStarted && commonWave.SpawnedCount == commonEnemies.Length;
            for (int i = 0; i < commonEnemies.Length; i++) commonEnemies[i].ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            CommonWaveCompleted = commonWave.IsCompleted;

            MovePlayer(18f);
            yield return new WaitForSeconds(0.2f);
            SecondCommonWaveTriggered = secondCommonWave.HasStarted && secondCommonWave.SpawnedCount == secondCommonEnemies.Length;
            for (int i = 0; i < secondCommonEnemies.Length; i++) secondCommonEnemies[i].ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            SecondCommonWaveCompleted = secondCommonWave.IsCompleted;
            MovePlayer(0f);
            yield return new WaitForSeconds(0.2f);
            MovePlayer(8f);
            yield return new WaitForSeconds(0.2f);
            MovePlayer(18f);
            yield return new WaitForSeconds(0.2f);
            NoRespawnOnBacktrack = commonWave.IsCompleted && secondCommonWave.IsCompleted
                && commonWave.SpawnedCount == 4 && secondCommonWave.SpawnedCount == 4;

            MovePlayer(24f);
            yield return new WaitForSeconds(0.2f);
            if (checkpoint.IsActivated && rewardChoice.IsOpen && rewardChoice.OptionCount == 3)
                CheckpointUpgradeConfirmed = rewardChoice.SelectOption(0);

            MovePlayer(36f);
            yield return new WaitForSeconds(0.2f);
            bool eliteStarted = eliteEncounter.HasStarted;
            eliteEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.3f);
            EliteRewardConfirmed = eliteStarted && eliteEncounter.IsCompleted && rewardChoice.IsOpen && rewardChoice.OptionCount == 3 && rewardChoice.SelectOption(0);

            MovePlayer(48f);
            yield return new WaitForSeconds(0.2f);
            bool bossStarted = bossEncounter.HasStarted;
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.5f);
            BossRewardShown = bossStarted && bossEncounter.IsCompleted && rewardChoice.IsOpen && rewardChoice.OptionCount == 3;
            BossCompletionShown = bossEncounter.IsCompleted && completionView.IsOpen;
            GetComponent<DemoRunFlowController>().RestartRun();
            yield return null;
            BothPointsRetryReset = playerBody.position.sqrMagnitude < 0.05f
                && !commonWave.HasStarted && !commonWave.IsCompleted && commonWave.SpawnedCount == 0
                && !secondCommonWave.HasStarted && !secondCommonWave.IsCompleted && secondCommonWave.SpawnedCount == 0;
            RetryDetails = $"position={playerBody.position}, first={commonWave.HasStarted}/{commonWave.IsCompleted}/{commonWave.SpawnedCount}, second={secondCommonWave.HasStarted}/{secondCommonWave.IsCompleted}/{secondCommonWave.SpawnedCount}";
            for (int i = 0; i < commonEnemies.Length; i++)
            {
                BothPointsRetryReset &= !commonEnemies[i].gameObject.activeSelf && commonEnemies[i].CurrentHealth == commonEnemies[i].MaxHealth;
                RetryDetails += $", A{i}={commonEnemies[i].gameObject.activeSelf}/{commonEnemies[i].CurrentHealth}/{commonEnemies[i].MaxHealth}";
            }
            for (int i = 0; i < secondCommonEnemies.Length; i++)
            {
                BothPointsRetryReset &= !secondCommonEnemies[i].gameObject.activeSelf && secondCommonEnemies[i].CurrentHealth == secondCommonEnemies[i].MaxHealth;
                RetryDetails += $", B{i}={secondCommonEnemies[i].gameObject.activeSelf}/{secondCommonEnemies[i].CurrentHealth}/{secondCommonEnemies[i].MaxHealth}";
            }
            MovePlayer(8f);
            yield return new WaitForSeconds(0.2f);
            bool firstRestarted = commonWave.HasStarted && commonWave.SpawnedCount == 4 && !secondCommonWave.HasStarted;
            MovePlayer(18f);
            yield return new WaitForSeconds(0.2f);
            BothPointsRestarted = firstRestarted && secondCommonWave.HasStarted && secondCommonWave.SpawnedCount == 4;
            Completed = StartedAtMapStart && StartsWithoutEnemies && CommonWaveTriggered && CommonWaveCompleted && SecondCommonWaveTriggered
                && SecondCommonWaveCompleted && BothPointsSameWave && NoRespawnOnBacktrack && BothPointsRetryReset
                && BothPointsRestarted && CheckpointUpgradeConfirmed && EliteRewardConfirmed && BossRewardShown && BossCompletionShown;
        }

        private void MovePlayer(float y)
        {
            playerBody.position = new Vector2(0f, y);
            Physics2D.SyncTransforms();
        }
    }
}
