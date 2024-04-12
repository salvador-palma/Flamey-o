using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using NUnit.Framework;
using TMPro;
using Unity.VisualScripting;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

[System.Serializable]
public class AnimalSaveData {
    public int AnimalID;
    public int DeathAmount;
    public int RetrievedRewards;
    public AnimalSaveData(int ID, int DeathAmount){this.AnimalID =ID; this.DeathAmount = DeathAmount;}
}
[System.Serializable]
public class BestiarySaveData{
    public List<AnimalSaveData> animals;
    public BestiarySaveData(){animals = new List<AnimalSaveData>();}
    public void AddMilestone(int ID, int N){animals[ID].DeathAmount+=N;}
}

[System.Serializable]
public class AnimalRunTimeData{
    public string name;    
    public Enemy enemy;
    public int Wave;
    [TextAreaAttribute]public string description;
    public AnimalAbility[] abilities;
    public Vector2 IconSize; //left top
    public Vector2 IconPos; //right bottom
}
[System.Serializable]
public class AnimalAbility{
    public Sprite icon;
    [TextAreaAttribute]public string description;
}

public class LocalBestiary : MonoBehaviour
{
    
    public static LocalBestiary INSTANCE;
    public static int AvailableClaims;
    string[] BestiaryTabs = new string[3]{"STATS","ABILITIES", "MILESTONES"};
    public int[] milestones = new int[5]{100, 500, 1500, 5000, 15000};
    private int[] milestone_rewards = new int[5]{500, 1000, 2500, 7500, 20000};
    
    string BestiaryDisplayTab = "STATS"; //STATS, ABILITIES, MILESTONES
    public event EventHandler ClaimRewardEvent;
    private int lastID;
    [SerializeField] List<AnimalRunTimeData> animals;

    [SerializeField] GameObject[] BestiaryPanels;
    [SerializeField] Sprite[] Stars;
    [SerializeField] Color[] StarsColor;
    [SerializeField] BestiarySaveData saved_milestones;

    //GENERAL
    TextMeshProUGUI title;
    Image AnimalImage;


    //TABS
    [SerializeField] Button RightButton;
    [SerializeField] Button LeftButton;
    [SerializeField] TextMeshProUGUI tabTitle;
    //STATS
    TextMeshProUGUI[] statLabels = new TextMeshProUGUI[8];
    //ABILITIES
    [SerializeField] GameObject[] AbilitiesPanel;
    //MILESTONES
    TextMeshProUGUI milestoneProgress;
    Image milestoneStars;
    Slider DeathAmountSlider;
    Button claimRewardButton;

    //GRID RELATED
    [SerializeField] GameObject SlotTemplate;
    [SerializeField] GameObject Container;

    [SerializeField] Transform InfoPanel;

    public void Awake(){
        INSTANCE = this;
        gameObject.SetActive(false);
        RetrieveReferences();
        ReadBestiaryData();
        InitSlots();

        RightButton.onClick.AddListener(()=>ChangeTab(1));
        LeftButton.onClick.AddListener(()=>ChangeTab(-1));

        claimRewardButton.onClick.AddListener(()=>ClaimRewards(lastID));

        if(SceneManager.GetActiveScene().name == "Game"){
            BestiaryTabs = new string[2]{"STATS","ABILITIES"};
        }
    }
    
    private void RetrieveReferences(){

        //STATS
        statLabels[0] = BestiaryPanels[0].transform.Find("InputHealth").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[1] = BestiaryPanels[0].transform.Find("InputDamage").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[2] = BestiaryPanels[0].transform.Find("InputArmor").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[3] = BestiaryPanels[0].transform.Find("InputSpeed").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[4] = BestiaryPanels[0].transform.Find("InputRange").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[5] = BestiaryPanels[0].transform.Find("InputAtkSpeed").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[6] = BestiaryPanels[0].transform.Find("InputEmbers").GetComponentInChildren<TextMeshProUGUI>();
        statLabels[7] = BestiaryPanels[0].transform.Find("InputWave").GetComponentInChildren<TextMeshProUGUI>();

        //PROFILE
        title = InfoPanel.Find("Title").GetComponent<TextMeshProUGUI>();
        AnimalImage = InfoPanel.Find("BestiaryInfoImage").GetChild(0).GetComponent<Image>();

        //MILESTONES
        
        milestoneProgress = BestiaryPanels[2].transform.Find("MilestoneProgress").GetComponent<TextMeshProUGUI>();
        milestoneStars = BestiaryPanels[2].transform.Find("Stars").GetComponent<Image>();
        DeathAmountSlider = BestiaryPanels[2].transform.Find("Slider").GetComponent<Slider>();
        claimRewardButton = BestiaryPanels[2].transform.Find("ClaimRewards").GetComponent<Button>();
        

    }

    /* ===== BESTIARY SLOTS ===== */
    private void InitSlots(){
        int i = 0;
        foreach(AnimalRunTimeData animal in animals){
            InitIndividualSlot(animal, i);
            i++;
        }
    }
    private void InitIndividualSlot(AnimalRunTimeData animal, int index){

        GameObject newSlot = Instantiate(SlotTemplate,Container.transform);
        Transform newSlotImage = newSlot.transform.Find("Animal");
        RectTransform RT = newSlotImage.GetComponent<RectTransform>();
        ResizeImage();
        checkMilestoneProgress();
        newSlot.SetActive(true);

        void ResizeImage(){
            if(getMilestoneProgressInt(index) != -1){
                newSlotImage.GetComponent<Image>().sprite = animal.enemy.GetComponent<SpriteRenderer>().sprite;
                newSlotImage.GetComponent<Image>().color = Color.white;
                RT.anchoredPosition = animal.IconPos;
                RT.sizeDelta = animal.IconSize;
            } 
        }
        void checkMilestoneProgress(){
            int milestone_lvl = getMilestoneProgressInt(index);
            if(milestone_lvl==-1){
                newSlotImage.GetComponent<Image>().color = Color.black;
                newSlot.transform.Find("Stars").GetComponent<Image>().sprite = Stars[0];
            }else{
                newSlot.GetComponent<Button>().onClick.AddListener(()=>DisplayProfile(index));
                newSlot.transform.Find("Stars").GetComponent<Image>().sprite = Stars[milestone_lvl >= 6 ? 5 : milestone_lvl];
                
            }

            if(SceneManager.GetActiveScene().name != "Game"){
                bool NotificationCondition = saved_milestones.animals[index].RetrievedRewards < milestone_lvl;
                newSlot.transform.Find("Notification").gameObject.SetActive(NotificationCondition); 
                if(NotificationCondition){AvailableClaims++;}
            }
        }
        
    }
    
    /* ===== TAB NAVIGATION ===== */
    private void UpdateCurrentTab(int ID){
        switch(BestiaryDisplayTab){
            case "STATS":
                DisplayStats(ID, statLabels);
                break;
            case "ABILITIES":
                DisplayDescription(ID, AbilitiesPanel);
                break;
            case "MILESTONES":
                DisplayMilestones(ID, milestoneProgress, DeathAmountSlider);
                
                break;
        }
    }
    private void ChangeTab(int direction){
        
        int currentIndex = Array.IndexOf(BestiaryTabs, BestiaryDisplayTab);
        BestiaryPanels[currentIndex].SetActive(false);
        currentIndex+= direction;
        if(currentIndex<0){currentIndex=BestiaryTabs.Length -1;}
        if(currentIndex>=BestiaryTabs.Length){currentIndex=0;}
        BestiaryPanels[currentIndex].SetActive(true);
        BestiaryDisplayTab = BestiaryTabs[currentIndex];
        tabTitle.text = BestiaryDisplayTab;
        UpdateCurrentTab(lastID);

    }


    /* ===== I/O FUNCTIONS ===== */
    private void ReadBestiaryData(){
        if(File.Exists(Application.persistentDataPath +"/bestiary.json")){
            string json = File.ReadAllText(Application.persistentDataPath +"/bestiary.json");
            saved_milestones = JsonUtility.FromJson<BestiarySaveData>(json);
        }else{
            CreateBeastiaryFile();
            ReadBestiaryData();
        }
    }
    private void CreateBeastiaryFile(){
        BestiarySaveData createdSaveData = new BestiarySaveData();
        for (int i = 0; i < animals.Count(); i++)
        {
            createdSaveData.animals.Add(new AnimalSaveData(i, -1));
        }
        File.WriteAllText(Application.persistentDataPath +"/bestiary.json", JsonUtility.ToJson(createdSaveData));
    }
    public void WritingData(){
        string json = JsonUtility.ToJson(saved_milestones);
        File.WriteAllText(Application.persistentDataPath + "/bestiary.json", json);
    }

    /* ===== UI FUNCTIONS ===== */
    private void DisplayProfile(int ID){
        lastID = ID;
        UpdateCurrentTab(ID);

        AnimalRunTimeData animal = animals[ID];

        AnimalImage.sprite = animal.enemy.GetComponent<SpriteRenderer>().sprite;
        title.text = animal.name;
        RectTransform TRR = AnimalImage.GetComponent<RectTransform>();
        TRR.anchoredPosition = animal.IconPos;
        TRR.sizeDelta = animal.IconSize;
    }
    private void DisplayStats(int ID, TextMeshProUGUI[] labels){

        AnimalRunTimeData animal = animals[ID];
        Enemy enemy = animal.enemy;

        labels[0].text = enemy.Health.ToString();
        labels[1].text = enemy.Damage.ToString();
        labels[2].text = enemy.Armor.ToString();
        labels[3].text = (enemy.Speed*100f).ToString();
        labels[4].text = (enemy.AttackRange * 10f).ToString();
        labels[5].text =  enemy.AttackDelay + "s";
        labels[6].text = enemy.EmberDropRange[0] + "-" + enemy.EmberDropRange[1];
        labels[7].text = animal.Wave + "h00";
    }
    private void DisplayMilestones(int ID, TextMeshProUGUI counter, Slider slider){

        int[] milestone_progress = getMilestoneProgress(ID);
        counter.text = milestone_progress==null ? "MAX" : milestone_progress[0]+"/"+milestone_progress[1];
        slider.maxValue = milestone_progress == null ? 1 : milestone_progress[1];
        slider.value = milestone_progress == null ? 1 : milestone_progress[0];
        int lvl = getMilestoneProgressInt(ID);
        milestoneStars.sprite = Stars[lvl];
        DeathAmountSlider.transform.GetChild(1).GetChild(0).GetComponent<Image>().color = StarsColor[lvl];

       
        claimRewardButton.gameObject.SetActive(saved_milestones.animals[ID].RetrievedRewards < lvl);
        
        
        

    }
    private void DisplayDescription(int ID, GameObject[] abilitiesContainer){
        AnimalRunTimeData animal = animals[ID];
        AnimalAbility[] abilities = animal.abilities;

        for (int i = 0; i < abilitiesContainer.Length; i++)
        {
            if(i <= abilities.Length - 1){
                abilitiesContainer[i].GetComponentsInChildren<Image>()[1].sprite = abilities[i].icon;
                abilitiesContainer[i].GetComponentInChildren<TextMeshProUGUI>().text = abilities[i].description;
                abilitiesContainer[i].SetActive(true);
            }else{
                abilitiesContainer[i].SetActive(false);
            }
            
        }
    }

    /* ===== MILESTONES ===== */
    private int[] getMilestoneProgress(int ID, bool returnID=false){
        AnimalSaveData animalSaveData = saved_milestones.animals.SingleOrDefault(a => a.AnimalID == ID);
        if(animalSaveData==null || animalSaveData.DeathAmount == -1){return null;}
        int deaths = animalSaveData.DeathAmount;
        int i=0;
        while(i < 5 && deaths >= milestones[i]){
            i++;
        }
        return i>=5 ? null : new int[2]{deaths, milestones[i]};
    }
    private int getMilestoneProgressInt(int ID){
        AnimalSaveData animalSaveData = saved_milestones.animals.SingleOrDefault(a => a.AnimalID == ID);
        if(animalSaveData==null || animalSaveData.DeathAmount == -1){return -1;}
        int deaths = animalSaveData.DeathAmount;
        int i=0;
        while(i < 5 && deaths >= milestones[i]){
            i++;
        }
        return i;
    }

    private void ClaimRewards(int AnimalToClaimID){
        int claimed_rewards = saved_milestones.animals[AnimalToClaimID].RetrievedRewards;
        int rewards_capable_to_claim = getMilestoneProgressInt(AnimalToClaimID);
        int total_rewards_in_embers = 0;
        for (int i = claimed_rewards; i < rewards_capable_to_claim; i++)
        {
           total_rewards_in_embers += milestone_rewards[i];
          
        }
        AvailableClaims--;
        saved_milestones.animals[AnimalToClaimID].RetrievedRewards = rewards_capable_to_claim;
        SkillTreeManager.Instance.changeEmberAmountUI(total_rewards_in_embers);
        SkillTreeManager.Instance.WritingData();
        UpdateCurrentTab(AnimalToClaimID);
        WritingData();

        Container.transform.GetChild(AnimalToClaimID+1).Find("Notification").gameObject.SetActive(false);
        ClaimRewardEvent?.Invoke(this, new EventArgs());
    }
    /* ===== API ===== */
    public Enemy[] getRandomEnemyCombination(int phase, int amount){
        AnimalRunTimeData[] result = new AnimalRunTimeData[amount];
        List<AnimalRunTimeData> phaseAnimals = animals.Where(a => a.Wave == phase).ToList();

        for (int i = 0; i < result.Length; i++)
        {
            AnimalRunTimeData picked;
            do{
                
                picked = phaseAnimals[UnityEngine.Random.Range(0, phaseAnimals.Count())];
            }while(result.Contains(picked));
            result[i] = picked;
        }
        Array.Sort(result, (a,b) => animals.IndexOf(a) - animals.IndexOf(b));
        return result.Select(e=>e.enemy).ToArray();
    }
    public List<Enemy> getEnemyList(){
        return animals.Select(a => a.enemy).ToList();
    }
    public void UpdateBestiaryValues(){
        for (int i = 0; i < animals.Count; i++)
        {
            saved_milestones.AddMilestone(i, animals[i].enemy.getDeathAmount());
            
        }
        WritingData();
    }

    public float[] getMeasurements(Enemy e){
        
        AnimalRunTimeData data = animals.FirstOrDefault(a => a.enemy == e);
        if(data==null){return null;}
        float[] results = new float[4]{data.IconPos[0], data.IconPos[1], data.IconSize[0], data.IconSize[1]};
        return results;
    }
    public int getAvailableRewards(){
        return AvailableClaims;
    }

    public int get_Amount_Of_Enemies_With_Milestones_Above(int N){
        return saved_milestones.animals.Where(e => e.DeathAmount >= N).Count();
    }
    public bool hasBeenUnlocked(Enemy e){
        int ID = animals.FindIndex(i => i.enemy == e);
        if(ID == -1){return false;}
        
        return saved_milestones.animals[ID].DeathAmount >= 0;
    }

}
