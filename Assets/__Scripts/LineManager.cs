using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;

public class LineManager : MonoBehaviour
{
	public StarDisplay CenterMostStar { get; private set; }

	public Dictionary<int, LineController> UsedLines;
	public SortedDictionary<int, List<LineController>> LineAnimationGroups;
	private List<LineController> lines;

	private Dictionary<int, StarDisplay> dynamicStars;
	private Transform dynamicStarsParent;
	private Transform linesParent;
	private ConstellationDisplay constDisplay;

	private StarDisplay starPrefab;
	private LineController linePrefab;

	private Coroutine disablingDynamicStarsCoroutine;

	private CancellationTokenSource cts;


	private enum AnimationState
	{
		Opening,
		Open,
		Closing,
		Closed
	}
	private AnimationState currentLineState;


	public void Init(ConstellationDisplay constDisplay, StarDisplay starPrefab, LineController linePrefab)
	{
		lines = new List<LineController>();
		UsedLines = new Dictionary<int, LineController>();
		LineAnimationGroups = new SortedDictionary<int, List<LineController>>();
		dynamicStars = new Dictionary<int, StarDisplay>();

		this.constDisplay = constDisplay;
		this.starPrefab = starPrefab;
		this.linePrefab = linePrefab;

		CreateDynamicStarsAndLines(Camera.main.transform);

		CenterMostStar = dynamicStars.Aggregate((minStar, nextStar) =>
			(constDisplay.constellationData.WorldPos - nextStar.Value.data.WorldPos).magnitude <
			(constDisplay.constellationData.WorldPos - minStar.Value.data.WorldPos).magnitude ?
			nextStar : minStar)
			.Value;

		currentLineState = AnimationState.Closed;
	}

	public void AnimateLinesOpen(Action OnComplete = null)
	{
		if (cts != null)
			return;

		cts = new CancellationTokenSource(); 
		AnimateLinesOpenAsync(OnComplete);
	}

	public void AnimateLinesClose(Action OnComplete = null) => StartCoroutine(AnimateLinesCloseCoroutine(OnComplete));

	public async void AnimateLinesOpenAsync(Action OnComplete = null)
	{
		// �������� ����� ���������
		if (currentLineState != AnimationState.Closed)
			return;
		currentLineState = AnimationState.Opening;

		if (disablingDynamicStarsCoroutine != null)
			StopCoroutine(disablingDynamicStarsCoroutine);
		dynamicStarsParent.gameObject.SetActive(true);

		linesParent.gameObject.SetActive(true);

		try
		{
			// ����������� ����� ��� ���������������� �������� (��� ��������)
			int animationGroup = 0;
			List<StarDisplay> animatingBatch = await CenterMostStar.AnimateAllNeighboursAsync(animationGroup, cts.Token); // �������� � ����� ����������� ������
			cts.Token.ThrowIfCancellationRequested();

			List<Task<List<StarDisplay>>> tasks = new List<Task<List<StarDisplay>>>(); // ������ ���� ������ ��� ���������� ��������

			while (true)
			{
				animationGroup++;
				foreach (var star in animatingBatch)
				{
					var item = star.AnimateAllNeighboursAsync(animationGroup, cts.Token);
					tasks.Add(item);
				}

				// ���� ������ �� ��������, ������ �������� �����������
				if (tasks.Count == 0)
					break;

				await Task.Run(() => Task.WhenAll(tasks), cts.Token);
				cts.Token.ThrowIfCancellationRequested();

				animatingBatch.Clear(); // ������� ��� ������������� ������
				foreach (var task in tasks)
					animatingBatch.AddRange(await task); // ��������� ����� (��, ��� ��������� � ����, ������� ���� �������)

				tasks.Clear();
			}

			OnComplete?.Invoke();
			disablingDynamicStarsCoroutine = this.DelayedExecute(1,
				() => dynamicStarsParent.gameObject.SetActive(false));
			currentLineState = AnimationState.Open;
		}
		catch (OperationCanceledException _) { }
		catch { throw; }
		finally
		{
			cts?.Dispose();
			cts = null;
		}
	}

	public IEnumerator AnimateLinesCloseCoroutine(Action OnComplete = null)
	{
		// �������� ��������
		if (LineAnimationGroups.Count == 0 || UsedLines.Count == 0 ||
			currentLineState == AnimationState.Closing || currentLineState == AnimationState.Closed)
			yield break;

		currentLineState = AnimationState.Closing;

		// ������������� ������� ��������
		cts?.Cancel();

		foreach (var star in dynamicStars.Values)
		{
			star.StopAllCoroutines();
			star.ResetScale();
		}
		foreach (var line in lines)
			line.StopAllCoroutines();

		// ������� �������� �������� �� ��� ��������, ������� ������ ��������� ���� ��������
		var animDuration = ConstellationManager.Instance.AnimationDuration;
		List<LineController> group;
		AnimationBuffer animBuffer;
		for (var i = LineAnimationGroups.Count - 1; i >= 0; i--)
		{
			animBuffer = new AnimationBuffer();
			group = LineAnimationGroups[i];

			foreach (var line in group)
			{
				line.RevertTarget();
				line.StretchLine(animDuration, () => line.IsEnabled = false, animBuffer);
			}

			yield return new WaitUntil(() => animBuffer.Finished);
		}

		LineAnimationGroups.Clear();
		UsedLines.Clear();
		linesParent.gameObject.SetActive(false);
		dynamicStarsParent.gameObject.SetActive(false);
		OnComplete?.Invoke();
		currentLineState = AnimationState.Closed;
	}

	public void UpdateLinesWidth()
	{
		// ����� ��� �������������� ������� ����� � ��������
		var width = ConstellationManager.Instance.LineWidth;
		foreach (var line in lines)
		{
			line.lineRenderer.startWidth = width;
			line.lineRenderer.endWidth = width;
		}
	}

	private void CreateDynamicStarsAndLines(Transform lookAt)
	{
		var constellationData = constDisplay.constellationData;
		// ������� "������������" ������ (������� ��������� � ��������� = ����� ���������) � ����������� �� �����

		// ������� ������������ ������ ��� ������������ �����
		dynamicStarsParent = new GameObject().transform;
		dynamicStarsParent.SetParent(transform);
		dynamicStarsParent.name = constellationData.name + "-Stars-Dynamic";
		dynamicStarsParent.gameObject.SetActive(false);

		// ������� ������������ ������ ��� �����
		linesParent = new GameObject("Lines").transform;
		linesParent.SetParent(transform);
		linesParent.gameObject.SetActive(false);

		// ��������� ������ �� ���� ������
		foreach (var connection in constellationData.pairs)
		{
			// ������� �������� �� id
			var fromStarData = constellationData.stars.First(star => star.id == connection.from);
			var toStarData = constellationData.stars.First(star => star.id == connection.to);

			// ������� ��� ������, ���� ��� �� ������� (� ���� ����, ��� ���� ����� ��������� ���������)
			if (!dynamicStars.ContainsKey(fromStarData.id))
			{
				StarDisplay starDisplay = Instantiate(starPrefab, dynamicStarsParent.transform);
				starDisplay.Init(fromStarData, this, lookAt);
				dynamicStars.Add(fromStarData.id, starDisplay);
			}

			if (!dynamicStars.ContainsKey(toStarData.id))
			{
				StarDisplay starDisplay = Instantiate(starPrefab, dynamicStarsParent.transform);
				starDisplay.Init(toStarData, this, lookAt);
				dynamicStars.Add(toStarData.id, starDisplay);
			}

			// ������� ����������� ����� ����� ��������
			CreateLine(dynamicStars[fromStarData.id], dynamicStars[toStarData.id]);
		}
	}

	private void CreateLine(StarDisplay fromStarDisplay, StarDisplay toStarDisplay)
	{
		// ������� ������ ���-�� ����� (��-��������� ���������)
		var line = Instantiate(linePrefab, linesParent);
		line.Init();

		line.CalcPositions(fromStarDisplay, toStarDisplay);
		line.SetPosition();

		fromStarDisplay.AddConnection(line, toStarDisplay);
		toStarDisplay.AddConnection(line, fromStarDisplay);
		lines.Add(line);
	}
}
