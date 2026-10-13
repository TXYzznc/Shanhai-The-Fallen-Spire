using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoHudStateAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private GameObject hud;
        [SerializeField] private DemoRewardChoiceController choice;
        [SerializeField] private DemoCheckpointController checkpoint;

        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            GetComponent<DemoPlayerController>().enabled = false;
            var flow = GetComponent<DemoRunFlowController>();
            flow.enabled = false;
            var health = GetComponent<DemoPlayerHealthController>();
            var combat = GetComponent<DemoPlayerCombatController>();
            var buffs = GetComponent<DemoBuffLoadout>();
            var fill = hud.transform.Find("HealthBar/Fill").GetComponent<RectTransform>();
            var skill = hud.transform.Find("SkillStatusText").GetComponent<TMP_Text>();
            var upgrade = hud.transform.Find("CheckpointStatusText").GetComponent<TMP_Text>();
            var status = hud.transform.Find("StatusText").GetComponent<TMP_Text>();
            flow.RestartRun();
            yield return new WaitForSeconds(0.2f);
            bool fullHealth = Mathf.Approximately(fill.anchorMax.x, 1f);
            health.ReceiveDamage(new DemoDamageInfo(25f, gameObject));
            yield return new WaitForSeconds(0.2f);
            bool damagedHealth = Mathf.Abs(fill.anchorMax.x - 0.75f) < 0.01f;
            bool used = combat.TryUseSkill();
            yield return new WaitForSeconds(0.2f);
            bool cooldown = used && combat.SkillCooldownRemaining > 0f && skill.text.Contains("技能冷却");
            combat.ResetForRetry();
            yield return new WaitForSeconds(0.2f);
            bool ready = skill.text.Contains("技能就绪");
            GetComponent<Rigidbody2D>().position = new Vector2(0f, 24f);
            Physics2D.SyncTransforms();
            yield return new WaitForSeconds(0.2f);
            bool waiting = checkpoint.IsActivated && upgrade.text.Contains("等待选择增益");
            bool applied = choice.SelectOption(0);
            yield return new WaitForSeconds(0.2f);
            bool upgraded = applied && upgrade.text.Contains("已获增益 1项");
            if (!buffs.HasBuff("buff_health")) buffs.TryApplyBuff("buff_health");
            yield return new WaitForSeconds(0.2f);
            bool maxHealth = Mathf.Abs(health.MaxHealth - 115f) < 0.01f && Mathf.Abs(fill.anchorMax.x - health.CurrentHealth / health.MaxHealth) < 0.01f && status.text.Contains("/115");
            health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            health.ReceiveDamage(new DemoDamageInfo(10000f, gameObject));
            yield return new WaitForSeconds(0.2f);
            bool failure = skill.text.Contains("本局失败") && Mathf.Approximately(fill.anchorMax.x, 0f);
            flow.RestartRun();
            yield return new WaitForSeconds(0.2f);
            bool reset = Mathf.Approximately(fill.anchorMax.x, 1f) && upgrade.text.Contains("待到达") && skill.text.Contains("技能就绪");
            flow.enabled = true;
            Debug.Log($"[DemoHudStateAcceptanceProbe] FullHealth={fullHealth}, DamagedHealth={damagedHealth}, Cooldown={cooldown}, Ready={ready}, WaitingUpgrade={waiting}, Upgraded={upgraded}, MaxHealthReflected={maxHealth}, FailureShown={failure}, RetryReset={reset}");
        }
    }
}
