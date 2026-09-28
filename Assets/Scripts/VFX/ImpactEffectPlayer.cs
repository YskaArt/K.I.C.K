using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Reproduce efectos <see cref="ImpactEffect"/> en cualquier punto de la
/// escena. Es un singleton: cualquier script llama
/// <c>ImpactEffectPlayer.Ensure().Play(effect, posicion)</c> y listo, no
/// hace falta poner nada a mano en la escena (se crea solo la primera vez).
///
/// El artista 2D nunca toca este script: solo crea/edita assets
/// <see cref="ImpactEffect"/> y los arrastra donde se pidan. Los parametros
/// se vuelven a aplicar cada vez que se reproduce el efecto, asi que podés
/// ajustar los numeros del asset CON el juego corriendo y ver el resultado
/// en el proximo gol/fallo sin reiniciar.
/// </summary>
public class ImpactEffectPlayer : MonoBehaviour
{
    public static ImpactEffectPlayer Instance { get; private set; }

    private readonly Dictionary<ImpactEffect, ParticleSystem> pool = new Dictionary<ImpactEffect, ParticleSystem>();

    /// <summary>Devuelve el reproductor de la escena o crea uno al vuelo.</summary>
    public static ImpactEffectPlayer Ensure()
    {
        if (Instance != null) return Instance;

        var existing = FindAnyObjectByType<ImpactEffectPlayer>();
        if (existing != null) return existing;

        return new GameObject("ImpactEffectPlayer").AddComponent<ImpactEffectPlayer>();
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this);
            return;
        }

        Instance = this;
    }

    /// <summary>Reproduce el efecto en esa posicion del mundo.</summary>
    public void Play(ImpactEffect effect, Vector3 position)
    {
        if (effect == null) return;

        ParticleSystem ps = GetOrCreate(effect);
        Configure(ps, effect);

        ps.transform.position = position;
        ps.Play(true);

        if (effect.sfx != null && AudioManager.Instance != null)
        {
            AudioManager.Instance.PlaySfx(effect.sfx);
        }
    }

    private ParticleSystem GetOrCreate(ImpactEffect effect)
    {
        if (pool.TryGetValue(effect, out ParticleSystem existing) && existing != null)
        {
            return existing;
        }

        var go = new GameObject("Impact_" + effect.name);
        go.transform.SetParent(transform, false);

        ParticleSystem ps = go.AddComponent<ParticleSystem>();
        pool[effect] = ps;
        return ps;
    }

    private void Configure(ParticleSystem ps, ImpactEffect effect)
    {
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        ParticleSystem.MainModule main = ps.main;
        main.loop = false;
        main.playOnAwake = false;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startSpeed = new ParticleSystem.MinMaxCurve(effect.speedRange.x, effect.speedRange.y);
        main.startSize = new ParticleSystem.MinMaxCurve(effect.sizeRange.x, effect.sizeRange.y);
        main.startLifetime = new ParticleSystem.MinMaxCurve(effect.lifetimeRange.x, effect.lifetimeRange.y);
        main.gravityModifier = effect.gravityModifier;
        main.startRotation = new ParticleSystem.MinMaxCurve(
            -effect.startRotationRandomDegrees * Mathf.Deg2Rad,
            effect.startRotationRandomDegrees * Mathf.Deg2Rad);

        ParticleSystem.EmissionModule emission = ps.emission;
        emission.rateOverTime = 0f;
        emission.SetBursts(new[] { new ParticleSystem.Burst(0f, (short)effect.burstCount) });

        ParticleSystem.ShapeModule shape = ps.shape;
        shape.shapeType = ParticleSystemShapeType.Cone;
        shape.angle = effect.spreadAngle;
        shape.radius = 0.05f;

        ParticleSystem.ColorOverLifetimeModule colorOverLifetime = ps.colorOverLifetime;
        colorOverLifetime.enabled = true;
        colorOverLifetime.color = new ParticleSystem.MinMaxGradient(effect.colorOverLifetime);

        var renderer = ps.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Billboard;
        renderer.material = BuildMaterial(effect.particleSprite);
    }

    private static Material BuildMaterial(Sprite sprite)
    {
        var material = new Material(Shader.Find("Sprites/Default"));
        if (sprite != null)
        {
            material.mainTexture = sprite.texture;
        }
        return material;
    }
}
