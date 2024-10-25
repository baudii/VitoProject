using UnityEngine;
using System.Collections;
using System;
using System.Threading.Tasks;
using Unity.VisualScripting.FullSerializer;
using System.Threading;
using System.Transactions;
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
	public static IEnumerator FadeAnimation(Renderer rend, float duration, float targetAlpha, bool lerpDuartion = true, Action OnComplete = null)
	{
		Color startColor = rend.material.color;
		Color currentColor = rend.material.color;

		bool isFading = startColor.a - targetAlpha > 0;

		startColor.a = isFading ? 1 : 0;
		
		float elapsedTime = 0;
		if (lerpDuartion)
		{
			// Линейно интерполируем elapsedTime относительно текущего значения color.a, чтобы продолжить анимацию с того места, где она была остановлена, не удлинняя ее по времени
			if (isFading)
			{
				elapsedTime = Mathf.Lerp(duration, 0, rend.material.color.a);
			}
			else
			{
				elapsedTime = Mathf.Lerp(0, duration, rend.material.color.a);
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
	public static IEnumerator ScaleBounceAnimation(Transform t, float duration, Vector3 initialScale, float scaleMultiplier, float coefficient = 0.5f)
	{
		Vector3 targetScale = Vector3.one * scaleMultiplier;

		// Линейно интерполируем elapsedTime по текущему скейлу звезды, чтобы продолжить анимацию
		// Умножаем на coefficient, чтобы анимация продолжилась с увеличения звезды
		float elapsedTime = coefficient * (t.localScale - initialScale).magnitude / (targetScale - initialScale).magnitude;

		float shrinked = duration * coefficient;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;

			// Сначала duration * coefficitent секунд увеличиваемcя, потом уменьшаемся
			if (elapsedTime < shrinked)
				t.localScale = Vector3.Lerp(initialScale, targetScale, elapsedTime / shrinked);
			else
				t.localScale = Vector3.Lerp(targetScale, initialScale, elapsedTime / shrinked - 1);

			yield return null;
		}

		t.localScale = initialScale;
	}

	// Эмуляция ожидания делегата при использовании асинхронных методов
	public static async Task WaitUntil(Func<bool> condition, int frequency = 25, int timeout = -1, CancellationToken token = default)
	{
		try
		{
			// Ждем пока не выполнится condition не выдаст true
			var waitTask = Task.Run(async () =>
			{
				while (!condition())
					await Task.Delay(frequency, token);

			}, token);

			var delayTask = Task.Delay(timeout, token);
			var finishedTask = await Task.WhenAny(waitTask, delayTask);
			token.ThrowIfCancellationRequested();

			if (finishedTask != waitTask)
				throw new TimeoutException();
		}
		catch (OperationCanceledException _) { }
		catch { throw; }
	}

	public static Coroutine DelayedExecute(this MonoBehaviour mono, float delay, Action action) => mono.StartCoroutine(DelayedExecute(delay, action));

	public static IEnumerator DelayedExecute(float delay, Action action)
	{
		yield return new WaitForSeconds(delay);
		action?.Invoke();
	}
}
