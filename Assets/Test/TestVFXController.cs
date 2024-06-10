using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.VFX;

[ExecuteAlways]
public class TestVFXController : MonoBehaviour
{
    public VisualEffect effect;
    public VisualEffectAsset effectAsset;

    public Texture2D texture;

    private int amount = 1000;
    public int Amount
    {
        get => amount;
        set 
        {
            OnChangeAmount(amount, value);
            amount = value;
        }
    }

    public Color[] Positions;
    public Vector3 offset;
    private void OnDisable()
    {
        DestroyImmediate(texture);//--- 미사용시 파괴해야됨 - 메모리 누수
    }

    // Update is called once per frame
    void Update()
    {
        if (effect != null)
        {
            // Mathf.Ceil(sqrt(Amount)) 한후 , 2의 배수인 공배수 구하기

            //-------- 이펙트마다 갯수 제한을 만개 정도로 하고 , 이펙트를 동시 실행 해야됨

            GetPositionMapSize(Amount, out var width, out var height);

            if (texture == null)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);//---mipChain -> 썸네일 처럼 동일한 이미지의 낮은 해상도 텍스쳐
                Positions = new Color[width * height];
            }
            Positions ??= new Color[width * height];

            for (int i = 0; i < amount; i++)
            {
                //texture.SetPixel(i % width, i / width, new Color(i % width,  i / width, 0) * 0.1f);
                Positions[i] = new Color(i % width, i / width, 0) * 0.1f + new Color(offset.x, offset.y, offset.z, 0);
            }
            texture.SetPixels(Positions, 0);// Color32 쓰는게 빠르다고 하는대? -> 정수값을 사용해서

            texture.Apply();
            effect.SetTexture("PositionMap", texture);

            // 갯수 증가시 이팩트 재시작 필요 , 감소는 필요X
        }
    }

    public bool GetPositionMapSize(int Amount, out int width, out int height)
    {
        if (Amount <= 0)
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
    public void OnChangeAmount(int pre , int vaule)
    {
        GetPositionMapSize(vaule, out var width, out var height);

        texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);
        Positions = new Color[width * height];

        effect.SetInt("Amount", vaule);

        effect.SetInt("PositionMapWidth", width);

        if (pre < vaule)
        {
            effect.Reinit();
        }
    }
}
[CustomEditor(typeof(TestVFXController))]
public class TestVFXControllerEditor : Editor
{
    TestVFXController onwer;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        
        if (onwer == null)
            onwer = target as TestVFXController;


        onwer.Amount = EditorGUILayout.IntField("Amount", onwer.Amount);
    }
}