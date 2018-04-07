using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;

[RequireComponent(typeof(SVControllerInput))]
public class SVGrabbable : MonoBehaviour {

    //------------------------
    // Variables
    //------------------------
    public float grabDistance = 1;
	public float grabFlyTime = 2f;
	public bool shouldFly = true;

    public Vector3 positionOffsetInHand = new Vector3(0, 0, 0);

    public bool forceRotationInHand = false;
    public Vector3 rotationInHand = new Vector3(0, 0, 0);

    [HideInInspector]
	public bool inHand = false;

    private SVOutline outlineComponent;

	private SVControllerInput input;
	private float grabStartTime;
	private Vector3 grabStartPosition;

    private Quaternion grabStartLocalRotation;
    private Quaternion grabStartWorldRotation;
		
    //------------------------
    // Init
    //------------------------
    void Start () {
		if (this.gameObject.GetComponent<SVOutline> ()) {
			outlineComponent = this.gameObject.GetComponent<SVOutline>();
		}

		this.input = this.gameObject.GetComponent<SVControllerInput> ();
    }

    //------------------------
    // Update
    //------------------------
    void Update() {
		if (this.input.activeController == SVControllerType.SVController_None) {
            this.UngrabbedUpdate();
        } else {
            this.GrabbedUpdate();
        }
    }

    private void UngrabbedUpdate() {
		this.inHand = false;

        float distanceToLeftHand = 1000;
		if (input.LeftControllerIsConnected) {
			distanceToLeftHand = (this.transform.position - input.LeftControllerPosition).magnitude;
        }

		float distanceToRightHand = 1000;
		if (input.RightControllerIsConnected) {
			distanceToRightHand = (this.transform.position - input.RightControllerPosition).magnitude;
		}

        if (grabDistance > distanceToLeftHand ||
            grabDistance > distanceToRightHand) {
            float distance = Mathf.Min(distanceToLeftHand, distanceToRightHand);
            if (this.outlineComponent) {
                float distanceForHighlight = grabDistance / 4f;
                float highlight = Mathf.Max(0, Mathf.Min(1, (grabDistance - distance) / distanceForHighlight));
                outlineComponent.outlineActive = highlight;
            }

			// order them based on distance
			SVControllerType firstController = SVControllerType.SVController_None;
			SVControllerType secondController = SVControllerType.SVController_None;

			if (distanceToLeftHand < distanceToRightHand) {
				if (SVControllerManager.nearestGrabbableToLeftController == this)
					firstController = SVControllerType.SVController_Left;
				
				if (SVControllerManager.nearestGrabbableToRightController == this)
					secondController = SVControllerType.SVController_Right;	
			} else {
				if (SVControllerManager.nearestGrabbableToRightController == this)
					firstController = SVControllerType.SVController_Right;	

				if (SVControllerManager.nearestGrabbableToLeftController == this)
					secondController = SVControllerType.SVController_Left;	
			}

			TrySetActiveController (firstController);
			TrySetActiveController (secondController);

			// Update grabbable distance so we always grab the nearest revolver
			if (distanceToLeftHand < SVControllerManager.distanceToLeftController || 
				SVControllerManager.nearestGrabbableToLeftController == null ||
				SVControllerManager.nearestGrabbableToLeftController == this) {
				SVControllerManager.nearestGrabbableToLeftController = this;
				SVControllerManager.distanceToLeftController = distanceToLeftHand;
			}

			if (distanceToRightHand < SVControllerManager.distanceToRightController || 
				SVControllerManager.nearestGrabbableToRightController == null ||
				SVControllerManager.nearestGrabbableToRightController == this) {
				SVControllerManager.nearestGrabbableToRightController = this;
				SVControllerManager.distanceToRightController = distanceToRightHand;
			}
				
        } else {
            if (this.outlineComponent) {
                outlineComponent.outlineActive = 0;
            }
        }
    }

    private void GrabbedUpdate() {
        if (input.gripAutoHolds) {
            if (input.GetReleaseGripButtonPressed(input.activeController)) {
                this.ClearActiveController();
                return;
            }
        } else if (!input.GetGripButtonDown(input.activeController)) {
            this.ClearActiveController();
            return;
        }

        // Get target Rotation
        Quaternion targetRotation;
        Quaternion controllerRotation = this.input.RotationForController(this.input.activeController);
        if (this.forceRotationInHand) {

            targetRotation = controllerRotation * Quaternion.Euler(this.rotationInHand);
        } else {
            targetRotation = controllerRotation * this.grabStartLocalRotation;
        }

		float percComplete = (Time.time - this.grabStartTime) / this.grabFlyTime;
		if (percComplete < 1 && this.shouldFly) {
			this.inHand = false;
			transform.position = Vector3.Lerp (this.grabStartPosition, this.input.PositionForController(this.input.activeController) + positionOffsetInHand, percComplete);
			transform.rotation = Quaternion.Lerp (this.grabStartWorldRotation, targetRotation, percComplete);
		} else {
			this.inHand = true;
			this.transform.SetPositionAndRotation(this.input.PositionForController(this.input.activeController) + positionOffsetInHand, targetRotation);
		}
    }

    //------------------------
    // State Changes
    //------------------------
	private void TrySetActiveController(SVControllerType controller) {
		if (this.input.activeController != SVControllerType.SVController_None ||
			controller == SVControllerType.SVController_None)
			return;	

		if (input.gripAutoHolds) {
			if (!input.GetGripButtonPressed(controller)) {
				return;
			}
		} else {
			if (!input.GetGripButtonDown (controller)) {
				return;
			}
		}

		if (this.input.SetActiveController (controller)) {
			this.grabStartTime = Time.time;
			this.grabStartPosition = this.gameObject.transform.position;
			this.grabStartWorldRotation = this.gameObject.transform.rotation;
            this.grabStartLocalRotation = Quaternion.Inverse(this.input.RotationForController(controller)) * this.grabStartWorldRotation;
            if (this.outlineComponent) {
                outlineComponent.outlineActive = 0;
            }
		
			Rigidbody rigidbody = this.GetComponent<Rigidbody> ();
			rigidbody.isKinematic = true;

			// hide the controller model
			this.input.HideActiveModel();
		}
    }

	private void ClearActiveController() {
		Rigidbody rigidbody = this.GetComponent<Rigidbody> ();
		rigidbody.isKinematic = false;
		rigidbody.velocity = this.input.ActiveControllerVelocity ();
		rigidbody.angularVelocity = this.input.ActiveControllerAngularVelocity ();

		// Show the render model
		this.input.ShowActiveModel();
		this.input.ClearActiveController ();
	}
}
