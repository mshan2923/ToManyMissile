using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Entities.UniversalDelegates;
using Unity.Mathematics;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

[ExecuteAlways]
public class VfxController : MonoBehaviour
{
    private static VfxController instance;
    public static VfxController Instance
    {
        get => instance;
        set => instance = value;
    }

    [SerializeField] private List<Texture2D> positionTextures;
    public List<Texture2D> PositionTextures
    {
        get => positionTextures;
        private set => positionTextures = value;
    }

    [SerializeField] private List<Texture2D> velocityTextures;
    public List<Texture2D> VelocityTextures
    {
        get => velocityTextures;
        private set => velocityTextures = value;
    }

    public struct EffectAssetData
    {
        public VisualEffectAsset asset;
        public GameObject origin;
    }
    [System.Serializable]
    public class EffectData
    {
        public VisualEffect Effect;
        public bool isPlaying;

        public EffectData(VisualEffect effect, bool isPlaying = false)
        {
            Effect = effect;
            this.isPlaying = isPlaying;
        }

        public void IsPlaying(bool vaule)
        {
            this.isPlaying = vaule;
        }

    }

    public List<Color[]> positionMapData = new ();
    public List<Color[]> velocityMapData = new ();
    public Dictionary<int, EffectAssetData> EffectAssets = new();


    [SerializeField] private List<EffectData> effectDatas = new();
    public List<EffectData> EffectDatas
    {
        get => effectDatas;
        private set => effectDatas = value;
    }


    [SerializeField] private List<float> effectAge = new();
    public List<float> EffectAge
    {
        get => effectAge;
        private set => effectAge = value;
    }

    public delegate void EndEffectDelegate(int EffectIndex);
    public EndEffectDelegate EndEffect;

    public bool AutoStart = false;
    public bool LoopPlay = false;
    public float PlayRate = 1f;

    private void OnEnable()
    {
        Debug.Log("Enable");
        Instance = Instance != null ? Instance : this;

        EffectAssets.Clear();

        EndEffect = OnEndEffect;
    }
    private void OnDisable()
    {

    }
    void OnEndEffect(int effectIndex)
    {
        StopEffect(effectIndex);
    }
    private void Update()
    {
        if (EffectDatas.Count <= 0)
            return;

        for (int i = 0; i < EffectDatas.Count; i++)
        {
            if (EffectAge[i] < 0)//(EffectDatas[i].isPlaying == false) <--- 이걸로 하면 왜 Loop 안됨??
                continue;

            effectAge[i] += Time.deltaTime * PlayRate;
        }
    }
    //================= VFX DataType이 Particle 이면 Capacity가 원하는 총합 갯수보다 많아야 정상 출력 , ParticleStrip 이면 aliveParticleCount이 항상 0
    //  ParticleStrip 으로 하고 , 도착했는지 확인해 실행중인지 결정 (NativeArray<bool> 으로 내보낸후 전부 도착했는지 확인후 트래일 수명 끝나면 종료)

    #region DataTexture
    public void ClearDataTextures()
    {
        positionTextures.Clear();
        velocityTextures.Clear();
    }
    public bool GetDataMapSize(int amount, out int width, out int height)
    {
        if (amount <= 0)
        {
            width = 0;
            height = 0;
            return false;
        }
        int widthStep = Mathf.CeilToInt(Mathf.Log(Mathf.Sqrt(amount), 2));
        width = Mathf.RoundToInt(Mathf.Pow(2, widthStep));
        int heightStep = Mathf.CeilToInt(Mathf.Log((float)amount / width, 2));
        height = Mathf.RoundToInt(Mathf.Pow(2, heightStep));

        return true;
    }

    public int AddDataTexture(int MaxAmount)
    {
        if (GetDataMapSize(MaxAmount, out var width, out var height))
        {
            var posTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            var veloTex = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
            PositionTextures.Add(posTex);
            VelocityTextures.Add(veloTex);
            return PositionTextures.Count - 1;
        }

        return -1;
    }

    public bool ApplyPositionMap(int index, Color[] data)
    {
        if (index < 0 || index >= PositionTextures.Count)
            return false;

        PositionTextures[index].SetPixels(data);
        PositionTextures[index].Apply();
        return true;
    }
    public bool ApplyVelocityMap(int index, Color[] data)
    {
        if (index < 0 || index >= VelocityTextures.Count)
            return false;

        VelocityTextures[index].SetPixels(data);
        VelocityTextures[index].Apply();
        return true;
    }

    public int2 GetTextureSize(int index)
    {
        if (index < 0 || index >= PositionTextures.Count)
            return int2.zero;

        var size = positionTextures[index].Size();
        return new int2(Mathf.RoundToInt(size.x), Mathf.RoundToInt(size.y));
    }
    public int GetDataMapArraySize(int index)
    {
        var size = GetTextureSize(index);

        return size.x * size.y;
    }
    #endregion

    public bool AddVisualEffectAsset(VisualEffect effect , GameObject prefab)
    {
        if (!EffectAssets.ContainsKey(effect.visualEffectAsset.GetInstanceID()))
        {
            EffectAssets.Add(effect.visualEffectAsset.GetInstanceID(), new EffectAssetData
            {
                asset = effect.visualEffectAsset,
                origin = prefab
            });
            return true;
        }
        return false;
    }

    #region VisualEffect
    public VisualEffect SpawnEffect(int effectAssetId, int Amount)
    {
        if (EffectAssets.TryGetValue(effectAssetId, out var effect))
        {
            var spawned = VisualEffect.Instantiate(effect.origin, this.transform);
            spawned.name = $"Test Effect {EffectDatas.Count}";

            var result = spawned.GetComponent<VisualEffect>();
            result.visualEffectAsset = effect.asset;
            result.Stop();


            int dataMapIndex = AddDataTexture(Amount);
            int dataMapSize = GetDataMapArraySize(dataMapIndex);
            positionMapData.Add(new Color[dataMapSize]);
            velocityMapData.Add(new Color[dataMapSize]);
            EffectDatas.Add(new EffectData(result , !AutoStart));
            EffectAge.Add(AutoStart ? 0f : -1f);
            return result;
        }

        return null;
    }
    public void PlayEffect(EcsVfxComponent ecsVfx)
    {
        int effectIndex = ecsVfx.PlayEffectIndex;

        EffectDatas[effectIndex].Effect.SetTexture("PositionMap", PositionTextures[ecsVfx.DataImageIndex]);
        EffectDatas[effectIndex].Effect.SetTexture("VelocityMap", Instance.VelocityTextures[ecsVfx.DataImageIndex]);
        EffectDatas[effectIndex].Effect.SetFloat("LifeTime", ecsVfx.LifeTime);
        EffectDatas[effectIndex].Effect.SetInt("Amount", ecsVfx.Amount);
        EffectDatas[effectIndex].Effect.SetInt("PositionMapWidth", GetTextureSize(ecsVfx.DataImageIndex).x);


        effectAge[effectIndex] = 0f;

        Array.Fill(positionMapData[effectIndex], Color.black);
        Array.Fill(velocityMapData[effectIndex], new Color(0,0,0,0));

        EffectDatas[effectIndex].Effect.Reinit();
        EffectDatas[effectIndex].Effect.Play();
        EffectDatas[effectIndex].IsPlaying(true);
    }
    public void StopEffect(int effectIndex)
    {
        effectAge[effectIndex] = LoopPlay ? 0f : -1f;
        EffectDatas[effectIndex].Effect.Stop();
        EffectDatas[effectIndex].IsPlaying(false);
        //OnLifeOutEffect?.Invoke(EffectDatas[effectIndex].Effect, effectIndex);
    }

    #endregion
}

[CustomEditor(typeof(VfxController))]
public class VfxControllerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        EditorGUILayout.BeginHorizontal();
        GUILayout.Label("Active");
        //EditorGUILayout.IntField(VfxController.Instance.ActiveEffects.Count);
        GUILayout.Label("Diabled");
        //EditorGUILayout.IntField(VfxController.Instance.EffectDatas.Count - VfxController.Instance.ActiveEffects.Count);
        EditorGUILayout.EndHorizontal();
    }
}