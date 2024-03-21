using System;
using System.Collections.Generic;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    
    public static EnemySpawner Instance {get; private set;}
    float height;
    float width;
    
    bool isOn = true;
    [HideInInspector] public bool isOnAugments = false;

    

    public int current_round = 0;

    float EnemyAmount;
    float RoundDuration;
    float TimerEnemySpawn;
    float TimerEnemySpawnCounter;

    [SerializeField] public GameObject ExplosionPrefab;
    
    public bool GameEnd = true;
    
    List<List<float>> ProbabiltyList = new List<List<float>>(){
        new List<float>(){1,0,0},
        new List<float>(){0.85f,.15f,0},
        new List<float>(){0.6f,0.4f,0},
        new List<float>(){0.5f,0.5f},
        new List<float>(){0.5f,0.5f},

        new List<float>(){0,.15f,.85f},
        new List<float>(){0.15f,.15f, 0.7f},
        new List<float>(){0.1f,0.3f,0.6f},
        new List<float>(){0.25f,0.25f, 0.5f},
        new List<float>(){0.3f, 0.3f, 0.4f},
    };

    public Enemy[] PhaseEnemies;
    public List<Enemy> PresentEnemies;
    private void Awake() {
        Instance = this;
        PresentEnemies = new List<Enemy>();
        resetInstances();
    }
    public void Start(){
        
        GameEnd =true;
        Flamey.Instance.GameEnd = true;

        SetSpawnLimits();
        StartRound();
        
    }
    public void StartGame(){ 
        Flamey.Instance.GameEnd = false;    
        if(PlayerPrefs.GetInt("PlayerLoad", 0) == 0){
            if(Deck.Instance.hasAtLeastOneUnlockable()){
                current_round = -1;
                isOnAugments = true;
                Deck.Instance.StartAugments(true, true);
            }else{
                GameEnd = false;
            }
        }else{
            newRound();
        }
        PlayerPrefs.DeleteKey("PlayerLoad");    
    }
    private Vector2 getPoint(){
        double angle = Math.PI * (float)Distribuitons.RandomUniform(0,360)/180f;
        double x = 0.52f * width * Math.Cos(angle);
        double y = 0.52f * height * Math.Sin(angle);
        return new Vector2((float)x,(float)y);
    }

    private void Update() {
        if(GameEnd){return;}
        UpdateEnemies();
        if(!isOn){
            if(GameObject.FindGameObjectWithTag("Enemy") == null && !isOnAugments){
                if(current_round==59){GameUI.Instance.ShowLimitRoundPanel();}
                else{
                    isOnAugments = true;
                    Deck.Instance.StartAugments((current_round+1)%5 == 0);
                }
                Flamey.Instance.poisonsLeft = 0;
            } 
            return;
        }
        if(TimerEnemySpawnCounter > 0){
            TimerEnemySpawnCounter-= Time.deltaTime;
        }else{
            TimerEnemySpawnCounter = TimerEnemySpawn;
            SpawnEnemy(PickRandomEnemy(current_round));
            EnemyAmount--;
            if(EnemyAmount <= 0){
                isOn = false;
            }
        }
    }
    public void UpdateEnemies(){
        PresentEnemies.ForEach(e => {if(e!=null && !e.Attacking){e.UpdateEnemy();}});
        List<Enemy> deadEnemies = PresentEnemies.Where(e => e==null || e.Health < 0).ToList();
        foreach(Enemy enemy in deadEnemies){
            PresentEnemies.Remove(enemy);
            enemy.Die();
        }
    }
    public void addEnemy(Enemy enemy){PresentEnemies.Add(enemy);}
    public void SpawnEnemy(GameObject enemy){
        GameObject g = Instantiate(enemy);
        PresentEnemies.Add(g.GetComponent<Enemy>());
        g.transform.position = getPoint();
        g.GetComponent<Enemy>().CheckFlip();
    }
    private void SetSpawnLimits(){
        height = 2f * Camera.main.orthographicSize;
        width = height * Camera.main.aspect;
    }
    

    public void newRound(){
        
        current_round++;

        isOn = true;
        isOnAugments = false;
        GameEnd = false; 

        Flamey.Instance.notEspecificEffects.ForEach(effect => effect.ApplyEffect());
        Flamey.Instance.ApplyTimedRound();

        StartRound();
        
    }
    private void StartRound(){

        EnemyAmount = getSpawnAmount(current_round);
        RoundDuration = getRoundTime(current_round);
        TimerEnemySpawn = EnemyAmount/RoundDuration;
        if(current_round%10==0){PhaseEnemies = pickEnemiesForPhase(current_round);}
        GameUI.Instance.UpdateProgressBar(current_round); 
        GameUI.Instance.UpdateMenuInfo(current_round); 
        Debug.Log("Expected Enemies: " + EnemyAmount);
    }


    /* ===== ENEMY PICK ===== */
    private GameObject PickRandomEnemy(int round){return PhaseEnemies[pickEnemyIndex(ProbabiltyList[round % 10])].gameObject;}
    private int pickEnemyIndex(List<float> prob){
        float val = UnityEngine.Random.Range(0f,1f);
        for(int i = 0 ; i< prob.Count; i++){
            if(prob[i] > val){
                return i;
            }else{
                val -= prob[i];
            }
        }
        return prob.Count - 1;
    }

    /* ===== ROUND SETTINGS ===== */
    private float getRoundTime(int round){return Math.Min(5 + 1.2f * round, 40);}
    private float getSpawnAmount(int round){return 5*(round%10)+30*(round/10)+5;}
    
    private void resetInstances(){
        FlameCircle.Instance = null;
        MoneyMultipliers.Instance = null;
        CandleTurrets.Instance = null;
        Summoner.Instance = null;

        ThornsOnHitted.Instance = null;

        VampOnDeath.Instance = null;
        Explosion.Instance = null;
        Necromancer.Instance= null;
        Bullets.Instance = null;

        VampOnHit.Instance = null;
        IceOnHit.Instance = null;
        ShredOnHit.Instance = null;
        ExecuteOnHit.Instance = null;
        StatikOnHit.Instance = null;

        BurnOnLand.Instance = null;
        IceOnLand.Instance = null;

        SecondShot.Instance = null;
        BurstShot.Instance = null;
        KrakenSlayer.Instance = null;
        CritUnlock.Instance = null;

        HealthRegen.Instance = null;
        LightningEffect.Instance = null;
        Immolate.Instance = null;

        LocalBestiary.INSTANCE.getEnemyList().ForEach(e => e.ResetStatic());     

    }

    private Enemy[] pickEnemiesForPhase(int round){
        return LocalBestiary.INSTANCE.getRandomEnemyCombination((round/10)+1, 3);
    }
    
}
