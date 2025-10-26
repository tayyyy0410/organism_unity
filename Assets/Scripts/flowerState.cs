using System;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;
using Random = System.Random;

public class flowerState : MonoBehaviour
{
    private enum State {Bloom,Wilt,Seed, Despawn}

    [SerializeField] private State state = State.Bloom;
    
    
    [Header("Life and Nectar")]
    [SerializeField] private float lifeSpan = 100f;
    [SerializeField] private float nectarMax = 100f;
    [SerializeField] private float nectarRegenPerSec = 0f;
    [SerializeField] private float attractRadius = 6f;
    [SerializeField] private float harvestInvincibleTime = 0f;

    private float nectar;
    private float birthTime;
    private float stateStartTime;
    
    
    [Header("Wilt")]
    [SerializeField] private float wiltDuration = 10f;
    [SerializeField] private Color wiltColor = new Color(0.6f, 0.6f, 0.6f, 0.85f);
    [SerializeField] private float wiltScale = 0.8f;

    [Header("Seed")] 
    [SerializeField] private bool spawnOnSeed = true;
    [SerializeField] private GameObject seedPrefab;
    [SerializeField] private int seedSpawnCount = 1;
    [SerializeField] private float seedSpawnRadius = 4f;
    [SerializeField] private float seedPause = 0.5f;

    [Header("Movement")] 
    [SerializeField] private float swayAmplitude = 3f;
    [SerializeField] private float swayFrequency = 0.5f;
    [SerializeField] private float bobAmplitude = 0.05f;
    [SerializeField] private float bobFrequency = 1.5f;

    [Header("Sprites")] 
    [SerializeField] private Sprite bloomSprite;
    [SerializeField] private Sprite wiltSprite;

    private SpriteRenderer sr;
    private Vector3 basePos;
    private float baseZRot;
    private bool isBlooming;


    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePos = transform.position;
        baseZRot = transform.localEulerAngles.z;
        birthTime = Time.time;
        nectar = nectarMax;
        EnterState(State.Bloom);
        

    }

    void Start()
    {
        
    }

    void Update()
    {
        switch (state)
        {
            case State.Bloom: BloomUpdate(); break;
            case State.Wilt: WiltUpdate(); break;
            case State.Seed: SeedUpdate(); break;
            case State.Despawn: break;
            
        }

        if (state == State.Bloom && Time.time - birthTime >= lifeSpan)
        {
            ChangeState(State.Wilt); 
        }
    }

    void EnterState(State next)
    {
        state = next;
        stateStartTime = Time.time;


        switch (state)
        {
            case State.Bloom:
                isBlooming = true;
                if (sr && bloomSprite)
                {
                    sr.sprite = bloomSprite;
                }

                if (sr)
                {
                    sr.color = Color.white;
                }
                transform.localScale = Vector3.one;
                break;
            case State.Wilt:
                isBlooming = false;
                if (sr && wiltSprite)
                {
                    sr.sprite = wiltSprite;
                }

                break;
            case State.Seed:
                if (spawnOnSeed && seedPrefab)
                {
                    for (int i = 0; i < Mathf.Max(0, seedSpawnCount); i++)
                    {
                        Vector2 dir = UnityEngine.Random.insideUnitCircle.normalized;
                        float dist = UnityEngine.Random.Range(seedSpawnRadius*0.4f, seedSpawnRadius);
                        Vector3 pos = transform.position + new Vector3(dir.x, dir.y, 0)*dist;
                        Instantiate(seedPrefab,pos,Quaternion.identity);
                        
                        
                    }
                }
                break;
            case State.Despawn:
                Destroy(gameObject);
                break;
            
            
        }
        
    }

    void ChangeState(State next)
    {
        ExitState(state);
        EnterState(next);
        
    }

    void ExitState(State next)
    {
        
    }

    void BloomUpdate()
    {
        float t = Time.time;
        float z = baseZRot + Mathf.Sin(t * swayFrequency) * swayAmplitude;
        transform.rotation = Quaternion.Euler(0f, 0f, z);
        
        Vector3 p = basePos;
        p.y += Mathf.Sin(t * bobFrequency)*bobAmplitude;
        transform.position = p; 


        if (nectarRegenPerSec > 0f)
        {
            nectar = Mathf.Min(nectarMax, nectar + nectarRegenPerSec * Time.deltaTime);
        }

        if (nectar <= 0f)
        {
            ChangeState(State.Wilt);
        }
        
    }

    void WiltUpdate()
    {
        float t = Mathf.Clamp01(Time.time - stateStartTime) / Mathf.Max(0.0001f, wiltDuration);
        if (sr)
        {
            sr.color = Color.Lerp(sr.color, Color.white, t);
        }

        float s = Mathf.Lerp(1f, wiltScale, t);
        transform.localScale = new Vector3(s, s, 1f);
        
        float z = Mathf.LerpAngle(transform.eulerAngles.z, baseZRot, t);
        transform.rotation = Quaternion.Euler(0f, 0f, z);


        if (Time.time - stateStartTime >= wiltDuration)
        {
            ChangeState(State.Seed);
        }
       
        
            
    }

    void SeedUpdate()
    {
        float dt = Time.time - stateStartTime;
        float pulse = 1f + 0.05f * Mathf.Sin(dt * 12f) * Mathf.Exp(-dt * 2f); 
        transform.localScale = new Vector3(pulse, pulse, 1f);

        if (dt >= seedPause)
        {
            ChangeState(State.Despawn);
        }
           
    }
    
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.25f);               
        Gizmos.DrawWireSphere(transform.position, attractRadius);       
    }
    
    public bool HasNectar => isBlooming && nectar > 0f;                 // 供外部判断是否值得靠近

    public float Harvest(float amountPerSec, float dt)                  // 采蜜：每秒速率 * dt，返回本帧实际采到
    {
        if (Time.time - birthTime < harvestInvincibleTime)
        { 
            return 0f;   
        }
        if (!isBlooming || nectar <= 0f)
        {
            return 0f;               
        }

        float take = Mathf.Min(nectar, amountPerSec * dt);              
        nectar -= take;                                                 
        return take;                                                 
    }
}
