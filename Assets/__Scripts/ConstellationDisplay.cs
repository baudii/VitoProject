using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using System.Linq;
using System.Threading.Tasks;
using System;

public class ConstellationDisplay : MonoBehaviour
{
	[SerializeField] private StarDisplay starPrefab;
	[SerializeField] private Transform GFX;
	[SerializeField] private Renderer rend;
	[SerializeField] private LineController linePrefab;
	[SerializeField] private MeshCombiner meshCombinerPrefab;

	public int LineRendererCount => lines.Count;

	private ConstellationData constellationData;
	private StarDisplay centerMostStar;

	private List<LineController> lines;
	public Dictionary<int, LineController> UsedLines;
	public SortedDictionary<int, List<LineController>> LineAnimationGroups;

	private Dictionary<int, StarDisplay> dynamicStars;
	private Transform linesParent;
	private Transform dynamicStarsParent;

	private bool isImageEnabled;
	private bool isLinesEnabled;
	private bool isOpeningLines;
	private bool isClosingLines;
	private bool linesToggler;
	private bool isAnimatingImage;

	private bool breakAsync;

	private void Awake()
	{
		lines = new List<LineController>();
		UsedLines = new Dictionary<int, LineController>();
		LineAnimationGroups = new SortedDictionary<int, List<LineController>>();
		dynamicStars = new Dictionary<int, StarDisplay>();
	}

	public void Init(ConstellationInstanceInfo instanceInfo)
	{
		// Инициализируем созвездие
		constellationData = JsonUtility.FromJson<JsonData>(instanceInfo.JsonAsset.text).items[0];
		rend.sharedMaterial = instanceInfo.Material;
		var cam = Camera.main;
		SetupTransform(constellationData, cam.transform);
		var color = rend.material.color;
		color.a = 0;
		rend.material.color = color; // rend - отображает изображение созвездия. По умолчанию оно выключено
		rend.gameObject.SetActive(false);

		CreateStars(cam.transform);

		centerMostStar = dynamicStars.Aggregate((minStar, nextStar) =>
			(constellationData.WorldPos - nextStar.Value.data.WorldPos).magnitude <
			(constellationData.WorldPos - minStar.Value.data.WorldPos).magnitude ?
			nextStar : minStar)
			.Value;
	}

	public void UpdateLinesWidth()
	{
		// Метод для редактирования толщины линии в рантайме
		var width = ConstellationManager.Instance.LineWidth;
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
		if (isImageEnabled == false)
			rend.gameObject.SetActive(true);


		StartCoroutine(Helper.FadeAnimation(rend, ConstellationManager.Instance.AnimationDuration, !isImageEnabled, OnComplete: () => 
		{
			isImageEnabled = !isImageEnabled;
			if (isImageEnabled == false)
				rend.gameObject.SetActive(false);
			isAnimatingImage = false;
			OnComplete?.Invoke();
		}));
	}


	public void ToggleAnimations()
	{
		if (linesToggler)
		{
			AnimateLinesClose();
		}
		else
		{
			AnimateLinesOpen();
		}
	}

	public void AnimateLinesOpen(Action OnComplete = null) => AnimateLinesOpenAsync(OnComplete);
	public void AnimateLinesClose(Action OnComplete = null) => StartCoroutine(AnimateLinesCloseCoroutine(OnComplete));

	#region Private Methods

	private async void AnimateLinesOpenAsync(Action OnComplete = null)
	{       
		// Анимация линий созвездия
		if (isOpeningLines || isLinesEnabled || isClosingLines)
			return;

		dynamicStarsParent.gameObject.SetActive(true);
		linesParent.gameObject.SetActive(true);
		isOpeningLines = true;
		linesToggler = true;

		// Асинхронный метод для последовательной анимации (без рекурсии)
		int animationGroup = 0;
		List<StarDisplay> animatingBatch = await centerMostStar.AnimateAllNeighboursAsync(animationGroup); // Начинаем с самой центральной звезды
		List<Task<List<StarDisplay>>> tasks = new List<Task<List<StarDisplay>>>(); // Список всех тасков для синхронной анимации

		while (true)
		{
			if (breakAsync)
				break;

			animationGroup++;
			foreach (var star in animatingBatch)
			{
				var item = star.AnimateAllNeighboursAsync(animationGroup);
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

		isOpeningLines = false;
		isLinesEnabled = true;
		OnComplete?.Invoke();
	}

	private IEnumerator AnimateLinesCloseCoroutine(Action OnComplete = null)
	{
		// Разворот анимации
		if (LineAnimationGroups.Count == 0 || UsedLines.Count == 0 || isClosingLines)
			yield break;

		isClosingLines = true;

		// Останавливаем текущие анимации
		breakAsync = true;
		foreach (var star in dynamicStars.Values)
		{
			star.StopAllCoroutines();
			star.ResetScale();
		}
		foreach (var line in lines)
			line.StopAllCoroutines();

		// Создаем обратные анимации на тех объектах, которые успели закончить свою анимацию
		var animDuration = ConstellationManager.Instance.AnimationDuration;
		List<LineController> group;
		for (var i = LineAnimationGroups.Count - 1; i >= 0; i--)
		{
			group = LineAnimationGroups[i];
			foreach (var line in group)
			{
				line.RevertTarget();
				line.StretchLine(animDuration, () => line.IsEnabled = false);
			}

			yield return new WaitForSeconds(animDuration);
		}

		isOpeningLines = false;
		isLinesEnabled = false;
		isClosingLines = false;
		breakAsync = false;
		linesToggler = false;
		LineAnimationGroups.Clear();
		UsedLines.Clear();
		OnComplete?.Invoke();
		linesParent.gameObject.SetActive(false);
		dynamicStarsParent.gameObject.SetActive(false);
	}

	private void SetupTransform(ConstellationData constellationData, Transform camTransform)
	{
		// Настраиваем трансформ для корректного расположения созвездия на небесной сфере
		transform.position = constellationData.WorldPos;
		GFX.name = constellationData.name + "_Image";
		GFX.LookAt(camTransform);
		camTransform.LookAt(GFX);
		GFX.transform.position = constellationData.ImagePos;
		GFX.transform.eulerAngles -= new Vector3(0, 0, constellationData.image.angle);
		GFX.transform.localScale *= constellationData.image.scale;
		transform.name = constellationData.name;
	}

	private void CreateLine(StarDisplay fromStarDisplay, StarDisplay toStarDisplay)
	{
		// Создаем нужное кол-во линий (по-умолчанию выключены)
		var line = Instantiate(linePrefab, linesParent);
		line.Init();

		line.CalcPositions(fromStarDisplay, toStarDisplay);
		line.SetPosition();

		fromStarDisplay.AddConnection(line, toStarDisplay);
		toStarDisplay.AddConnection(line, fromStarDisplay);
		lines.Add(line);
	}

	private void CreateStars(Transform lookAt)
	{
		CreateDynamicStars(lookAt);
		CreateStaticStars(lookAt);
	}

	private void CreateDynamicStars(Transform lookAt)
	{
		// Создаем "динамические" звезды (которые участвуют в анимациях = части созвездий)
		dynamicStarsParent = new GameObject().transform;
		dynamicStarsParent.SetParent(transform);
		dynamicStarsParent.name = constellationData.name + "-Stars-Dynamic";
		dynamicStarsParent.gameObject.SetActive(false);

		linesParent = new GameObject("Lines").transform;
		linesParent.SetParent(transform); 
		linesParent.gameObject.SetActive(false);


		foreach (var connection in constellationData.pairs)
		{
			var fromStarData = constellationData.stars.First(star => star.id == connection.from);
			var toStarData = constellationData.stars.First(star => star.id == connection.to);

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

			// Создаем соединяющие линии созвездий
			CreateLine(dynamicStars[fromStarData.id], dynamicStars[toStarData.id]);
		}
	}

	private void CreateStaticStars(Transform lookAt)
	{
		// Создаем и объединяем статические звезды
		var meshCombiner = Instantiate(meshCombinerPrefab, transform);
		meshCombiner.name = constellationData.name + "-Stars-Static";
		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, meshCombiner.transform);
			starDisplay.Init(starData, this, lookAt);
			meshCombiner.Add(starDisplay.MeshSchemaIndex, starDisplay.meshFilter);
		}
		meshCombiner.CombineAllMeshes();
	}

	#endregion
}