﻿using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif
[ExecuteInEditMode]
public class PixelArt : MonoBehaviour
{
	//public static readonly int MAX_NUM_COLORS = 8;
	public int pixelScale = 1;
	public bool isOrthographic = false;
	public float shaderOutlineWidth = 1;
	public int horizontalResolution = 160;
	public int verticalResolution = 200;
	public bool setManually = false;
	public static Texture2D IndexLUT;
	public Shader BufferShader; 
	public LayerMask BufferLayer;
	public static int BufferAA = 2;
	public Texture2D defaultLUT;
	public Texture2D ditherTex; 
	RenderTexture AABuffer;
	RenderTexture Buffer;
	Camera RenderCam;
	float OutlinePixelScaling;
	Camera BufferCam; 
	GameObject BufferPlane;
	Material BufferMat;
	public static float FPS = 60f; 
	
	float updateInterval = .5f;
	float accum   = 0; // FPS accumulated over the interval
	public static int frames  = 0; // Frames drawn over the interval
	public static int framecount  = 0; // total framecount!
	float timeleft; // Left time for current interval
	
	
	void Start(){
		Application.targetFrameRate = 60;
		QualitySettings.vSyncCount = 0;
		FPS = 60;
		CleanBuffers ();
		RenderCam = gameObject.GetComponent<Camera> ();
		RegisterBuffer ();
		timeleft = updateInterval; 
		if(ditherTex != null)
			Shader.SetGlobalTexture ("_DitherTex",ditherTex);
	}
	void CleanBuffers(){
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
		if (transform.FindChild ("Buffer Camera"))
			DestroyImmediate(transform.FindChild ("Buffer Camera").gameObject);
	}
	int OldAA;
	Vector2 OldSize;
	void UpdateRTT(){
		RenderCam = gameObject.GetComponent<Camera> ();
		Buffer = new RenderTexture (horizontalResolution, verticalResolution, 24);
		Buffer.filterMode = FilterMode.Point;
		//Buffer.antiAliasing = BufferAA;
		Buffer.name = "Pixel Buffer!";
		RenderCam.targetTexture = Buffer; 
		if (BufferAA > 1) {
			AABuffer = new RenderTexture(horizontalResolution*3,verticalResolution*3,24);
			AABuffer.filterMode = FilterMode.Point;
			AABuffer.antiAliasing = BufferAA;
			Buffer.name = "AA Pixel Buffer!";
			RenderCam.targetTexture = AABuffer;
		}
		if(BufferMat)
			BufferMat.mainTexture = Buffer;
		OldAA = BufferAA;
		OldSize = new Vector2 (horizontalResolution,verticalResolution);
		
	}
	
	void UpdateBufferPlane(){
		float pos = (BufferCam.nearClipPlane + 0.01f);
		BufferPlane.transform.localPosition = pos*Vector3.forward;
		float h = Mathf.Tan(BufferCam.fieldOfView*Mathf.Deg2Rad*0.5f)*pos*2f;
		BufferPlane.transform.localScale = new Vector3(h*BufferCam.aspect,h,0f);
	}
	
	void RegisterBuffer()
	{
		CleanBuffers ();
		UpdateRTT ();
		GameObject CamObj = new GameObject ("Buffer Camera");
		CamObj.transform.parent = gameObject.transform;
		CamObj.transform.localPosition = Vector3.zero;
		CamObj.transform.localRotation = Quaternion.identity;
		CamObj.transform.localScale = Vector3.one;
		CamObj.AddComponent<Camera> ();
		BufferCam = CamObj.GetComponent<Camera> ();
		BufferCam.cullingMask = BufferLayer;
		BufferCam.farClipPlane = 2f;
		BufferPlane = GameObject.CreatePrimitive (PrimitiveType.Quad);
		BufferPlane.name = "Buffer Display";
		BufferPlane.transform.parent = BufferCam.transform;
		BufferPlane.transform.localPosition = Vector3.zero;
		BufferPlane.transform.localRotation = Quaternion.identity;
		BufferPlane.transform.localScale = Vector3.one;
		BufferPlane.layer = 8;
		BufferMat = new Material (BufferShader);
		BufferMat.mainTexture = Buffer;
		if(IndexLUT != null)
			BufferMat.SetTexture ("_LUTTex", IndexLUT);
		else
			BufferMat.SetTexture ("_LUTTex", defaultLUT);
		BufferMat.SetFloat ("_LUTSize", 32);
		BufferPlane.GetComponent<Renderer> ().material = BufferMat;
		
		UpdateBufferPlane ();
	}
	
	
	
	public void OnPostRender()
	{
		if (BufferMat) {
			if(IndexLUT != null)
				BufferMat.SetTexture ("_LUTTex", IndexLUT);
			else
				BufferMat.SetTexture ("_LUTTex", defaultLUT);
		}
		if (!setManually) {
			pixelScale = Mathf.CeilToInt (Screen.height / 320);
			horizontalResolution = Screen.width / pixelScale;
			verticalResolution = Screen.height / pixelScale;
		} else {
			horizontalResolution = Screen.width / pixelScale;
			verticalResolution = Screen.height / pixelScale;
		}
		
		horizontalResolution = Mathf.Clamp (horizontalResolution, 1, 2048);
		verticalResolution = Mathf.Clamp (verticalResolution, 1, 2048);
		
		if (!BufferCam) {
			RegisterBuffer ();
		}
		if (OldAA != BufferAA || horizontalResolution != OldSize.x || verticalResolution != OldSize.y) {
			UpdateRTT ();
			UpdateBufferPlane ();
		}
		
		if (pixelScale == 5)
			OutlinePixelScaling = 4f;
		if (pixelScale == 4)
			OutlinePixelScaling = 3f;
		if (pixelScale == 3)
			OutlinePixelScaling = 1.125f;
		if (pixelScale == 2)
			OutlinePixelScaling = .875f;
		if (pixelScale == 1)
			OutlinePixelScaling = .5f;
		if (Application.platform == RuntimePlatform.OSXWebPlayer || Application.platform == RuntimePlatform.Android || Application.platform == RuntimePlatform.WebGLPlayer) {
			OutlinePixelScaling *= -.05f;	
		} 
		OutlinePixelScaling *= 1.125f;	
		if(BufferAA == 1)
			OutlinePixelScaling *= .9f;	
		Shader.SetGlobalFloat("_DitherScale", 24f/2*(768f/Screen.height)*pixelScale  );
		if (!isOrthographic) {
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth / 2f) * (768f / Screen.height)); 
		}
		if(isOrthographic)
		{	
			
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 1600 )* (768f/Screen.height) * (Camera.main.orthographicSize/12)*OutlinePixelScaling ); 
			Shader.SetGlobalVector("_DitherScale", new Vector4(Screen.width/256f/pixelScale, Screen.height/256f/pixelScale,0,0) );
			//Debug.Log (Screen.width/256f);
		}
		#if UNITY_EDITOR
		if(!Application.isPlaying && isOrthographic)
			Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 2f )* (768f/Screen.height));
		#endif

		//Debug.Log (FPS+" is the FPS");
		if (BufferAA > 1) {
			Graphics.Blit (AABuffer, Buffer);
		}
		
	}
	void Update()
	{
		
		timeleft -= Time.deltaTime;
		accum += Time.timeScale/Time.deltaTime;
		++frames;
		++framecount;
		// Interval ended - update GUI text and start new interval
		if( timeleft <= 0.0 )
		{
			// display two fractional digits (f2 format)
			FPS = accum/frames;
			//if( FPS > 60)
			FPS = 60f;
			//Debug.Log("FPS = " +1f/Time.deltaTime +" VS Accumulated FPS: " +FPS);
			//FPS = 1f/Time.deltaTime;
			timeleft = updateInterval;
			accum = 0.0F;
			frames = 0;
		}
		//FPS = (1f/Time.deltaTime);
		//	Debug.Log("FPS = " +1f/Time.deltaTime);

	}
	void OnApplicationQuit(){
		Shader.SetGlobalFloat ("_OutlineWidth", (shaderOutlineWidth * 2f )* (768f/Screen.height));
		CleanBuffers();
	}
	void OnDisable ()
	{
		CleanBuffers();
		RenderCam.targetTexture = null;
	}
}




