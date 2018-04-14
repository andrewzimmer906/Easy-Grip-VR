using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVButtonSoundFX : MonoBehaviour {
    public AudioClip buttonDown;
    public AudioClip buttonUp;

    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public float volume = 1f;

    private SVButton button;
    private AudioSource audioSource;

	void Start () {
        button = GetComponent<SVButton>();
    }
	
	// Update is called once per frame
	void Update () {
        if (button.buttonPressed && buttonDown) {
            if (audioSource == null) {
                audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
            }
            audioSource.clip = buttonDown;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = volume;
            audioSource.Play();
        } else if (button.buttonUnpressed && buttonUp) {
            if (audioSource == null) {
                audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
            }
            audioSource.clip = buttonUp;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
