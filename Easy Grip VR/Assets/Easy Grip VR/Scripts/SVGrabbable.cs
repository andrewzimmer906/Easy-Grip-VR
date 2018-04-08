using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;



[RequireComponent(typeof(SVControllerInput))]
[RequireComponent(typeof(Rigidbody))]
public class SVGrabbable : MonoBehaviour {
    //------------------------
    // Constants
    //------------------------
    private const float kGrabResetTime = 0.5f; // 120 ms.

    //------------------------
    // Variables
    //------------------------
    [Space(15)]
    [Header("Pickup Settings")]
    public float grabDistance = 1;
	public float grabFlyTime = 2f;
	public bool shouldFly = true;

    [Space(15)]
    [Header("Hold Settings")]
    public Vector3 positionOffsetInHand = new Vector3(0, 0, 0);

    public bool forceRotationInHand = false;
    public Vector3 rotationInHand = new Vector3(0, 0, 0);

    [Space(15)]
    [Header("Collision Settings")]
    public bool ignorePhysicsInHand = true;

    public  bool isKnockable = true;
    private bool lastKnockableValue = true;

    [HideInInspector]
	public bool inHand = false;

    // Private Components
    private SVAbstractGripIndicator gripIndicatorComponent;
	private SVControllerInput input;

    // Private AND Static. Noice! Smort!
    private static GameObject leftHandCollider;
    private static GameObject rightHandCollider;

    struct GrabData {
       public float grabStartTime;
       public float grabEndTime;
       public bool recentlyReleased;
       public Vector3 grabStartPosition;
       public Quaternion grabStartLocalRotation;
       public Quaternion grabStartWorldRotation;
       public bool wasKinematic;
       public bool didHaveGravity;
       public bool wasKnockable;
       public bool hasJoint;
    };

    private GrabData grabData;
    private Rigidbody rb;
    private Collider[] colliders;

    //------------------------
    // Init
    //------------------------
    void Start () {
		if (this.gameObject.GetComponent<SVAbstractGripIndicator> ()) {
            gripIndicatorComponent = this.gameObject.GetComponent<SVAbstractGripIndicator>();
		}

		this.input = this.gameObject.GetComponent<SVControllerInput> ();
        this.rb = this.GetComponent<Rigidbody>();
        this.colliders = (Collider[])SVUtilities.AllComponentsOfType<Collider>(gameObject);

        if (SVGrabbable.leftHandCollider == null) {
            GameObject obj = new GameObject("Left Hand Collider");
            obj.AddComponent<SVColliderUpdater>().isLeft = true;
            SVGrabbable.leftHandCollider = obj;
        }

        if (SVGrabbable.rightHandCollider == null) {
            GameObject obj = new GameObject("Right Hand Collider");
            obj.AddComponent<SVColliderUpdater>().isLeft = false;
            SVGrabbable.rightHandCollider = obj;
        }
    }

    //------------------------
    // Update
    //------------------------
    /* Why Fixed Update? Good Question kind sir / madam. It's so we can run BEFORE our physics calculations.  This enables us to force position to hand position while
     * still respecting the Unity physics engine. 
	*/
    void FixedUpdate () {
	    if (this.input.activeController == SVControllerType.SVController_None) {
		    this.UngrabbedUpdate();
	    } else {
		    this.GrabbedUpdate();
	    }

        // Fix up our colliders in case we switched from not knockable to knockable
        if (lastKnockableValue != isKnockable) {
            lastKnockableValue = isKnockable;
            if (leftHandCollider) {
                Collider lhCol = leftHandCollider.GetComponent<Collider>();
                foreach (Collider collider in colliders) {
                    Physics.IgnoreCollision(lhCol, collider, !isKnockable);
                }
            }

            if (rightHandCollider) {
                Collider rhCol = rightHandCollider.GetComponent<Collider>();
                foreach (Collider collider in colliders) {
                    Physics.IgnoreCollision(rhCol, collider, !isKnockable);
                }
            }
        }
    }

    private void UngrabbedUpdate() {
		this.inHand = false;

        // Reset our knockable state after dropping this bad boy
        if (grabData.recentlyReleased && (Time.time - grabData.grabEndTime) > kGrabResetTime) {
            grabData.recentlyReleased = false;
            this.isKnockable = grabData.wasKnockable;
        }

        float distanceToLeftHand = 1000;
		if (input.LeftControllerIsConnected) {
			distanceToLeftHand = (this.transform.position - input.LeftControllerPosition).magnitude;
        }

		float distanceToRightHand = 1000;
		if (input.RightControllerIsConnected) {
			distanceToRightHand = (this.transform.position - input.RightControllerPosition).magnitude;
		}

        // Within grabbing distance?
        if (grabDistance > distanceToLeftHand ||
            grabDistance > distanceToRightHand) {
            float distance = Mathf.Min(distanceToLeftHand, distanceToRightHand);
            if (this.gripIndicatorComponent) {
                float distanceForHighlight = grabDistance / 4f;
                float highlight = Mathf.Max(0, Mathf.Min(1, (grabDistance - distance) / distanceForHighlight));
                gripIndicatorComponent.indicatorActive = highlight;
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
            if (this.gripIndicatorComponent) {
                gripIndicatorComponent.indicatorActive = 0;
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
            targetRotation = controllerRotation * this.grabData.grabStartLocalRotation;
        }

        // Make sure position offset respects rotation
        Vector3 targetOffset = targetRotation * -positionOffsetInHand;

        float percComplete = (Time.time - this.grabData.grabStartTime) / this.grabFlyTime;
        if (percComplete < 1 && this.shouldFly && !this.grabData.hasJoint) {
            this.inHand = false;
            transform.position = Vector3.Lerp(this.grabData.grabStartPosition, this.input.PositionForController(this.input.activeController) + targetOffset, percComplete);
            transform.rotation = Quaternion.Lerp(this.grabData.grabStartWorldRotation, targetRotation, percComplete);
        } else {
            this.inHand = true;
            Vector3 targetPosition = this.input.PositionForController(this.input.activeController);
            // If we've got a joint let's forget about setting the rotation and just focus on the position.
            // This keeps us from losing our minds!
            if (this.grabData.hasJoint) {
                transform.position = this.input.PositionForController(this.input.activeController) + targetOffset;
            } else {  // otherwise just lock to the hand position so there is no delay
                this.transform.SetPositionAndRotation(this.input.PositionForController(this.input.activeController) + targetOffset, targetRotation);
            }
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
			this.grabData.grabStartTime = Time.time;
            this.grabData.grabStartPosition = this.gameObject.transform.position;
            this.grabData.grabStartWorldRotation = this.gameObject.transform.rotation;
            this.grabData.grabStartLocalRotation = Quaternion.Inverse(this.input.RotationForController(controller)) * this.grabData.grabStartWorldRotation;
            this.grabData.hasJoint = (this.gameObject.GetComponent<Joint>() != null);
            if (this.gripIndicatorComponent) {
                gripIndicatorComponent.indicatorActive = 0;
            }

            // Update our rigidbody to respect being controlled by the player
            grabData.wasKinematic = rb.isKinematic;
            grabData.didHaveGravity = rb.useGravity;
            if (this.ignorePhysicsInHand) {
                rb.isKinematic = true;
            } else {
                rb.useGravity = false;
            }

            if (!grabData.recentlyReleased) {
                grabData.wasKnockable = this.isKnockable;
                this.isKnockable = false;
            }
            
            // hide the controller model
            this.input.HideActiveModel();
		}
    }

	private void ClearActiveController() {
        grabData.grabEndTime = Time.time;
        grabData.recentlyReleased = true;

        rb.isKinematic = grabData.wasKinematic;
		rb.useGravity = grabData.didHaveGravity;

		rb.velocity = this.input.ActiveControllerVelocity ();
		rb.angularVelocity = this.input.ActiveControllerAngularVelocity ();

		// Show the render model
		this.input.ShowActiveModel();
		this.input.ClearActiveController ();
    }
}
