using System.Collections;
using System.Collections.Generic;
using UnityEngine;


[ExecuteInEditMode]
[RequireComponent(typeof(Camera))]
[AddComponentMenu("Image Effects/VHS Effect")]
public class VHS_Effect : MonoBehaviour {
	public Material _VHS_Material;
	public Texture _VHSNoise;

	[Range (0.995f, 0f)]
	public float _textureIntensity = 0.713f;

	[Range (0f, 1f)]
	public float _verticalOffset = 0.076f;
	[Range (0.005f, 0.1f)]
	public float offsetColor = 0.0108f;

	[Range (1500f, 1f)]
	public float _OffsetDistortion = 1210f;

	[Header("Scan")]
	public bool _scan;
	 
	 	
	[Range(1f,10f)]
	public float _adjustLines = 2f;
	public Color _scanLinesColor;
	

void Start(){

_VHS_Material.SetFloat("_OffsetPosY", _verticalOffset);

}



	public void OnRenderImage(RenderTexture source, RenderTexture destination)
	{
		if(_scan){
			
			_VHS_Material.shader = Shader.Find("Hidden/VHSwithLines");
			_VHS_Material.SetColor ("_ScanLinesColor", _scanLinesColor);
			_VHS_Material.SetFloat ("_ScanLines", _adjustLines);
			

		}else{
			_VHS_Material.shader = Shader.Find("Hidden/VHS");
		}

		_VHS_Material.SetFloat("_OffsetDistortion", _OffsetDistortion);
		_VHS_Material.SetFloat("_OffsetColor", offsetColor);
		_VHS_Material.SetFloat("_OffsetNoiseX", Random.Range(0f, 0.6f));
		_VHS_Material.SetTexture ("_SecondaryTex", _VHSNoise);
		float offsetNoise = _VHS_Material.GetFloat("_OffsetNoiseY");
		_VHS_Material.SetFloat("_OffsetNoiseY", offsetNoise + Random.Range(-0.03f, 0.03f));
		_VHS_Material.SetFloat ("_Intensity", _textureIntensity);
		
		if(_verticalOffset == 0.0f)
		{
			_VHS_Material.SetFloat("_OffsetPosY", _verticalOffset);
		}
		if(_verticalOffset > 0.0f)
		{
			_VHS_Material.SetFloat("_OffsetPosY", _verticalOffset - Random.Range(0f, _verticalOffset));
		}
		else if (_verticalOffset < 0.0f)
		{
			_VHS_Material.SetFloat("_OffsetPosY", _verticalOffset + Random.Range(0f, -_verticalOffset));
		}
		else if (Random.Range(0, 150) == 1)
		{
			_VHS_Material.SetFloat("_OffsetPosY", Random.Range(-0.5f, 0.5f));
		}

		offsetColor = _VHS_Material.GetFloat("_OffsetColor");

		Graphics.Blit(source, destination, _VHS_Material);
	}
}