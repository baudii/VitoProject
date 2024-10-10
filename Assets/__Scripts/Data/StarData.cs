using UnityEngine;
using System;

[Serializable]
public struct StarData
{
	public int id;
	public float ra;
	public float dec;
	public float magnitude;
	public string color;

	Vector3? worldPos;
	public Vector3 WorldPos
	{
		get
		{
			if (worldPos == null)
			{
				worldPos = Helper.RaDecToPosition(ra, dec);
			}
			return (Vector3)worldPos;
		}
	}

	public Color GetColor()
	{
		Color newCol;

		if (ColorUtility.TryParseHtmlString("#" + color, out newCol))
		{
			return newCol;
		}

		Debug.LogError("Couldn't parse the color!");
		return Color.white;
	}
}
