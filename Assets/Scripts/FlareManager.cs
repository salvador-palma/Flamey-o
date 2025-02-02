using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

[System.Serializable]
public class FlareType {
    public int DmgTextID;
    public Color FlareColor;
    public Color SpotColor;
    public Color[] ParticleColors;
}
public class FlareManager : MonoBehaviour
{   
    public static FlareManager INSTANCE;
    public static LayerMask EnemyMask;

    const int POOLED_FLARES_AMOUNT = 250;
    const int POOL_ADDITION_AMOUNT = 50;
    [SerializeField] List<GameObject> pooledFlares = new List<GameObject>();

    
    
    [SerializeField] GameObject DefaultFlarePrefab;
    [SerializeField] GameObject DefaultFlareSpotPrefab;
    [SerializeField] FlareType[] FlareTypes;
    void Awake(){
        INSTANCE = this;
        StartPool();
    }

    public FlareType getFlareType(int i){return FlareTypes[i];}

    public static void transformFlare(int i, GameObject flare){
        
        FlareType flareData = INSTANCE.FlareTypes[i];
        flare.GetComponent<SpriteRenderer>().color = flareData.FlareColor;
        Flare f = flare.GetComponent<Flare>();
        f.DmgTextID = flareData.DmgTextID;
        f.SpotColor = flareData.FlareColor;
        f.Damage = (int)GetDmgByType(i);
        ParticleSystem.MainModule main = flare.GetComponentInChildren<ParticleSystem>().main;
        main.startColor = new ParticleSystem.MinMaxGradient(flareData.ParticleColors[0], flareData.ParticleColors[1]);
    }
    public static float GetDmgByType(int type){
        Flamey f = Flamey.Instance;
        switch(type){
            case 0: return f.Dmg;
            case 1: return f.Dmg * CritUnlock.Instance.mult;
            case 2: return f.Dmg * CritUnlock.Instance.mult * CritUnlock.Instance.mult;
            case 3: return f.Dmg + KrakenSlayer.Instance.extraDmg;
            case 4: return (f.Dmg + KrakenSlayer.Instance.extraDmg) * CritUnlock.Instance.mult;
            case 5: return (f.Dmg + KrakenSlayer.Instance.extraDmg) * CritUnlock.Instance.mult * CritUnlock.Instance.mult;
            case 6: return (f.Dmg + KrakenSlayer.Instance.extraDmg) * 5;
            case 7: return (f.Dmg + KrakenSlayer.Instance.extraDmg) * 5 * CritUnlock.Instance.mult;
            case 8: return (f.Dmg + KrakenSlayer.Instance.extraDmg) * 5 * CritUnlock.Instance.mult * CritUnlock.Instance.mult;
            default: return f.Dmg;
        }
    }

    public void StartPool(){
        for(int i = 0; i< POOLED_FLARES_AMOUNT; i++){
            pooledFlares.Add(Instantiate(DefaultFlarePrefab));
        }
    }
    public void IncreasePool(){
        for(int i = 0; i< POOL_ADDITION_AMOUNT; i++){
            pooledFlares.Add(Instantiate(DefaultFlarePrefab));
        }
    }
    public static GameObject InstantiateFlare(int type){
        GameObject pooledFlare = getPooledObject();
        if(pooledFlare==null){INSTANCE.IncreasePool(); return InstantiateFlare(type);}
        transformFlare(type,pooledFlare);
        pooledFlare.transform.localRotation = new Quaternion(0f,0f,180f,0f);
        pooledFlare.GetComponent<Flare>().VirtualStart();
        pooledFlare.SetActive(true);
        
        return pooledFlare;
    }
    public static GameObject getPooledObject(){
        foreach(GameObject g in INSTANCE.pooledFlares){
            if(!g.activeInHierarchy){
                return g;
            }
        }
        return null;
    }

    public static void DestroyFlare(GameObject flare){
        flare.SetActive(false);
    }

   
}
