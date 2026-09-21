using Integrations.Mojang;
using UnityEngine;

namespace Logic.PlayerPreview
{
    public class PlayerPuppet : MonoBehaviour
    {
        [Header("Visuals")]
        public Texture2D DefaultSkin;
        public Texture2D DefaultCape;
        public Material SkinMaterial;
        public Material CapeMaterial;
        
        [Header("Physics")]
        public Transform PuppetMaster;
        public Transform Head;
        public Rigidbody Body;
        public Rigidbody[] BodyParts;
        
        private ConfigurableJoint _headJoint;
        private Vector3 _headForward;

        private readonly Vector3 _hiddenPosition = new(0f, 5f, 0f);
        private readonly Vector3 _shownPosition = new(0f, 1f, 0f);

        public void Awake()
        {
            _headJoint = Head.GetComponent<ConfigurableJoint>();
            _headForward = Head.forward;
        }

        public void Show()
        {
            PuppetMaster.localPosition = _shownPosition;
        }

        public void Hide()
        {
            PuppetMaster.localPosition = _hiddenPosition;   
        }

        public void Punch()
        {
            Vector3 onUnitSphere = Random.onUnitSphere;
            onUnitSphere.z = Mathf.Abs(onUnitSphere.z);
            Body.AddForce(onUnitSphere * 4800f);
            Body.AddTorque(onUnitSphere * 8000f);
        }

        public void Sleep()
        {
            for (int i = 0; i < BodyParts.Length; i++) 
                BodyParts[i].Sleep();
        }

        public void WakeUp()
        {
            for (int i = 0; i < BodyParts.Length; i++) 
                BodyParts[i].WakeUp();
        }

        public void LookAt(Vector3 target)
        {
            Vector3 to = target - Head.position;
            _headJoint.targetRotation = Quaternion.FromToRotation(_headForward, to);
        }

        // move to separate class
        public void ApplySkin(PlayerSkinData skinData)
        {
            SkinMaterial.mainTexture = skinData?.SkinTexture ?? DefaultSkin;
            CapeMaterial.mainTexture = skinData?.CapeTexture ?? DefaultCape;
        }
    }
}