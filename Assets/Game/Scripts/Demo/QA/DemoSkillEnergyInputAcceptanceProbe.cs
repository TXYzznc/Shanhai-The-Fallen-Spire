using System.Collections;
using System.Reflection;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Configuration;
using Shanhai.Demo.Input;
using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    /// <summary>通过输入抽象验证技能扇形/冷却与自然充能大招；不直接写入能量。</summary>
    public sealed class DemoSkillEnergyInputAcceptanceProbe : MonoBehaviour, IDemoInputSource
    {
        [SerializeField] private TMP_Text statusText;
        public bool SkillFanAndDamage { get; private set; }
        public bool SkillCooldownBlocksRepeat { get; private set; }
        public bool SkillCooldownExpires { get; private set; }
        public bool SkillRangeAdjustable { get; private set; }
        public bool CustomSkillDamageAndCooldown { get; private set; }
        public bool EnergyStartsEmpty { get; private set; }
        public bool EnergyRegeneratesNaturally { get; private set; }
        public bool PartialEnergyCannotRelease { get; private set; }
        public bool FullEnergyVisible { get; private set; }
        public bool UltimateDamageAndConsumption { get; private set; }
        public bool UltimateCannotRepeat { get; private set; }
        public bool EnergyResumesAndCaps { get; private set; }
        public bool CustomEnergyAndUltimate { get; private set; }
        public bool RetryClearsEnergyAndCooldown { get; private set; }
        public bool Completed { get; private set; }
        public bool Finished { get; private set; }
        public float DefaultChargeDuration { get; private set; }
        private DemoPlayerCombatController _combat;
        private DemoPlayerController _movement;
        private object _combatInput;
        private object _movementInput;
        private DemoGameplayConfig _originalConfig;
        private DemoGameplayConfig _temporaryConfig;
        private float _captureDelta;
        private float _radius;
        private bool _captured;
        private DemoInputState _input;
        private DemoDamageReceiver[] _targets;
        public DemoInputState ReadState() => _input;
        private static FieldInfo Field(object target, string name) => target.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
        private void SetInput(bool skill = false, bool ultimate = false) =>
            _input = new DemoInputState(Vector2.zero, Vector2.zero, false, false, skill, ultimate, Vector2.up);

        private IEnumerator Start()
        {
            yield return null;
            _combat = GetComponent<DemoPlayerCombatController>();
            _movement = GetComponent<DemoPlayerController>();
            _combatInput = Field(_combat, "_inputSource").GetValue(_combat);
            _movementInput = Field(_movement, "_inputSource").GetValue(_movement);
            _originalConfig = (DemoGameplayConfig)Field(_combat, "gameplayConfig").GetValue(_combat);
            _radius = (float)Field(_combat, "attackRadius").GetValue(_combat);
            _captureDelta = Time.captureDeltaTime;
            _captured = true;
            Field(_combat, "_inputSource").SetValue(_combat, this);
            Field(_movement, "_inputSource").SetValue(_movement, this);
            Time.captureDeltaTime = 0.05f;
            SetInput();
            _combat.ResetForRetry();
            _targets = new[] { Target("QA_SkillFront", Vector2.up), Target("QA_SkillBack", Vector2.down),
                Target("QA_SkillSide", Vector2.right), Target("QA_SkillFar", Vector2.up * 1.9f) };
            yield return null;
            Physics2D.SyncTransforms();
            SetInput(skill: true);
            yield return null;
            SetInput();
            yield return new WaitForSeconds(0.1f);
            SkillFanAndDamage = Damage(0, 20f) && Damage(1, 0f) && Damage(2, 0f) && Damage(3, 0f);
            float firstCooldown = _combat.SkillCooldownRemaining;
            SetInput(skill: true);
            yield return null;
            SetInput();
            SkillCooldownBlocksRepeat = Damage(0, 20f) && _combat.SkillCooldownRemaining < firstCooldown;
            yield return new WaitForSeconds(5.1f);
            SetInput(skill: true);
            yield return null;
            SetInput();
            SkillCooldownExpires = Damage(0, 40f) && _combat.SkillCooldownRemaining > 4.8f;
            ResetTargets();
            Field(_combat, "attackRadius").SetValue(_combat, 2.2f);
            _combat.ResetForRetry();
            SetInput(skill: true);
            yield return null;
            SetInput();
            SkillRangeAdjustable = Damage(3, 20f);
            Field(_combat, "attackRadius").SetValue(_combat, _radius);
            _temporaryConfig = Instantiate(_originalConfig);
            _temporaryConfig.Player.baseAttackDamage = 7f;
            _temporaryConfig.Player.skillDamageMultiplier = 3f;
            _temporaryConfig.Player.skillCooldown = 0.6f;
            Field(_combat, "gameplayConfig").SetValue(_combat, _temporaryConfig);
            ResetTargets();
            _combat.ResetForRetry();
            SetInput(skill: true);
            yield return null;
            SetInput();
            CustomSkillDamageAndCooldown = Damage(0, 21f) && _combat.SkillCooldownRemaining > 0.5f && _combat.SkillCooldownRemaining <= 0.6f;
            yield return new WaitForSeconds(0.7f);
            SetInput(skill: true);
            yield return null;
            SetInput();
            CustomSkillDamageAndCooldown &= Damage(0, 42f);
            Field(_combat, "gameplayConfig").SetValue(_combat, _originalConfig);
            ResetTargets();
            _combat.ResetForRetry();
            EnergyStartsEmpty = _combat.CurrentEnergy == 0f;
            float chargedAt = Time.time;
            yield return new WaitForSeconds(1f);
            EnergyRegeneratesNaturally = _combat.CurrentEnergy >= 4.5f && _combat.CurrentEnergy <= 5.5f;
            SetInput(ultimate: true);
            yield return null;
            SetInput();
            PartialEnergyCannotRelease = Damage(0, 0f) && _combat.CurrentEnergy > 0f;
            while (_combat.CurrentEnergy < 100f && Time.time - chargedAt < 21f) yield return null;
            DefaultChargeDuration = Time.time - chargedAt;
            yield return new WaitForSeconds(0.3f);
            FullEnergyVisible = _combat.CurrentEnergy == 100f && statusText != null && statusText.text.Contains("能量 100/100");
            SetInput(ultimate: true);
            yield return null;
            SetInput();
            UltimateDamageAndConsumption = Damage(0, 50f) && Damage(1, 0f) && Damage(2, 0f)
                && _combat.CurrentEnergy < 0.6f;
            SetInput(ultimate: true);
            yield return null;
            SetInput();
            UltimateCannotRepeat = Damage(0, 50f) && _combat.CurrentEnergy < 1f;
            yield return new WaitForSeconds(0.5f);
            EnergyResumesAndCaps = _combat.CurrentEnergy > 2f && _combat.CurrentEnergy < 4f;
            _temporaryConfig.Player.energyMax = 40f;
            _temporaryConfig.Player.energyRegenPerSecond = 20f;
            _temporaryConfig.Player.ultimateDamageMultiplier = 4f;
            Field(_combat, "gameplayConfig").SetValue(_combat, _temporaryConfig);
            _combat.ResetForRetry();
            ResetTargets();
            yield return new WaitForSeconds(2.4f);
            EnergyResumesAndCaps &= _combat.CurrentEnergy == 40f;
            SetInput(ultimate: true);
            yield return null;
            SetInput();
            CustomEnergyAndUltimate = Damage(0, 28f) && _combat.CurrentEnergy < 1.1f;
            SetInput(skill: true);
            yield return null;
            SetInput();
            GetComponent<DemoRunFlowController>().RestartRun();
            RetryClearsEnergyAndCooldown = _combat.CurrentEnergy == 0f && _combat.SkillCooldownRemaining == 0f;
            Completed = SkillFanAndDamage && SkillCooldownBlocksRepeat && SkillCooldownExpires && SkillRangeAdjustable
                && CustomSkillDamageAndCooldown && EnergyStartsEmpty && EnergyRegeneratesNaturally && PartialEnergyCannotRelease
                && FullEnergyVisible && UltimateDamageAndConsumption && UltimateCannotRepeat && EnergyResumesAndCaps
                && CustomEnergyAndUltimate && RetryClearsEnergyAndCooldown;
            Finished = true;
            Restore();
        }

        private DemoDamageReceiver Target(string name, Vector2 offset)
        {
            var root = new GameObject(name);
            root.transform.position = transform.position + (Vector3)offset;
            root.AddComponent<CircleCollider2D>().radius = 0.1f;
            DemoDamageReceiver receiver = root.AddComponent<DemoDamageReceiver>();
            receiver.ConfigureHealth(200f, 0f);
            return receiver;
        }
        private bool Damage(int index, float damage) => Mathf.Abs(200f - _targets[index].CurrentHealth - damage) < 0.001f;
        private void ResetTargets()
        {
            for (int i = 0; i < _targets.Length; i++) _targets[i].ResetHealth();
        }
        private void OnDisable() => Restore();
        private void Restore()
        {
            if (!_captured) return;
            Field(_combat, "_inputSource").SetValue(_combat, _combatInput);
            Field(_movement, "_inputSource").SetValue(_movement, _movementInput);
            Field(_combat, "gameplayConfig").SetValue(_combat, _originalConfig);
            Field(_combat, "attackRadius").SetValue(_combat, _radius);
            Time.captureDeltaTime = _captureDelta;
            if (_temporaryConfig != null) Destroy(_temporaryConfig);
            if (_targets != null) for (int i = 0; i < _targets.Length; i++) if (_targets[i] != null) Destroy(_targets[i].gameObject);
            _captured = false;
        }
    }
}
