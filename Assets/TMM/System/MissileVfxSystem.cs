using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Unity.Burst;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.UniversalDelegates;
using Unity.Jobs;
using Unity.Mathematics;
using Unity.Transforms;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;
using static UnityEditor.Progress;

public partial class MissileVfxSystem : SystemBase
{
    EntityQuery Effects;

    protected override void OnStartRunning()
    {
        base.OnStartRunning();
        Effects = EntityManager.CreateEntityQuery(typeof(MissileVfxComponent));
        Debug.Log($"VisualEffect : {Effects.CalculateEntityCount()}");

        if (Effects.CalculateEntityCount() == 0)
        {
            Enabled = false;
            return;
        }

        //============= Baker�� �ϴϱ� ����Ʈ�� ���� �ȵǹǷ� , ���⼭ 
        VfxController.Instance.ClearDataTextures();
        var effectEntity = Effects.ToEntityArray(Allocator.TempJob);

        for (int i = 0; i < effectEntity.Length; i++)
        {
            var ecsVfx = SystemAPI.GetComponentRW<EcsVfxComponent>(effectEntity[i]);

            var vfx = VfxController.Instance.SpawnEffect(ecsVfx.ValueRO.EffectAssetID , ecsVfx.ValueRO.Amount);

            ecsVfx.ValueRW.DataImageIndex = VfxController.Instance.positionMapData.Count - 1;
            ecsVfx.ValueRW.PlayEffectIndex = i;
        }


        if (VfxController.Instance.AutoStart)
        {
            for (int i = 0; i < effectEntity.Length; i++)
            {
                var vfx = SystemAPI.GetComponentRO<EcsVfxComponent>(effectEntity[i]).ValueRO;

                VfxController.Instance.PlayEffect(vfx);//effect.Play();
            }
        }

        effectEntity.Dispose();
    }

    protected override void OnStopRunning()
    {
        base.OnStopRunning();
    }
    protected override void OnUpdate()
    {

        //------------- Job �ȿ��� ���� NativeArray �ȵ� , ������ SystemBase �Ǵϱ� 
        //                 for������ ���� Job�� �����Ű���� , for�� ������ Complete 

        //Effets = EntityManager.CreateEntityQuery(typeof(VisualEffect));
        #region Calculate DataMap
        var effectEntity = Effects.ToEntityArray(Allocator.TempJob);
        var missiles = Effects.ToComponentDataArray<MissileVfxComponent>(Allocator.TempJob);

        {
            /*
            EffectsArray = Effets.ToComponentArray<VisualEffect>();
            if (EffectsArray[0].aliveParticleCount <= 0)
                EffectsArray[0].Play();//---- Working
            */
        }// Ecs Component - VisualEffect

        
        var posMap = new NativeArray<NativeArray<Color>>(effectEntity.Length, Allocator.TempJob);
        var veloMap = new NativeArray<NativeArray<Color>>(effectEntity.Length, Allocator.TempJob);
        var effectindexes = new NativeArray<int>(effectEntity.Length, Allocator.TempJob);

        for (int i = 0; i < effectEntity.Length; i++)
        {
            var vfxcomponent = SystemAPI.GetComponentRO<EcsVfxComponent>(effectEntity[i]);
            var effectIndex = vfxcomponent.ValueRO.PlayEffectIndex;

            effectindexes[i] = effectIndex;

            posMap[i] = new NativeArray<Color>(VfxController.Instance.positionMapData[effectIndex], Allocator.TempJob);//buffer.AsNativeArray();
            veloMap[i] = new NativeArray<Color>(VfxController.Instance.velocityMapData[effectIndex], Allocator.TempJob);//buffer.AsNativeArray();
        }

        var CalculateHandle = Dependency;
        for (int i = 0; i < effectEntity.Length; i++)
        {
            //var vfxcomponent = SystemAPI.GetComponentRO<EcsVfxComponent>(effectEntity[i]);
            //var effectIndex = vfxcomponent.ValueRO.PlayEffectIndex;
            uint Lseed = (uint)(VfxController.Instance.EffectDatas[effectindexes[i]].Effect.GetInstanceID());

            var localHandle = new CalculateMissile()
            {
                poseMap = posMap[i].AsReadOnly(),
                veloMap = veloMap[i].AsReadOnly(),
                EffectPos = SystemAPI.GetComponentRO<LocalTransform>(effectEntity[i]).ValueRO.Position,//VfxController.Instance.EffectDatas[effectindexes[i]].Effect.transform.position,
                missiles = missiles[effectindexes[i]],
                random = new Unity.Mathematics.Random(Lseed),
                DT = SystemAPI.Time.DeltaTime,
                FireBetween = missiles[effectindexes[i]].WarmUpFireDuration / posMap[i].Length,
                Amount = posMap[i].Length,

                effectIndex = i,
                activeTime = VfxController.Instance.EffectAge[effectindexes[i]],
            }.Schedule(posMap[i].Length, 64, Dependency);
            CalculateHandle = JobHandle.CombineDependencies(CalculateHandle, localHandle);
           
        }
        CalculateHandle.Complete();
        #endregion

        for (int i = 0; i < effectEntity.Length; i++)
        {
            var vfxcomponent = SystemAPI.GetComponentRO<EcsVfxComponent>(effectEntity[i]);

            VfxController.Instance.ApplyPositionMap(i, VfxController.Instance.positionMapData[effectindexes[i]]);
            VfxController.Instance.ApplyVelocityMap(i, VfxController.Instance.velocityMapData[effectindexes[i]]);

            var isMove = VfxController.Instance.velocityMapData[effectindexes[i]].Sum(t => t.a);
            if (isMove == 0)
            {
                VfxController.Instance.EndEffect.Invoke(effectindexes[i]);
            }else
            {
                if (VfxController.Instance.EffectDatas[effectindexes[i]].isPlaying == false && VfxController.Instance.LoopPlay)
                {
                    VfxController.Instance.PlayEffect(vfxcomponent.ValueRO);
                }
            }

            //var effect = VfxController.Instance.EffectDatas[effectindexes[i]].Effect;


            
        }//Color[] To Texture


        {
            effectEntity.Dispose();
            foreach (var v in posMap)
            {
                v.Dispose();
            }
            posMap.Dispose();
            foreach (var v in veloMap)
            {
                v.Dispose();
            }
            veloMap.Dispose();
            effectindexes.Dispose();
            missiles.Dispose();
        }//Dispose
    }

    //[BurstCompile] // ---- �̱��� ���� ����
    public struct CalculateMissile : IJobParallelFor
    {
        public NativeArray<Color>.ReadOnly poseMap;
        public NativeArray<Color>.ReadOnly veloMap;
        public float3 EffectPos;
        [ReadOnly] public MissileVfxComponent missiles;
        public Unity.Mathematics.Random random;
        public float FireBetween;
        public float DT;
        public int Amount;

        public int effectIndex;

        public float activeTime;
        public void Execute(int index)
        {
            {
                // Read => Colors[index] 
                // Write => VfxController.Instance.positionMapData[effectIndex][index]

                // RotationMapData ���� VelocityMapData ���� ȸ������ �ӵ��� ���� , ���İ����� �������� ��������
                //          - Sum()���� ���İ��� �ִ°͸� ���� ���� , ����Ʈ ���� ī��Ʈ�ٿ� ����(Ʈ���� ������)

                // ���� �߻� �ӵ� , ���� �߻� ���� ���� , ���� �߻� ���� �ӵ� ,  ���� �߻� �ð� (�߻� ���� ���) , ���� �Ϸ� �ð�  , ���� ���� �Ÿ�
                // ȸ�� ����, ���� �ִ� �ӵ�, ���� ���ӵ��� , ���� ������ , ���� ���� ��������

                // VFX ��ƼŬ ������ ��� �浹�� OnKillTrigger ���� , 


                // VelocityMapData �߰��� �ӽ� , �߰� �ϸ� ȸ������  + �ӵ� ���� + ������ �� �������� ��� �̵� ()
            }//GuideLine

            var FireOffset = RandomSphere(missiles.WarmUpVeloOffsetRadius, false);
            var TargetPos = missiles.Target + RandomSphere(missiles.TargetOffsetRadius, true);
            
            var indexRate = (float)index / Amount;
            //���� ==> WarmUpDuration ���� �߻簡 �Ǹ� , WarmUpDistance ��ŭ �־����� ��ǥ�� �̵�
            //=======> �߻�(���� ����) : (WarmUpDuration / �� ����) * index < WarmUpDuration

            if (activeTime < indexRate * missiles.WarmUpFireDuration)
            {
                VfxController.Instance.positionMapData[effectIndex][index] = AddColor(Color.black, EffectPos);
                VfxController.Instance.velocityMapData[effectIndex][index] = Color.black;
            }//�߻� ���
            else if (distance(poseMap[index], EffectPos) < missiles.WarmUpEndDistance * missiles.WarmUpEndDistance 
                && activeTime < indexRate * missiles.WarmUpFireDuration + (missiles.WarmUpEndDistance / Vector3.Magnitude(missiles.WarmUpVelocity)))
            {
                var addPos = math.normalize(missiles.WarmUpVelocity + FireOffset) * Vector3.Magnitude(missiles.WarmUpVelocity);
                VfxController.Instance.positionMapData[effectIndex][index] = AddColor(poseMap[index], addPos * DT * VfxController.Instance.PlayRate);
                VfxController.Instance.velocityMapData[effectIndex][index] = AddColor(Color.black, addPos);
            }// ����
            else
            {
                if (InCylinder(poseMap[index], TargetPos, missiles.TargetOffsetRadius, missiles.TargetOffsetHeight) == false && veloMap[index].a > 0)
                    //(distance(poseMap[index], TargetPos) > (missiles.TargetOffsetRadius  * missiles.TargetOffsetRadius))//=============== ���� -> Y�� ������ ��ġ�� ������ + y���� ���� ����
                {
                    var addPos = Normailze(poseMap[index], TargetPos) * missiles.TraceSpeed;

                    var angle = math.degrees(math.dot(Normailze(poseMap[index], TargetPos), math.normalize(addPos)));
                    if (angle < missiles.LimitRotation)
                    {
                        VfxController.Instance.positionMapData[effectIndex][index] = AddColor(poseMap[index], addPos * DT * VfxController.Instance.PlayRate);
                        VfxController.Instance.velocityMapData[effectIndex][index] = AddColor(Color.black, addPos);
                    }
                    else
                    {
                        var LimitedDir = Vector3.Lerp(Tofloat3(veloMap[index]), addPos, (missiles.LimitRotation / angle) * DT * VfxController.Instance.PlayRate);

                        VfxController.Instance.positionMapData[effectIndex][index] = AddColor(poseMap[index], LimitedDir * DT * VfxController.Instance.PlayRate);
                        VfxController.Instance.velocityMapData[effectIndex][index] = AddColor(Color.black, LimitedDir);
                    }

                }
                else
                {
                    var temp = veloMap[index];
                    temp.a = 0;
                    VfxController.Instance.positionMapData[effectIndex][index] = AddColor(Color.black, TargetPos);//AddColor(poseMap[index], Tofloat3(veloMap[index]) * DT);
                    VfxController.Instance.velocityMapData[effectIndex][index] = temp;
                }
            }//����
        }

        public float3 RandomSphere(float radius , bool isCircle)
        {
            var vaule = random.NextFloat3Direction() * random.NextFloat(-radius, radius);
            if (isCircle)
            {
                vaule.y = 0;
            }
            return vaule;
        }
        public Color AddColor(Color color, float3 pos)
        {
            color.r += pos.x;
            color.g += pos.y;
            color.b += pos.z;

            return color;
        }
        public float distance(Color color, float3 pos)
        {
            pos.x -= color.r;
            pos.y -= color.g;
            pos.z -= color.b;

            return Vector3.SqrMagnitude(pos);
        }
        public float3 Normailze(Color color, float3 pos)
        {
            pos.x -= color.r;
            pos.y -= color.g;
            pos.z -= color.b;

            return math.normalizesafe(pos);
        }
        public quaternion ToEuler(Color color)
        {
            return quaternion.Euler(color.r, color.g, color.b);
        }
        public float3 Tofloat3(Color color)
        {
            return math.float3(color.r, color.g, color.b);
        }
        public bool InCylinder(Color color, float3 pos , float radius, float minHeight, float maxHeight)
        {
            pos.x -= color.r;
            pos.y -= color.g;
            pos.z -= color.b;

            if (pos.x * pos.x + pos.z * pos.z <= radius * radius)
            {
                return pos.y >= minHeight && pos.y <= maxHeight;
            }
            else
            {
                return false;
            }
        }
        public bool InCylinder(Color color, float3 pos, float radius, float AbsHeight)
        {
            return InCylinder(color, pos, radius, -AbsHeight, AbsHeight);
        }
    }
}
