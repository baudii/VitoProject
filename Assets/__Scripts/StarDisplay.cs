using UnityEngine;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class StarDisplay : MonoBehaviour
{
	[SerializeField] Renderer rend;
	[SerializeField] float brightStarMagnitude;
	[SerializeField] Material brightStarMaterial;

	public StarData data;
	public List<(LineController, StarDisplay)> connections;

	private ConstellationDisplay constellation;
	private bool recursionFinished;

	Vector3 initialScale;

	public void Init(StarData starData, ConstellationDisplay constDisplay, Transform cameraTransform)
	{
		// �������������� ������
		constellation = constDisplay;
		connections = new List<(LineController, StarDisplay)>();
		data = starData;
		initialScale = transform.localScale;

		// ���������� �������� � ����
		if (data.magnitude > brightStarMagnitude)
			rend.material = brightStarMaterial; 
		rend.material.color = starData.GetColor();

		// ���������� ���������
		transform.position = starData.WorldPos;
		transform.LookAt(cameraTransform);
		transform.localScale = initialScale * starData.magnitude;
	}

	public void ResetScale() => transform.localScale = initialScale * data.magnitude;

	public void AddConnection(LineController renderer, StarDisplay other)
	{
		// ������ ������ ������ ���� ����� ������� � ����������� �����
		connections.Add((renderer, other));
	}

	// ��� ������� ���������� ���������������� ��������:

	// ���������� - ����� ����������
	public async Task<List<StarDisplay>> AnimateAllNeighboursAsync(bool stretchMode)
	{
		List<StarDisplay> returnValue = new List<StarDisplay>();
		int unUsedStars = connections.Count; // ���������� ������ ���-�� �����, ������� ��� �� ���� �����������

		if (unUsedStars > 0)
		{
			StartCoroutine(Helper.ScaleBounceAnimation(transform, 1, 4)); // ��������� ������
		}

		foreach (var connection in connections)
		{
			if (constellation.UsedLines.ContainsKey(connection.Item1.GetInstanceID()))
			{
				unUsedStars--;
				continue;
			}

			constellation.UsedLines.Add(connection.Item1.GetInstanceID(), connection.Item1);

			// ������ �������� � ����������� �� �������� ����������
			if (stretchMode)
			{
				// �������� ������������
				var startStar = this;
				var endStar = connection.Item2;

				// ���������� �������� LineController
				connection.Item1.CalcPositions(startStar, endStar);
				connection.Item1.SetPosition();

				// ���������
				connection.Item1.StretchLine(1, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					returnValue.Add(connection.Item2);
				});
			}
			else
			{
				// �������� ������������
				StartCoroutine(Helper.FadeAnimation(connection.Item1.lineRenderer, 2, true, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					returnValue.Add(connection.Item2);
				}));
			}
		}
		
		// ���� ���� ��� �������� �� �������� ���� ����
		await Helper.WaitUntil(() => returnValue.Count == unUsedStars, 50);

		return returnValue;
	}


	// ���������� - ����� ��� �����
	public void ToggleConnections(bool stretchMode, Action OnComplete = null)
	{
		if (recursionFinished) 
			return;

		if (constellation.UsedLines.Count == constellation.LineRendererCount)
		{ 
			// ������� ���-�� ����������
			recursionFinished = true;
			constellation.UsedLines.Clear();
			OnComplete?.Invoke();
			return;
		}

		StartCoroutine(Helper.ScaleBounceAnimation(transform, 2, 4));
		
		// �������� ������ �� ���� �������� �������
		foreach (var connection in connections)
		{
			if (constellation.UsedLines.ContainsKey(connection.Item1.GetInstanceID())) // ���� �� ��� ��������� ����������� �����, �� ������ �� ������
				continue;

			constellation.UsedLines.Add(connection.Item1.GetInstanceID(), connection.Item1); // ��������� ����������� �����

			// ������ �������� � ����������� �� �������� ����������
			if (stretchMode)
			{
				// �������� ������������
				var startStar = this;
				var endStar = connection.Item2;

				// ���������� �������� LineController
				connection.Item1.CalcPositions(startStar, endStar);
				connection.Item1.SetPosition();

				connection.Item1.StretchLine(1, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					connection.Item2.ToggleConnections(stretchMode, OnComplete); // �������� ��� �� �������
				});
			}
			else
			{
				// �������� ������������
				StartCoroutine(Helper.FadeAnimation(connection.Item1.lineRenderer, 2, true, OnComplete: () => {
					connection.Item1.IsEnabled = true;
					connection.Item2.ToggleConnections(stretchMode, OnComplete); // �������� ��� �� �������
				}));
			}
		}
	}
}

