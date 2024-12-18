﻿using System;
using UnityEngine;

public class ConstellationManager : Singleton<ConstellationManager>
{
	[SerializeField, Range(0.01f, 10)] private float lineStarOffset;
	[SerializeField, Range(0.01f, 10)] private float lineWidth;
	[SerializeField, Range(0.01f,  5)] private float animationDuration;
	[SerializeField] private ConstellationDisplay constallationPrefab;
	[SerializeField] private ConstellationInstanceInfo[] constallationInstanceInfo;
	[SerializeField] private bool OnStartAnimation;
	private ConstellationDisplay[] constellations;

	public float LineStarOffset => lineStarOffset;
	public float LineWidth => lineWidth;
	public float AnimationDuration => animationDuration;

	private void Start()
	{
		constellations = new ConstellationDisplay[constallationInstanceInfo.Length];
		for (int i = 0; i < constallationInstanceInfo.Length; i++)
		{
			var con = Instantiate(constallationPrefab, transform);
			con.Init(constallationInstanceInfo[i]);
			constellations[i] = con;
		}
	}

	private void Update()
	{

#if UNITY_EDITOR
		// Обновляем ширину линий в рантайме
		foreach (var con in constellations)
		{
		//	con.LineManager.UpdateLinesWidth();
		}
#endif

		if (Input.GetKeyDown(KeyCode.Alpha1))
		{
			AnimateImage(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha2))
		{
			AnimateLines(0);
		}
		if (Input.GetKeyDown(KeyCode.Alpha3))
		{
			AnimateImage(1);
		}
		if (Input.GetKeyDown(KeyCode.Alpha4))
		{
			AnimateLines(1);
		}

	}

	public void AnimateImage(int i)
	{
		constellations[i].ToggleAnimateImage();
	}
	public void AnimateLines(int i)
	{
		constellations[i].ToggleAnimateLines();
	}

}
