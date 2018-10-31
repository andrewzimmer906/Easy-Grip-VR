using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVLeverSoundFX : MonoBehaviour {
    public AudioClip leverDown;
    public AudioClip leverUp;

    public float minPitch = 0.8f;
    public float maxPitch = 1.2f;
    public float volume = 1f;

    private SVLever lever;
    private AudioSource audioSource;

    void Start() {
        lever = GetComponent<SVLever>();
    }

    // Update is called once per frame
    void Update() {
        if (lever.leverWasSwitched && lever.leverIsOn && leverUp) {
            if (audioSource == null) {
                audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
            }
            audioSource.clip = leverUp;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = volume;
            audioSource.Play();
        } else if (lever.leverWasSwitched && !lever.leverIsOn && leverDown) {
            if (audioSource == null) {
                audioSource = SVUtilities.SetOrAddAudioSource(gameObject);
            }
            audioSource.clip = leverDown;
            audioSource.pitch = Random.Range(minPitch, maxPitch);
            audioSource.volume = volume;
            audioSource.Play();
        }
    }
}
