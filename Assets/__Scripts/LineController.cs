using UnityEngine;
using System.Collections;
using System;

public class LineController : MonoBehaviour
{
	// ����� ��� ���������� ���������� ������ (� �������� �� ����� ��������)

	[SerializeField] 
	public LineRenderer lineRenderer;

	public bool IsEnabled;

	private Vector3 from, to;
	private bool isStretchAnimMode;
	private float lineStarOffset;

	public void Init(bool isStretchMode, float lineStarOffset)
	{
		// �������������
		var lineColor = lineRenderer.material.color;
		isStretchAnimMode = isStretchMode;
		lineColor.a = isStretchMode ? 1 : 0;
		lineRenderer.material.color = lineColor;
		lineRenderer.startColor = lineColor;
		lineRenderer.endColor = lineColor;
		this.lineStarOffset = lineStarOffset;
	}

	public void CalcPositions(StarDisplay fromStarDisplay, StarDisplay toStarDisplay) 
	{
		// ������������ ������� �� � �� ��������� �����
		var fromPos = fromStarDisplay.data.WorldPos;
		var toPos = toStarDisplay.data.WorldPos;
		var dir = (toPos - fromPos);
		var lso = lineStarOffset;

		if (dir.magnitude < lineStarOffset)
			lso = 1;

		from = fromPos + dir.normalized * lso;
		to = toPos - dir.normalized * lso;
	}

	public void RevertTarget()
	{
		// ������ ������� (��� �������)
		var tmp = from;
		from = to;
		to = tmp;
	}

	public void SetPosition()
	{
		// ���������� ������������ � CalcPositions �������� � ������ lineRenderer
		lineRenderer.SetPosition(0, from);

		if (isStretchAnimMode)
			lineRenderer.SetPosition(1, from); 
		else
			lineRenderer.SetPosition(1, to);
	}

	public void StretchLine(float duration, Action OnComplete = null) => StartCoroutine(StretchLineCoroutine(duration, OnComplete));

	private IEnumerator StretchLineCoroutine(float duration, Action OnComplete = null)
	{
		// �������� ��� �������� ������������

		if (lineRenderer.positionCount != 2)
			yield break;

		Vector3 currentPos;

		float elapsedTime = 0;
		while (elapsedTime < duration)
		{
			elapsedTime += Time.deltaTime;
			currentPos = Vector3.Lerp(from, to, elapsedTime / duration);
			lineRenderer.SetPosition(1, currentPos);
			yield return null;
		}

		OnComplete?.Invoke();
	}
}
