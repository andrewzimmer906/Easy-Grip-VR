using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

public enum SVControllerType {
	SVController_None,
	SVController_Left,
	SVController_Right
};

public enum SVInputButton {
	SVButton_None = -1,
	SVButton_A  = 0,
	SVButton_B,
	SVButton_System,
	SVButton_Menu,
	SVButton_Thumbstick_Press,
	SVButton_Trigger,
	SVButton_Grip,
	SVButton_Thumbstick_Left,
	SVButton_Thumbstick_Right,
	SVButton_Thumbstick_Down,
	SVButton_Thumbstick_Up
};

public class SVControllerInput : MonoBehaviour {

    //------------------------
    // Variables
    //------------------------
    [Space(15)]
    [Header("Grip Settings")]

    [Tooltip("The button that should grab the object.")]
    public SVInputButton gripButton = SVInputButton.SVButton_Trigger;

    [Tooltip("If you are auto holding this button will drop the object.")]
    public SVInputButton releaseGripButton = SVInputButton.SVButton_None;

    [Tooltip("If true the object stays in your hand until the releaseGripButton is pressed. If false you drop the object when you release the grip button.")]
    public bool gripAutoHolds = false;

	//------------------------
	// Variables
	//------------------------
	[HideInInspector]
	public SVControllerType activeController;

	#if USES_STEAM_VR
	[HideInInspector]
	public SteamVR_Controller.Device activeControllerDevice;
	[HideInInspector]
	public SteamVR_RenderModel activeRenderModel;
	private SteamVR_ControllerManager controllerManager;
	#elif USES_OPEN_VR
	private OVRManager controllerManager;
	private Dictionary<int, bool>buttonStateLeft;
	private Dictionary<int, bool>buttonStateRight;
	OVRHapticsClip clipHard;
	#endif


	//------------------------
	// Setup
	//------------------------
	void Start() {
		#if USES_STEAM_VR
		controllerManager = Object.FindObjectOfType<SteamVR_ControllerManager> ();
		Assert.IsNotNull (controllerManager, "SVControllerInput (with SteamVR) Needs a SteamVR_ControllerManager in the scene to function correctly.");
		#elif USES_OPEN_VR
		controllerManager = Object.FindObjectOfType<OVRManager> ();
		Assert.IsNotNull (controllerManager, "SVControllerInput (with Open VR) Needs a OVRManager in the scene to function correctly.");

		buttonStateLeft = new Dictionary<int, bool>();
		buttonStateRight = new Dictionary<int, bool>();

		int cnt = 10;
		clipHard = new OVRHapticsClip(cnt);
		for (int i = 0; i < cnt; i++) {
			clipHard.Samples[i] = i % 2 == 0 ? (byte)0 : (byte)180;
		}
		clipHard = new OVRHapticsClip(clipHard.Samples, clipHard.Samples.Length);
		#else
		Debug.LogError("Easy Grab VR requires you to choose either Steam VR or Oculus SDK as your VR platform. Please open \"Window -> Easy Grip VR SDK\" and select your framework.");
		#endif
	}

	//------------------------
	// Getters
	//------------------------
	#if USES_STEAM_VR
	private GameObject SteamController(SVControllerType type) {
		return (type == SVControllerType.SVController_Left ? controllerManager.left : controllerManager.right); //TODO : Cache this component for performance
	}

	private SteamVR_Controller.Device Controller(SVControllerType type) {
		GameObject steamController = (type == SVControllerType.SVController_Left ? controllerManager.left : controllerManager.right); //TODO : Cache this component for performance
		return SteamVR_Controller.Input((int)steamController.GetComponent<SteamVR_TrackedObject>().index);
	}
	#endif

	public bool LeftControllerIsConnected {
		get {
#if USES_STEAM_VR
			return (controllerManager.left != null &&
			controllerManager.left.activeInHierarchy);
#elif USES_OPEN_VR
			return ((OVRInput.GetConnectedControllers() & OVRInput.Controller.LTouch) == OVRInput.Controller.LTouch);
#else
			return false;
#endif
		}
	}

	public bool RightControllerIsConnected {
		get {
#if USES_STEAM_VR
			return (controllerManager.right != null &&
			controllerManager.right.activeInHierarchy);
#elif USES_OPEN_VR
			return ((OVRInput.GetConnectedControllers() & OVRInput.Controller.RTouch) == OVRInput.Controller.RTouch);
#else
			return false;
#endif
		}
	}

	public Vector3 LeftControllerPosition {
		get {
#if USES_STEAM_VR
			if (this.LeftControllerIsConnected) {
				return controllerManager.left.transform.position;
			}
			return Vector3.zero;
#elif USES_OPEN_VR
            Vector3 leftHandPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.LTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
                return trackingSpace.TransformPoint(leftHandPosition);
            }
            return leftHandPosition;
#else
			return Vector3.zero;
#endif
		}
	}

	public Vector3 RightControllerPosition {
		get {
#if USES_STEAM_VR
			if (this.RightControllerIsConnected) {
				return controllerManager.right.transform.position;
			}
			return Vector3.zero;	
#elif USES_OPEN_VR
            Vector3 rightHandPosition = OVRInput.GetLocalControllerPosition(OVRInput.Controller.RTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
               return trackingSpace.TransformPoint(rightHandPosition);
            }
            return rightHandPosition;
#else
			return Vector3.zero;
#endif
		}
	}

	public Quaternion LeftControllerRotation {
		get {
			if (this.LeftControllerIsConnected) {
#if USES_STEAM_VR
				return controllerManager.left.transform.rotation;
#elif USES_OPEN_VR
				Quaternion leftHandRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.LTouch);
                Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
                if (trackingSpace != null) {
                    return trackingSpace.rotation * leftHandRotation;
                }
                return leftHandRotation;
#endif
            }

			return Quaternion.identity;
		}
	}

	public Quaternion RightControllerRotation {
		get {
			if (this.RightControllerIsConnected) {
#if USES_STEAM_VR
				return controllerManager.right.transform.rotation;
#elif USES_OPEN_VR
                Quaternion rightHandRotation = OVRInput.GetLocalControllerRotation(OVRInput.Controller.RTouch);
                Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
                if (trackingSpace != null) {
                    return trackingSpace.rotation * rightHandRotation;
                }
                return rightHandRotation;
#endif
			}

			return Quaternion.identity;
		}
	}

    public Vector3 LeftControllerVelocity {
        get {
#if USES_STEAM_VR
            if (this.LeftControllerIsConnected) {
                return Controller(SVControllerType.SVController_Left).velocity;
            }
            return Vector3.zero;
#elif USES_OPEN_VR
            Vector3 leftHandVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.LTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
                return trackingSpace.TransformDirection(leftHandVelocity);
            }
            return leftHandVelocity;
#else
			return Vector3.zero;
#endif
        }
    }

    public Vector3 RightControllerVelocity {
        get {
#if USES_STEAM_VR
            if (this.LeftControllerIsConnected) {
                return Controller(SVControllerType.SVController_Right).velocity;
            }
            return Vector3.zero;
#elif USES_OPEN_VR
            Vector3 rightHandVelocity = OVRInput.GetLocalControllerVelocity(OVRInput.Controller.RTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
                return trackingSpace.TransformDirection(rightHandVelocity);
            }
            return rightHandVelocity;
#else
			return Vector3.zero;
#endif
        }
    }

    public Vector3 LeftControllerAngularVelocity {
        get {
#if USES_STEAM_VR
            if (this.LeftControllerIsConnected) {
                return Controller(SVControllerType.SVController_Left).angularVelocity;
            }
            return Vector3.zero;
#elif USES_OPEN_VR
            Vector3 leftHandAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.LTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
                return trackingSpace.TransformDirection(leftHandAngularVelocity);
            }
            return leftHandAngularVelocity;
#else
		return Vector3.zero;
#endif
        }
    }

    public Vector3 RightControllerAngularVelocity {
        get {
#if USES_STEAM_VR
            if (this.RightControllerIsConnected) {
                return Controller(SVControllerType.SVController_Right).angularVelocity;
            }
            return Vector3.zero;
#elif USES_OPEN_VR
            Vector3 rightHandAngularVelocity = OVRInput.GetLocalControllerAngularVelocity(OVRInput.Controller.RTouch);
            Transform trackingSpace = GameObject.FindObjectOfType<OVRCameraRig>().trackingSpace;
            if (trackingSpace != null) {
                return trackingSpace.TransformDirection(rightHandAngularVelocity);
            }
            return rightHandAngularVelocity;
#else
		return Vector3.zero;
#endif
        }
    }

    //------------------------
    // Controller Info
    //------------------------
    public Vector3 PositionForController(SVControllerType controller) {
		if (controller == SVControllerType.SVController_Left) {
			return LeftControllerPosition;
		} else if (controller == SVControllerType.SVController_Right) {
			return RightControllerPosition;
		}

		return Vector3.zero;
	}

	public Quaternion RotationForController(SVControllerType controller) {
		if (controller == SVControllerType.SVController_Left) {
			return LeftControllerRotation;
		} else if (controller == SVControllerType.SVController_Right) {
			return RightControllerRotation;
		}

		return Quaternion.identity;
	}

	public bool ControllerIsConnected(SVControllerType controller) {
		if (controller == SVControllerType.SVController_Left) {
			return LeftControllerIsConnected;
		} else if (controller == SVControllerType.SVController_Right) {
			return RightControllerIsConnected;
		}

		return false;
	}

	//------------------------
	// Input Checkers
	//------------------------

	public bool GetGripButtonDown(SVControllerType controller) {
		return this.GetButtonDown (controller, this.gripButton);
	}

	public bool GetGripButtonPressed(SVControllerType controller) {
		return this.GetButtonPressDown (controller, this.gripButton);
	}

	public bool GetReleaseGripButtonPressed(SVControllerType controller) {
		return this.GetButtonPressDown (controller, this.releaseGripButton);
	}

	//------------------------
	// Public
	//------------------------

	public bool GetButtonDown(SVControllerType controller, SVInputButton button) {
		if (button == SVInputButton.SVButton_None || !ControllerIsConnected(controller))
			return false;
		
#if USES_STEAM_VR
		return Controller(controller).GetPress(GetSteamButtonMapping(button));
#elif USES_OPEN_VR
		return GetOVRButtonDown(controller, button);
#else
		return false;
#endif
	}

	public bool GetButtonPressDown(SVControllerType controller, SVInputButton button) {
		if (button == SVInputButton.SVButton_None || !ControllerIsConnected(controller))
			return false;
		
#if USES_STEAM_VR
		return Controller(controller).GetPressDown(GetSteamButtonMapping(button));
#elif USES_OPEN_VR
		return GetOVRButtonPressDown(controller, button);
#else
		return false;
#endif
	}

	public bool SetActiveController(SVControllerType activeController) {
		if (activeController == SVControllerType.SVController_Left &&
		    SVControllerManager.leftControllerActive) {
			return false;
		}

		if (activeController == SVControllerType.SVController_Right &&
			SVControllerManager.rightControllerActive) {
			return false;
		}

		this.activeController = activeController;

#if USES_STEAM_VR
		this.activeControllerDevice = Controller (activeController);
		this.activeRenderModel = SteamController(this.activeController).GetComponentInChildren<SteamVR_RenderModel>();
#endif

		if (this.activeController == SVControllerType.SVController_Right) {
			SVControllerManager.rightControllerActive = true;
		} else {
			SVControllerManager.leftControllerActive = true;
		}
			
		return true;
	}

	public void ClearActiveController() {
#if USES_STEAM_VR
		this.activeControllerDevice = null;
		this.activeRenderModel = null;
#endif

		if (this.activeController == SVControllerType.SVController_Right) {
			SVControllerManager.rightControllerActive = false;
		} else {
			SVControllerManager.leftControllerActive = false;
		}

		this.activeController = SVControllerType.SVController_None;
	}

	public void RumbleActiveController(float rumbleLength) {
#if USES_STEAM_VR
		if (activeControllerDevice != null) {
			StartCoroutine( LongVibration(activeControllerDevice, rumbleLength, 1.0f) );
		}
#elif USES_OPEN_VR
		StartCoroutine( OVRVibrateForTime(rumbleLength) );
#endif
	}

	public Vector3 ActiveControllerVelocity() {
        if (activeController == SVControllerType.SVController_Left) {
            return LeftControllerVelocity;
        } else if (activeController == SVControllerType.SVController_Right) {
            return RightControllerVelocity;
        } else {
            return Vector3.zero;
        }
	}

	public Vector3 ActiveControllerAngularVelocity() {
        if (activeController == SVControllerType.SVController_Left) {
            return LeftControllerAngularVelocity;
        } else if (activeController == SVControllerType.SVController_Right) {
            return RightControllerAngularVelocity;
        } else {
            return Vector3.zero;
        }
    }

	//------------------------
	// Visibility
	//------------------------

	public void HideActiveModel() {
#if USES_STEAM_VR
		this.activeRenderModel.gameObject.SetActive (false);
#endif
	}

	public void ShowActiveModel() {
#if USES_STEAM_VR
		this.activeRenderModel.gameObject.SetActive (true);
#endif
	}

	//------------------------
	// Haptics
	//------------------------

#if USES_STEAM_VR
	//length is how long the vibration should go for
	//strength is vibration strength from 0-1
	private IEnumerator LongVibration(SteamVR_Controller.Device device, float totalLength, float strength) {
		ushort rLength = (ushort)Mathf.Lerp (0, 3999, strength);
		for (float i = 0f; i < totalLength; i += Time.deltaTime) {
			device.TriggerHapticPulse(rLength);
			yield return null;
		}
	}

	//vibrationCount is how many vibrations
	//vibrationLength is how long each vibration should go for
	//gapLength is how long to wait between vibrations
	//strength is vibration strength from 0-1
	IEnumerator LongVibration(SteamVR_Controller.Device device, int vibrationCount, float vibrationLength, float gapLength, float strength) {
		strength = Mathf.Clamp01(strength);
		for(int i = 0; i < vibrationCount; i++) {
			if(i != 0) yield return new WaitForSeconds(gapLength);
			yield return StartCoroutine(LongVibration(device, vibrationLength, strength));
		}
	}
#endif

#if USES_OPEN_VR
	public IEnumerator OVRVibrateForTime(float time)
	{
		OVRHaptics.OVRHapticsChannel channel;
		if (activeController == SVControllerType.SVController_Left) {
			channel = OVRHaptics.LeftChannel;
		} else {
			channel = OVRHaptics.RightChannel;
		}

		for (float t = 0; t <= time; t += Time.deltaTime) {
			channel.Queue(clipHard);
		}
		
		yield return new WaitForSeconds(time);
		channel.Clear();
		yield return null;
	}
#endif


	//------------------------
	// Steam Mappings
	//------------------------
#if USES_STEAM_VR

	private Valve.VR.EVRButtonId GetSteamButtonMapping(SVInputButton button) {
		switch (button) {
			case SVInputButton.SVButton_A:
			return Valve.VR.EVRButtonId.k_EButton_A;
			case SVInputButton.SVButton_B:
			return Valve.VR.EVRButtonId.k_EButton_A;
			case SVInputButton.SVButton_Grip:
			return Valve.VR.EVRButtonId.k_EButton_Grip;
			case SVInputButton.SVButton_Menu:
			return Valve.VR.EVRButtonId.k_EButton_ApplicationMenu;
			case SVInputButton.SVButton_System:
			return Valve.VR.EVRButtonId.k_EButton_System;
			case SVInputButton.SVButton_Trigger:
			return Valve.VR.EVRButtonId.k_EButton_SteamVR_Trigger;
			case SVInputButton.SVButton_Thumbstick_Down:
			return Valve.VR.EVRButtonId.k_EButton_DPad_Down;
			case SVInputButton.SVButton_Thumbstick_Left:
			return Valve.VR.EVRButtonId.k_EButton_DPad_Left;
			case SVInputButton.SVButton_Thumbstick_Right:
			return Valve.VR.EVRButtonId.k_EButton_DPad_Right;
			case SVInputButton.SVButton_Thumbstick_Up:
			return Valve.VR.EVRButtonId.k_EButton_DPad_Up;
			case SVInputButton.SVButton_Thumbstick_Press:
			return Valve.VR.EVRButtonId.k_EButton_SteamVR_Touchpad;
		}
		return (Valve.VR.EVRButtonId)0;
	}
#endif


	//------------------------
	// OVR Mappings
	//------------------------
#if USES_OPEN_VR

	private OVRInput.Button GetOVRButtonMapping(SVInputButton button) {
		switch (button) {
		case SVInputButton.SVButton_A:
			return OVRInput.Button.One;
		case SVInputButton.SVButton_B:
			return OVRInput.Button.Two;
		case SVInputButton.SVButton_System:
			return OVRInput.Button.Start;
		case SVInputButton.SVButton_Thumbstick_Press:
			return OVRInput.Button.PrimaryThumbstick;
		}

		return (OVRInput.Button)0;
	}

	private bool GetOVRButtonPressDown(SVControllerType controller, SVInputButton button) {
		bool isRight = (controller == SVControllerType.SVController_Right);
		Dictionary<int, bool> buttonState = isRight ? this.buttonStateRight : this.buttonStateLeft;

		bool isDown = GetOVRButtonDown (controller, button);
		bool inputIsDown = buttonState.ContainsKey ((int)button) && (bool)buttonState [(int)button];
		bool isPressDown = (!inputIsDown && isDown);
		buttonState [(int)button] = isDown;
		return isPressDown;
	}

	private bool GetOVRButtonDown(SVControllerType controller, SVInputButton button) {
		bool isRight = (controller == SVControllerType.SVController_Right);
		OVRInput.Controller ovrController = (isRight ? OVRInput.Controller.RTouch : OVRInput.Controller.LTouch);

		switch (button) {
		// Buttons
		case SVInputButton.SVButton_A:
		case SVInputButton.SVButton_B:
		case SVInputButton.SVButton_System:
		case SVInputButton.SVButton_Thumbstick_Press:
			return OVRInput.Get (GetOVRButtonMapping(button), ovrController);

		// 2D Axis
		case SVInputButton.SVButton_Thumbstick_Down:
		case SVInputButton.SVButton_Thumbstick_Left:
		case SVInputButton.SVButton_Thumbstick_Right:
		case SVInputButton.SVButton_Thumbstick_Up:
			{
				OVRInput.Axis2D axis2D = OVRInput.Axis2D.PrimaryThumbstick;

				Vector2 vec = OVRInput.Get (axis2D, ovrController);

				if (button == SVInputButton.SVButton_Thumbstick_Down) {
					return vec.y < -0.75;
				} else if (button == SVInputButton.SVButton_Thumbstick_Up) {
					return vec.y > 0.75;
				} else if (button == SVInputButton.SVButton_Thumbstick_Left) {
					return vec.x < -0.75;
				} else if (button == SVInputButton.SVButton_Thumbstick_Right) {
					return vec.x > 0.75;
				}
				return false;
			}

		// 1D Axis
		case SVInputButton.SVButton_Trigger:
		case SVInputButton.SVButton_Grip:
			{
				OVRInput.Axis1D axis = OVRInput.Axis1D.PrimaryIndexTrigger;
				if (button == SVInputButton.SVButton_Trigger) {
					axis = OVRInput.Axis1D.PrimaryIndexTrigger;
				} else if (button == SVInputButton.SVButton_Grip) {
					axis = OVRInput.Axis1D.PrimaryHandTrigger;
				}
				return (OVRInput.Get (axis, ovrController) > 0.75f);
			}

		default:
			return false;	
		}
			
	}

#endif
}
