using UnityEngine;
using System.Collections;
using System;
public static class Helper
{
	public static float radius = 400;
	public static Vector3 RaDecToPosition(float ra, float dec)
	{
		// 360 / 24 = 15 - коэффициент перевода из часов в градусы
		float raRad = ra * 15 * Mathf.Deg2Rad ; 
		float decRad = dec * Mathf.Deg2Rad;

		float x = radius * Mathf.Cos(decRad) * Mathf.Cos(raRad);
		float y = radius * Mathf.Sin(decRad);
		float z = radius * Mathf.Cos(decRad) * Mathf.Sin(raRad);

		return new Vector3(x, y, z);
	}

	public static IEnumerator FadeAnimation(Renderer rend, float duration, bool startFromTransparent = true, float delay = 0)
	{
		if (duration == 0)
			throw new ArgumentException("Duration can't be zero");

		yield return null; 
		Color startColor = rend.material.color;
		startColor.a = 0;
		Func<float, float> calcAlpha = (elapsedTime) => (elapsedTime / duration);
		if (!startFromTransparent)
		{
			startColor.a = 1;
			calcAlpha = (elapsedTime) => (1 - elapsedTime / duration);
		}
		rend.material.color = startColor;

		yield return new WaitForSeconds(delay);

		float elapsedTime = 0;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			float alpha = Mathf.Clamp01(calcAlpha(elapsedTime));
			startColor.a = alpha;
			rend.material.color = startColor;
			yield return null;
		}
	}
}
