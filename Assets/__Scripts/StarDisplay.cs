using UnityEngine;
using System.Collections;
using System.Linq;

public class StarDisplay : MonoBehaviour
{
	[SerializeField] Renderer rend;
	public StarData data;

	void SetNeighbours(ConstellationData constellationData)
	{
		var thisPairs = constellationData.pairs.Where(pair => pair.from == data.id).ToList();
		//var stars = constellationData.stars.Where(star => thisPairs.Contains())
	}
	public void Init(StarData starData, ConstellationData constellationData, Transform cameraTransform)
	{
		data = starData;

		transform.position = starData.WorldPos;
		transform.LookAt(cameraTransform);
		transform.localScale *= starData.magnitude;

		rend.material.color = starData.GetColor();
	}


	public IEnumerator FadeInStar(GameObject starObject, float duration)
	{
		Color startColor = rend.material.color;
		startColor.a = 0;
		rend.material.color = startColor;

		float elapsedTime = 0;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			float alpha = Mathf.Clamp01(elapsedTime / duration);
			startColor.a = alpha;
			rend.material.color = startColor;
			yield return null;
		}
	}

	public void AnimateNeighbours()
	{
		
	}
}
