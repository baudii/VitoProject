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
		// Инициализируем созвездие
		var cam = Camera.main;
		SetupTransform(cam.transform);

		var color = rend.material.color;
		color.a = 0;
		rend.material.color = color;

		// Создаем звезды
		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, starsParent.transform);
			starDisplay.Init(starData, this, cam.transform);
			stars.Add(starDisplay);
		}

		// Находим самую центральную звезду
		centerMostStar = stars.Aggregate((minStar, nextStar) =>
			(constellationData.WorldPos - nextStar.data.WorldPos).magnitude < (constellationData.WorldPos - minStar.data.WorldPos).magnitude ? nextStar : minStar);

		// Создаем соединяющие линии созвездий
		foreach (var connection in constellationData.pairs)
		{
			CreateLine(stars.First(star => star.data.id == connection.from), 
						stars.First(star => star.data.id == connection.to), 
						lineStarOffset);
		}
	}
	public void UpdateLinesWidth(float width)
	{
		// Метод для редактирования толщины линии в рантайме
		foreach (var line in lines)
		{
			line.lineRenderer.startWidth = width;
			line.lineRenderer.endWidth = width;
		}
	}

	public void AnimateImage(Action OnComplete = null)
	{
		// Анимация изображения созвездия
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
		// Анимация линий созвездия
		if (isAnimatingLines)
			return;
		isAnimatingLines = true;

		ActivateLinesAnimationAsync(OnComplete);
	}

	public void RevertAnimation()
	{
		// Разворот анимации
		if (UsedLines.Count == 0)
			return;
		isAnimatingLines = true;

		// Останавливаем текущие анимации
		breakAsync = true;
		foreach (var star in stars)
		{
			star.StopAllCoroutines();
			star.ResetScale();
		}
		foreach (var line in lines)
			line.StopAllCoroutines();

		// Создаем обратные анимации на тех объектах, которые успели закончить свою анимацию
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

		// Асинхронный метод для последовательной анимации (без рекурсии)
		List<StarDisplay> animatingBatch = await centerMostStar.AnimateAllNeighboursAsync(stretchAnimationMode); // Начинаем с самой центральной звезды
		List<Task<List<StarDisplay>>> tasks = new List<Task<List<StarDisplay>>>(); // Список всех тасков для синхронной анимации

		while (true)
		{
			if (breakAsync)
				break;

			foreach (var star in animatingBatch)
			{
				var item = star.AnimateAllNeighboursAsync(stretchAnimationMode);
				tasks.Add(item);
			}
			
			// Если тасков не получено, значит анимации закончились
			if (tasks.Count == 0)
				break;

			await Task.WhenAll(tasks);

			animatingBatch.Clear(); // Удаляем уже анимированные звезды
			foreach (var task in tasks) 
				animatingBatch.AddRange(task.Result); // Добавляем новые (те, что соединены с теми, которые были удалены)

			tasks.Clear();
		}

		isAnimatingLines = false;
		isLinesEnabled = true;
		OnComplete?.Invoke();
	}

	private void SetupTransform(Transform camTransform)
	{
		// Настраиваем трансформ для корректного расположения созвездия на небесной сфере
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
		// Создаем нужное кол-во линий (по-умолчанию выключены)

		var line = Instantiate(linePrefab, transform);
		line.Init(stretchAnimationMode, lineStarOffset);

		line.CalcPositions(fromStarDisplay, toStarDisplay);
		line.SetPosition();

		fromStarDisplay.AddConnection(line, toStarDisplay);
		toStarDisplay.AddConnection(line, fromStarDisplay);
		lines.Add(line);
	}
}