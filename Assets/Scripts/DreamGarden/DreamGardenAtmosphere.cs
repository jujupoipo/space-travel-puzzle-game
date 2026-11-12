using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

// a local volume follows the garden; planet 1 and shared scene lighting stay untouched
public sealed class DreamGardenAtmosphere : MonoBehaviour
{
    public Volume volume;
    public GameObject bloomDetails;
    public GameObject ruinDetails;
    public ParticleSystem motes;
    public DreamPuzzleController puzzle;
    private Camera view;
    private Color previousBackground;
    private CameraClearFlags previousClear;
    private bool ownsBackground;
    private ColorAdjustments colors;
    private Bloom glow;
    private VolumeProfile ownedProfile;
    private bool bloom = true;
    private float blend = 1f;

    private void Start()
    {
        if (puzzle != null && puzzle.player != null)
        {
            PlayerController actor = puzzle.player.GetComponent<PlayerController>();
            if (actor != null && actor.cameraTransform != null) view = actor.cameraTransform.GetComponent<Camera>();
        }
        if (volume != null)
        {
            ownedProfile = volume.profile;
            ownedProfile.TryGet(out colors);
            ownedProfile.TryGet(out glow);
        }
    }

    public void SetPhase(bool isBloom)
    {
        bloom = isBloom;
        if (bloomDetails != null) bloomDetails.SetActive(bloom);
        if (ruinDetails != null) ruinDetails.SetActive(!bloom);
        if (motes != null)
        {
            var main = motes.main;
            main.startColor = bloom ? new Color(1f, .67f, .79f, .6f) : new Color(.49f, .67f, 1f, .45f);
        }
    }

    private void Update()
    {
        blend = Mathf.MoveTowards(blend, bloom ? 1f : 0f, Time.deltaTime * .8f);
        if (colors != null)
        {
            colors.colorFilter.value = Color.Lerp(new Color(.66f, .75f, 1f), new Color(1f, .93f, .88f), blend);
            colors.saturation.value = Mathf.Lerp(-32f, 8f, blend);
        }
        if (glow != null) glow.intensity.value = Mathf.Lerp(.18f, .3f, blend);
        if (view != null && puzzle != null && puzzle.PlayerInGarden)
        {
            if (!ownsBackground)
            {
                previousBackground = view.backgroundColor; previousClear = view.clearFlags; ownsBackground = true;
            }
            view.clearFlags = CameraClearFlags.SolidColor;
            view.backgroundColor = Color.Lerp(new Color(.025f, .035f, .07f), new Color(.11f, .075f, .15f), blend);
        }
        else RestoreBackground();
    }

    private void RestoreBackground()
    {
        if (!ownsBackground || view == null) return;
        view.backgroundColor = previousBackground; view.clearFlags = previousClear; ownsBackground = false;
    }

    private void OnDisable() => RestoreBackground();

    private void OnDestroy()
    {
        RestoreBackground();
        if (ownedProfile != null) Destroy(ownedProfile);
    }
}
