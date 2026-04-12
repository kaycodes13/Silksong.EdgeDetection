using Silksong.ModMenu.Elements;
using Silksong.ModMenu.Screens;
using System.Collections.Generic;
using System.Linq;
using TeamCherry.SharedUtils;
using UnityEngine.UI;
using static EdgeDetection.Menu.MenuUtils;

namespace EdgeDetection.Menu;

/// <summary>
/// A vertically scrolling menu screen with a single VerticalGroup for the content panel.
/// Sufficient for simple menus with an arbitrary number of elements.
/// </summary>
internal class ScrollMenuScreen : AbstractMenuScreen {

	const int
		MASK_SOFTNESS = 100,
		PADDING_BOTTOM = 100,
		PADDING_FULL = 125;
	const float
		VIEWPORT_WIDTH = 1920,
		VIEWPORT_HEIGHT = 825,
		SCROLLPANE_WIDTH = 1480;

	readonly GameObject ScrollPane, Viewport, Bar, Handle;

	public ScrollMenuScreen(LocalizedText title, VerticalGroup content) : base(title) {
		// create objects
		ScrollPane = UIGameObject("ScrollPane", parent: Container);
			Viewport = UIGameObject("Viewport", parent: ScrollPane);
				ContentPane.transform.SetParentReset(Viewport.transform);
					Content = content;
			Bar = UIGameObject("Scrollbar", parent: ScrollPane);
				Handle = UIGameObject("Handle", parent: Bar);

		Vector2 center = Vector2.one * 0.5f;

		// add & set up components, from the bottom up
		Handle.AddComponent<Image>().material = UIMaterial(Color.white);
		Handle.SetSizeDelta(Vector2.one);

		Bar.AddComponent<Image>().material = UIMaterial(new Color(1, 1, 1, 0.4f));
		var scrollbar = Bar.AddComponent<Scrollbar>();
		scrollbar.direction = Scrollbar.Direction.BottomToTop;
		scrollbar.handleRect = Handle.RectTransform;
		Bar.SetAnchors(new Vector2(1, 0.5f));
		Bar.SetSizeDelta(new Vector2(7, VIEWPORT_HEIGHT));

		ContentPane.SetAnchors(center);

		Viewport.AddComponent<Image>().material = UIMaterial(Color.clear);
		Viewport.AddComponent<RectMask2D>().softness = new Vector2Int(0, MASK_SOFTNESS);
		Viewport.SetAnchors(center);
		Viewport.SetSizeDelta(new Vector2(VIEWPORT_WIDTH, VIEWPORT_HEIGHT + PADDING_FULL));

		var scrollRect = ScrollPane.AddComponent<ScrollRect>();
		scrollRect.horizontal = false;
		scrollRect.vertical = true;
		scrollRect.verticalScrollbarVisibility = ScrollRect.ScrollbarVisibility.AutoHide;
		scrollRect.movementType = ScrollRect.MovementType.Clamped;
		scrollRect.scrollSensitivity = 50;
		scrollRect.viewport = Viewport.RectTransform;
		scrollRect.content = ContentPane.RectTransform;
		scrollRect.verticalScrollbar = scrollbar;
		ScrollPane.SetAnchors(center);
		ScrollPane.SetSizeDelta(new Vector2(SCROLLPANE_WIDTH, VIEWPORT_HEIGHT + PADDING_FULL));
		ScrollPane.RectTransform.localPosition = new Vector3(0, -40, 0);

		// additional scrolling logic
		OnShow += navType => {
			UpdateLayout();
			SelectOnShow(navType).GetComponent<ScrollNavHelper>().ScrollToInstant();
		};
	}

	/// <summary>
	/// The content displayed by this menu screen, minus the back button.
	/// </summary>
	public VerticalGroup Content {
		get => field;
		set {
			if (value == null)
				throw new System.ArgumentNullException(nameof(Content));
			if (field != value && field != null) {
				field.ClearParents();
				foreach (var e in field.AllSelectables())
					if (e.TryGetComponent<ScrollNavHelper>(out var navHelper))
						Object.Destroy(navHelper);
			}
			field = value;
			AddChild(field);
			field.SetGameObjectParent(ContentPane);
		}
	}

	/// <summary>
	/// The actual <see cref="ScrollRect"/> component that controls the
	/// scrolling sensitivity and movement type.
	/// </summary>
	public ScrollRect ScrollRect => ScrollPane.GetComponent<ScrollRect>();

	/// <inheritdoc/>
	protected override IEnumerable<IMenuEntity> AllEntities() => [Content];

	/// <inheritdoc/>
	protected override SelectableElement? GetDefaultSelectableInternal()
		=> Content.GetDefaultSelectable();

	/// <inheritdoc/>
	protected override void UpdateLayout() {
		MenuElement[] elements = [..
			Content.AllElements()
			.Where(x => !Content.HideInactiveElements || x.Visibility.VisibleInHierarchy)
		];
		float sizeY = Mathf.Max(VIEWPORT_HEIGHT, elements.Length * Content.VerticalSpacing) + PADDING_FULL;

		ContentPane.SetSizeDelta(new Vector2(VIEWPORT_WIDTH, sizeY));
		Content.UpdateLayout(new(0, sizeY / 2f - PADDING_BOTTOM));

		foreach(var e in Content.AllSelectables()) {
			e.gameObject.AddComponentIfNotPresent<ScrollNavHelper>()
				.Panes = (ScrollPane, Viewport, ContentPane);
		}

		Content.SetNeighborDown(BackButton);
		Content.SetNeighborUp(BackButton);

		BackButton.navigation = new Navigation {
			mode = Navigation.Mode.Explicit,
			wrapAround = false,
			selectOnDown = Content.GetNeighborDown(out var down) ? down : null,
			selectOnUp = Content.GetNeighborUp(out var up) ? up : null,
		};
	}

}
