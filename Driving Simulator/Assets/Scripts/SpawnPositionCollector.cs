using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SpawnPositionCollector : ScriptableObject
{
	public static List<Vector3> SpawnPositions = new List<Vector3>() ;


	public static void PushNewPositions(List<Vector3> positions)
	{
		if (positions.Count != 0)
		{
			foreach (Vector3 pos in positions)
			{
				if(!SpawnPositions.Contains(pos))
				SpawnPositions.Add(pos) ;
			}
		}
	}

	private static void DeletePos(Vector3 pos)
	{
		if (SpawnPositions.Contains(pos))
		{
			SpawnPositions.Remove(pos) ;
		}
	}

	public static void CalculatePositionsForSpawning()
	{
		// todo according to the map delete spawn absolete positions.
	}
}
