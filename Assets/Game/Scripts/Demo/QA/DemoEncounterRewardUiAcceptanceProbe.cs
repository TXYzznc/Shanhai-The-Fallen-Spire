using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Shanhai.Demo.QA
{
    /// <summary>隔离遭遇奖励编排，通过真实UGUI射线/按钮完成精英和Boss奖励；不代替实战验收。</summary>
    public sealed class DemoEncounterRewardUiAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private GameObject hud;
        [SerializeField] private DemoGameplayConfig gameplayConfig;
        [SerializeField] private DemoEncounterController eliteEncounter;
        [SerializeField] private DemoDamageReceiver eliteEnemy;
        [SerializeField] private DemoEncounterController bossEncounter;
        [SerializeField] private DemoDamageReceiver bossEnemy;
        public bool EliteDefeatOpensThreeOptions { get; private set; }
        public bool EliteHoverAndPreview { get; private set; }
        public bool EliteConfirmationUpdatesBuild { get; private set; }
        public bool BossDefeatOpensThreeOptions { get; private set; }
        public bool BossCompletionDoesNotBlockReward { get; private set; }
        public bool BossHoverAndPreview { get; private set; }
        public bool BossConfirmationUpdatesBuild { get; private set; }
        public bool CompletionCloseWorks { get; private set; }
        public bool NoRewardReopensAfterConfirmation { get; private set; }
        public bool RetryButtonResetsRun { get; private set; }
        public bool DefaultEnemyConfigsValid { get; private set; }
        public bool EncountersUseDropTables { get; private set; }
        public bool NonDefaultEnemyConfigRead { get; private set; }
        public bool ChoiceTypesAndChanceApplied { get; private set; }
        public bool EliteGateClosesAndOpens { get; private set; }
        public bool BossGateClosesAndOpens { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public string Details { get; private set; }
        private DemoMarkLoadout _marks;
        private DemoRewardChoiceController _choice;
        private DemoCompletionView _completion;
        private DemoRunFlowController _flow;
        private Transform _panel;
        private PointerEventData _pointer;
        private readonly List<RaycastResult> _hits = new List<RaycastResult>(32);
        private DemoEnemyController _eliteAi, _bossAi;
        private bool _eliteEnabled, _bossEnabled, _captured;
        private float _captureDelta;
        private string _blockedBy = "";

        private IEnumerator Start()
        {
            yield return null;
            _marks = GetComponent<DemoMarkLoadout>();
            _flow = GetComponent<DemoRunFlowController>();
            _choice = hud.GetComponent<DemoRewardChoiceController>();
            _completion = hud.GetComponent<DemoCompletionView>();
            _panel = hud.transform.Find("RewardChoicePanel");
            _pointer = new PointerEventData(EventSystem.current) { button = PointerEventData.InputButton.Left };
            _eliteAi = eliteEnemy.GetComponent<DemoEnemyController>();
            _bossAi = bossEnemy.GetComponent<DemoEnemyController>();
            _eliteEnabled = _eliteAi.enabled;
            _bossEnabled = _bossAi.enabled;
            _captureDelta = Time.captureDeltaTime;
            _captured = true;
            Time.captureDeltaTime = 0.05f;
            _eliteAi.enabled = _bossAi.enabled = false;
            ValidateConfiguration();
            _flow.RestartRun();
            var gateField = typeof(DemoEncounterController).GetField("closedGate", BindingFlags.Instance | BindingFlags.NonPublic);
            var eliteGate = (GameObject)gateField.GetValue(eliteEncounter);
            var bossGate = (GameObject)gateField.GetValue(bossEncounter);
            MoveTo(36f);
            yield return new WaitForSeconds(0.2f);
            EliteGateClosesAndOpens = eliteGate.activeSelf;
            eliteEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            EliteDefeatOpensThreeOptions = eliteEncounter.HasStarted && eliteEncounter.IsCompleted && IsThreeOptions();
            EliteGateClosesAndOpens &= !eliteGate.activeSelf;
            string eliteId = _choice.GetOption(0);
            string eliteName = _choice.GetOptionConfig(0).displayName;
            EliteHoverAndPreview = Preview(0, 0);
            EliteConfirmationUpdatesBuild = Click(Option(0)) && _marks.GetMarkId(0) == eliteId
                && _marks.EquippedCount == 1 && IsConfirmed() && !_choice.SelectOption(0);
            yield return new WaitForSecondsRealtime(0.3f);
            EliteConfirmationUpdatesBuild &= hud.transform.Find("LoadoutPanel/Slot_0").GetComponent<TMP_Text>().text
                .Contains(eliteName);
            MoveTo(48f);
            yield return new WaitForSeconds(0.2f);
            BossGateClosesAndOpens = bossGate.activeSelf;
            bossEnemy.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            BossDefeatOpensThreeOptions = bossEncounter.HasStarted && bossEncounter.IsCompleted && IsThreeOptions();
            BossGateClosesAndOpens &= !bossGate.activeSelf;
            BossCompletionDoesNotBlockReward = _completion.IsOpen && Click(_panel.Find("RewardSlot_1").gameObject)
                && _choice.TargetSlot == 1;
            string bossId = _choice.GetOption(1);
            string bossName = _choice.GetOptionConfig(1).displayName;
            BossHoverAndPreview = Preview(1, 1);
            BossConfirmationUpdatesBuild = Click(Option(1)) && _marks.GetMarkId(1) == bossId
                && _marks.GetMarkId(0) == eliteId && _marks.EquippedCount == 2 && IsConfirmed() && !_choice.SelectOption(1);
            yield return new WaitForSecondsRealtime(0.3f);
            BossConfirmationUpdatesBuild &= hud.transform.Find("LoadoutPanel/Slot_1").GetComponent<TMP_Text>().text.Contains(bossName);
            CompletionCloseWorks = Click(hud.transform.Find("CompletionPanel/Close").gameObject) && !_completion.IsOpen;
            yield return new WaitForSeconds(0.5f);
            NoRewardReopensAfterConfirmation = !_choice.IsOpen && !_panel.gameObject.activeSelf && !_completion.IsOpen
                && _flow.CurrentCurrency == 300 && _marks.EquippedCount == 2;
            _completion.Open();
            yield return null;
            Canvas.ForceUpdateCanvases();
            RetryButtonResetsRun = Click(hud.transform.Find("CompletionPanel/Retry").gameObject);
            yield return null;
            RetryButtonResetsRun &= !eliteEncounter.HasStarted && !bossEncounter.HasStarted && !eliteEncounter.IsCompleted
                && !bossEncounter.IsCompleted && !eliteEnemy.gameObject.activeSelf && !bossEnemy.gameObject.activeSelf
                && !_completion.IsOpen && !_choice.IsOpen && !_panel.gameObject.activeSelf && _marks.EquippedCount == 0
                && _flow.CurrentCurrency == 0 && GetComponent<Rigidbody2D>().position.sqrMagnitude < 0.01f;
            RetryButtonResetsRun &= eliteGate.activeSelf && !bossGate.activeSelf;
            Completed = EliteDefeatOpensThreeOptions && EliteHoverAndPreview && EliteConfirmationUpdatesBuild
                && BossDefeatOpensThreeOptions && BossCompletionDoesNotBlockReward && BossHoverAndPreview
                && BossConfirmationUpdatesBuild && CompletionCloseWorks && NoRewardReopensAfterConfirmation && RetryButtonResetsRun
                && DefaultEnemyConfigsValid && EncountersUseDropTables && NonDefaultEnemyConfigRead && ChoiceTypesAndChanceApplied
                && EliteGateClosesAndOpens && BossGateClosesAndOpens;
            Details = $"elite={eliteId}, boss={bossId}, raycastBlockedBy={_blockedBy}, retryCount={_flow.RetryCount}";
            Finished = true;
            Restore();
        }
        private bool Preview(int index, int slot)
        {
            GameObject option = Option(index);
            var config = _choice.GetOptionConfig(index);
            if (config == null) return false;
            string before = _marks.GetMarkId(slot);
            ExecuteEvents.Execute(option, _pointer, ExecuteEvents.pointerEnterHandler);
            bool hovered = _panel.Find("Tooltip").GetComponent<TMP_Text>().text == config.description;
            return hovered && Click(option) && _marks.GetMarkId(slot) == before && _choice.IsOpen && !_choice.IsConfirmed;
        }
        private void ValidateConfiguration()
        {
            var elite = gameplayConfig.FindEnemy("elite_beast");
            var boss = gameplayConfig.FindEnemy("fallen_spire_guardian");
            DefaultEnemyConfigsValid = elite != null && boss != null && elite.isElite && !elite.isBoss
                && !boss.isElite && boss.isBoss && elite.maxHealth == 250 && boss.maxHealth == 1000
                && elite.attackDamage == 15f && boss.attackDamage == 20f && elite.moveSpeed == 2.2f && boss.moveSpeed == 1.8f
                && elite.canStagger && elite.staggerThreshold == 100f && boss.staggerThreshold == 200f
                && boss.secondPhaseHealthRatio == 0.6f && boss.secondPhaseCooldownMultiplier == 0.7f
                && boss.secondPhaseAreaRadius == 3f && boss.estimatedFightMinSeconds == 120f && boss.estimatedFightMaxSeconds == 180f;
            var eliteDrop = gameplayConfig.FindDrop(elite.dropTableId);
            var bossDrop = gameplayConfig.FindDrop(boss.dropTableId);
            EncountersUseDropTables = eliteEncounter.HasValidConfiguration && bossEncounter.HasValidConfiguration
                && eliteEncounter.RewardTableId == elite.dropTableId && bossEncounter.RewardTableId == boss.dropTableId
                && eliteDrop.rewardId == DemoDropConfig.BeastMarkChoice && bossDrop.rewardId == DemoDropConfig.BeastMarkChoice
                && eliteDrop.chance == 1f && bossDrop.guaranteedForBoss && eliteDrop.amount == 1 && bossDrop.amount == 1;
            var copy = Instantiate(gameplayConfig);
            var custom = copy.FindEnemy("fallen_spire_guardian");
            custom.maxHealth = 333; custom.attackDamage = 17f; custom.moveSpeed = 4f;
            custom.attackRange = 2.7f; custom.chargeDuration = 0.6f; custom.attackCooldown = 0.9f;
            custom.secondPhaseHealthRatio = 0.4f; custom.canStagger = false;
            var target = new GameObject("QA_ConfigEnemy"); target.SetActive(false);
            target.transform.position = new Vector3(100f, 100f);
            target.AddComponent<Rigidbody2D>();
            var receiver = target.AddComponent<DemoDamageReceiver>();
            var controller = target.AddComponent<DemoEnemyController>();
            var flags = BindingFlags.Instance | BindingFlags.NonPublic;
            typeof(DemoEnemyController).GetField("gameplayConfig", flags).SetValue(controller, copy);
            typeof(DemoEnemyController).GetField("enemyId", flags).SetValue(controller, custom.id);
            typeof(DemoEnemyController).GetField("target", flags).SetValue(controller, transform);
            target.SetActive(true);
            NonDefaultEnemyConfigRead = controller.HasEnemyConfig && controller.HasTargetReceiver
                && receiver.MaxHealth == 333f && controller.AttackRange == 2.7f && controller.ChargeDuration == 0.6f
                && ReferenceEquals(typeof(DemoEnemyController).GetField("_enemyConfig", flags).GetValue(controller), custom)
                && controller.Phase == 1;
            receiver.ReceiveDamage(new DemoDamageInfo(200f, gameObject));
            NonDefaultEnemyConfigRead &= controller.Phase == 2;
            Destroy(target); Destroy(copy);
            var view = hud.GetComponent<DemoRewardChoiceView>();
            var testDrop = new DemoDropConfig { rewardId = DemoDropConfig.BuffChoice, chance = 1f, amount = 1 };
            ChoiceTypesAndChanceApplied = UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenDrop(testDrop, false) && _choice.IsBuffChoice && _choice.OptionCount == 3;
            testDrop.rewardId = DemoDropConfig.BeastMarkChoice; testDrop.chance = 0f;
            ChoiceTypesAndChanceApplied &= !UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenDrop(testDrop, false) && !_choice.IsOpen && !_panel.gameObject.activeSelf;
            testDrop.guaranteedForBoss = true;
            ChoiceTypesAndChanceApplied &= UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenDrop(testDrop, true) && !_choice.IsBuffChoice && _choice.OptionCount == 3;
            testDrop.amount = 2;
            ChoiceTypesAndChanceApplied &= !UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenDrop(testDrop, true) && !_choice.IsOpen;
            testDrop.amount = 1; testDrop.rewardId = "invalid_reward";
            ChoiceTypesAndChanceApplied &= !UnityEngine.Object.FindObjectOfType<Shanhai.Demo.Player.DemoWorldEntry>().Ui.OpenDrop(testDrop, true) && !_choice.IsOpen;
        }
        private bool IsThreeOptions() => _choice.IsOpen && !_choice.IsBuffChoice && _choice.OptionCount == 3
            && _choice.GetOption(0) != _choice.GetOption(1) && _choice.GetOption(1) != _choice.GetOption(2)
            && _choice.GetOption(0) != _choice.GetOption(2) && _panel.gameObject.activeSelf;
        private bool IsConfirmed() => _choice.IsConfirmed && !_choice.IsOpen && !_panel.gameObject.activeSelf;
        private GameObject Option(int index) => _panel.Find("RewardOption_" + (index + 1)).gameObject;
        private bool Click(GameObject button)
        {
            _pointer.position = RectTransformUtility.WorldToScreenPoint(null, button.transform.position);
            _hits.Clear();
            EventSystem.current.RaycastAll(_pointer, _hits);
            if (_hits.Count == 0 || !(_hits[0].gameObject == button || _hits[0].gameObject.transform.IsChildOf(button.transform)))
            {
                _blockedBy += button.name + ":" + (_hits.Count > 0 ? _hits[0].gameObject.name : "none") + ";";
                return false;
            }
            return ExecuteEvents.ExecuteHierarchy(_hits[0].gameObject, _pointer, ExecuteEvents.pointerClickHandler) != null;
        }
        private void MoveTo(float y) { GetComponent<Rigidbody2D>().position = new Vector2(0f, y); Physics2D.SyncTransforms(); }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            _eliteAi.enabled = _eliteEnabled;
            _bossAi.enabled = _bossEnabled;
            Time.captureDeltaTime = _captureDelta;
            _captured = false;
        }
    }
}
