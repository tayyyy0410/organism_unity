using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class beeControl : MonoBehaviour
{
    private enum State { Hatch, Adult, Forage, Old, Die }
    [SerializeField] private State state = State.Hatch;

    [Header("Nest")]
    [SerializeField] private Transform nest;
    private Vector3 homePos;

    [Header("Lifespan")]
    [SerializeField] private float lifeSpan = 30f;
    [SerializeField] private float adultBufferMin = 2f;
    [SerializeField] private float adultBufferMax = 3f;
    [SerializeField] private float oldRatio = 0.7f;
    [SerializeField] private float hatchDuration = 2.0f;
    private float birthTime, stateStart, adultBufferDur;

    [Header("Movement")]
    [SerializeField] private float moveSpeed = 4f;      
    [SerializeField] private float hatchSpeed = 2.5f;  
    [SerializeField] private float forageSpeed = 5f;   
    [SerializeField] private float noiseFreq = 0.9f;    
    [SerializeField] private float bobAmp = 0.003f;    
    [SerializeField] private float bobFreq = 8f;

    [Header("Forage")]
    [SerializeField] private float queryInterval = 0.5f;
    [SerializeField] private float harvestRange = 1.0f;
    [SerializeField] private float harvestRate = 80f;
    [SerializeField] private Vector2 lingerRange = new Vector2(1f, 2f);
    private float nextQueryTime;
    private flowerState targetFlower;
    private float lingerEndTime = Mathf.Infinity;
    
    private Vector3 hoverAnchor;
    private float hoverStartTime;

    [Header("Sprites")]
    [SerializeField] private Sprite hatchSprite;
    [SerializeField] private Sprite adultSprite;
    [SerializeField] private Sprite forageSprite;
    [SerializeField] private Sprite oldSprite;
    private SpriteRenderer sr;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        homePos = nest ? nest.position : transform.position;
        birthTime = Time.time;
        Enter(State.Hatch);
    }

    void Update()
    {
        if (state != State.Die && Time.time - birthTime >= lifeSpan) Change(State.Die);

        switch (state)
        {
            case State.Hatch:  HatchUpdate();  break;
            case State.Adult:  AdultUpdate();  break;
            case State.Forage: ForageUpdate(); break;
            case State.Old:    OldUpdate();    break;
            case State.Die:         break;
        }
    }

    void LateUpdate() => ClampToScreen();

    void Change(State s) => Enter(s);

    void Enter(State s)
    {
        state = s;
        stateStart = Time.time;

        switch (s)
        {
            case State.Hatch:
                if (hatchSprite) sr.sprite = hatchSprite;
                transform.position = homePos;
                break;

            case State.Adult:
                if (adultSprite) sr.sprite = adultSprite;
                adultBufferDur = Random.Range(adultBufferMin, adultBufferMax);
                break;

            case State.Forage:
                if (forageSprite) sr.sprite = forageSprite;
                targetFlower = FindClosestFlowerWithNectar();
                nextQueryTime = Time.time + queryInterval;
                lingerEndTime = Mathf.Infinity;
                break;

            case State.Old:
                if (oldSprite) sr.sprite = oldSprite;
                break;

            case State.Die:
                Destroy(gameObject);
                break;
        }
    }

    void HatchUpdate()
    {
        Wander(hatchSpeed);
        if (Time.time - stateStart >= hatchDuration) Change(State.Adult);
    }

    void AdultUpdate()
    {
        Wander(moveSpeed);
        if (Time.time - stateStart >= adultBufferDur)
        {
            var f = FindClosestFlowerWithNectar();
            if (f) Change(State.Forage);
        }
        if ((Time.time - birthTime) / lifeSpan >= oldRatio) Change(State.Old);
    }

    void ForageUpdate()
    {
   
        if (float.IsInfinity(lingerEndTime))
        {
            if (Time.time >= nextQueryTime)
            {
                targetFlower = FindClosestFlowerWithNectar();
                nextQueryTime = Time.time + queryInterval;
            }
            if (!targetFlower) { Change(State.Adult); return; }

            Vector3 to = targetFlower.transform.position - transform.position;
            float dist = to.magnitude;
            Vector3 dir = dist > 0.001f ? to / dist : Vector3.right;

        
            MoveNoRotate(dir, forageSpeed);

            
            if (dist <= harvestRange)
            {
                hoverAnchor = targetFlower.transform.position;
                hoverStartTime = Time.time;
                lingerEndTime = Time.time + Random.Range(lingerRange.x, lingerRange.y);
            }
        }
        else
        {
            
            HoverWiggleAroundAnchor();

          
            if (targetFlower) targetFlower.Harvest(harvestRate, Time.deltaTime);

         
            if (Time.time >= lingerEndTime)
            {
                if ((Time.time - birthTime) / lifeSpan >= oldRatio) Change(State.Old);
                else Change(State.Adult);
            }
        }
    }

    void OldUpdate()
    {
        Wander(moveSpeed * 0.8f);
    }




    void Wander(float speed)
    {
        float t = Time.time;
        float nx = Mathf.PerlinNoise(t * noiseFreq, GetInstanceID() * 0.137f) - 0.5f;
        float ny = Mathf.PerlinNoise(GetInstanceID() * 0.271f, t * noiseFreq) - 0.5f;
        Vector3 dir = new Vector3(nx, ny, 0f).normalized;
        MoveNoRotate(dir, speed);
    }

    void MoveNoRotate(Vector3 dir, float speed)
    {
        transform.position += dir * speed * Time.deltaTime;
        transform.position += new Vector3(0f, Mathf.Sin(Time.time * bobFreq) * bobAmp, 0f);

        if (state != State.Hatch) 
        {
            var sc = transform.localScale;
            sc.x = (dir.x >= 0f) ? Mathf.Abs(sc.x) : -Mathf.Abs(sc.x);
            transform.localScale = sc;
        }
    }


    void HoverWiggleAroundAnchor()
    {
       
        float t = Time.time - hoverStartTime;
        float decay = Mathf.Exp(-t * 2.0f);             
        float wiggle = 0.05f * decay;                    

        float dx = Mathf.Sin(t * 9f) * wiggle;
        float dy = Mathf.Sin(t * 11f) * wiggle;

     
        transform.position = Vector3.Lerp(transform.position, hoverAnchor, Time.deltaTime * 8f);
        transform.position += new Vector3(dx, dy, 0f);
    }

    flowerState FindClosestFlowerWithNectar()
    {
        flowerState best = null;
        float bestSqr = float.MaxValue;
        foreach (var f in FindObjectsOfType<flowerState>())
        {
            if (f && f.HasNectar)
            {
                float d = (f.transform.position - transform.position).sqrMagnitude;
                if (d < bestSqr) { bestSqr = d; best = f; }
            }
        }
        return best;
    }

    void ClampToScreen()
    {
        var cam = Camera.main;
        if (!cam || !cam.orthographic) return;

        float vert = cam.orthographicSize;
        float horiz = vert * cam.aspect;
        Vector3 pos = transform.position;
        pos.x = Mathf.Clamp(pos.x, cam.transform.position.x - horiz, cam.transform.position.x + horiz);
        pos.y = Mathf.Clamp(pos.y, cam.transform.position.y - vert,  cam.transform.position.y + vert);
        transform.position = pos;
    }


    public void SetNest(Transform t)
    {
        nest = t;
        homePos = nest ? nest.position : transform.position;
    }
}
