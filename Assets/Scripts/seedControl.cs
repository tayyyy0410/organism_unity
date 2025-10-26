using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class seedControl : MonoBehaviour
{
    private enum State { Seed, Grow, SpawnFlower, Despawn }
    [SerializeField] private State state = State.Seed;

    [Header("Germination")]
    [SerializeField] private float germinationDelay = 2.0f;
    [SerializeField] private float growDuration = 1.0f;
    [SerializeField] private Vector2 growScaleRange = new Vector2(0.6f, 1.0f);

    [Header("prefab")]
    [SerializeField] private GameObject flowerPrefab;

    [SerializeField] private Sprite growSprite;
    [SerializeField] private Sprite seedSprite;

    private float stateStart;
    private Vector3 baseScale;
    private Vector3 basePos;          
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        basePos = transform.position;  
        Enter(State.Seed);
    }

    void Update()
    {
        switch (state)
        {
            case State.Seed:        SeedUpdate();        break;
            case State.Grow:        GrowUpdate();        break;
            case State.SpawnFlower: SpawnFlower();       break;
            case State.Despawn:                              break;
        }
    }

    void Enter(State s)
    {
        state = s;
        stateStart = Time.time;

        switch (state)
        {
            case State.Seed:
                transform.localScale = baseScale * growScaleRange.x;
                if (sr && seedSprite) sr.sprite = seedSprite;   
                break;

            case State.Grow:
                
                transform.position = basePos;
                if (sr && growSprite) sr.sprite = growSprite;   
                break;
        }
    }

    void Change(State s) => Enter(s);

    void SeedUpdate()
    {
     
        float offset = Mathf.Sin(Time.time * 2f) * 0.02f;
        transform.position = basePos + new Vector3(0, offset, 0);

        if (Time.time - stateStart >= germinationDelay)
            Change(State.Grow);
    }

    void GrowUpdate()
    {
        float t = Mathf.Clamp01((Time.time - stateStart) / Mathf.Max(0.0001f, growDuration));
        float s = Mathf.Lerp(growScaleRange.x, growScaleRange.y, t);
        transform.localScale = baseScale * s;

        if (t >= 1f) Change(State.SpawnFlower);
    }

    void SpawnFlower()
    {
        if (flowerPrefab)
            Instantiate(flowerPrefab, transform.position, Quaternion.identity);
        
        Destroy(gameObject);
    }
}
