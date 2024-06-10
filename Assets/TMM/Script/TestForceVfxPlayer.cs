using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.VFX;

public class TestForceVfxPlayer : MonoBehaviour
{
    public VisualEffect effect;

    void Start()
    {
        effect = GetComponent<VisualEffect>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
[CustomEditor(typeof(TestForceVfxPlayer))]
public class TestForceVfxPlayerEditor : Editor
{
    TestForceVfxPlayer onwer;
    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();

        if (onwer == null)
        {
            onwer = target as TestForceVfxPlayer;
        }

        if (GUILayout.Button("Play"))
        {
            onwer.effect.Play();
        }
    }
}
