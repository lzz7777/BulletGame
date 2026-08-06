using System;
using UnityEngine;

[RequireComponent(typeof(Renderer))]
public class SpineTintController : MonoBehaviour
{
    public bool Play = false;
    public Color WhiteColor = Color.white;
    public Color BlackColor = Color.black;

    Renderer renderer;
    MaterialPropertyBlock mpb;

    private bool _lastPlay;
    
    void Awake()
    {
        renderer = GetComponent<Renderer>();
        mpb = new MaterialPropertyBlock();
    }

    public void Reset()
    {
        Play = false;
        mpb.SetColor("_Color", Color.white);
        mpb.SetColor("_Black", Color.black);
        renderer.SetPropertyBlock(mpb);
    }

    void Update()
    {
        if (Play)
        {
            mpb.SetColor("_Color", WhiteColor);
            mpb.SetColor("_Black", BlackColor);
            renderer.SetPropertyBlock(mpb);
        }
        else if (_lastPlay)
        {
            renderer.SetPropertyBlock(null);
        }
        _lastPlay = Play;
    }
}