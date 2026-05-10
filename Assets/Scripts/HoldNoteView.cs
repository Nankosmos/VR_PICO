using UnityEngine;

public class HoldNoteView : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public GameObject startOrb;
    public GameObject startHitEffectPrefab;
    public GameObject holdLoopEffectPrefab;
    public bool useBuiltInHoldLoopEffect = true;
    public AudioClip startHitSound;

    private LineRenderer line;
    private GameObject holdLoopEffectInstance;
    private ParticleSystem builtInHoldLoopParticles;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        if (startOrb == null && startPoint != null)
        {
            Transform startOrbTransform = startPoint.Find("StartOrb");
            if (startOrbTransform != null)
            {
                startOrb = startOrbTransform.gameObject;
            }
        }

        if (line != null)
        {
            line.positionCount = 2;
            line.useWorldSpace = true;
        }
    }

    public void Configure(
        Transform viewStartPoint,
        Transform viewEndPoint,
        GameObject viewStartOrb,
        GameObject viewStartHitEffectPrefab,
        GameObject viewHoldLoopEffectPrefab,
        bool viewUseBuiltInHoldLoopEffect,
        AudioClip viewStartHitSound
    )
    {
        startPoint = viewStartPoint;
        endPoint = viewEndPoint;
        startOrb = viewStartOrb;
        startHitEffectPrefab = viewStartHitEffectPrefab;
        holdLoopEffectPrefab = viewHoldLoopEffectPrefab;
        useBuiltInHoldLoopEffect = viewUseBuiltInHoldLoopEffect;
        startHitSound = viewStartHitSound;
    }

    public void SetEndpoints(Vector3 startPosition, Vector3 endPosition)
    {
        if (startPoint != null)
        {
            startPoint.position = startPosition;
        }

        if (endPoint != null)
        {
            endPoint.position = endPosition;
        }

        UpdateLineRenderer();
    }

    public void SetStartOrbActive(bool active)
    {
        if (startOrb != null)
        {
            startOrb.SetActive(active);
        }
    }

    public void PlayStartHitFeedback(Vector3 position)
    {
        if (startHitEffectPrefab != null)
        {
            Instantiate(startHitEffectPrefab, position, Quaternion.identity);
        }

        if (startHitSound != null)
        {
            AudioPlayback.PlaySfx(startHitSound);
        }
    }

    public void StartHoldLoopEffect(Vector3 position)
    {
        StopHoldLoopEffect();

        if (holdLoopEffectPrefab != null)
        {
            holdLoopEffectInstance = Instantiate(holdLoopEffectPrefab, position, Quaternion.identity);
            return;
        }

        if (!useBuiltInHoldLoopEffect)
        {
            return;
        }

        holdLoopEffectInstance = new GameObject("HoldLoopEffect");
        holdLoopEffectInstance.transform.position = position;

        builtInHoldLoopParticles = holdLoopEffectInstance.AddComponent<ParticleSystem>();

        ParticleSystem.MainModule main = builtInHoldLoopParticles.main;
        main.loop = true;
        main.startLifetime = 0.35f;
        main.startSpeed = 0.15f;
        main.startSize = 0.08f;
        main.simulationSpace = ParticleSystemSimulationSpace.World;

        ParticleSystem.EmissionModule emission = builtInHoldLoopParticles.emission;
        emission.rateOverTime = 45f;

        ParticleSystem.ShapeModule shape = builtInHoldLoopParticles.shape;
        shape.shapeType = ParticleSystemShapeType.Sphere;
        shape.radius = 0.08f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = builtInHoldLoopParticles.colorOverLifetime;
        colorOverLifetime.enabled = true;

        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[]
            {
                new GradientColorKey(Color.white, 0f),
                new GradientColorKey(Color.cyan, 1f)
            },
            new GradientAlphaKey[]
            {
                new GradientAlphaKey(0.9f, 0f),
                new GradientAlphaKey(0f, 1f)
            }
        );
        colorOverLifetime.color = gradient;

        builtInHoldLoopParticles.Play();
    }

    public void UpdateHoldLoopEffect(Vector3 position)
    {
        if (holdLoopEffectInstance == null) return;

        holdLoopEffectInstance.transform.position = position;
    }

    public void StopHoldLoopEffect()
    {
        if (builtInHoldLoopParticles != null)
        {
            builtInHoldLoopParticles.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            builtInHoldLoopParticles = null;
        }

        if (holdLoopEffectInstance != null)
        {
            Destroy(holdLoopEffectInstance, 1f);
            holdLoopEffectInstance = null;
        }
    }

    public void UpdateLineRenderer()
    {
        if (line == null) return;
        if (startPoint == null || endPoint == null) return;

        line.SetPosition(0, startPoint.position);
        line.SetPosition(1, endPoint.position);
    }
}
