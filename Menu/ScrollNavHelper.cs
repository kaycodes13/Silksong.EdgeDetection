using System.Collections;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EdgeDetection.Menu;

internal class ScrollNavHelper : EventTrigger {
	public (GameObject scroll, GameObject viewport, GameObject content) Panes {
		set {
			(scroll, viewport, content) = (
				value.scroll.RectTransform,
				value.viewport.RectTransform,
				value.content.RectTransform
			);
			scrollRect = scroll.GetComponent<ScrollRect>();
		}
	}
	RectTransform scroll, viewport, content;
	ScrollRect scrollRect;

	static Coroutine? scrollRoutine;
	const float SCROLL_TIME = 0.2f;

	/// <summary>
	/// Allows mouse-wheel scrolling when hovering over a selectable with this component;
	/// EventTriggers stop all events from bubbling.
	/// </summary>
	public override void OnScroll(PointerEventData eventData) => scrollRect.OnScroll(eventData);

	/// <summary>
	/// When this selectable is selected through keyboard/controller navigation, the
	/// scroll pane will scroll so that it's centered in the viewport.
	/// </summary>
	public override void OnSelect(BaseEventData eventData) {
		if (eventData is not AxisEventData || content.sizeDelta.y <= viewport.sizeDelta.y)
			return;
		ScrollToSmooth();
	}

    /// <summary>
    /// Scrolls the content pane to this selectable instantly.
    /// </summary>
    public void ScrollToInstant() {
		scrollRect.normalizedPosition = new Vector2(0, GetScrollPoint());
	}

	/// <summary>
	/// Scrolls the content pane to this selectable smoothly over time.
	/// </summary>
	public void ScrollToSmooth() {
		if (scrollRoutine != null)
			StopCoroutine(scrollRoutine);
		scrollRoutine = StartCoroutine(Coro());

		IEnumerator Coro() {
			float y = GetScrollPoint();
			var curve = AnimationCurve.EaseInOut(0, scrollRect.normalizedPosition.y, SCROLL_TIME, y);

			for (float time = 0; time <= SCROLL_TIME; time += Time.deltaTime) {
                scrollRect.normalizedPosition = new Vector2(0, curve.Evaluate(time));
                yield return null;
			}
            scrollRect.normalizedPosition = new Vector2(0, y);
		}
	}

	/// <summary>
	/// The y-coordinate to set the scroll pane's normalized position to
	/// in order to scroll to this selectable.
	/// Favours placing itself in the middle of the viewport.
	/// </summary>
	float GetScrollPoint() {
        var target = gameObject.transform;
        float maxOffset = (content.sizeDelta.y - viewport.sizeDelta.y) / 2f;
        Vector3
            contentPos = scroll.InverseTransformPoint(content.position),
            targetPos = scroll.InverseTransformPoint(target.position);

		float offset = Mathf.Clamp((contentPos - targetPos).y, -maxOffset, maxOffset);

		// normalized to (0, 1)
		return (offset - maxOffset) / (2 * -maxOffset);
    }
}