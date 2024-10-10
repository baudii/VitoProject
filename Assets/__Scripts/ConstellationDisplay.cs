using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ConstellationDisplay : MonoBehaviour
{
	[SerializeField] StarDisplay starPrefab;
	[SerializeField] Transform starsParent;
	[SerializeField] Transform GFX;
	[SerializeField] Renderer rend;
	[SerializeField] TextAsset JsonAsset;

	private ConstellationData constellationData;

	private List<StarDisplay> stars = new List<StarDisplay>();
	private List<LineRenderer> lines = new List<LineRenderer>();

	void Awake()
	{
		stars = new List<StarDisplay>(); 
		lines = new List<LineRenderer>();
		constellationData = JsonUtility.FromJson<JsonData>(JsonAsset.text).items[0];
	}
	public void Init(float lineStarOffset)
	{
		var cam = Camera.main;

		transform.position = constellationData.WorldPos;
		GFX.LookAt(cam.transform);
		cam.transform.LookAt(GFX);
		GFX.transform.position = constellationData.ImagePos;
		GFX.transform.eulerAngles -= new Vector3(0, 0, constellationData.image.angle);
		GFX.transform.localScale *= constellationData.image.scale;
		transform.name = constellationData.name;

		StartCoroutine(Helper.FadeAnimation(rend, 5, delay: 2));

		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, starsParent.transform);
			starDisplay.Init(starData, constellationData, cam.transform);
			stars.Add(starDisplay);
		}


		foreach (var connection in constellationData.pairs)
		{
			CreateLines(stars.First(star => star.data.id == connection.from).data.WorldPos, 
				stars.First(star => star.data.id == connection.to).data.WorldPos, 
				lineStarOffset);
		}
	}

	public void UpdateLines(float width)
	{
		if (lines == null)
			return;

		foreach (var line in lines)
		{
			line.startWidth = width;
			line.endWidth = width;
		}
	}

	private void CreateLines(Vector3 startPos, Vector3 endPos, float lineStarOffset)
	{
		GameObject lineObj = new GameObject("Line");
		LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

		var dir = (endPos - startPos);
		if (dir.magnitude < lineStarOffset)
			lineStarOffset = 1;

		lineRenderer.positionCount = 2;
		lineRenderer.SetPosition(0, startPos + dir.normalized * lineStarOffset);
		lineRenderer.SetPosition(1, endPos - dir.normalized * lineStarOffset);
		lineRenderer.startWidth = 1;
		lineRenderer.endWidth = 1;
		lineRenderer.material = new Material(Shader.Find("Unlit/Color"));
		lineRenderer.material.color = Color.white;

		lineObj.transform.SetParent(transform);
		lines.Add(lineRenderer);
	}
}