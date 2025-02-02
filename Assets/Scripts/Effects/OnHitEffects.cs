using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public interface Effect{
    public string getText();
    public string getType();
    public string getDescription();
    public string getIcon();
    public string getCaps();
    public GameObject getAbilityOptionMenu();
    
}
public interface OnHitEffects: Effect
{
    public bool addList();
    public void ApplyEffect(float dmg = 0, float health = 0, Enemy en = null);
}
public class VampOnHit : OnHitEffects
{
    public static VampOnHit Instance;
    public float perc;
    public float prob;

    float DamageToOverheal = 1000;
    float DamageOverhealed = 0;
    public Image cooldownImage;
    public VampOnHit(float perc, float prob){
        
        this.perc = perc;
        this.prob = prob;
        if(Instance == null){
            Instance = this;
        }else{
            Instance.Stack(this);
        }
    }
    public void ApplyEffect(float dmg, float health = 0, Enemy en = null)
    {
        if(UnityEngine.Random.Range(0f,1f) < prob){
            Flamey.Instance.addHealth(Math.Abs(dmg * perc * (SkillTreeManager.Instance.getLevel("Vampire") >= 1 && en.Health < Flamey.Instance.Dmg ? 2f : 1f)));
        }
    }
    public void OverHeal(float f){
        DamageOverhealed += f;
        if(DamageOverhealed > DamageToOverheal){
            DamageOverhealed = 0;
            Flamey.Instance.MaxHealth++;
        }
        cooldownImage.fillAmount = DamageOverhealed/DamageToOverheal; 
    }
    public void Stack(VampOnHit vampOnHit){
        perc += vampOnHit.perc;
        prob += vampOnHit.prob;
        RemoveUselessAugments();
    }
    private void RemoveUselessAugments(){
        if(prob >= 1f){
            prob = 1;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("VampProb");
        }
        if(perc >= 1f && SkillTreeManager.Instance.getLevel("Vampire") < 2)  {
            perc = 1;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("VampPerc");
        }    
        if(!maxed){CheckMaxed();}
    }
    public bool maxed;
    private void CheckMaxed(){
        if(prob >= 1f && !Character.Instance.isACharacter()){
            Character.Instance.SetupCharacter("Vampire");
            maxed = true;
        }
    }
    
    public bool addList(){
        return Instance == this;
    }

    public string getText()
    {
        return "Vampire";
    }

    public string getType()
    {
        return "On-Hit Effect";
    }

    public string getDescription()
    {
        return "You have a chance of <color=#0CD405>healing</color> a percentage of the <color=#FF5858>damage</color> dealt.";
    }
    public string getCaps()
    {
        if(SkillTreeManager.Instance.getLevel("Vampire") < 2){
            return string.Format("Chance: {0}% (Max. 100%) <br>Healing Percentage: {1}% (Max. 100%)", Mathf.Round(prob*100), Mathf.Round(perc*100));
        }
        return string.Format("Chance: {0}% (Max. 100%) <br>Healing Percentage: {1}% (Max. Infinite%)", Mathf.Round(prob*100), Mathf.Round(perc*100));
        
    }
    public string getIcon()
    {
        return "VampUnlock";
    }

    public GameObject getAbilityOptionMenu(){
        return null;
    }
}

public class IceOnHit : OnHitEffects

{
    private Color IceColor;
    public static IceOnHit Instance;
    public float duration;
    public float prob;
    private Button activeCooldownImage;
    private int activeRoundsLeft;
    private int activeRoundsCooldown = 2;
    public IceOnHit(float duration, float prob){
       
        this.duration = duration;
        this.prob = prob;
        if(Instance == null){
            Instance = this;
        }else{
            Instance.Stack(this);
        }
    }
    public void ApplyEffect(float dmg, float health = 0, Enemy en = null)
    {
        if(en==null || en.getSlowInfo("IceHit")[0] > 0){return;}
        if(Flamey.Instance.MaxHealth <= 1000){return;}
        if(UnityEngine.Random.Range(0f,1f) < prob){   
            int fator = SkillTreeManager.Instance.getLevel("Freeze") >= 1 ? 2 : 1;
            en.SlowDown(duration/1000f, Mathf.Clamp((Flamey.Instance.MaxHealth-1000) * 0.00033f * fator, 0, 0.75f), "IceHit");
            DamageUI.InstantiateTxtDmg(en.transform.position, "SLOWED", 4);

        }
    }
    public void Stack(IceOnHit iceOnHit){
        duration += iceOnHit.duration;
        prob += iceOnHit.prob;
        RemoveUselessAugments();
    }
    private void RemoveUselessAugments(){
        Deck deck = Deck.Instance;
        if(prob >= 1f){
            prob = 1;
            deck.removeClassFromDeck("IceProb");
        }
        if(duration >= 10000){
            duration= 10000;
            deck.removeClassFromDeck("IceDuration");
        }    
        if(!maxed){CheckMaxed();}
    }
    public bool maxed;
    private void CheckMaxed(){
        if(prob >= 1f && duration >= 10000 && !Character.Instance.isACharacter()){
            Character.Instance.SetupCharacter("Freeze");
            maxed = true;
        }

    }
    public void SpawnExtraAssets(){
        activeCooldownImage = GameUI.Instance.SpawnUIActiveMetric(Resources.Load<Sprite>("Icons/IceUnlock"));
        activeCooldownImage.transform.GetChild(0).GetComponent<Image>().fillAmount = 1;
        Deck.RoundOver += UpdateActive;
        activeCooldownImage.onClick.AddListener(() => {
            
            Flamey.Instance.GetComponent<Animator>().SetBool("IceSquimo", true);
            foreach (Enemy enemy in EnemySpawner.Instance.PresentEnemies)
            {
                enemy.SlowEffectsDuration["IceHit"] = new float[2];
                int fator = SkillTreeManager.Instance.getLevel("Freeze") >= 1 ? 2 : 1;
                enemy.SlowDown(30, .99f, "IceHit");
            }
            
            Flamey.Instance.callFunctionAfter(() =>{Flamey.Instance.GetComponent<Animator>().SetBool("IceSquimo", false);}, 30f);
            activeCooldownImage.interactable = false;
            activeRoundsLeft = 0;
            activeCooldownImage.transform.GetChild(0).GetComponent<Image>().fillAmount = 0;

        });
    }
    
    private void UpdateActive(object sender, EventArgs e){
        if(activeRoundsLeft<activeRoundsCooldown){
            activeRoundsLeft++;
            activeCooldownImage.transform.GetChild(0).GetComponent<Image>().fillAmount = ((float)activeRoundsLeft)/activeRoundsCooldown;
        }
        if(activeRoundsLeft>=activeRoundsCooldown){
             activeCooldownImage.interactable = true;
        }
    }
    public bool addList(){
        return Instance == this;
    }

    public string getText()
    {
        return "Freeze";
    }

    public string getType()
    {
        return "On-Hit Effect";
    }

    public string getDescription()
    {
        return "You have a chance of slowing the enemy for a percentage of its <color=#AFEDFF>speed</color> for a certain duration. This effect scales with <color=#0CD405>Max Health</color> <color=#FFCC7C>(+1% slow per 33 Extra Max Health)" ;
    }
    public string getCaps()
    {
        int fator = SkillTreeManager.Instance.getLevel("Freeze") >= 1 ? 2 : 1;
        float percentage = Mathf.Clamp((Flamey.Instance.MaxHealth-1000) * 0.00033f * fator,0f,0.75f);
        Debug.Log("Duration: " + duration);
        
        return string.Format("Chance: {0}% (Max. 100%) <br>Slow Percentage: {1}% (Max 75%)<br>Duration: {2}s (Max. 10s)", Mathf.Round(prob*100), Mathf.Round(percentage * 100), Mathf.Round(duration/10f)/100f);
    }

    public string getIcon()
    {
        return "IceUnlock";
    }
    public GameObject getAbilityOptionMenu(){
        return null;
    }
}

public class ShredOnHit : OnHitEffects
{
    
    public static ShredOnHit Instance;
    public float percReduced;
    public float prob;

    float DamageToArmor = 500;
    float DamageArmor = 0;
    private Image cooldownImage;

    public ShredOnHit(float prob, float percReduced){
       
        this.percReduced = percReduced;
        this.prob = prob;
        if(Instance == null){
            Instance = this;
        }else{
            Instance.Stack(this);
        }
    }
    public void ApplyEffect(float dmg, float health = 0, Enemy en = null)
    {
        if(en==null){return;}
        
        if(UnityEngine.Random.Range(0f,1f) < prob){
            float actualPercReduced = percReduced;
            if(SkillTreeManager.Instance.getLevel("Resonance") >= 1){
                actualPercReduced += (Flamey.Instance.accuracy/100f + (Flamey.Instance.BulletSpeed-5)/15f)/10;
            }
            float prevArmor = en.Armor;
            en.Armor -=  (int)(en.Armor *  actualPercReduced);

            if(Character.Instance.isCharacter("Shred")){
                DamageArmor += prevArmor - en.Armor;
                if(DamageArmor > DamageToArmor){
                    DamageArmor = 0;
                    Flamey.Instance.Armor++;
                }
                cooldownImage.fillAmount = DamageArmor/DamageToArmor; 
            }
            

            if(SkillTreeManager.Instance.getLevel("Resonance") >= 2){
                if(prevArmor - en.Armor > 0){
                    Flamey.Instance.addHealth(prevArmor - en.Armor);
                }
                
            }
            
        }
    }
    public void Stack(ShredOnHit shredOnHit){
        percReduced += shredOnHit.percReduced;
        prob += shredOnHit.prob;
        RemoveUselessAugments();
    }
    private void RemoveUselessAugments(){
        if(prob >= 1f){
            prob = 1;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("ShredProb");
        }  
        if(percReduced >= 0.5f){
            percReduced = 0.5f;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("ShredPerc");
        }      
        if(!maxed){CheckMaxed();}
    }
    public bool maxed;
    private void CheckMaxed(){
        if(percReduced >= 0.5f && prob >= 1f && !Character.Instance.isACharacter()){
            Character.Instance.SetupCharacter("Shred");
            maxed = true;
        }
    }

    public void SpawnExtraAssets(){
        cooldownImage = GameUI.Instance.SpawnUIMetric(Resources.Load<Sprite>("Icons/ShredUnlock"));
    }
    public bool addList(){
        return Instance == this;
    }

    public string getText()
    {
        return "Resonance";
    }

    public string getType()
    {
        return "On-Hit Effect";
    }

    public string getDescription()
    {
        return "You have a chance of reducing the target's <color=#919191>armor</color> by a certain percentage" ;
    }
    public string getCaps()
    {
        return string.Format("Chance: {0}% (Max. 100%) <br>Percentage Reduced: {1}% (Max. 50%)", Mathf.Round(prob*100), Mathf.Round(percReduced * 100));
    }

    public string getIcon()
    {
        return "ShredUnlock";
    }
    public GameObject getAbilityOptionMenu(){
        return null;
    }
}

public class ExecuteOnHit : OnHitEffects
{
    
    public static ExecuteOnHit Instance;
    public float percToKill;
    public ExecuteOnHit(float percToKill){
       
        this.percToKill = percToKill;
        
        if(Instance == null){
            Instance = this;
        }else{
            Instance.Stack(this);
        }
    }
    public async void  ApplyEffect(float dmg, float health = 0, Enemy en = null)
    {
        if(en==null){return;}
        if(en.Health < en.MaxHealth * percToKill){
            float f = en.Health;
            Vector2 v = en.transform.position;
            en.Health = 0;
            Debug.Log("Executed");
            if(SkillTreeManager.Instance.getLevel("Assassin")>=2){
                Flamey.Instance.ApplyOnKill(en.HitCenter.position);
                Flamey.Instance.ApplyOnKill(en.HitCenter.position);
            }
            await Task.Delay(50);
            DamageUI.InstantiateTxtDmg(v, "EXECUTED",5);
        }
        
    }
    public void Stack(ExecuteOnHit executeOnHit){
        percToKill = Mathf.Min(0.5f, percToKill + executeOnHit.percToKill);
        RemoveUselessAugments();
    }
    private void RemoveUselessAugments(){
        if(percToKill >= 0.5f){
            percToKill = 0.5f;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("Execute");
        } 

        if(!maxed){CheckMaxed();}
    }
    public bool maxed;
    private void CheckMaxed(){
        if(percToKill >= 0.5f && Flamey.Instance.ArmorPen >= .8f && !Character.Instance.isACharacter()){
            Character.Instance.SetupCharacter("Assassin");
            maxed = true;
        }
    }
    public bool addList(){
        return Instance == this;
    }

    public string getText()
    {
        return "Assassin";
    }

    public string getType()
    {
        return "On-Hit Effect";
    }

    public string getDescription()
    {
        return "You <color=#FF5858>penetrate</color> through a percentage of enemy <color=#919191>armor</color>. Additionally, hitting enemies below a portion of their <color=#0CD405>Max Health</color> <color=#FFCC7C>executes</color> them." ;
    }
    public string getCaps()
    {
        if(SkillTreeManager.Instance.getLevel("Assassin")>=1){
            return string.Format("Armor Penetration: {0}% (Max. 80%) <br>Execution: {1}% (Max. 50%)", Mathf.Round(Flamey.Instance.ArmorPen * 100), Mathf.Round(percToKill*100));
        }
        return string.Format("Armor Penetration: {0}% (Max. 80%)", Mathf.Round(Flamey.Instance.ArmorPen * 100));
    }

    public string getIcon()
    {
        return "Assassins";
    }
    public GameObject getAbilityOptionMenu(){
        return null;
    }
}

public class StatikOnHit : OnHitEffects
{
    
    public static StatikOnHit Instance;
    public GameObject prefab;
    public GameObject prefabPowered;
    private GameObject statikMeter;
    private Slider statikMeterSlider;
    public float prob;
    public int dmg;
    public int ttl;

    public int procAmount; //for character
    public StatikOnHit(float prob, int dmg, int ttl){
        
        this.prob = prob;
        this.dmg = dmg;
        this.ttl = ttl;
        prefab = Resources.Load<GameObject>("Prefab/StatikShiv");
        prefabPowered = Resources.Load<GameObject>("Prefab/StatikShivEmpowered");
        statikMeter = Resources.Load<GameObject>("Prefab/AbilityCharacter/Statik Meter UI");
        if(Instance == null){
            Instance = this;
        }else{
            Instance.Stack(this);
        }
    }
    public void ApplyEffect(float dmg, float health = 0, Enemy en = null)
    {
        if(en==null){return;}
        
        if(UnityEngine.Random.Range(0f,1f) < prob){

            

            if(Character.Instance.isCharacter("Statik")){
                procAmount++;
                statikMeterSlider.value = procAmount;
                if(procAmount > 100){
                    GameObject g2 = Flamey.Instance.SpawnObject(prefabPowered);
                    g2.transform.position = en.HitCenter.position;
                    StatikShiv s2 = g2.GetComponent<StatikShiv>();

                    procAmount = 0;
                    statikMeterSlider.value = procAmount;

                    s2.TTL = 30;
                    s2.isPowered = true;
                    s2.MAXTTL = 30;
                    s2.Damage = (int)dmg;
                    s2.currentTarget = en;
                    s2.locationOfEnemy = en.HitCenter.position;
                    s2.Started = true;
                    return;
                }
            }

            GameObject g = Flamey.Instance.SpawnObject(prefab);
            g.transform.position = en.HitCenter.position;
            StatikShiv s = g.GetComponent<StatikShiv>();

            s.TTL = ttl;
            s.MAXTTL = ttl;
            s.Damage = this.dmg;
            s.currentTarget = en;
            s.locationOfEnemy = en.HitCenter.position;
            s.Started = true;

            
            
        }
        
    }
    public void Stack(StatikOnHit statikOnHit){
        prob += statikOnHit.prob;
        dmg += statikOnHit.dmg;
        ttl += statikOnHit.ttl;
        RemoveUselessAugments();
    }
    private void RemoveUselessAugments(){
        if(prob >= 1f){
            prob = 1f;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("StatikProb");
        }  
        if(ttl >= 10){
            ttl = 10;
            Deck deck = Deck.Instance;
            deck.removeClassFromDeck("StatikTTL");
        }      
    if(!maxed){CheckMaxed();}
    }
    public bool maxed;
    private void CheckMaxed(){
        if(prob >= 1f && ttl >= 10 && !Character.Instance.isACharacter()){
            Character.Instance.SetupCharacter("Statik");
            maxed = true;
        }
    }
    public void SpawnExtraAssets(){
        GameObject g = GameUI.Instance.SpawnUI(statikMeter);
        statikMeterSlider = g.GetComponent<Slider>();
    }
    public bool addList(){
        return Instance == this;
    }

    public string getText()
    {
        return "Static Energy";
    }

    public string getType()
    {
        return "On-Hit Effect";
    }

    public string getDescription()
    {
        return "When you hit an enemy, you have a chance of unleashing a <color=#FFCC7C>static energy chain</color> that travels through enemies nearby, dealing damage to each while applying <color=#FF99F3>On-Hit effects</color>. The more the chain travels the less damage it deals";
    }
    public string getCaps()
    {
        return string.Format("Chance: {0}% (Max. 100%) <br>Travel Distance: {1} Enemies (Max. 10) <br>Damage: +{2}", Mathf.Round(prob * 100), ttl, dmg);

    }

    public string getIcon()
    {
        return "StatikUnlock";
    }
    public GameObject getAbilityOptionMenu(){
        return null;
    }
}