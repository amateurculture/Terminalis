using UnityEngine;
using System.Collections;
using OrbCreationExtensions;

public class SimpleLODDemoCamera : MonoBehaviour {

	public float moveSpeed = 0.15f;
	public float scrollSpeed = 2f;
	public float sensitivityX = 4f;
	public float sensitivityY = 4f;
	public float minimumY = -40f;
	public float maximumY = 40f;
	public float manualRotationAcceleration= 40f;

	private Vector3 startPosition;
	private Vector3 targetPosition;
	private GameObject currentScene;
	private GameObject clickedObject;
	private int totTriangles = 0;
//	private int totMeshes = 0;
	private int totSubMeshes = 0;
	private int frameCount = 0;
	private float frameTotTime = 60f;
	private float fps = 60f;
	private float displayFPS = 60f;
	private int lodLevel = -1;
	private float rotationX = 0f;
	private float rotationY = 0f;

	void Start () {
		startPosition = transform.position;
		targetPosition = startPosition;
		InvokeRepeating("GetStats", 0.1f, 1f);
	}
	
	public void SetCurrentScene(GameObject aGO) {
		string clickedObjectName = null;
		if(clickedObject != null && clickedObject != currentScene) {
			clickedObjectName = clickedObject.name;
		}
		currentScene = aGO;
		if(clickedObjectName != null) {
			clickedObject = currentScene.FindFirstChildWithName(clickedObjectName);
		} else clickedObject = aGO;
	}

	public void SetClickedObject(GameObject aGO) {
		if(aGO == null) {
			aGO = currentScene;
			targetPosition = startPosition;
		} else {
			clickedObject = aGO;
			Bounds bounds = clickedObject.GetWorldBounds();
			targetPosition = bounds.center + (transform.rotation * (Vector3.back * (bounds.extents.magnitude + 0.5f)));
		}
		GetStats();
	}

	private void GetStats() {
		Mesh[] meshes = clickedObject.GetMeshes(false);
		totSubMeshes = 0;
		totTriangles = 0;
		if(meshes != null) {
			foreach(Mesh mesh in meshes) {
				if(mesh != null) {
					totSubMeshes += mesh.subMeshCount;
					totTriangles += mesh.GetTriangleCount();
				}
			}
		}
		if(clickedObject != currentScene) {
			LODSwitcher switcher = clickedObject.GetFirstComponentInChildren<LODSwitcher>();
			if(switcher) lodLevel = switcher.GetLODLevel();
			else lodLevel = -1;
		} else lodLevel = -1;
		if(frameCount > 0) displayFPS = Mathf.Lerp(displayFPS, (frameTotTime/frameCount), 0.5f);
		frameTotTime = 0f;
		frameCount = 0;
	}

	void Update () {
	    frameTotTime += Time.timeScale/Time.deltaTime;
		frameCount++;
		fps = (fps * 0.9f) + ((1f / Time.deltaTime) * 0.1f);
		transform.position = Vector3.Lerp(transform.position, targetPosition, 3f * Time.deltaTime);

		float forward = Input.GetAxis("Vertical") * moveSpeed;
		float sideways = Input.GetAxis("Horizontal") * moveSpeed * 0.8f;
		if (Input.GetMouseButton(0) || forward != 0f || sideways != 0f) {
			rotationX += Input.GetAxis("Mouse X") * sensitivityX;
			rotationY = Mathf.Clamp(rotationY + (Input.GetAxis("Mouse Y") * sensitivityY), minimumY, maximumY);
		}
		transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.Euler(-rotationY, rotationX, 0), manualRotationAcceleration * Time.deltaTime);
		forward += Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
        targetPosition += transform.rotation * (Vector3.forward * forward);
        targetPosition += transform.rotation * (Vector3.right * sideways);
        if(targetPosition.y < -1f) targetPosition.y = -1f;
	}

	void OnGUI() {
//		string str = displayFPS.MakeString(0) + " fps\n";
		GUI.skin.label.normal.textColor = Color.black;
		string str = (clickedObject == currentScene ? "All objects" : clickedObject.name) + ":\n";
		str = str + totSubMeshes + " submeshes / materials\n";
		str = str + totTriangles + " triangles " + (lodLevel>=0 ? "(LOD "+lodLevel+")" : "");
		GUI.Label(new Rect(2,2,200,100), str);

		if(GUI.Button(new Rect(Screen.width - 97f, Screen.height - 27, 95, 25), "Reset camera")) {
			targetPosition = startPosition;
			clickedObject = currentScene;
			rotationX = 0f;
			rotationY = 0f;
		}
	}

}
