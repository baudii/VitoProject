using System.Collections.Generic;
using System;
using UnityEngine;
using System.Linq;

[Serializable]
public struct ConstellationData
{
	public string name;
	public float ra;
	public float dec;
	public ConstellationImageSettings image;
	public List<StarConnection> pairs;
	public List<StarData> stars;

	Vector3? worldPos;
	Vector3? imagePos;
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

	public Vector3 ImagePos
	{
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

public struct JsonData
{
	public ConstellationData[] items;
}