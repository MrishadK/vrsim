using UnityEngine;

public class ProceduralSit : MonoBehaviour
{
    private Animator anim;
    
    [Header("Sitting Offsets")]
    public Vector3 upperLegOffset = new Vector3(-80f, 0, 0);
    public Vector3 lowerLegOffset = new Vector3(80f, 0, 0);
    public Vector3 spineOffset = new Vector3(10f, 0, 0);

    void Start()
    {
        anim = GetComponentInChildren<Animator>();
    }

    void LateUpdate()
    {
        if (anim == null) return;
        
        Transform leftUp = anim.GetBoneTransform(HumanBodyBones.LeftUpperLeg);
        Transform rightUp = anim.GetBoneTransform(HumanBodyBones.RightUpperLeg);
        Transform leftLow = anim.GetBoneTransform(HumanBodyBones.LeftLowerLeg);
        Transform rightLow = anim.GetBoneTransform(HumanBodyBones.RightLowerLeg);
        Transform spine = anim.GetBoneTransform(HumanBodyBones.Spine);

        if (leftUp) leftUp.localRotation *= Quaternion.Euler(upperLegOffset);
        if (rightUp) rightUp.localRotation *= Quaternion.Euler(upperLegOffset);
        
        if (leftLow) leftLow.localRotation *= Quaternion.Euler(lowerLegOffset);
        if (rightLow) rightLow.localRotation *= Quaternion.Euler(lowerLegOffset);
        
        if (spine) spine.localRotation *= Quaternion.Euler(spineOffset);
    }
}
