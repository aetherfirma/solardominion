using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SkyboxCamera : MonoBehaviour
{

    public Camera MainCamera;

    public Vector3 SkyboxRotation;

    private Quaternion SkyboxQuaternion
    {
        get
        {
            return Quaternion.Euler(SkyboxRotation);
        }
    }
    
    // Update is called once per frame
    void Update()
    {
        transform.rotation = SkyboxQuaternion * MainCamera.transform.rotation;
    }
}