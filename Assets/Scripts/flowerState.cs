using UnityEngine; 


public class flowerState : MonoBehaviour
{

    private enum State { Bloom, Wilt, Seed, Despawn } 
    [SerializeField] private State state = State.Bloom; 


    [Header("Life")]
    [SerializeField] private float lifeSpan = 100f; 
    private float birthTime;                       
    private float stateStartTime;                  

    
    [Header("Seed")]
    [SerializeField] private bool spawnOnSeed = true; 
    [SerializeField] private GameObject seedPrefab;   
    [SerializeField] private float seedSpawnRadius = 5f; 
    [SerializeField] private float seedPause = 0.5f;     
   
    private int seedSpawnCountRuntime = 1;               

   
    [Header("Wilt")]
    [SerializeField] private float wiltDuration = 10f; 
    [SerializeField] private float wiltScale = 0.8f;   

    
    [Header("Animation")]
    [SerializeField] private float swayAmplitude = 5f; 
    [SerializeField] private float swayFrequency = 0.7f; 
    [SerializeField] private float bobAmplitude = 0.1f;  
    [SerializeField] private float bobFrequency = 1.8f;  

   
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
        baseZRot = transform.eulerAngles.z;       
        birthTime = Time.time;                    
        EnterState(State.Bloom);                
    }

    private void Update()
    {
      
        if (state == State.Bloom && Time.time - birthTime >= lifeSpan)
            ChangeState(State.Wilt);

 
        switch (state)
        {
            case State.Bloom:   BloomUpdate();  break; 
            case State.Wilt:    WiltUpdate();   break; 
            case State.Seed:    SeedUpdate();   break; 
            case State.Despawn:    break;
        }
    }

    
    private void ChangeState(State next)
    {
        
        EnterState(next);  
    }

    private void EnterState(State next)
    {
        state = next;              
        stateStartTime = Time.time; 

        switch (state)
        {
            case State.Bloom: 
                isBlooming = true;                    
                if (sr && bloomSprite) sr.sprite = bloomSprite; 
                transform.localScale = Vector3.one;   
                basePos = transform.position;         
                baseZRot = transform.eulerAngles.z;   
                break;

            case State.Wilt:  
                isBlooming = false;                  
                if (sr && wiltSprite) sr.sprite = wiltSprite; 
                break;

            case State.Seed: 
                
                seedSpawnCountRuntime = 1;
            
                if (spawnOnSeed && seedPrefab)
                {
                    for (int i = 0; i < seedSpawnCountRuntime; i++) 
                    {
                        Vector2 dir = Random.insideUnitCircle.normalized; 
                        float dist = Random.Range(seedSpawnRadius * 0.7f, seedSpawnRadius); 
                        Vector3 pos = transform.position + new Vector3(dir.x, dir.y, 0f) * dist;
                        Instantiate(seedPrefab, pos, Quaternion.identity);
                    }
                }
                break;

            case State.Despawn: 
                Destroy(gameObject); 
                break;
        }
    }



    private void BloomUpdate()
    {
        float t = Time.time; 

      
        float z = baseZRot + Mathf.Sin(t * swayFrequency) * swayAmplitude; 
        transform.rotation = Quaternion.Euler(0f, 0f, z); 

        Vector3 p = basePos;                           
        p.y += Mathf.Sin(t * bobFrequency) * bobAmplitude; 
        transform.position = p;                        
    
    }

    private void WiltUpdate()
    {
  
        float t = Mathf.Clamp01((Time.time - stateStartTime) / Mathf.Max(0.0001f, wiltDuration));

     
        float s = Mathf.Lerp(1f, wiltScale, t);                  
        transform.localScale = new Vector3(s, s, 1f);            

        
        float z = Mathf.LerpAngle(transform.eulerAngles.z, baseZRot, t); 
        transform.rotation = Quaternion.Euler(0f, 0f, z);               


        if (Time.time - stateStartTime >= wiltDuration)
            ChangeState(State.Seed); 
    }

    private void SeedUpdate()
    {
        float dt = Time.time - stateStartTime; 
     
        float pulse = 1f + 0.05f * Mathf.Sin(dt * 12f) * Mathf.Exp(-dt * 2f); 
        transform.localScale = new Vector3(pulse, pulse, 1f); 

     
        if (dt >= seedPause)
            ChangeState(State.Despawn); 
    }


    public bool HasNectar => isBlooming; 

    public float Harvest(float amountPerSec, float dt)
    {
       
        if (state == State.Bloom)
            ChangeState(State.Wilt); 

        return 1f; 
    }
}
