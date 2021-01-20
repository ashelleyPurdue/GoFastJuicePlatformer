using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class FabrikIK : MonoBehaviour
{
    public Transform target;
    public int _chainLength = 3;

    private int _prevChainLength = -1;

    private BoneData[] _bones;
    private struct BoneData
    {
        /// <summary>
        /// The GameObject associated with this bone.
        /// Its rotation will only be updated in play mode.
        /// </summary>
        public Transform transform;

        /// <summary>
        /// The distance between this bone and the next bone in the chain
        /// </summary>
        public float length;

        /// <summary>
        /// The position of this bone's "base", relative to the first bone in
        /// the chain.
        /// </summary>
        public Vector3 point;
    }

    private void Init(int chainLength)
    {
        var bones = new BoneData[chainLength];
        
        Transform currentBone = transform;
        Transform previousBone = transform;
        for (int i = 0; i < chainLength; i++)
        {
            if (currentBone == null)
                throw new System.Exception("Bone chain too long--not enough parents");

            bones[i].transform = currentBone;
            bones[i].length    = Vector3.Distance(currentBone.position, previousBone.position);

            previousBone = currentBone;
            currentBone  = currentBone.parent;
        }

        _bones = bones.Reverse().ToArray();

        // Find the bone positions relative to the first bone in the chain.
        Vector3 rootPos = _bones[0].transform.position;
        for (int i = 0; i < chainLength; i++)
        {
            _bones[i].point = _bones[i].transform.position - rootPos;
        }
    }

    void LateUpdate()
    {
        // Re-initialize if the chain length changed
        if (_chainLength != _prevChainLength || _bones == null)
        {
            Init(_chainLength);
            _prevChainLength = _chainLength;
        }

        if (target != null)
        {
            SetTargetPos(target.position, 10);

            if (Application.isPlaying)
                UpdateBoneTransforms();
        }
    }

    public void SetTargetPos(Vector3 targetPos, int numIterations)
    {
        for (int i = 0; i < numIterations; i++)
            SingleIteration(targetPos);
    }

    public void UpdateBoneTransforms()
    {
        Vector3 rootPos = _bones[0].transform.position;

        // Make every bone look at the next bone in the chain.
        // Except the last bone, because obviously doesn't *have* a next bone.
        for (int i = 0; i < _bones.Length - 1; i++)
        {
            Transform boneObj = _bones[i].transform;
            Vector3 nextBonePos = _bones[i + 1].point + rootPos;
            boneObj.LookAt(nextBonePos, Vector3.up);

            // The "local Z" direction might not be the side that should be
            // facing the next bone, so apply an additional rotation to
            // compensate.
            Vector3 actualForward = _bones[i + 1].transform.localPosition.normalized;
            Vector3 desiredForward = Vector3.forward;
            var compensationRot = Quaternion.FromToRotation(actualForward, desiredForward);
            boneObj.rotation *= compensationRot;
        }
    }

    private void SingleIteration(Vector3 targetPos)
    {
        // Transform the target point so that it's relative to the first bone
        targetPos = targetPos - _bones[0].transform.position;

        // Backward pass: set the last point equal to the target and work backwards
        _bones[_bones.Length - 1].point = targetPos;

        for (int i = 1; i < _bones.Length; i++)
        {
            int currBone = _bones.Length - 1 - i;
            int prevBone = currBone + 1;

            Vector3 currPos = _bones[currBone].point;
            Vector3 nextPos = _bones[prevBone].point;
            
            var offset = _bones[currBone].length * (nextPos - currPos).normalized;
            _bones[currBone].point = nextPos + offset;
        }

        // Forward pass: Set the first point equal to zero and work fowards
        _bones[0].point = Vector3.zero;
        for (int i = 1; i < _bones.Length; i++)
        {
            int currBone = i;
            int prevBone = i - 1;

            Vector3 currPos = _bones[currBone].point;
            Vector3 prevPos = _bones[prevBone].point;

            var offset = _bones[prevBone].length * (currPos - prevPos).normalized;
            _bones[currBone].point = prevPos + offset;
        }
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // Bail if Init() hasn't been called yet.
        if (_bones == null)
            return;

        Color originalColor = Gizmos.color;
        Gizmos.color = Color.green;

        // Draw a line between all of the bones' points.
        Vector3 origin = _bones[0].transform.position;
        for (int i = 0; i < _bones.Length - 1; i++)
        {
            Vector3 currentPoint = origin + _bones[i].point;
            Vector3 nextPoint    = origin + _bones[i + 1].point;

            float dist = Vector3.Distance(currentPoint, nextPoint);
            float scale = dist * 0.1f;
            var rot = Quaternion.FromToRotation(
                Vector3.up,
                nextPoint - currentPoint
            );
            Handles.matrix = Matrix4x4.TRS(
                currentPoint,
                rot,
                new Vector3(scale, dist, scale)
            );
            Handles.color = Color.green;
            Handles.DrawWireCube(Vector3.up * 0.5f, Vector3.one);
        }

        Gizmos.color = originalColor;
    }
#endif
}