using System.Collections;
using Shanhai.Demo.Build;
using Shanhai.Demo.Combat;
using Shanhai.Demo.Enemies;
using Shanhai.Demo.Player;
using TMPro;
using UnityEngine;

namespace Shanhai.Demo.QA
{
    public sealed class DemoLoadoutTextAcceptanceProbe : MonoBehaviour
    {
        [SerializeField] private GameObject hud;

        private IEnumerator Start()
        {
            yield return null;
            foreach (var enemy in FindObjectsOfType<DemoEnemyController>(true)) enemy.enabled = false;
            GetComponent<DemoPlayerController>().enabled = false;
            var marks = GetComponent<DemoMarkLoadout>();
            string[] ids = { "beast_01_crack", "beast_02_mountain", "beast_03_shadow" };
            string[,] expected = {
                { "二段普攻伤害", "技能失衡累计", "大招伤害", "攻击 +" },
                { "命中后减伤", "技能范围", "大招减伤", "生命上限" },
                { "普攻时移速", "技能冷却", "能量恢复", "移速 +" }
            };
            var labels = new TMP_Text[4];
            for (int i = 0; i < 4; i++) labels[i] = hud.transform.Find("LoadoutPanel/Slot_" + i).GetComponent<TMP_Text>();
            int passed = 0;
            bool slotOnly = true;
            bool fits = true;
            bool uniqueClear = true;
            for (int mark = 0; mark < ids.Length; mark++)
            {
                for (int slot = 0; slot < 4; slot++)
                {
                    marks.ResetForRetry();
                    marks.TryEquip(ids[mark], slot);
                    yield return new WaitForSeconds(0.25f);
                    if (labels[slot].text.Contains(expected[mark, slot])) passed++;
                    for (int other = 0; other < 4; other++)
                    {
                        if (other == slot) continue;
                        uniqueClear &= labels[other].text.Contains("空槽");
                        slotOnly &= !labels[slot].text.Contains("\n" + expected[mark, other]);
                    }
                    labels[slot].ForceMeshUpdate();
                    fits &= labels[slot].preferredHeight <= labels[slot].rectTransform.rect.height + 0.1f;
                }
            }
            GetComponent<DemoRunFlowController>().RestartRun();
            yield return new WaitForSeconds(0.25f);
            bool retryCleared = true;
            foreach (TMP_Text label in labels) retryCleared &= label.text.Contains("空槽");
            marks.TryEquip(ids[0], 0);
            marks.TryEquip(ids[1], 1);
            marks.TryEquip(ids[2], 3);
            GetComponent<DemoPlayerHealthController>().ReceiveDamage(new DemoDamageInfo(25f, gameObject));
            yield return new WaitForSeconds(0.2f);
            GetComponent<DemoPlayerCombatController>().TryUseSkill();
            Debug.Log($"[DemoLoadoutTextAcceptanceProbe] PassedSlotCases={passed}/12, SlotOnly={slotOnly}, Fits={fits}, UniqueClear={uniqueClear}, RetryCleared={retryCleared}");
        }
    }
}
