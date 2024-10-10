using UnityEngine;

public class ConstellationoManager : MonoBehaviour
{
	[SerializeField, Range(0, 30)] float lineStarOffset;
	[SerializeField, Range(0,10)] float lineWidth;
	[SerializeField] ConstellationDisplay constallationPrefab;
	[SerializeField] ConstellationDisplay[] constellations;

#if UNITY_EDITOR
	[SerializeField, Tooltip("Check this to tune thing on runtime")] bool runtimeUpdate;


	private void Update()
	{
		if (!runtimeUpdate)
			return;

		foreach (var con in constellations)
		{
			con.UpdateLines(lineWidth, lineStarOffset);
		}
	}

#endif

	private void Start()
	{
		foreach (var con in constellations)
		{
			con.Init(transform, lineStarOffset);
		}
	}
}