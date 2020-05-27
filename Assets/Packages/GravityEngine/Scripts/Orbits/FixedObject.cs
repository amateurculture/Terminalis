using UnityEngine;
using System.Collections;

/// <summary>
/// Fixed object.
///
/// Object does not move (but it's gravity will affect others). 
///
/// Good choice for e.g. central star in a system
/// </summary>
public class FixedObject : MonoBehaviour, IFixedOrbit {

    private NBody nbody;

    private Vector3 phyPosition;

    public void Start() {
        nbody = GetComponent<NBody>();
        if (GravityEngine.Instance().units == GravityScaler.Units.DIMENSIONLESS) {
            phyPosition = transform.position;
        } else {
            phyPosition = nbody.initialPhysPosition;
        }
    }

    public bool IsOnRails() {
		return true;
	}

	public void PreEvolve(float physicalScale, float massScale) {
		// nothing to do
	}
	
	public void Evolve(double physicsTime, ref double[] r) {
        // dynamic origin shifting may move the position around
        r[0] = phyPosition.x;
        r[1] = phyPosition.y;
        r[2] = phyPosition.z;
	}


    public Vector3 GetPosition() {
        return phyPosition;
    }

    public void Move(Vector3 moveBy) {
        phyPosition += moveBy;
    }

    public void GEUpdate(GravityEngine ge) {
        // MapToScene may change things,so need to map every frame
        transform.position = ge.MapToScene(phyPosition);
    }

    public void SetNBody(NBody nbody) {
        throw new System.NotImplementedException();
    }

    public bool IsKepler() {
        return false;
    }

    public NBody GetCenterNBody() {
        return null;
    }

    public Vector3 ApplyImpulse(Vector3 impulse) {
        Debug.LogWarning("Not supported");
        return new Vector3(float.NaN, float.NaN, float.NaN);
    }

    public void UpdatePositionAndVelocity(Vector3 pos, Vector3 vel) {
        throw new System.NotImplementedException();
    }

    public string DumpInfo() {
        return "      FixedObject\n";
    }

    public void SetPositionDouble(Vector3d pos) {
        phyPosition = pos.ToVector3();
    }

    public Vector3 GetVelocity() {
        return Vector3.zero;
    }
}
