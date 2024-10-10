using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class ConstellationDisplay : MonoBehaviour
{
	[SerializeField] StarDisplay starPrefab;
	[SerializeField] Transform GFX;
	[SerializeField] Renderer rend;
	[SerializeField] TextAsset JsonAsset;

	private ConstellationData constellationData;

	private List<StarDisplay> stars = new List<StarDisplay>();
	private List<LineRenderer> lines = new List<LineRenderer>();
	float lineStarOffset;

	void Awake()
	{
		stars = new List<StarDisplay>(); 
		lines = new List<LineRenderer>();
		constellationData = JsonUtility.FromJson<JsonData>(JsonAsset.text).items[0];
	}
	public void Init(Transform mainParent, float lineStarOffset)
	{
		var cam = Camera.main;

		this.lineStarOffset = lineStarOffset;
		transform.position = constellationData.WorldPos;
		GFX.LookAt(cam.transform);
		cam.transform.LookAt(GFX);
		GFX.transform.position = constellationData.ImagePos;
		GFX.transform.eulerAngles -= new Vector3(0, 0, constellationData.image.angle);
		GFX.transform.localScale *= constellationData.image.scale;
		transform.name = constellationData.name;

		StartCoroutine(Helper.FadeAnimation(rend, 5, delay: 2));

		var starsParent = new GameObject("Stars");
		starsParent.transform.parent = mainParent;

		foreach (var starData in constellationData.stars)
		{
			StarDisplay starDisplay = Instantiate(starPrefab, starsParent.transform);
			starDisplay.Init(starData, constellationData, cam.transform);
			stars.Add(starDisplay);
		}


		foreach (var connection in constellationData.pairs)
		{
			CreateLines(stars.First(star => star.data.id == connection.from).data.WorldPos, stars.First(star => star.data.id == connection.to).data.WorldPos);
		}
	}

	public void UpdateLines(float width, float lineStarOffset)
	{
		if (lines == null)
			return;

		foreach (var line in lines)
		{
			line.startWidth = width;
			line.endWidth = width;

			var posfrom = line.GetPosition(0);
			var posto = line.GetPosition(1);

			var dir = (posto - posfrom).normalized;

			line.SetPosition(0, posfrom + dir * lineStarOffset);
		}
	}

	private void CreateLines(Vector3 startPos, Vector3 endPos)
	{
		GameObject lineObj = new GameObject("Line");
		LineRenderer lineRenderer = lineObj.AddComponent<LineRenderer>();

		var dir = (endPos - startPos);
		

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