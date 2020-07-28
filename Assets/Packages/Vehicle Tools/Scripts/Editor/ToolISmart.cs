using UnityEngine;
using UnityEditor;

class ToolISmart : EditorWindow
{
	bool CloseAfterCreateCar = false;
	int m_AxlesCount = 2;
	float m_Mass = 1000;
	float m_AxleStep = 2;
	float m_AxleWidth = 2;
	float m_AxleShift = -0.5f;
	public GameObject obj = null;

	[MenuItem ("Vit Labs/ISMART/Create Car")]
	public static void  ShowWindow ()
    {
		GetWindow(typeof(ToolISmart));
	}

	void OnGUI ()
    {
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		obj = (GameObject)EditorGUI.ObjectField(new Rect(3, 3, position.width - 6, 20), "Select 3D Model", obj, typeof(GameObject));
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.LabelField (" Select only 3D model file");
		EditorGUILayout.Space();
		EditorGUILayout.Space();

		m_AxlesCount = EditorGUILayout.IntSlider ("Axles: ", m_AxlesCount, 2, 10);
		m_Mass = EditorGUILayout.FloatField ("Mass: ", m_Mass);
		m_AxleStep = EditorGUILayout.FloatField ("Axle step: ", m_AxleStep);
		m_AxleWidth = EditorGUILayout.FloatField ("Axle width: ", m_AxleWidth);
		m_AxleShift = EditorGUILayout.FloatField ("Axle shift: ", m_AxleShift);
		EditorGUILayout.Space();

		CloseAfterCreateCar = EditorGUILayout.Toggle("Close After Create", CloseAfterCreateCar);
		EditorGUILayout.Space();


		if (GUILayout.Button("Create Car")) 
        {
			CreateCar ();
		}
		EditorGUILayout.Space();
		EditorGUILayout.Space();
		EditorGUILayout.Space();

		if (GUILayout.Button("Close")) 
		{
			this.Close();
		}


	}

	void CreateCar()
	{
		if(obj !=null)
		{	
		var root = new GameObject (obj.name);
		var rootBody = root.AddComponent<Rigidbody> ();
		rootBody.mass = m_Mass;
		



		var body = Instantiate (obj.gameObject);
		body.transform.parent = root.transform;

		float length = (m_AxlesCount - 1) * m_AxleStep;
		float firstOffset = length * 0.5f;

		body.transform.localScale = new Vector3(1, 1, 1);


		body.AddComponent<BoxCollider> ();
		//body.AddComponent<MeshCollider> ();



		for (int i = 0; i < m_AxlesCount; ++i) 
		{
			var leftWheel = new GameObject (string.Format("a{0}l", i));
			var rightWheel = new GameObject (string.Format("a{0}r", i));

			leftWheel.AddComponent<WheelCollider> ();
			rightWheel.AddComponent<WheelCollider> ();

			leftWheel.transform.parent = root.transform;
			rightWheel.transform.parent = root.transform;

            leftWheel.transform.localPosition = new Vector3(-m_AxleWidth * 0.5f, m_AxleShift, firstOffset - m_AxleStep * i);
            rightWheel.transform.localPosition = new Vector3(m_AxleWidth * 0.5f, m_AxleShift, firstOffset - m_AxleStep * i);
		}

		root.AddComponent<EasySuspension>();
		root.AddComponent<ISMART>();

			if(CloseAfterCreateCar == true)
				this.Close();

	    }


	}
}