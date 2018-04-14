using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(SVGrabbable))]
[RequireComponent(typeof(SVControllerInput))]
public class SVGrabHaptics : MonoBehaviour {

    public bool hapticsOnGrab = true;
    public bool hapticsOnCollision = true;
    public float vibrationLengthInSeconds = 0.25f;
    public AudioClip soundOnHit;
    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public float volume = 1f;

    private bool inHand = false;
    private SVGrabbable grabbable;
    private SVControllerInput input;
    private AudioSource audioSource;

    void Start () {
        grabbable = this.GetComponent<SVGrabbable>();
        input = this.GetComponent<SVControllerInput>();
    }
	
	// Update is called once per frame
	void Update () {
		if (grabbable.inHand && !inHand && hapticsOnGrab) {
            input.RumbleActiveController(vibrationLengthInSeconds);
        }

        inHand = grabbable.inHand;
    }

    private void OnCollisionEnter(Collision collision) {
        if (grabbable.inHand && hapticsOnCollision) {
            input.RumbleActiveController(vibrationLengthInSeconds);

            if (soundOnHit) {
                if (audioSource == null) {
                    audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
                    audioSource.clip = soundOnHit;
                }

                audioSource.pitch = Random.Range(minPitch, maxPitch);
                audioSource.volume = volume;
                audioSource.Play();
            }
        }
    }
}
