using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class StarDisplay : MonoBehaviour
{
	[SerializeField] Renderer rend;
	[SerializeField] float brightStarMagnitude;
	[SerializeField] Material brightStarMaterial;

	public StarData data;
	public List<(LineController, StarDisplay)> connections;

	private ConstellationDisplay constellation;
	private bool recursionFinished;

	Vector3 initialScale;

	public void Init(StarData starData, ConstellationDisplay constDisplay, Transform cameraTransform)
	{
		// Инициализируем звезду
		constellation = constDisplay;
		connections = new List<(LineController, StarDisplay)>();
		data = starData;
		initialScale = transform.localScale;

		// Выставляем текстуру и цвет
		if (data.magnitude > brightStarMagnitude)
			rend.material = brightStarMaterial; 
		rend.material.color = starData.GetColor();

		// Выставляем трансформ
		transform.position = starData.WorldPos;
		transform.LookAt(cameraTransform);
		transform.localScale = initialScale * starData.magnitude;
	}

	public void ResetScale() => transform.localScale = initialScale * data.magnitude;

	public void AddConnection(LineController renderer, StarDisplay other)
	{
		// Каждая звезда хранит всех своих соседей и соединяющую линию
		connections.Add((renderer, other));
	}

	// Два способа реализации последовательной анимации:

	// Асинхронно - более стабильная
	public async Task<List<StarDisplay>> AnimateAllNeighboursAsync(bool stretchMode)
	{
		List<StarDisplay> returnValue = new List<StarDisplay>();
		int unUsedStars = connections.Count; // Переменная хранит кол-во линий, которые еще не были анимированы

		if (unUsedStars > 0)
		{
			StartCoroutine(Helper.ScaleBounceAnimation(transform, 1, 4)); // Анимируем звезду
		}

		foreach (var connection in connections)
		{
			if (constellation.UsedLines.ContainsKey(connection.Item1.GetInstanceID()))
			{
				unUsedStars--;
				continue;
			}

			constellation.UsedLines.Add(connection.Item1.GetInstanceID(), connection.Item1);

			// Разные анимации в зависимости от настроек инспектора
			if (stretchMode)
			{
				// Анимация растягивания
				var startStar = this;
				var endStar = connection.Item2;

				// Выставляем значения LineController
				connection.Item1.CalcPositions(startStar, endStar);
				connection.Item1.SetPosition();

				// Анимируем
				connection.Item1.StretchLine(1, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					returnValue.Add(connection.Item2);
				});
			}
			else
			{
				// Анимация прозрачности
				StartCoroutine(Helper.FadeAnimation(connection.Item1.lineRenderer, 2, true, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					returnValue.Add(connection.Item2);
				}));
			}
		}
		
		// Ждем пока все корутины не закончат свой цикл
		await Helper.WaitUntil(() => returnValue.Count == unUsedStars, 50);

		return returnValue;
	}


	// Рекурсивно - нужно еще любви
	public void ToggleConnections(bool stretchMode, Action OnComplete = null)
	{
		if (recursionFinished) 
			return;

		if (constellation.UsedLines.Count == constellation.LineRendererCount)
		{ 
			// Сделать что-то напоследок
			recursionFinished = true;
			constellation.UsedLines.Clear();
			OnComplete?.Invoke();
			return;
		}

		StartCoroutine(Helper.ScaleBounceAnimation(transform, 2, 4));
		
		// Проходим циклом по всем соседним звездам
		foreach (var connection in connections)
		{
			if (constellation.UsedLines.ContainsKey(connection.Item1.GetInstanceID())) // Если мы уже встречали соединяющую линию, то ничего не делаем
				continue;

			constellation.UsedLines.Add(connection.Item1.GetInstanceID(), connection.Item1); // Добавляем соединяющую линию

			// Разные анимации в зависимости от настроек инспектора
			if (stretchMode)
			{
				// Анимация растягивания
				var startStar = this;
				var endStar = connection.Item2;

				// Выставляем значения LineController
				connection.Item1.CalcPositions(startStar, endStar);
				connection.Item1.SetPosition();

				connection.Item1.StretchLine(1, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					connection.Item2.ToggleConnections(stretchMode, OnComplete); // Вызываем эту же функцию
				});
			}
			else
			{
				// Анимация прозрачности
				StartCoroutine(Helper.FadeAnimation(connection.Item1.lineRenderer, 2, true, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					connection.Item2.ToggleConnections(stretchMode, OnComplete); // Вызываем эту же функцию
				}));
			}
		}
	}
}

