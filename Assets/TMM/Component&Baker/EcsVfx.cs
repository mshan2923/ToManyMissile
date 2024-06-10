using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using Unity.Entities;
using UnityEngine;
using UnityEngine.VFX;

public class EcsVfx : MonoBehaviour
{
    public int Amount = 10000;
    public float LifeTime = 10f;
}

public struct EcsVfxComponent : IComponentData
{
    public int Amount;
    public float LifeTime;
    public int DataImageIndex;
    public int PlayEffectIndex;
    public int EffectAssetID;
}

public class EcsVfxBaker : Baker<EcsVfx>
{
    public override void Bake(EcsVfx authoring)
    {
        if (VfxController.Instance == null)
            new System.Exception("Need VfxController");
        if (! authoring.TryGetComponent<VisualEffect>(out var effect))
        {
            return;
        }


        AddComponent(GetEntity(authoring, TransformUsageFlags.Renderable), new EcsVfxComponent
        {
            Amount = authoring.Amount,
            LifeTime = authoring.LifeTime,
            DataImageIndex = -1,
            PlayEffectIndex = -1,
            EffectAssetID = effect.visualEffectAsset.GetInstanceID()
        });
    }
}
