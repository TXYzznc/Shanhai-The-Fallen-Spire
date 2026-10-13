#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Shanhai.Demo.Player;
using Shanhai.Demo.UI;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;
using UnityEngine.UI;

public sealed class DemoRewardHoverTests
{
    [UnityTest]
    public IEnumerator MovingInsideEachRewardCardKeepsItsOwnDescriptionForMarksAndBuffs()
    {
        yield return new DemoGameplayAcceptanceTests().LoadDemoScene();
        var world = Object.FindObjectOfType<DemoWorldEntry>();
        var choice = world.Ui.RewardChoice;
        DemoRewardChoiceView view = null;
        var eventSystem = EventSystem.current; Assert.That(eventSystem, Is.Not.Null);
        var data = new PointerEventData(eventSystem); var hits = new List<RaycastResult>();
        var corners = new Vector3[4]; GameObject hovered = null;
        try
        {
            for (int mode = 0; mode < 2; mode++)
            {
                if (mode == 0) world.Ui.Open(); else world.Ui.OpenBuffs();
                yield return DemoUiCompositionTests.WaitFor(() => world.Ui.RewardView != null);
                view = world.Ui.RewardView;
                var panel = view.transform.Find("RewardChoicePanel");
                var tooltip = panel.Find("Tooltip").GetComponent<TMP_Text>();
                Canvas.ForceUpdateCanvases(); yield return null;
                var canvas = panel.GetComponentInParent<Canvas>();
                Camera camera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
                for (int index = 0; index < choice.OptionCount; index++)
                {
                    var card = panel.Find("RewardOption_" + (index + 1)); var rect = (RectTransform)card;
                    foreach (var graphic in card.GetComponentsInChildren<Graphic>(true))
                        Assert.That(graphic.raycastTarget, Is.EqualTo(graphic.transform == card), graphic.name);
                    foreach (var name in new[] { "Label", "Icon", "MarkIcon" })
                    {
                        ((RectTransform)card.Find(name)).GetWorldCorners(corners);
                        foreach (var corner in corners) Assert.That(rect.rect.Contains(rect.InverseTransformPoint(corner)), Is.True, name + "超出卡片范围");
                    }
                    string description = mode == 0 ? choice.GetOptionConfig(index).description : choice.GetBuffOptionConfig(index).description;
                    // Use actual UGUI raycasts across the card, including text and icon regions.
                    for (int x = 0; x < 5; x++) for (int y = 0; y < 4; y++)
                    {
                        var local = new Vector2(Mathf.Lerp(rect.rect.xMin, rect.rect.xMax, .08f + .21f * x),
                            Mathf.Lerp(rect.rect.yMin, rect.rect.yMax, .08f + .28f * y));
                        data.position = RectTransformUtility.WorldToScreenPoint(camera, rect.TransformPoint(local));
                        hits.Clear(); eventSystem.RaycastAll(data, hits);
                        Assert.That(hits.Count, Is.GreaterThan(0));
                        var target = ExecuteEvents.GetEventHandler<IPointerEnterHandler>(hits[0].gameObject);
                        Assert.That(target, Is.SameAs(card.gameObject), "鼠标命中了其他奖励项：" + hits[0].gameObject.name);
                        if (hovered != target)
                        {
                            if (hovered != null) ExecuteEvents.Execute(hovered, data, ExecuteEvents.pointerExitHandler);
                            ExecuteEvents.Execute(target, data, ExecuteEvents.pointerEnterHandler); hovered = target;
                        }
                        StringAssert.StartsWith(description, tooltip.text);
                    }
                }
                // Moving off the card restores the selected card's confirmation description.
                var first = panel.Find("RewardOption_1").gameObject;
                ExecuteEvents.Execute(first, data, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(first, data, ExecuteEvents.pointerExitHandler);
                string selectedDescription = mode == 0 ? choice.GetOptionConfig(0).description : choice.GetBuffOptionConfig(0).description;
                StringAssert.StartsWith(selectedDescription, tooltip.text);
                StringAssert.Contains("再次点击该项确认", tooltip.text);
                view.Hide(); choice.ResetForRetry(); hovered = null;
            }
        }
        finally { world.Ui.Hide(); choice.ResetForRetry(); }
    }
}
#endif
