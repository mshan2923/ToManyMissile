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
        DestroyImmediate(texture);//--- �̻��� �ı��ؾߵ� - �޸� ����
    }

    // Update is called once per frame
    void Update()
    {
        if (effect != null)
        {
            // Mathf.Ceil(sqrt(Amount)) ���� , 2�� ����� ����� ���ϱ�

            //-------- ����Ʈ���� ���� ������ ���� ������ �ϰ� , ����Ʈ�� ���� ���� �ؾߵ�

            GetPositionMapSize(Amount, out var width, out var height);

            if (texture == null)
            {
                texture = new Texture2D(width, height, TextureFormat.RGBAFloat, false);//---mipChain -> ����� ó�� ������ �̹����� ���� �ػ� �ؽ���
                Positions = new Color[width * height];
            }
            Positions ??= new Color[width * height];

            for (int i = 0; i < amount; i++)
            {
                //texture.SetPixel(i % width, i / width, new Color(i % width,  i / width, 0) * 0.1f);
                Positions[i] = new Color(i % width, i / width, 0) * 0.1f + new Color(offset.x, offset.y, offset.z, 0);
            }
            texture.SetPixels(Positions, 0);// Color32 ���°� �����ٰ� �ϴ´�? -> �������� ����ؼ�

            texture.Apply();
            effect.SetTexture("PositionMap", texture);

            // ���� ������ ����Ʈ ����� �ʿ� , ���Ҵ� �ʿ�X
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