using FishNet.Object;
using UnityEngine;

public class HitController : NetworkBehaviour
{
    public float lifeTime;
    public float disappearSpeed;

    SpriteRenderer spriteRenderer;

    void Start()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        if (lifeTime > 0)
        {
            lifeTime -= Time.deltaTime;
            return;
        }

        Color color = spriteRenderer.color;
        color.a = Mathf.LerpUnclamped(color.a, 0, disappearSpeed * Time.deltaTime);

        spriteRenderer.color = color;

        if (color.a < 0.01f)
        {
            Despawn();
        }

    }
}
