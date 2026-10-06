using UnityEngine;

public class FollowHeadset : MonoBehaviour
{
    public Transform vrCameraTarget;

    void LateUpdate()
    {
        if (vrCameraTarget != null)
        {
            transform.position = vrCameraTarget.position;
            transform.rotation = Quaternion.identity;
        }
    }
}
