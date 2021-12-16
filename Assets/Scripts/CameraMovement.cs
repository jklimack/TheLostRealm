using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour
{
    public Transform target; //target that the camera follows
    public float smoothing;
    

    private const int CAM_SIZE = 8;

    // Start is called before the first frame update
    void Start()
    {
        Camera.main.orthographicSize = CAM_SIZE;
        Vector3 initRotation = new Vector3(20f, 0f, 0f);
        Quaternion rotation = Quaternion.Euler(initRotation);
        transform.rotation = rotation;
    }

    // Update is called once per frame
    //LateUpdate is called after the update function
    void LateUpdate()
    {
        if(transform.position != target.position){
            // create a new target position with the x,y coordinates coming from the target object
            // and the z coordinate coming from the camera. 
            Vector3 targetPos = new Vector3(target.position.x, target.position.y, transform.position.z);
            // linear interpolation
            transform.position = Vector3.Lerp(transform.position, targetPos, smoothing);
        }
    }
}
