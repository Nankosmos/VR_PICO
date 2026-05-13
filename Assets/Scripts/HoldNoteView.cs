using UnityEngine;

public class HoldNoteView : MonoBehaviour
{
    public Transform startPoint;
    public Transform endPoint;
    public GameObject startOrb;
    public GameObject endOrb;
    public GameObject startHitEffectPrefab;
    public GameObject holdLoopEffectPrefab;
    public bool useBuiltInHoldLoopEffect = true;
    public AudioClip startHitSound;

    private LineRenderer line;
    private GameObject startOrbInstance;
    private GameObject endOrbInstance;
    private GameObject holdLoopEffectInstance;
    private ParticleSystem builtInHoldLoopParticles;
    private ParticleSystem[] holdLoopParticles;

    void Awake()
    {
        line = GetComponent<LineRenderer>();

        if (startOrb == null && startPoint != null)
        {
            startOrb = FindOrbChild(startPoint, "StartOrb");
        }

        if (endOrb == null && endPoint != null)
        {
            endOrb = FindOrbChild(endPoint, "EndOrb");
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
        GameObject viewStartHitEffectPrefab,
        GameObject viewHoldLoopEffectPrefab,
        bool viewUseBuiltInHoldLoopEffect,
        AudioClip viewStartHitSound
    )
    {
        startPoint = viewStartPoint;
        endPoint = viewEndPoint;
        startOrb = FindOrbChild(startPoint, "StartOrb") ?? startOrb;
        endOrb = FindOrbChild(endPoint, "EndOrb") ?? endOrb;
        startHitEffectPrefab = viewStartHitEffectPrefab;
        holdLoopEffectPrefab = viewHoldLoopEffectPrefab;
        useBuiltInHoldLoopEffect = viewUseBuiltInHoldLoopEffect;
        startHitSound = viewStartHitSound;

        EnsureOrbInstances();
        HideLegacyOrbChild(startPoint, startOrbInstance, "StartOrb");
        HideLegacyOrbChild(endPoint, endOrbInstance, "EndOrb");
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
        if (startOrbInstance != null)
        {
            startOrbInstance.SetActive(active);
            return;
        }

        if (startOrb != null && startOrb.scene.IsValid())
        {
            startOrb.SetActive(active);
        }
    }

    public void SetEndOrbActive(bool active)
    {
        if (endOrbInstance != null)
        {
            endOrbInstance.SetActive(active);
            return;
        }

        if (endOrb != null && endOrb.scene.IsValid())
        {
            endOrb.SetActive(active);
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
            ConfigureHoldLoopParticles(holdLoopEffectInstance);
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
        holdLoopParticles = new[] { builtInHoldLoopParticles };
    }

    public void UpdateHoldLoopEffect(Vector3 position)
    {
        if (holdLoopEffectInstance == null) return;

        holdLoopEffectInstance.transform.position = position;
        KeepHoldLoopParticlesPlaying();
    }

    public void StopHoldLoopEffect()
    {
        if (holdLoopParticles != null)
        {
            foreach (ParticleSystem particle in holdLoopParticles)
            {
                if (particle == null) continue;
                particle.Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }

            holdLoopParticles = null;
        }

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

    void ConfigureHoldLoopParticles(GameObject effectRoot)
    {
        holdLoopParticles = effectRoot.GetComponentsInChildren<ParticleSystem>(true);

        foreach (ParticleSystem particle in holdLoopParticles)
        {
            if (particle == null) continue;

            ParticleSystem.MainModule main = particle.main;
            main.loop = true;
            main.stopAction = ParticleSystemStopAction.None;

            particle.Clear(true);
            particle.Play(true);
        }
    }

    void KeepHoldLoopParticlesPlaying()
    {
        if (holdLoopParticles == null) return;

        foreach (ParticleSystem particle in holdLoopParticles)
        {
            if (particle == null) continue;

            ParticleSystem.MainModule main = particle.main;
            if (!main.loop)
            {
                main.loop = true;
            }

            if (!particle.isPlaying)
            {
                particle.Play(true);
            }
        }
    }

    public void UpdateLineRenderer()
    {
        if (line == null) return;
        if (startPoint == null || endPoint == null) return;

        line.SetPosition(0, startPoint.position);
        line.SetPosition(1, endPoint.position);
    }

    void EnsureOrbInstances()
    {
        startOrbInstance = EnsureOrbInstance(startOrb, startPoint, startOrbInstance, "StartOrbVisual");
        endOrbInstance = EnsureOrbInstance(endOrb, endPoint, endOrbInstance, "EndOrbVisual");
    }

    GameObject EnsureOrbInstance(GameObject orbSource, Transform parent, GameObject existingInstance, string instanceName)
    {
        if (parent == null) return existingInstance;

        if (orbSource == null)
        {
            return existingInstance;
        }

        if (orbSource.scene.IsValid())
        {
            DisableTapBehaviour(orbSource);
            return orbSource;
        }

        if (existingInstance != null)
        {
            return existingInstance;
        }

        GameObject instance = Instantiate(orbSource, parent);
        instance.name = instanceName;
        instance.transform.localPosition = Vector3.zero;
        instance.transform.localRotation = Quaternion.identity;
        instance.transform.localScale = orbSource.transform.localScale;

        DisableTapBehaviour(instance);
        return instance;
    }

    void DisableTapBehaviour(GameObject instance)
    {
        NoteOrb[] noteOrbs = instance.GetComponentsInChildren<NoteOrb>(true);
        foreach (NoteOrb noteOrb in noteOrbs)
        {
            noteOrb.enabled = false;
        }

        HoldStartTrigger[] holdStartTriggers = instance.GetComponentsInChildren<HoldStartTrigger>(true);
        foreach (HoldStartTrigger holdStartTrigger in holdStartTriggers)
        {
            holdStartTrigger.enabled = false;
        }

        Collider[] colliders = instance.GetComponentsInChildren<Collider>(true);
        foreach (Collider collider in colliders)
        {
            collider.enabled = false;
        }

        Rigidbody[] rigidbodies = instance.GetComponentsInChildren<Rigidbody>(true);
        foreach (Rigidbody rigidbody in rigidbodies)
        {
            rigidbody.isKinematic = true;
            rigidbody.useGravity = false;
            rigidbody.detectCollisions = false;
        }
    }

    void HideLegacyOrbChild(Transform parent, GameObject activeOrb, string childName)
    {
        if (parent == null) return;

        Transform legacyChild = parent.Find(childName);
        if (legacyChild == null) return;
        if (activeOrb != null && legacyChild.gameObject == activeOrb) return;

        legacyChild.gameObject.SetActive(false);
    }

    GameObject FindOrbChild(Transform parent, string childName)
    {
        if (parent == null) return null;

        GameObject inactiveMatch = null;
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);
            if (child.name != childName) continue;

            if (child.gameObject.activeSelf)
            {
                return child.gameObject;
            }

            inactiveMatch = child.gameObject;
        }

        return inactiveMatch;
    }
}
