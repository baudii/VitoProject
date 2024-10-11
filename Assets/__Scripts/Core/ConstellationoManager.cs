	using UnityEngine;

public class ConstellationoManager : MonoBehaviour
{
	[SerializeField, Range(0, 30)] float lineStarOffset;
	[SerializeField, Range(0,10)] float lineWidth;
	[SerializeField] ConstellationDisplay constallationPrefab;
	[SerializeField] ConstellationDisplay[] constellations;
	[SerializeField] bool OnStartAnimation;


	private void Update()
	{

#if UNITY_EDITOR
		foreach (var con in constellations)
		{
			con.UpdateLinesWidth(lineWidth);
		}
#endif

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			AnimateImage(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			AnimateImage(1);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			AnimateLines(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			AnimateLines(1);
		}
		if (Input.GetKeyDown(KeyCode.Alpha5))
		{
			RevertAnimation(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha6))
		{
			RevertAnimation(1);
		}
	}


	private void Start()
	{
		foreach (var con in constellations)
		{
			con.Init(lineStarOffset);
			con.AnimateLines(() =>
			{
				con.AnimateImage();
			});
		}
	}

	public void AnimateImage(int i)
	{
		constellations[i].AnimateImage();
	}
	public void AnimateLines(int i)
	{
		constellations[i].AnimateLines();
	}
	public void RevertAnimation(int i)
	{
		constellations[i].RevertAnimation();
	}
}