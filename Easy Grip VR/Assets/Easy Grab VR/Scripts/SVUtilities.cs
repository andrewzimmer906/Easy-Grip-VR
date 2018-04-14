using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SVUtilities {
    public static Component[] AllComponentsOfType<T>(GameObject parent) where T : Component {
        T parentComponent = null;
        if (parent.GetComponent<T>()) {
            parentComponent = parent.GetComponent<T>();
        }

        T[] componets = parent.GetComponentsInChildren<T>();

        if (parentComponent != null) {
            T[] returnComponents = new T[componets.Length + 1];
            for (int i = 0; i < componets.Length; i++) {
                returnComponents[i] = componets[i];
            }
            returnComponents[componets.Length] = parentComponent;

            return returnComponents;
        }

        return componets;
    }

    public static AudioSource SetOrAddAudioSource(GameObject parent) {
        if (!parent.GetComponent<AudioSource>()) {
            return parent.AddComponent<AudioSource>();
        } else {
            return parent.GetComponent<AudioSource>();
        }
    }
}
