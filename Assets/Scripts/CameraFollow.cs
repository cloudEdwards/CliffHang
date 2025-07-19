using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements.Experimental;

public class CameraFollow : MonoBehaviour
{
    private static CameraFollow _singleton; 
    private Transform target; 

    public static CameraFollow Singleton
    {
        get => _singleton;
        set
        {
            if (value == null) {
                _singleton = null;
            } else if (_singleton == null) {
                _singleton = value;
            } else if (_singleton != value) {
                Destroy(value);
                Debug.LogError($"There can only be one {nameof(CameraFollow)}");

            }
        }
    }

    private void Awake()
    {
        Singleton = this;
    }

    private void OnDestroy()
    {
        if (Singleton == this) {
            Singleton = null;
        }
    }

    private void LateUpdate()
    {
        if (target != null) {
            transform.SetPositionAndRotation(target.position, target.rotation);
        }
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
    }
}
