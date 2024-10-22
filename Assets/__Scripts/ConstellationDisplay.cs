using UnityEngine;
using System;


public class ConstellationDisplay : MonoBehaviour
{
	[SerializeField] private StarDisplay starPrefab;
	[SerializeField] private Transform GFX;
	[SerializeField] private Renderer rend;
	[SerializeField] private LineController linePrefab;
	[SerializeField] private MeshCombiner meshCombinerPrefab;
	[SerializeField] private LineManager lineManagerPrefab;
	[SerializeField] bool stop;

	public ConstellationData constellationData { get; private set; }
	public LineManager LineManager { get; private set; }
	

	private bool isLinesEnabled;
	private bool imageToggler;


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

		CreateSkybox(cam.transform);
		LineManager = Instantiate(lineManagerPrefab, transform);
		LineManager.Init(this, starPrefab, linePrefab);

	}

	public void ToggleAnimateImage(Action OnComplete = null)
	{
		// Анимация изображения созвездия
		rend.gameObject.SetActive(true);
		float targetAlpha = 1;
		if (imageToggler)
		{
			targetAlpha = 0;
		}

		StartCoroutine(Helper.FadeAnimation(rend, ConstellationManager.Instance.AnimationDuration, targetAlpha, OnComplete: () =>
		{
			if (imageToggler)
				rend.gameObject.SetActive(false);
			imageToggler = !imageToggler;
			OnComplete?.Invoke();
		}));
	}
	public void ToggleAnimateLines()
	{
		if (isLinesEnabled)
		{
			LineManager.AnimateLinesClose(OnComplete: () => isLinesEnabled = false);
		}
		else
		{
			isLinesEnabled = true;
			LineManager.AnimateLinesOpen();
		}
	}

	#region Private Methods

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


	private void CreateSkybox(Transform lookAt)
	{
		// Создаем статичный скайбокс

		// Создаем экземпляр MeshCombiner для объединения мешей в один
		var meshCombiner = Instantiate(meshCombinerPrefab, transform);
		meshCombiner.name = constellationData.name + "-Stars-Static";
		
		// Пробегаем циклом по всем звездам (в т.ч. динамическим), создаем их и добавляем в MeshCombiner
		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, meshCombiner.transform);
			starDisplay.Init(starData, null, lookAt);
			meshCombiner.Add(starDisplay.MeshSchemaIndex, starDisplay.meshFilter);
		}

		// Комбинируем
		meshCombiner.CombineAllMeshes();
	}

	#endregion
}