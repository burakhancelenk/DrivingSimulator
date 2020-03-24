using System ;
using System.Collections;
using System.Collections.Generic;
using System.Net.Configuration ;
using UnityEngine;
using UnityEngine.UI ;
using Wrld ;

public class MenuHandler : MonoBehaviour
{

	public GameObject MainMenu ;
	public GameObject CarPlacementMenu ;
	public GameObject FinishMenu ;
	public GameObject CarPrefab;
	public GameObject CarCursorPrefab ;
	private Camera _carInsideCamera;
	public Text LongitudeText ;
	public Text LatitudeText ;
	public Camera CarFollowerCamera ;
	public Camera ThirdPersonCamera ;
	private WrldMap _wrldMapInstance ;
	private GameObject _carInstance ;
	private Vector3 _tpsPos ;
	private Transform _carCursorInstanceTransform ;
	private bool _waitForFinish ;
	


	void BackButton()
	{
		MainMenu.SetActive(true);
		LongitudeText.text = "" ;
		LatitudeText.text = "" ;
		CarPlacementMenu.SetActive(false) ;
		_wrldMapInstance.SetLatAndLong(37.7851f,-122.3936f);
		_wrldMapInstance.SetDefaultCameraConf();
		_wrldMapInstance.ReinitializeApiInstance();
		_wrldMapInstance.m_useBuiltInCameraControls = false ;
		WrldMap.isCameraControlsEnabled = false ;
		ThirdPersonCamera.transform.position = _tpsPos ;
	}

	void GoButton()
	{
		_tpsPos = ThirdPersonCamera.transform.position ;
		if (LatitudeText.text == "" || LongitudeText.text == "")
		{
			return;
		}
		string latitude = LatitudeText.text.Replace('.' , ',') ;
		string longitude = LongitudeText.text.Replace('.' , ',') ;
		_wrldMapInstance.SetLatAndLong(
			double.Parse(latitude),
			double.Parse(longitude));
		Debug.Log(double.Parse(LatitudeText.text));
		_wrldMapInstance.m_useBuiltInCameraControls = true ;
		WrldMap.isCameraControlsEnabled = true ;
		_wrldMapInstance.ReinitializeApiInstance();
		CarPlacementMenu.SetActive(true);
		MainMenu.SetActive(false);
	}

	void DriveButton()
	{
		_wrldMapInstance.m_useBuiltInCameraControls = false ;
		WrldMap.isCameraControlsEnabled = false ;
		_wrldMapInstance.m_streamingCamera = CarFollowerCamera ;
		ThirdPersonCamera.enabled = false ;
		_carInstance = Instantiate(CarPrefab , _carCursorInstanceTransform.position + Vector3.up * 5 ,
			Quaternion.identity) ;
		_carInsideCamera = _carInstance.transform.Find("FPSCamRoot").GetComponentInChildren<Camera>();
		_carInsideCamera.enabled = true ;
		Destroy(_carCursorInstanceTransform.gameObject);
		CarPlacementMenu.SetActive(false);
		_waitForFinish = true ;
	}

	void BackToMainMenuButton()
	{
		Destroy(_carInstance);
		RenderSettings.fogDensity = 0.0004f ;
		MainMenu.SetActive(true);
		LongitudeText.text = "" ;
		LatitudeText.text = "" ;
		FinishMenu.SetActive(false) ;
		_wrldMapInstance.SetLatAndLong(37.7851f,-122.3936f);
		_wrldMapInstance.SetDefaultCameraConf();
		_wrldMapInstance.m_useBuiltInCameraControls = false ;
		WrldMap.isCameraControlsEnabled = false ;
		_wrldMapInstance.m_streamingCamera = ThirdPersonCamera ;
		ThirdPersonCamera.enabled = true ;
		ThirdPersonCamera.transform.position = _tpsPos ;
		_wrldMapInstance.ReinitializeApiInstance();
	}

	void Start ()
	{
		_wrldMapInstance = GameObject.Find("WRLD").GetComponent<WrldMap>() ;
		MainMenu.transform.Find("GoButton").GetComponent<Button>().onClick.AddListener(GoButton);
		CarPlacementMenu.transform.Find("BackButton").GetComponent<Button>().onClick.AddListener(BackButton);
		CarPlacementMenu.transform.Find("DriveButton").GetComponent<Button>().onClick.AddListener(DriveButton);
		FinishMenu.transform.Find("MainMenuButton").GetComponent<Button>().onClick.AddListener(BackToMainMenuButton);
		_wrldMapInstance.m_useBuiltInCameraControls = false ;
		WrldMap.isCameraControlsEnabled = false ;
		_waitForFinish = false ;
	}

	private void OnEnable()
	{
		MainMenu.SetActive(true);
		CarPlacementMenu.SetActive(false);
		FinishMenu.SetActive(false);
	}

	private void Update()
	{
		if (CarPlacementMenu.activeSelf)
		{
			if (Input.GetMouseButtonDown(2))
			{
				Ray myRay;
				RaycastHit rchit;
			
				myRay = ThirdPersonCamera.ScreenPointToRay(Input.mousePosition);
				if(Physics.Raycast(myRay, out rchit))
				{
					if (rchit.transform.gameObject.name.Substring(0,6) == "Roads_")
					{
						if (_carCursorInstanceTransform == null)
						{
							_carCursorInstanceTransform = Instantiate(CarCursorPrefab , rchit.point , CarCursorPrefab.transform.rotation)
								.GetComponent<Transform>() ;
						}
						else
						{
							_carCursorInstanceTransform.position = rchit.point ;
						}
					}
				}
			}
			
		}

		if (_waitForFinish)
		{
			if (Input.GetKeyDown(KeyCode.F))
			{
				Time.timeScale = 0 ;
				_carInstance.transform.Find("sls_amg").Find("SideScreen").GetComponent<EMG>().CloseEMG();
				_carInstance.transform.Find("sls_amg").Find("SideScreen").GetComponent<ICATEmpaticaBLEClient>().DisconnectEnpatica();
				FinishMenu.SetActive(true) ;
				_waitForFinish = false ;

			}
		}
	}
}
