using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Collections;
using Unity.Entities;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.VFX;

public class MissileVfxAuthoring : EcsVfx
{
    public GameObject EffectPrefab;

    [InspectorLabel("Warm Up")]
    public Vector3 WarmUpVelocity = Vector3.up;
    public float WarmUpVeloOffset = 0.5f;
    public float WarmUpEndDistance = 1f;
    public float WarmUpFireDuration = 1f;

    [Space(10), InspectorLabel("Trace")]
    public float TraceSpeed = 10f;
    public float LimitRotation = 10f;
    public Vector3 Target = Vector3.zero;
    public float TargetOffsetRadius = 1f;
    public float TargetOffsetHeight = 0.1f;
}

public struct MissileVfxComponent : IComponentData
{
    public float3 WarmUpVelocity;
    public float WarmUpVeloOffsetRadius;
    public float WarmUpEndDistance;
    public float WarmUpFireDuration;

    public float TraceSpeed;
    public float TraceAcclation;//+++
    public float LimitRotation;
    public float3 Target;
    public float TargetOffsetRadius;
    public float TargetOffsetHeight;

    // ���� �߻� �ӵ� , ���� �߻� ���� ���� , ���� �߻� ���� �ӵ� ,  ���� �߻� �ð� (�߻� ���� ���) , ���� �Ϸ� �ð�  , ���� ���� �Ÿ�
    // ȸ�� ����, ���� �ִ� �ӵ�, ���� ���ӵ��� , ���� ������ , ���� ���� ��������
}

public class MissileVfxBaker : Baker<MissileVfxAuthoring>
{
    public override void Bake(MissileVfxAuthoring authoring)
    {

        if (VfxController.Instance == null)
        {
            new System.Exception("Need VfxController");
        }

        if (! authoring.EffectPrefab.TryGetComponent<VisualEffect>(out var effect))
        {
            return;
        }

        VfxController.Instance.AddVisualEffectAsset(effect, authoring.EffectPrefab);

        AddComponent(GetEntity(authoring, TransformUsageFlags.Dynamic), new EcsVfxComponent
        {
            Amount = authoring.Amount,
            LifeTime = authoring.LifeTime,
            DataImageIndex = -1,
            PlayEffectIndex = -1,
            EffectAssetID = effect.visualEffectAsset.GetInstanceID()
        });
        //AddBuffer<positionMapSlot>(GetEntity(authoring, TransformUsageFlags.Renderable));

        AddComponent(GetEntity(authoring, TransformUsageFlags.Renderable), new MissileVfxComponent
        {
            WarmUpVelocity = authoring.WarmUpVelocity,
            WarmUpVeloOffsetRadius = authoring.WarmUpVeloOffset,
            WarmUpEndDistance = authoring.WarmUpEndDistance,
            WarmUpFireDuration = authoring.WarmUpFireDuration,

            TraceSpeed = authoring.TraceSpeed,
            LimitRotation = authoring.LimitRotation,
            Target = authoring.Target,
            TargetOffsetRadius = authoring.TargetOffsetRadius,
            TargetOffsetHeight = authoring.TargetOffsetHeight,
        });
    }
}