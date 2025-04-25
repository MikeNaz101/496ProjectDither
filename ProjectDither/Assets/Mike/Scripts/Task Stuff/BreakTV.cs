using UnityEngine;
using UnityEngine.UI; // Required for RawImage

public class BreakTV : Task
{
    public AudioClip staticSound;
    public AudioClip breakSound;
    private AudioSource tvAudioSource;
    public RawImage tvRawImage; // Reference to the RawImage
    public float staticIntensityIncrease = 0.2f;
    public float maxStaticIntensity = 1.0f;
    public int hitsToBreak = 3;
    private int hitCount = 0;
    private bool isBroken = false;
    private Material tvMaterial;

    void Start()
    {
        tvAudioSource = GetComponent<AudioSource>();

        if (tvRawImage != null)
        {
            tvMaterial = tvRawImage.material; // Get the material instance from RawImage
        }
        else
        {
            Debug.LogError("BreakTheTV: No RawImage assigned for the TV screen!");
            enabled = false;
            return;
        }

        if (tvAudioSource == null)
        {
            Debug.LogError("BreakTheTV: No AudioSource found on the TV!");
            enabled = false;
            return;
        }
        if (taskName == null)
        {
            taskName = "Break the TV";
        }

        // Ensure initial intensity is 0
        if (tvMaterial != null && tvMaterial.HasProperty("_Intensity"))
        {
            tvMaterial.SetFloat("_Intensity", 0f);
        }
        else
        {
            Debug.LogWarning("BreakTheTV: Shader does not have an '_Intensity' property or no material on RawImage!");
        }
    }

    public override void Complete()
    {
        if (!isBroken)
        {
            Debug.Log("Task Completed: Break the TV!");
            isBroken = true;
        }
    }

    public void HitTV()
    {
        hitCount++;
        Debug.Log($"TV Hit {hitCount}!");

        if (hitCount == 1)
        {
            tvAudioSource.clip = staticSound;
            tvAudioSource.Play();
            // Increase static intensity on the first hit
            if (tvMaterial != null && tvMaterial.HasProperty("_Intensity"))
            {
                float newIntensity = Mathf.Min(tvMaterial.GetFloat("_Intensity") + staticIntensityIncrease, maxStaticIntensity);
                tvMaterial.SetFloat("_Intensity", newIntensity);
            }
        }
        else if (hitCount >= hitsToBreak)
        {
            tvAudioSource.clip = breakSound;
            tvAudioSource.Play();
            Destroy(tvAudioSource);
            Debug.Log("TV Broken!");
            Complete();
            TaskCompleted();
            // Add any visual breaking effects for UI (e.g., enable/disable other UI elements)
        }
        else
        {
            // Increase static intensity with each subsequent hit before breaking
            if (tvMaterial != null && tvMaterial.HasProperty("_Intensity"))
            {
                float newIntensity = Mathf.Min(tvMaterial.GetFloat("_Intensity") + staticIntensityIncrease, maxStaticIntensity);
                tvMaterial.SetFloat("_Intensity", newIntensity);
            }
            // You might want to add a sound for intermediate hits here
        }
    }

    void OnMouseDown()
    {
        HitTV();
    }
}