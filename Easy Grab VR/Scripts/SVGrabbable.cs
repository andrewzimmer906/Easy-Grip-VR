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
    [Tooltip("Defines if its it possible to pick this object up")]
    public bool canGrab = true;
    [Tooltip("If true, this object can fly to your hand.")]
    public bool shouldFly = true;
    [Tooltip("How far the object can fly to your hand.")]
    public float grabFlyDistance = .35f;
    [Tooltip("How long it takes the object to complete its flight.")]
    public float grabFlyTime = .1f;

    [Space(15)]
    [Header("Hold Settings")]

    [Tooltip("This allows you to mirror the offset on the Local X Axis if the object is held in your left hand. You usually want this on.")]
    public bool mirrorXOffsetInLeftHand = true;
    [Tooltip("Where on the object should you grip it? Offsets are useful for tools like hammers and other things.")]
    public Vector3 positionOffsetInHand = new Vector3(0, 0, 0);

    [Tooltip("If true this forces the object to a specific local rotation when you pick it up.")]
    public bool forceRotationInHand = false;
    [Tooltip("The rotation forced in hand. Only applies if forceRotationInHand is true.")]
    public Vector3 rotationInHand = new Vector3(0, 0, 0);

    [Tooltip("If true a held object won't collide with anything and doesn't use inHandLerpSpeed, rather it sets its position directly.")]
    public bool ignorePhysicsInHand = true;

    [Tooltip("If true, physics will work better with collisions for objects in your hand. This is really useful for things like joints, but it will make the object fall from your hand while moving around.")]
    public bool locomotionSupported = false;

    [Tooltip("How quickly the object will match your hands position when moving. Higher values give you less lag but less realistic physics when interacting with immobile objects.")]
    [Range(0.0f, 1.0f)]
    public float inHandLerpSpeed = 0.4f;

    [Tooltip("How far from your hand the object needs to be before we drop it automatically. Useful for collisions.")]
    public float objectDropDistance = 0.3f;

    [Space(15)]
    [Header("Collision Settings")]

    [Tooltip("If you can knock the object around with your controller.")]
    public bool isKnockable = true;
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
        public bool recentlyDropped;
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
    void Start() {
        if (this.gameObject.GetComponent<SVAbstractGripIndicator>()) {
            gripIndicatorComponent = this.gameObject.GetComponent<SVAbstractGripIndicator>();
        }

        this.input = this.gameObject.GetComponent<SVControllerInput>();
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
    // Public
    //------------------------

    public void DropFromHand() {
        if (this.inHand) {
            this.ClearActiveController();
        }
    }

    //------------------------
    // Update
    //------------------------
    /* Why Fixed Update? Good Question kind sir / madam. It's so we can run BEFORE our physics calculations.  This enables us to force position to hand position while
     * still respecting the Unity physics engine. This is great when you hand joints connected to your objects.
	*/
    private void FixedUpdate() {
        if (!locomotionSupported) {
            DoGrabbedUpdate();
        }
    }

    /* Why Late Update? Good Question kind sir / madam. It's so we can run AFTER our physics calculations.  This enables us to lerp objects that you need to carry around with you
     * think sword for example.
	*/
    void LateUpdate() {
        if(locomotionSupported) {
            DoGrabbedUpdate();
        }
    }

    void DoGrabbedUpdate() {
        if (this.input.activeController == SVControllerType.SVController_None) {
            this.UngrabbedUpdate();
        } else {
            if (this.canGrab) {
                this.GrabbedUpdate();
            }
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
            grabData.recentlyDropped = false;
            this.isKnockable = grabData.wasKnockable;
        }

        // If we drop something, give it a little cooldown so it can drop to the floor before we snag it.
        if (grabData.recentlyDropped) {
            return;
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
        if (grabFlyDistance > distanceToLeftHand ||
            grabFlyDistance > distanceToRightHand) {
            float distance = Mathf.Min(distanceToLeftHand, distanceToRightHand);
            if (this.gripIndicatorComponent) {
                float distanceForHighlight = grabFlyDistance / 4f;
                float highlight = Mathf.Max(0, Mathf.Min(1, (grabFlyDistance - distance) / distanceForHighlight));
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

            TrySetActiveController(firstController);
            TrySetActiveController(secondController);

            // Update grabbable distance so we always grab the nearest object
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
            // Clear our object as nearest if it's not in grabbin range!
            if (SVControllerManager.nearestGrabbableToRightController == this) {
                SVControllerManager.nearestGrabbableToRightController = null;
            }
            if (SVControllerManager.nearestGrabbableToLeftController == this) {
                SVControllerManager.nearestGrabbableToLeftController = null;
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
        Vector3 targetOffset;
        if (this.input.activeController == SVControllerType.SVController_Left && mirrorXOffsetInLeftHand) {
            Matrix4x4 mirrorMatrix = Matrix4x4.Scale(new Vector3(-1, 1, 1));
            Matrix4x4 offsetAndRotation = Matrix4x4.TRS(-positionOffsetInHand, Quaternion.Euler(this.rotationInHand), Vector3.one);
            Matrix4x4 finalOffsetAndRotation = mirrorMatrix * offsetAndRotation;

            targetRotation = controllerRotation * Quaternion.LookRotation(finalOffsetAndRotation.GetColumn(2), finalOffsetAndRotation.GetColumn(1));
            targetOffset = targetRotation * finalOffsetAndRotation.GetColumn(3);
            targetRotation *= Quaternion.AngleAxis(180, Vector3.up);
        } else {
            targetOffset = targetRotation * -positionOffsetInHand;
        }

        float percComplete = (Time.time - this.grabData.grabStartTime) / this.grabFlyTime;
        if (percComplete < 1 && this.shouldFly && !this.grabData.hasJoint) {
            this.inHand = false;
            transform.position = Vector3.Lerp(this.grabData.grabStartPosition, this.input.PositionForController(this.input.activeController) + targetOffset, percComplete);
            transform.rotation = Quaternion.Lerp(this.grabData.grabStartWorldRotation, targetRotation, percComplete);
        } else {
            this.inHand = true;
            Vector3 targetPosition = this.input.PositionForController(this.input.activeController);

            // If we're moving too quickly and allow physics, drop the object. This also gives us the ability to drop it if you are trying to move it through
            // a solid object.
            if (!this.ignorePhysicsInHand &&
                (transform.position - targetPosition).magnitude >= objectDropDistance) {
                grabData.recentlyDropped = true;
                this.ClearActiveController();
                return;
            }

            // If we've got a joint let's forget about setting the rotation and just focus on the position.
            // This keeps us from losing our minds!
            if (this.grabData.hasJoint) {
                transform.position = targetPosition + targetOffset;
            } else {  // otherwise just lock to the hand position so there is no delay
                if (this.ignorePhysicsInHand) {
                    this.transform.SetPositionAndRotation(targetPosition + targetOffset, targetRotation);
                } else {
                    transform.position = Vector3.Lerp(this.transform.position, targetPosition + targetOffset, inHandLerpSpeed);
                    transform.rotation = Quaternion.Lerp(this.transform.rotation, targetRotation, inHandLerpSpeed);
                    rb.velocity = this.input.ActiveControllerVelocity();
                    rb.angularVelocity = this.input.ActiveControllerAngularVelocity();
                }
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
            if (!input.GetGripButtonDown(controller)) {
                return;
            }
        }

        if (this.input.SetActiveController(controller)) {
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
                foreach (Collider collider in this.colliders) {
                    collider.enabled = false;
                }
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

        if (this.ignorePhysicsInHand) {
            foreach (Collider collider in this.colliders) {
                collider.enabled = true;
            }
        }

        if (!grabData.recentlyDropped) {
            rb.velocity = this.input.ActiveControllerVelocity();
            rb.angularVelocity = this.input.ActiveControllerAngularVelocity();
        }

        // Show the render model
        this.input.ShowActiveModel();
        this.input.ClearActiveController();
    }
}
