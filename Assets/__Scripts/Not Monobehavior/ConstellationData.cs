using System.Collections.Generic;
using System;
using UnityEngine;

[Serializable]
public struct ConstellationData
{
	// Класс для хранения данных о созвездии

	public string name;
	public float ra;
	public float dec;
	public ConstellationImageSettings image;
	public List<StarConnection> pairs;
	public List<StarData> stars;

	private Vector3? worldPos;
	private Vector3? imagePos;

	public ConstellationData(string name, float ra, float dec, ConstellationImageSettings image, List<StarConnection> pairs, List<StarData> stars)
	{
		this.name = name;
		this.ra = ra;
		this.dec = dec;
		this.image = image;
		this.pairs = pairs;
		this.stars = stars;
		worldPos = null;
		imagePos = null;
	}

	public Vector3 WorldPos
	{
		// Рассчитываем значение, если его нет, иначе возращаем
		get
		{
			if (worldPos == null)
			{
				worldPos = Helper.RaDecToPosition(ra, dec);
			}
			return (Vector3)worldPos;
		}
	}

	public Vector3 ImagePos
	{
		// Рассчитываем значение, если его нет, иначе возращаем
		get
		{
			if (imagePos == null)
			{
				imagePos = Helper.RaDecToPosition(image.ra, image.dec);
			}
			return (Vector3)imagePos;
		}
	}
}

// Дополнительные классы для извлечения данных JSON
[Serializable]
public struct ConstellationImageSettings
{
	public float ra;
	public float dec;
	public float scale;
	public int angle;
}

[Serializable]
public struct StarConnection
{
	public int from;
	public int to;
}

[Serializable]
public struct JsonData
{
	public ConstellationData[] items;
}