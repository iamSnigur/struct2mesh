using UnityEngine;

namespace Logic.PlayerPreview
{
    public class PlayerPuppet : MonoBehaviour
    {
        public Camera PuppetCamera;
        public Transform PuppetRoot;

        [Space] 
        public Transform Head;

        public Rigidbody RightArm;

        public Rigidbody LeftLeg;
        public Rigidbody RightLeg;
        public Rigidbody Body;
        
        private ConfigurableJoint _joint;
        private Vector3 _headForward;
        
        private void Start()
        {
            // setup camera
            PuppetRoot.localPosition = new Vector3(0f, 1f, 12f);
            _joint = Head.GetComponent<ConfigurableJoint>();
            _headForward = Head.forward;
        }

        public void Show()
        {
            PuppetRoot.localPosition = new Vector3(0f, 1f, 0f);
        }

        public void Hide()
        {
            PuppetRoot.localPosition = new Vector3(0f, 1f, 12f);   
        }

        public void Punch()
        {
            Body.AddForce(Vector3.forward * 2800f);
            LeftLeg.AddTorque(Vector3.right * 400f);
            RightLeg.AddTorque(Vector3.left * 400f);
            //Vector3 force = Vector3.right * 1000f + Vector3.back * 700f;
            //RightArm.AddTorque(force);
        }

        public void LookAt(Vector2 screenPos)
        {
            Ray worldRay = PuppetCamera.ViewportPointToRay(screenPos);
            Vector3 target = worldRay.GetPoint(1f);
            target.x *= -1f;
            Vector3 to = target - Head.position;
            _joint.targetRotation = Quaternion.FromToRotation(_headForward, to);
        }
    }
}