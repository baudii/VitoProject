using UnityEngine;
using System.Collections;
using System;

public class LineController : MonoBehaviour
{
	// Класс для управление поведением линией (в основной по части анимаций)
	[SerializeField] 
	public LineRenderer lineRenderer;

	public bool IsEnabled;
	public float Length;
	private Vector3 from, to;

	public void Init()
	{
		// Иницилазиация
		var lineColor = lineRenderer.material.color;
		lineRenderer.material.color = lineColor;
		lineRenderer.startColor = lineColor;
		lineRenderer.endColor = lineColor;
	}

	public void CalcPositions(StarDisplay fromStarDisplay, StarDisplay toStarDisplay) 
	{
		// Рассчитываем позиции от и до указанных звезд
		var fromPos = fromStarDisplay.data.WorldPos;
		var toPos = toStarDisplay.data.WorldPos;
		var dir = (toPos - fromPos);
		var lso = ConstellationManager.Instance.LineStarOffset;

		if (dir.magnitude < lso)
			lso = 1;

		from = fromPos + dir.normalized * lso;
		to = toPos - dir.normalized * lso;

		Length = (toPos - fromPos).magnitude;
	}

	public void RevertTarget()
	{
		// Меняем местами (для анимации реверта)
		var tmp = from;
		from = to;
		to = tmp;
	}

	public void SetPosition()
	{
		// Выставляем начальные значения для линий
		lineRenderer.SetPosition(0, from);
		lineRenderer.SetPosition(1, from);
	}

	public void StretchLine(float duration, Action OnComplete = null) => StartCoroutine(StretchLineCoroutine(duration, OnComplete));

	private IEnumerator StretchLineCoroutine(float duration, Action OnComplete = null)
	{
		// Корутина для анимации растягивания
		if (lineRenderer.positionCount != 2)
			yield break;

		Vector3 currentPos;

		float elapsedTime = 0;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			currentPos = Vector3.Lerp(from, to, elapsedTime / duration);
			lineRenderer.SetPosition(1, currentPos);
			yield return null;
		}

		OnComplete?.Invoke();
	}
}
