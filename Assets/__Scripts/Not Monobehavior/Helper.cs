using UnityEngine;
using System.Collections;
using System;
using System.Threading.Tasks;
public static class Helper
{
	// Статический класс для максимально общих функций

	
	// Перевод из координат Ra/Dec в декартовы
	public static Vector3 RaDecToPosition(float ra, float dec)
	{
		float radius = 400;
		// 360 / 24 = 15 - коэффициент перевода из часов в градусы
		float raRad = ra * 15 * Mathf.Deg2Rad; 
		float decRad = dec * Mathf.Deg2Rad;

		float x = radius * Mathf.Cos(decRad) * Mathf.Cos(raRad);
		float y = radius * Mathf.Sin(decRad);
		float z = radius * Mathf.Cos(decRad) * Mathf.Sin(raRad);

		return new Vector3(x, y, z);
	}

	// Анимация типа Fade
	public static IEnumerator FadeAnimation(Renderer rend, float duration, bool toAppear = true, bool lerpDuartion = true, Action OnComplete = null)
	{
		if (duration == 0)
			throw new ArgumentException("Duration can't be zero"); // Потому что мы делим на duration
		
		Color startColor = rend.material.color;
		Color currentColor = rend.material.color;

		startColor.a = toAppear ? 0 : 1;
		float targetAlpha = toAppear ? 1 : 0;
		
		float elapsedTime = 0;
		if (lerpDuartion)
		{
			// Линейно интерполируем elapsedTime относительно текущего значения color.a, чтобы продолжить анимацию с того места, где она была остановлена, не удлинняя ее по времени
			if (toAppear)
			{
				elapsedTime = Mathf.Lerp(0, duration, rend.material.color.a);
			}
			else
			{
				elapsedTime = Mathf.Lerp(duration, 0, rend.material.color.a);
			}
		}

		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			currentColor.a = Mathf.Clamp01(Mathf.Lerp(startColor.a, targetAlpha, elapsedTime / duration));
			rend.material.color = currentColor;
			yield return null;
		}

		currentColor.a = targetAlpha;
		rend.material.color = currentColor;
		OnComplete?.Invoke();
	}

	// Анимация увеличения параметра scale
	// coefficient - число в (0,1), такое, что мы увеличиваемся duration * coefficient времени и уменьшаемся остальное время
	public static IEnumerator ScaleBounceAnimation(Transform t, float duration, float scaleMultiplier, float coefficient = 0.5f)
	{
		if (duration == 0)
			throw new ArgumentException("Duration can't be zero");

		Vector3 initialScale = t.localScale;
		Vector3 targetScale = initialScale * scaleMultiplier;
		float elapsedTime = 0;
		float shrinked = duration * coefficient;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			if (elapsedTime < shrinked) // первую половину времени увеличиваемя, потом уменьшаемся. Если нужно поменять коэффциент, то надо менять и двойку ниже
			{
				t.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / shrinked); // перенёс 1/2 из знаменателя
			}
			else
			{
				t.localScale = Vector3.Lerp(targetScale, initialScale, elapsedTime / shrinked - 1);
			}
			yield return null;
		}

		t.localScale = initialScale;
	}

	// Эмуляция ожидания делегата при использовании асинхронных методов
	public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1)
	{
		// Ждем пока не выполнится condition не выдаст true
		var waitTask = Task.Run(async () =>
		{
			while (!condition()) await Task.Delay(frequency);
		});

		if (waitTask != await Task.WhenAny(waitTask, Task.Delay(timeout)))
			throw new TimeoutException();
	}
}
