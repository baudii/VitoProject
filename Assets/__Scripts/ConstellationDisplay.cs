using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System;

public class ConstellationDisplay : MonoBehaviour
{
	[SerializeField, Tooltip("Lines will stretch instead of fading")] bool stretchAnimationMode;
	[SerializeField] StarDisplay starPrefab;
	[SerializeField] Transform starsParent;
	[SerializeField] Transform GFX;
	[SerializeField] Renderer rend;
	[SerializeField] TextAsset JsonAsset;
	[SerializeField] LineController linePrefab;

	public int LineRendererCount => lines.Count;
	public Dictionary<int, LineController> UsedLines;

	private ConstellationData constellationData;

	private List<StarDisplay> stars;
	private List<LineController> lines;
	private StarDisplay centerMostStar;
	
	private bool isImageEnabled;
	private bool isLinesEnabled;
	private bool isAnimatingLines;
	private bool isAnimatingImage;

	private bool breakAsync;


	private void Awake()
	{
		stars = new List<StarDisplay>();
		lines = new List<LineController>();
		constellationData = JsonUtility.FromJson<JsonData>(JsonAsset.text).items[0];
		UsedLines = new Dictionary<int, LineController>();
	}

	public void Init(float lineStarOffset)
	{
		// �������������� ���������
		var cam = Camera.main;
		SetupTransform(cam.transform);

		var color = rend.material.color;
		color.a = 0;
		rend.material.color = color;

		// ������� ������
		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, starsParent.transform);
			starDisplay.Init(starData, this, cam.transform);
			stars.Add(starDisplay);
		}

		// ������� ����� ����������� ������
		centerMostStar = stars.Aggregate((minStar, nextStar) =>
			(constellationData.WorldPos - nextStar.data.WorldPos).magnitude < (constellationData.WorldPos - minStar.data.WorldPos).magnitude ? nextStar : minStar);

		// ������� ����������� ����� ���������
		foreach (var connection in constellationData.pairs)
		{
			CreateLine(stars.First(star => star.data.id == connection.from), 
						stars.First(star => star.data.id == connection.to), 
						lineStarOffset);
		}
	}
	public void UpdateLinesWidth(float width)
	{
		// ����� ��� �������������� ������� ����� � ��������
		foreach (var line in lines)
		{
			line.lineRenderer.startWidth = width;
			line.lineRenderer.endWidth = width;
		}
	}

	public void AnimateImage(Action OnComplete = null)
	{
		// �������� ����������� ���������
		if (isAnimatingImage)
			return;
		isAnimatingImage = true;
		
		StartCoroutine(Helper.FadeAnimation(rend, 1, !isImageEnabled, OnComplete: () => 
		{ 
			isImageEnabled = !isImageEnabled;
			isAnimatingImage = false;
			OnComplete?.Invoke();
		}));
	}

	public void AnimateLines(Action OnComplete = null)
	{
		// �������� ����� ���������
		if (isAnimatingLines)
			return;
		isAnimatingLines = true;

		ActivateLinesAnimationAsync(OnComplete);
	}

	public void RevertAnimation()
	{
		// �������� ��������
		if (UsedLines.Count == 0)
			return;
		isAnimatingLines = true;

		// ������������� ������� ��������
		breakAsync = true;
		foreach (var star in stars)
		{
			star.StopAllCoroutines();
			star.ResetScale();
		}
		foreach (var line in lines)
			line.StopAllCoroutines();

		// ������� �������� �������� �� ��� ��������, ������� ������ ��������� ���� ��������
		foreach (var line in UsedLines.Values)
		{
			if (stretchAnimationMode)
			{
				line.RevertTarget();
				line.StretchLine(1, () =>
				{
					OnRevertComplete(line);
				});
			}
			else
			{
				line.StartCoroutine(Helper.FadeAnimation(line.lineRenderer, 3, line.IsEnabled, OnComplete: () =>
				{
					OnRevertComplete(line);
				}));
			}
		}

		void OnRevertComplete(LineController line)
		{
			// OnComplete
			line.IsEnabled = false;
			isAnimatingLines = false;
			isLinesEnabled = false;
			breakAsync = false;
			UsedLines.Clear();
		}
	}

	private async void ActivateLinesAnimationAsync(Action OnComplete = null)
	{
		if (isLinesEnabled)
			return;

		// ����������� ����� ��� ���������������� �������� (��� ��������)
		List<StarDisplay> animatingBatch = await centerMostStar.AnimateAllNeighboursAsync(stretchAnimationMode); // �������� � ����� ����������� ������
		List<Task<List<StarDisplay>>> tasks = new List<Task<List<StarDisplay>>>(); // ������ ���� ������ ��� ���������� ��������

		while (true)
		{
			if (breakAsync)
				break;

			foreach (var star in animatingBatch)
			{
				var item = star.AnimateAllNeighboursAsync(stretchAnimationMode);
				tasks.Add(item);
			}
			
			// ���� ������ �� ��������, ������ �������� �����������
			if (tasks.Count == 0)
				break;

			await Task.WhenAll(tasks);

			animatingBatch.Clear(); // ������� ��� ������������� ������
			foreach (var task in tasks) 
				animatingBatch.AddRange(task.Result); // ��������� ����� (��, ��� ��������� � ����, ������� ���� �������)

			tasks.Clear();
		}

		isAnimatingLines = false;
		isLinesEnabled = true;
		OnComplete?.Invoke();
	}

	private void SetupTransform(Transform camTransform)
	{
		// ����������� ��������� ��� ����������� ������������ ��������� �� �������� �����
		transform.position = constellationData.WorldPos;
		GFX.LookAt(camTransform);
		camTransform.LookAt(GFX);
		GFX.transform.position = constellationData.ImagePos;
		GFX.transform.eulerAngles -= new Vector3(0, 0, constellationData.image.angle);
		GFX.transform.localScale *= constellationData.image.scale;
		transform.name = constellationData.name;
	}

	private void CreateLine(StarDisplay fromStarDisplay, StarDisplay toStarDisplay, float lineStarOffset)
	{
		// ������� ������ ���-�� ����� (��-��������� ���������)

		var line = Instantiate(linePrefab, transform);
		line.Init(stretchAnimationMode, lineStarOffset);

		line.CalcPositions(fromStarDisplay, toStarDisplay);
		line.SetPosition();

		fromStarDisplay.AddConnection(line, toStarDisplay);
		toStarDisplay.AddConnection(line, fromStarDisplay);
		lines.Add(line);
	}
}