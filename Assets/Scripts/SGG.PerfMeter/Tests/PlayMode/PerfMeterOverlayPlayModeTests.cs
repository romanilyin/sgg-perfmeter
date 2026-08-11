using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using UnityEngine.UIElements;

namespace SGG.PerfMeter.Tests.PlayMode
{
	public sealed class PerfMeterOverlayPlayModeTests
	{
		private GameObject _owner;

		[TearDown]
		public void TearDown()
		{
			if (_owner != null)
			{
				Object.DestroyImmediate(_owner);
				_owner = null;
			}
		}

		[Test]
		public void FpsOnlyRowThresholdUsesTolerance()
		{
			Assert.That(PerfMeterOverlay.ShouldUseFpsOnlyTwoRows(100f, 100f), Is.False);
			Assert.That(PerfMeterOverlay.ShouldUseFpsOnlyTwoRows(100f, 100.5f), Is.False);
			Assert.That(PerfMeterOverlay.ShouldUseFpsOnlyTwoRows(100.51f, 100f), Is.True);
			Assert.That(PerfMeterOverlay.ShouldUseFpsOnlyTwoRows(99f, 100f), Is.False);
		}

		[UnityTest]
		public IEnumerator OwnedHostPreservesForeignUiAndDoesNotDuplicateOnRebuild()
		{
			PanelSettings panelSettings = Resources.Load<PanelSettings>("PerfMeterOverlayPanelSettings");
			Assert.That(panelSettings, Is.Not.Null);

			_owner = new GameObject("PerfMeter Overlay Ownership Test");
			UIDocument foreignDocument = _owner.AddComponent<UIDocument>();
			foreignDocument.panelSettings = panelSettings;
			VisualElement foreignChild = new VisualElement { name = "foreign-ui-child" };
			foreignDocument.rootVisualElement.Add(foreignChild);

			PerfMeterOverlay overlay = _owner.AddComponent<PerfMeterOverlay>();
			for (int frame = 0; frame < 30 && overlay.OwnedContainer == null; frame++)
			{
				yield return null;
			}

			Assert.That(overlay.PanelHostObject, Is.Not.Null);
			Assert.That(overlay.PanelHostObject.transform.parent, Is.EqualTo(_owner.transform));
			Assert.That(overlay.PanelRoot, Is.Not.Null);
			Assert.That(overlay.OwnedContainer, Is.Not.Null);
			Assert.That(overlay.PanelRoot, Is.Not.SameAs(foreignDocument.rootVisualElement));
			Assert.That(foreignDocument.panelSettings, Is.SameAs(panelSettings));
			Assert.That(foreignDocument.rootVisualElement.Q<VisualElement>(foreignChild.name), Is.SameAs(foreignChild));

#if UNITY_6000_5_OR_NEWER
			PanelRenderer renderer = overlay.PanelHostObject.GetComponent<PanelRenderer>();
			Assert.That(renderer, Is.Not.Null);
			Assert.That(renderer.panelSettings, Is.SameAs(panelSettings));
			Assert.That(overlay.PanelHostObject.GetComponent<UIDocument>(), Is.Null);
#else
			UIDocument document = overlay.PanelHostObject.GetComponent<UIDocument>();
			Assert.That(document, Is.Not.Null);
			Assert.That(document.panelSettings, Is.SameAs(panelSettings));
#endif

			for (int iteration = 0; iteration < 5; iteration++)
			{
				overlay.enabled = false;
				yield return null;
				overlay.enabled = true;
				yield return null;
			}

			overlay.SetTheme(PerfMeterOverlayTheme.Glass);
			overlay.SetFontFamily(PerfMeterOverlayFontFamily.JetBrainsMono);
			overlay.SetLayout(PerfMeterOverlayLayout.CompactCards);
			yield return null;
			yield return null;

			Assert.That(CountOwnedContainers(overlay.PanelRoot), Is.EqualTo(1));
			Assert.That(foreignDocument.panelSettings, Is.SameAs(panelSettings));
			Assert.That(foreignDocument.rootVisualElement.Q<VisualElement>(foreignChild.name), Is.SameAs(foreignChild));
		}

		[UnityTest]
		public IEnumerator FpsOnlyUsesStableNumericSlotsAndNumericFont()
		{
			_owner = new GameObject("PerfMeter Overlay Numeric Layout Test");
			PerfMeterOverlay overlay = _owner.AddComponent<PerfMeterOverlay>();
			yield return WaitForOverlay(overlay);

			overlay.SetFontFamily(PerfMeterOverlayFontFamily.Manrope);
			overlay.SetModules(PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Warnings);
			overlay.SetMode(PerfMeterOverlayMode.FpsOnly);
			overlay.SetTuning(2f, 0.84f, 24f, 0.25f, 120);
			yield return null;
			yield return null;
			Assert.That(overlay.FpsOnlyUsesTwoRows, Is.True);

			VisualElement root = overlay.OwnedContainer;
			Label currentValue = root.Q<Label>("sgg-perfmeter-fps-only-current-value");
			Label averageValue = root.Q<Label>("sgg-perfmeter-fps-only-average-value");
			Label renderValue = root.Q<Label>("sgg-perfmeter-fps-only-render-value");
			Assert.That(currentValue, Is.Not.Null);
			Assert.That(averageValue, Is.Not.Null);
			Assert.That(renderValue, Is.Not.Null);
			Assert.That(currentValue.resolvedStyle.width, Is.GreaterThan(0f));
			Assert.That(currentValue.resolvedStyle.flexShrink, Is.EqualTo(0f));
			Assert.That(averageValue.resolvedStyle.width, Is.EqualTo(currentValue.resolvedStyle.width).Within(0.01f));

			float stableWidth = currentValue.resolvedStyle.width;
			currentValue.text = "1.0";
			Assert.That(currentValue.resolvedStyle.width, Is.EqualTo(stableWidth).Within(0.01f));
			currentValue.text = "888.8";
			Assert.That(currentValue.resolvedStyle.width, Is.EqualTo(stableWidth).Within(0.01f));
			currentValue.text = "9999.9";
			Assert.That(currentValue.resolvedStyle.width, Is.EqualTo(stableWidth).Within(0.01f));

			PerfMeterOverlayFontResources resources = Resources.Load<PerfMeterOverlayFontResources>("PerfMeterOverlayFonts");
			Font numericFont = resources != null ? resources.JetBrainsMonoMedium ?? resources.JetBrainsMonoRegular : null;
			if (numericFont != null)
			{
				Assert.That(currentValue.resolvedStyle.unityFont, Is.SameAs(numericFont));
				Assert.That(renderValue.resolvedStyle.unityFont, Is.SameAs(numericFont));
			}

			Assert.That(overlay.FpsOnlyRequiredWidth, Is.GreaterThan(20f));

			overlay.SetTuning(0.5f, 0.84f, 12f, 0.25f, 120);
			yield return null;
			yield return null;
			Assert.That(overlay.FpsOnlyUsesTwoRows, Is.False);
		}

		[UnityTest]
		public IEnumerator WidgetRowsWrapAndCardValuesUseNumericSlots()
		{
			_owner = new GameObject("PerfMeter Overlay Widget Layout Test");
			PerfMeterOverlay overlay = _owner.AddComponent<PerfMeterOverlay>();
			yield return WaitForOverlay(overlay);

			overlay.SetModules(PerfMeterOverlayModule.Fps | PerfMeterOverlayModule.Timing | PerfMeterOverlayModule.Overdraw);
			overlay.SetMode(PerfMeterOverlayMode.Full);
			overlay.SetLayout(PerfMeterOverlayLayout.CompactCards);
			overlay.SetTuning(1f, 0.84f, 24f, 0.25f, 120);
			yield return null;
			yield return null;

			VisualElement root = overlay.OwnedContainer;
			VisualElement cardRow = root.Q<VisualElement>("sgg-perfmeter-widget-cards");
			VisualElement budgetRow = root.Q<VisualElement>("sgg-perfmeter-widget-bars");
			VisualElement fpsCard = root.Q<VisualElement>("sgg-perfmeter-widget-card-fps");
			Label fpsValue = root.Q<Label>("sgg-perfmeter-widget-card-fps-value");
			Assert.That(cardRow, Is.Not.Null);
			Assert.That(budgetRow, Is.Not.Null);
			Assert.That(fpsCard, Is.Not.Null);
			Assert.That(fpsValue, Is.Not.Null);
			Assert.That(cardRow.resolvedStyle.flexWrap, Is.EqualTo(Wrap.Wrap));
			Assert.That(budgetRow.resolvedStyle.flexWrap, Is.EqualTo(Wrap.Wrap));
			Assert.That(fpsValue.resolvedStyle.width, Is.GreaterThan(0f));
			Assert.That(fpsValue.resolvedStyle.width, Is.LessThanOrEqualTo(fpsCard.resolvedStyle.width));
			Assert.That(fpsValue.style.overflow.value, Is.EqualTo(Overflow.Visible));

			PerfMeterOverlayFontResources resources = Resources.Load<PerfMeterOverlayFontResources>("PerfMeterOverlayFonts");
			Font numericFont = resources != null ? resources.JetBrainsMonoMedium ?? resources.JetBrainsMonoRegular : null;
			Font regularFont = resources != null ? resources.ManropeRegular : null;
			if (numericFont != null)
			{
				Assert.That(fpsValue.resolvedStyle.unityFont, Is.SameAs(numericFont));
				Assert.That(root.Q<Label>("sgg-perfmeter-widget-card-fps-unit").resolvedStyle.unityFont, Is.SameAs(numericFont));
			}

			if (regularFont != null)
			{
				Assert.That(root.Q<Label>("sgg-perfmeter-widget-card-fps-caption").resolvedStyle.unityFont, Is.SameAs(regularFont));
			}

			string[] cardIds = { "fps", "cpu", "gpu", "spikes", "overdraw" };
			for (int i = 0; i < cardIds.Length; i++)
			{
				AssertCardChildrenWithin(root, cardIds[i]);
			}

			AssertBudgetChildrenWithin(root, "cpu-budget");
			AssertBudgetChildrenWithin(root, "gpu-budget");
		}

		[UnityTest]
		public IEnumerator RawFrameTimeStripAdvancesIndependentlyOfTextRefreshWithoutRebuildingTree()
		{
			_owner = new GameObject("PerfMeter Raw Frame Time Strip Test");
			PerfMeterOverlay overlay = _owner.AddComponent<PerfMeterOverlay>();
			yield return WaitForOverlay(overlay);

			overlay.SetModules(PerfMeterOverlayModule.Graphs);
			overlay.SetMode(PerfMeterOverlayMode.Full);
			overlay.SetTuning(1f, 0.84f, 12f, 2f, 120);
			yield return null;

			VisualElement container = overlay.OwnedContainer;
			int childCount = container.childCount;
			Assert.That(container[childCount - 1].name, Is.EqualTo("sgg-perfmeter-frame-time-strip-block"));
			Assert.That(container.Q<VisualElement>("sgg-perfmeter-frame-time-strip"), Is.Not.Null);

			overlay.RecordFrameTimeSample(100, 16d, true);
			overlay.RecordFrameTimeSample(101, 80d, true);
			overlay.RecordFrameTimeSample(102, 0d, false);
			Assert.That(overlay.FrameTimeStripSampleCount, Is.EqualTo(3));
			Assert.That(overlay.FrameTimeStripLastFrame, Is.EqualTo(102));

			for (int frame = 103; frame < 303; frame++)
			{
				overlay.RecordFrameTimeSample(frame, 16d, true);
			}

			Assert.That(overlay.FrameTimeStripSampleCount, Is.EqualTo(120));
			Assert.That(overlay.FrameTimeStripLastFrame, Is.EqualTo(302));
			Assert.That(container.childCount, Is.EqualTo(childCount));
			Assert.That(container.Q<VisualElement>("sgg-perfmeter-frame-time-strip"), Is.Not.Null);
		}

		private static void AssertCardChildrenWithin(VisualElement root, string id)
		{
			string cardName = "sgg-perfmeter-widget-card-" + id;
			VisualElement card = root.Q<VisualElement>(cardName);
			Assert.That(card, Is.Not.Null, cardName);
			Assert.That(card.resolvedStyle.width, Is.EqualTo(144f).Within(0.01f), cardName + " width");

			VisualElement title = root.Q<VisualElement>(cardName + "-title");
			VisualElement valueRow = root.Q<VisualElement>(cardName + "-value-row");
			VisualElement value = root.Q<VisualElement>(cardName + "-value");
			VisualElement unit = root.Q<VisualElement>(cardName + "-unit");
			VisualElement caption = root.Q<VisualElement>(cardName + "-caption");
			AssertWithin(title, card, cardName + " title");
			AssertWithin(valueRow, card, cardName + " value row");
			AssertWithin(value, card, cardName + " value");
			AssertWithin(unit, card, cardName + " unit");
			AssertWithin(caption, card, cardName + " caption");
			Assert.That(caption.resolvedStyle.whiteSpace, Is.EqualTo(WhiteSpace.NoWrap), cardName + " caption wrapping");
			Assert.That(caption.style.overflow.value, Is.EqualTo(Overflow.Visible), cardName + " caption overflow");

			float innerHeight = card.resolvedStyle.height - 15f - 2f;
			float childHeight = title.resolvedStyle.height + valueRow.resolvedStyle.height + caption.resolvedStyle.height;
			Assert.That(childHeight, Is.LessThanOrEqualTo(innerHeight + 0.01f), cardName + " child height");
		}

		private static void AssertBudgetChildrenWithin(VisualElement root, string id)
		{
			string budgetName = "sgg-perfmeter-widget-budget-" + id;
			VisualElement budget = root.Q<VisualElement>(budgetName);
			Assert.That(budget, Is.Not.Null, budgetName);
			Assert.That(budget.resolvedStyle.width, Is.EqualTo(372f).Within(0.01f), budgetName + " width");
			AssertWithin(root.Q<VisualElement>(budgetName + "-title"), budget, budgetName + " title");
			AssertWithin(root.Q<VisualElement>(budgetName + "-value"), budget, budgetName + " value");
			AssertWithin(root.Q<VisualElement>(budgetName + "-track"), budget, budgetName + " track");
		}

		private static void AssertWithin(VisualElement element, VisualElement parent, string description)
		{
			Assert.That(element, Is.Not.Null, description);
			if (element.resolvedStyle.display == DisplayStyle.None)
			{
				return;
			}

			const float tolerance = 0.5f;
			Rect elementBounds = element.worldBound;
			Rect parentBounds = parent.worldBound;
			Assert.That(elementBounds.xMin, Is.GreaterThanOrEqualTo(parentBounds.xMin - tolerance), description + " xMin");
			Assert.That(elementBounds.xMax, Is.LessThanOrEqualTo(parentBounds.xMax + tolerance), description + " xMax");
			Assert.That(elementBounds.yMin, Is.GreaterThanOrEqualTo(parentBounds.yMin - tolerance), description + " yMin");
			Assert.That(elementBounds.yMax, Is.LessThanOrEqualTo(parentBounds.yMax + tolerance), description + " yMax");
		}

		private static IEnumerator WaitForOverlay(PerfMeterOverlay overlay)
		{
			for (int frame = 0; frame < 30 && overlay.OwnedContainer == null; frame++)
			{
				yield return null;
			}

			Assert.That(overlay.OwnedContainer, Is.Not.Null);
			Assert.That(overlay.PanelRoot, Is.Not.Null);
		}

		private static int CountOwnedContainers(VisualElement root)
		{
			return root.Query<VisualElement>(name: "sgg-perfmeter-overlay").ToList().Count;
		}
	}
}
