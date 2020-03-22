using UnityEngine;
using System.Collections;

public interface IOrbitPositions  {

	Vector3[] OrbitPositions(int numPoints, Vector3 centerPos, bool doSceneMapping);
}
