using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class VHSCanvasControl : MonoBehaviour {
public VHS_Effect _vhs;
	// Use this for initialization
	void Start () {
		
	}
	
	// Update is called once per frame
	void Update () {
		
	}

	public void NoiseTexture(float f){

_vhs._textureIntensity = f;

	}


	public void VerticalOffset(float f){

_vhs._verticalOffset = f;

	}

		public void OffsetColor(float f){

_vhs.offsetColor = f;

	}

			public void OffsetDistortion(float f){

_vhs._OffsetDistortion = f;

	}
}
