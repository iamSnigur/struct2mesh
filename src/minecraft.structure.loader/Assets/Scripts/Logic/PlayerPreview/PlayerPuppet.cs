using Integrations.Mojang;
using UnityEngine;

namespace Logic.PlayerPreview
{
    public class PlayerPuppet : MonoBehaviour
    {
        public Texture2D DefaultSkin;
        public Texture2D DefaultCape;
        public Material SkinMaterial;
        public Material CapeMaterial;
        
        public Transform PuppetRoot;

        [Space] 
        public Transform Head;

        public Rigidbody RightArm;

        public Rigidbody LeftLeg;
        public Rigidbody RightLeg;
        public Rigidbody Body;
        
        private ConfigurableJoint _joint;
        private Vector3 _headForward;

        public void Awake()
        {
            PuppetRoot.localPosition = new Vector3(0f, 7f, 0f);
            _joint = Head.GetComponent<ConfigurableJoint>();
            _headForward = Head.forward;
        }

        public void Show()
        {
            PuppetRoot.localPosition = new Vector3(0f, 1f, 0f);
        }

        public void Hide()
        {
            PuppetRoot.localPosition = new Vector3(0f, 7f, 0f);   
        }

        public void Punch()
        {
            Vector3 onUnitSphere = Random.onUnitSphere;
            onUnitSphere.z = Mathf.Abs(onUnitSphere.z);
            Body.AddForce(onUnitSphere * 4800f);
            LeftLeg.AddTorque(Vector3.right * 200f);
            RightLeg.AddTorque(Vector3.left * 200f);
            //Vector3 force = Vector3.right * 1000f + Vector3.back * 700f;
            //RightArm.AddTorque(force);
        }

        public void LookAt(Vector3 target)
        {
            Vector3 to = target - Head.position;
            _joint.targetRotation = Quaternion.FromToRotation(_headForward, to);
        }

        // move to separate class
        public void ApplySkin(PlayerSkinData skinData)
        {
            SkinMaterial.mainTexture = skinData?.SkinTexture ?? DefaultSkin;
            CapeMaterial.mainTexture = skinData?.CapeTexture ?? DefaultCape;
        }
    }
}