using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public class StarDisplay : MonoBehaviour
{
	[SerializeField] public MeshFilter meshFilter;
	[SerializeField] private Renderer rend;
	[SerializeField] private float brightStarMagnitude;

	public StarData data;
	public List<(LineController, StarDisplay)> connections;
	public bool IsDynamic; // Участвует ли в анимациях
	public int MeshSchemaIndex; // 0 - маленькая, 1 - большая
	
	private ConstellationDisplay constellation;
	private Vector3 initialScale;

	public void Init(StarData starData, ConstellationDisplay constDisplay, Transform cameraTransform)
	{
		// Инициализируем звезду
		constellation = constDisplay;
		connections = new List<(LineController, StarDisplay)>();
		data = starData;

		// Выставляем текстуру и цвет
		MeshSchemaIndex = 0;
		if (data.magnitude > brightStarMagnitude)
			MeshSchemaIndex = 1; 
		rend.material.color = starData.GetColor();

		// Выставляем трансформ
		initialScale = transform.localScale * starData.magnitude;
		transform.position = starData.WorldPos;
		transform.LookAt(cameraTransform);
		transform.localScale = initialScale;
	}

	public void ResetScale() => transform.localScale = initialScale;

	public void AddConnection(LineController renderer, StarDisplay other)
	{
		// Каждая звезда хранит всех своих соседей и соединяющую линию
		connections.Add((renderer, other));
	}


	// Два способа реализации последовательной анимации:

	// Асинхронно - более стабильная
	public async Task<List<StarDisplay>> AnimateAllNeighboursAsync(int i)
	{
		List<StarDisplay> returnValue = new List<StarDisplay>();
		int unUsedStars = connections.Count; // Переменная хранит кол-во линий, которые еще не были анимированы

		var animDuration = ConstellationManager.Instance.AnimationDuration;

		StartCoroutine(Helper.ScaleBounceAnimation(transform, 1, initialScale, 4, 0.3f)); // Анимируем звезду

		foreach (var connection in connections)
		{
			if (constellation.UsedLines.ContainsKey(connection.Item1.GetInstanceID()))
			{
				unUsedStars--;
				continue;
			}

			var key = connection.Item1.GetInstanceID();
			constellation.UsedLines.Add(key, connection.Item1);

			if (!constellation.LineAnimationGroups.ContainsKey(i))
				constellation.LineAnimationGroups[i] = new List<LineController>();
			constellation.LineAnimationGroups[i].Add(connection.Item1);

			// Анимация растягивания
			var startStar = this;
			var endStar = connection.Item2;

			// Выставляем значения LineController
			connection.Item1.CalcPositions(startStar, endStar);
			connection.Item1.SetPosition();

			// Анимируем
			connection.Item1.StretchLine(animDuration, OnComplete: () => {
				connection.Item1.IsEnabled = true;
				returnValue.Add(connection.Item2);
			});
		}
		
		// Ждем пока все корутины не закончат свой цикл
		await Helper.WaitUntil(() => returnValue.Count == unUsedStars);

		return returnValue;
	}
}

