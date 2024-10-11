using UnityEngine;
using System;

[Serializable]
public struct StarData
{
	//  ласс дл€ хранени€ данных о звезде

	public int id;
	public float ra;
	public float dec;
	public float magnitude;
	public string color;


	Vector3? worldPos;

	public StarData(int id, float ra, float dec, float magnitude, string color)
	{
		this.id = id;
		this.ra = ra;
		this.dec = dec;
		this.magnitude = magnitude;
		this.color = color;
		worldPos = null;
	}

	public Vector3 WorldPos
	{
		// –ассчитываем значение, если его нет, иначе возращаем
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
