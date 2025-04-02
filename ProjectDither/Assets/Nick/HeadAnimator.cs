using UnityEngine;
using UnityEngine.Rendering;

public class HeadAnimator : MonoBehaviour
{
    [SerializeField]
    Texture[] textures;

    int num = 0;
    float time = 0;
    private void Update()
    {
        time += Time.deltaTime;
        if (time > .75f)
        {
            time = 0;
            updateTexture();
        }
    }

    void updateTexture()
    {
        Debug.Log(textures[num]);
        GetComponent<Renderer>().material.SetTexture("_BaseMap", textures[num]);
        //GetComponent<Renderer>().material.SetTexture("_EmissionMap", textures[num]);
        num++;
        if (num >= textures.Length)
        {
            num = 0;
        }
    }
}
