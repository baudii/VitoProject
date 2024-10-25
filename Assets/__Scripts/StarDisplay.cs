using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;

public class StarDisplay : MonoBehaviour
{
	[SerializeField] public MeshFilter meshFilter;
	[SerializeField] private Renderer rend;
	[SerializeField] private float brightStarMagnitude;

	public StarData data;
	public List<(LineController, StarDisplay)> connections;
	public bool IsDynamic; // Участвует ли в анимациях
	public int MeshSchemaIndex; // 0 - маленькая, 1 - большая
	
	private AnimationManager lineManager;
	private Vector3 initialScale;

	public void Init(StarData starData, AnimationManager lineManager, Transform cameraTransform)
	{
		// Инициализируем звезду
		this.lineManager = lineManager;
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
	public async Task<List<StarDisplay>> AnimateAllNeighboursAsync(int animationGroup, CancellationToken token)
	{
		List<StarDisplay> returnValue = new List<StarDisplay>();
		try
		{
			int unUsedStars = connections.Count; // Переменная хранит кол-во линий, которые еще не были анимированы

			var animDuration = ConstellationManager.Instance.AnimationDuration;

			StopAllCoroutines();
			StartCoroutine(Helper.ScaleBounceAnimation(transform, 1, initialScale, 4, 0.3f)); // Анимируем звезду

			foreach (var connection in connections)
			{
				if (lineManager.UsedLines.ContainsKey(connection.Item1.GetInstanceID()))
				{
					unUsedStars--;
					continue;
				}

				var key = connection.Item1.GetInstanceID();
				lineManager.UsedLines.Add(key, connection.Item1);

				if (!lineManager.LineAnimationGroups.ContainsKey(animationGroup))
					lineManager.LineAnimationGroups[animationGroup] = new List<LineController>();
				lineManager.LineAnimationGroups[animationGroup].Add(connection.Item1);

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
			await Helper.WaitUntil(() => returnValue.Count == unUsedStars, timeout: 6000, token: token);
			token.ThrowIfCancellationRequested();
		}
		catch (OperationCanceledException _) { }
		catch { throw; }
		return returnValue;
	}
}

