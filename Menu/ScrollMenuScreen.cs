using GlobalEnums;
using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Screens;
using Silksong.UnityHelper.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace EdgeDetection.Menu;

/// <summary>
/// A vertically scrolling menu screen with a single VerticalGroup for the content panel.
/// Sufficient for simple menus with any number of elements at any size.
/// </summary>
internal class ScrollMenuScreen : AbstractMenuScreen {

	public VerticalGroup Content {
		get => _content;
		set {
			if (value == null)
				throw new ArgumentNullException(nameof(Content));
			if (_content != value && _content != null){
				_content.ClearParents();
				foreach (var e in _content.AllElements().OfType<SelectableElement>())
					e.SelectableComponent.gameObject.RemoveComponent<OnSelectedTrigger>();
			}
			_content = value;
			_content.SetGameObjectParent(Elements);
		}
	}
	VerticalGroup _content;

	public ScrollRect ScrollRect => ScrollPane.GetComponent<ScrollRect>();
	public RectMask2D ViewportMask => Viewport.GetComponent<RectMask2D>();

	GameObject ScrollPane => ContentPane;
	GameObject Viewport { get; set; }
	GameObject Elements { get; set; }
	GameObject Bar { get; set; }
	GameObject BarHandle { get; set; }

	RectTransform ScrollPaneT => (RectTransform)ScrollPane.transform;
	RectTransform ViewportT => (RectTransform)Viewport.transform;
	RectTransform ElementsT => (RectTransform)Elements.transform;
	VerticalLayoutGroup ElementsGrp => Elements.GetComponent<VerticalLayoutGroup>();
	RectTransform BarT => (RectTransform)Bar.transform;
	RectTransform BarHandleT => (RectTransform)BarHandle.transform;

	static Sprite ViewportSprite =>
		UIManager.instance.transform
		.Find("UICanvas/BrightnessMenuScreen/Content/BrightnessCalibrationImage")
		.GetComponent<Image>().sprite;

	public ScrollMenuScreen(LocalizedText title, VerticalGroup content) : base(title) {
		// create objects
		ScrollPane.name = "ScrollPane";
		Viewport = new("Viewport");
		Elements = new("Elements");
		Bar = new("Scrollbar");
		BarHandle = new("Handle");
		ScrollPane.layer = Viewport.layer = Elements.layer = Bar.layer = BarHandle.layer = (int)PhysLayers.UI;

		// setup hierarchy
		ScrollPane.transform.Reset();

		Viewport.transform.SetParent(ScrollPane.transform, false);
		Viewport.transform.Reset();
		Elements.transform.SetParent(Viewport.transform, false);
		Elements.transform.Reset();

		Bar.transform.SetParent(ScrollPane.transform, false);
		Bar.transform.Reset();
		BarHandle.transform.SetParent(Bar.transform, false);
		BarHandle.transform.Reset();

		// add components - content container
		var group = Elements.AddComponent<VerticalLayoutGroup>();
		group.childAlignment = TextAnchor.UpperCenter;
		group.padding.bottom = 100;
		group.padding.top = 50;
		group.childForceExpandHeight = true;

		// add content
		Content = content;

		Shader uishader = Shader.Find("UI/Default");

		// add components - viewport
		var image = Viewport.AddComponent<Image>();
		image.sprite = ViewportSprite;
		image.SetNativeSize();
		image.material = new Material(uishader) { color = Color.clear };
		var mask = Viewport.AddComponent<RectMask2D>();
		mask.softness = new Vector2Int(0, 100);

		// add components - scrollbar
		var handleImg = BarHandle.AddComponent<Image>();
		handleImg.material = new Material(uishader) { color = Color.white };

		var barImg = Bar.AddComponent<Image>();
		barImg.material = new Material(uishader) { color = new Color(0.428f, 0.439f, 0.459f, 1) };
		var scrollbar = Bar.AddComponent<Scrollbar>();
		scrollbar.direction = Scrollbar.Direction.BottomToTop;
		scrollbar.handleRect = BarHandleT;

		// add components - scroll pane
		var scroller = ScrollPane.AddComponent<ScrollRect>();
		scroller.horizontal = false;
		scroller.vertical = true;
		scroller.movementType = ScrollRect.MovementType.Clamped;
		scroller.scrollSensitivity = 50;
		scroller.viewport = ViewportT;
		scroller.content = ElementsT;
		scroller.verticalScrollbar = scrollbar;
		scroller.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;

		// ensure all anchors/sizes/positions are correct
		Vector2 center = Vector2.one * 0.5f;

		ScrollPaneT.anchorMax = ScrollPaneT.anchorMin = center;
		ScrollPaneT.sizeDelta = new Vector2(1500, 766);
		ScrollPaneT.localPosition = new Vector3(0, -40, 0);

		ViewportT.anchorMax = ViewportT.anchorMin = center;
		ViewportT.sizeDelta = new Vector2(1920, 950);
		
		ElementsT.anchorMax = ElementsT.anchorMin = center;
		ElementsT.sizeDelta = new(1920, Content.AllElements().Count() * Content.VerticalSpacing + ElementsGrp.padding.vertical);

		BarT.anchorMax = BarT.anchorMin = new Vector2(1, 0.5f);
		BarT.sizeDelta = new Vector2(7, 800);

		BarHandleT.sizeDelta = Vector2.one;

		// make sure it always starts at the top
		scroller.normalizedPosition = Vector2.up;
		OnShow += _ => scroller.normalizedPosition = Vector2.up;
	}

	protected override IEnumerable<IMenuEntity> AllEntities() => [Content];

	protected override SelectableElement? GetDefaultSelectableInternal() => Content.GetDefaultSelectable();

	protected override void UpdateLayout() {
		Content.UpdateLayout(SpacingConstants.TOP_CENTER_ANCHOR);
		ElementsT.sizeDelta = new(1920, Content.AllElements().Count() * Content.VerticalSpacing + ElementsGrp.padding.vertical);

		foreach (var e in _content.AllElements().OfType<SelectableElement>()) {
			var go = e.SelectableComponent.gameObject;
			if (!go.GetComponent<OnSelectedTrigger>())
				go.AddComponent<OnSelectedTrigger>().Fn = ScrollIntoView;
		}

		BackButton.navigation = new Navigation() {
			mode = Navigation.Mode.Explicit,
			wrapAround = false
		};
		Content.SetNeighborDown(BackButton);
		Content.SetNeighborUp(BackButton);
		if (Content.GetNeighborDown(out var selectable))
			BackButton.navigation = BackButton.navigation with { selectOnDown = selectable };
		if (Content.GetNeighborUp(out selectable))
			BackButton.navigation = BackButton.navigation with { selectOnUp = selectable };
	}

	// TODO: this keeps firing for mouse events :))))))))
	void ScrollIntoView(BaseEventData baseData) {
		if (baseData is not AxisEventData || ElementsT.sizeDelta.y <= ViewportT.sizeDelta.y)
			return;

		RectTransform target = (RectTransform)baseData.selectedObject.transform;

		Vector3 newPos =
			ScrollPaneT.InverseTransformPoint(ElementsT.position)
			- ScrollPaneT.InverseTransformPoint(target.position);

		float maxOffset = (ElementsT.sizeDelta.y - ViewportT.sizeDelta.y) / 2f;

		ElementsT.anchoredPosition = new(0, Mathf.Clamp(newPos.y, -maxOffset, maxOffset));
	}

	private class OnSelectedTrigger : EventTrigger {
		public Action<BaseEventData>? Fn;
		public override void OnSelect(BaseEventData eventData) => Fn?.Invoke(eventData);
	}

}
