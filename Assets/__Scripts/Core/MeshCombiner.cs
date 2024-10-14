using UnityEngine;
using System.Collections.Generic;
using System;

public class MeshCombiner : MonoBehaviour
{
	[SerializeField] MeshSchema[] meshSchemas;

	public void Add(int i, MeshFilter filter)
	{
		meshSchemas[i].filters.Add(filter);
	}

	public void AddRange(int i, MeshFilter[] filters)
	{
		meshSchemas[i].filters.AddRange(filters);
	}

	public void CombineAllMeshes(Transform parent = null)
	{
		for (int i = 0; i < meshSchemas.Length; i++)
			CombineMeshes(i, parent);
	}

	public void CombineMeshes(int i, Transform parent = null)
	{
		CombineMeshes(meshSchemas[i].material, meshSchemas[i].filters, meshSchemas[i].name, parent);
	}

	private void CombineMeshes(Material material, List<MeshFilter> filters, string name, Transform parent)
	{
		CombineInstance[] combineInstance = new CombineInstance[filters.Count];

		for (int i = 0; i < filters.Count; i++)
		{
			combineInstance[i].mesh = filters[i].sharedMesh;
			combineInstance[i].transform = filters[i].transform.localToWorldMatrix;
			Destroy(filters[i].transform.parent.gameObject);
		}

		// Присваиваем новый меш главному объекту
		if (parent == null)
			parent = transform;
		var mainParent = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
		mainParent.transform.SetParent(parent);
		MeshFilter filter = mainParent.GetComponent<MeshFilter>();
		filter.mesh = new Mesh();
		filter.mesh.CombineMeshes(combineInstance);
		mainParent.GetComponent<MeshRenderer>().material = material;
	}
}

[Serializable]
public class MeshSchema
{
	public string name;
	public Material material;
	[HideInInspector]
	public List<MeshFilter> filters;
}