using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;
using TMPro;
using UnityEngine.UI;
using NUnit.Framework;
using RangeAttribute = UnityEngine.RangeAttribute;
using UnityEngine.Rendering;
public class Naal : NPC
{   
    [Header("Haggling")]
    [Range(0,100)] public int Mood;
    [SerializeField] Item ItemAtHand;
    [SerializeField] int initialOffer;
    [SerializeField] int lastPlayerOffer;
    [SerializeField] int lastNPCOffer2;
    [SerializeField] int PriceAtHand;

    public int lastNPCOffer { // This is a property.
        get {
            return lastNPCOffer2; 
        }

        set {
             

            int digits = value == 0  ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;
            int insignificant = digits - (digits/2 + digits%2); 
            int rest = (int) (value % Math.Pow(10,insignificant));
            int last = lastNPCOffer2;
            lastNPCOffer2 = value - rest;

            Debug.Log("Set: " + lastNPCOffer2);
            if(lastNPCOffer2 == last){
                lastNPCOffer2 = (int)Math.Max(lastNPCOffer2-Math.Pow(10, insignificant), lastPlayerOffer);
            }
            
        }
  }
    [SerializeField] GameObject counterOfferPanel;
    [SerializeField] TMP_InputField counterOfferInput;
    public Sprite defaultSprite;

    
    public Transform ItemGrid;

    protected override void CharacterLoad()
    {
        ReadMood();
        if(Mood==-1){WriteMood(50);}
        switch(GameVariables.GetVariable("BlackMarketReady")){
            case -1:gameObject.SetActive(false);break;
            case 0:QueueDialogue(0);GameVariables.SetVariable("BlackMarketReady", 1); break;
        }

        foreach (Transform t in ItemGrid){
            t.GetComponent<Item>().ItemStart();
        }
    }
    public void ReadMood(){
        Mood = GameVariables.GetVariable("NaalMood");
        Chat.Instance.MoodSlider.value = Mood;
    }
    public void WriteMood(int m){
        
        m = Math.Clamp(m, 0, 100);
        Debug.Log("Mood: " + m);
        GameVariables.SetVariable("NaalMood", m);
        ReadMood();
    }
    public void GiveBetsyBinocles(){
        Betsy.QueueDialogue(2);
    }

    public override void ClickedCharacter(){

        if(GameVariables.GetVariable("NaalPresentation") <= -1){
            StartDialogue(1);
            return;
        }
        
        base.ClickedCharacter();
    }

    [SerializeField] NPC Betsy;

    
    public void UnlockBlackMarket(){
        MetaMenuUI.Instance.UnlockableScreen("UNLOCKED", "NAAL'S BLACK MARKET", "You can now access the <color=#FFCC7C>Black Market</color> to buy especial uprades", 3);
        GameVariables.SetVariable("NaalPresentation", 1);
    }
    
    
    //****** HAGGLE SYSTEM *******//
    int bargainingRounds = 0;
    int patience = 5;
    int charismaTries = 0;
    int charismaLimit = 0;
    int declineAmt = 0;
    
    public void BargainItem(Item item){

        
        
        patience = Random.Range(4, Math.Max(6, 6 + (int)(Mood/100f * 5)));
        Debug.Log("Bargaining...");
        bargainingRounds = 0;
        
        ItemAtHand = item;
        initialOffer = item.MinimumPrice * 5;
        lastNPCOffer = initialOffer;
        lastPlayerOffer = -1;

        charismaTries = 0;
        charismaLimit = patience/2;

        declineAmt=0;

        UnityEvent e  = new UnityEvent();
        if(SkillTreeManager.Instance.PlayerData.embers < (item.MinimumPrice + initialOffer)*0.35f){
             e.AddListener(() => {
                UnityEvent ev2 = new UnityEvent();
                ev2.AddListener(()=>QuitStore());

                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("But... heeeeeeyyy! You don't have enough embers for this!", defaultSprite,"Jhat"),
                    new Dialogue("You gotta at least have close to " + initialOffer + " embers if you even want to try and haggle for it!", defaultSprite,"Jhat"),   
                },
                after:ev2, endAfter:false));
             });
        }else{
             e.AddListener(() => ShowOffer("So with that said, what about " + initialOffer + " embers for it?"));
        }
       

        StartCoroutine(Chat.Instance.StartDialogue(item.presentation, defaultName:"Jhat", after: e, endAfter:false));
        Debug.Log("Started Presentation...");
        
    }

    private int DealType(int value){
        int diff = initialOffer - ItemAtHand.MinimumPrice;
        int priceDelta = (int) (diff / (2f + 3f*(Mood/100f)));
        if(value < ItemAtHand.MinimumPrice + priceDelta){
            return 0;
        }else if(value < ItemAtHand.MinimumPrice + 2*priceDelta){
            return 1;
        }else{
            return 2;
        }
    }
    private int getOfferPrice(int counterOffer){
        if(counterOffer > SkillTreeManager.Instance.PlayerData.embers){

            UnityEvent ev2 = new UnityEvent();
            ev2.AddListener(()=>ShowOffer("Then again, what about " + lastNPCOffer + " embers for this?"));

            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("WHAT! Do- do you even have all those embers? ", defaultSprite,"Jhat"),
                new Dialogue("It's useless to offer money you don't have!", defaultSprite,"Jhat"),
              
            },
            after:ev2, endAfter:false));

            return 0;
        }
        int diff = initialOffer - ItemAtHand.MinimumPrice;
        int priceDelta = (int) (diff / (2f + 3f*(Mood/100f)));

        if(counterOffer < lastPlayerOffer){ 
            UnityEvent ev2 = new UnityEvent();

            float lowChance = Math.Min(.6f - Mood * .5f/100f, .6f);
            float medChance = Math.Min(1 - Mood * .7f/100f, 1);
            float res = Random.Range(0f,1f);
            if(res <= lowChance){

                //OFFER LOWER THAN LAST BARGAIN - LOW MOOD
                WriteMood(Mood - 15);
                int increasedValue = lastPlayerOffer - counterOffer + lastNPCOffer; 
                ev2.AddListener(()=>ShowOffer("Given that, the price is now " + increasedValue + " embers! Hmpf!"));
                
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Dear lord! Stop messing around please!", defaultSprite,"Jhat"),
                    new Dialogue("You just said you were able to pay " + lastPlayerOffer + " and now you are asking for that?", defaultSprite,"Jhat"),
                    new Dialogue("We can both play this game! Since you aren't being considerate, I will mirror your behaviour!", defaultSprite,"Jhat"),
                },
                after:ev2, endAfter:false));
                lastNPCOffer = increasedValue;
            }else if(res <= medChance){

                //OFFER LOWER THAN LAST BARGAIN - MEDIUM MOOD
                WriteMood(Mood - 5);
                ev2.AddListener(()=>ShowOffer("So again, what about " + lastNPCOffer + " embers for this?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("I see...", defaultSprite,"Jhat"),
                    new Dialogue("I'm not mad... I'm just... disappointed...", defaultSprite,"Jhat"),
                    new Dialogue("I thought haggling was the fun part of business, but you aren't being very cooperative right now...", defaultSprite,"Jhat"),
                    new Dialogue("Let's try this again shall we?", defaultSprite,"Jhat"),
                },
                after:ev2, endAfter:false));
            }else{

                //OFFER LOWER THAN LAST BARGAIN - HIGH MOOD
                ev2.AddListener(()=>ShowOffer("So again, what about " + lastNPCOffer + " embers for this?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Uhhh, I'm a bit confused...", defaultSprite,"Jhat"),
                    new Dialogue("Did you just really bargain for " + counterOffer + " right after asking for " + lastPlayerOffer + "?", defaultSprite,"Jhat"),
                    new Dialogue("We are supposed to reach an agreement, that's the whole point! So don't go off-track here! I'm going to pretend I didn't hear that...", defaultSprite,"Jhat"),
                },
                after:ev2, endAfter:false));
            }
            return 0;
        }
        if(counterOffer == lastPlayerOffer || (lastPlayerOffer > 0 && (lastPlayerOffer + (lastNPCOffer - lastPlayerOffer)*0.04f > counterOffer))){ 
            
            //OFFER BARELY MOVED
            WriteMood(Mood - 5);
            UnityEvent ev2 = new UnityEvent();
            ev2.AddListener(()=>ShowOffer("Let's try once more, what do you think of " + lastNPCOffer + " embers for this?"));

            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Hmmm... You aren't really giving me anything to work with", defaultSprite,"Jhat"),
                new Dialogue("You need to at least give in a bit! Raising the price by a mere " +  (counterOffer-lastPlayerOffer) + " embers is not giving in at all!", defaultSprite,"Jhat"),
            },
            after:ev2, endAfter:false));
            return 0;
        }
        if(counterOffer>=lastNPCOffer){

            //SURPRSIED GOOD
            Debug.Log("Unexpected Good Offer!");
            WriteMood(Mood + 10);
            Deal(counterOffer);
            return 0;
        }
        lastPlayerOffer = counterOffer;
        if(counterOffer < ItemAtHand.MinimumPrice){
            //LOWER THAN MINIMUM PRICE
            //LOOSE 10 TO 20 MOOD
            Debug.Log("Super Bad Offer!" );
            WriteMood(Mood - Random.Range(10,20));
            makeOffer(0, counterOffer);

        }else if(counterOffer < ItemAtHand.MinimumPrice + priceDelta){
            //LOW OFFER
            //LOOSE 5 MOOD
            Debug.Log("Bad Offer!");
            int delta = counterOffer - ItemAtHand.MinimumPrice;
            WriteMood(Mood - 5);
            makeOffer(1, lastNPCOffer - (int)(delta/(2 - Mood/100f*1.5f)));

        }else if(counterOffer < ItemAtHand.MinimumPrice + 2*priceDelta){
            //MEDIUM 
            //NORMAL COUNTER
            Debug.Log("Medium Offer!" );
            
            makeOffer(2, lastPlayerOffer + (int)(3*(lastNPCOffer-lastPlayerOffer)/4f));

        }else{
            //GOOD OFFER
            //WIN +5 MOOD
            
            WriteMood(Mood + 5);

            Debug.Log("Good Offer!");
            float chanceToTake = Math.Min(.1f + Mood * .8f/100f, .9f);
            Debug.Log("Chance to take the deal: " + chanceToTake + "%");

            if(Random.Range(0f,1f) <= chanceToTake){
                //AGREE ON COUNTER OFFER
                if(bargainingRounds < patience/2){

                    lastNPCOffer = (lastNPCOffer + counterOffer)/2;
                    UnityEvent ev1 = new UnityEvent();
                    ev1.AddListener(()=>ShowOffer("Let's do it like this, if you can afford " + lastNPCOffer + ", then it's all yours!"));

                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("Hmmm... the offer is quite tempting... ", defaultSprite,"Jhat"),
                        new Dialogue("BUT! We just started our negotiations right? It's too soon to close the deal!", defaultSprite,"Jhat"),
                        
                    },
                    after:ev1, endAfter:false));
                }else{
                    UnityEvent ev1 = new UnityEvent();
                    ev1.AddListener(()=>CloseDeal(counterOffer));

                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("Hmmm... <size=60%> the offer is not that bad... and we have been negotiating for far too long now...", defaultSprite,"Jhat"),
                        new Dialogue("<size=115%>Oh-<size=100%> Di-did you hear that? Well, I guess now there's no point in bargaining anymore. I'll take your deal!", defaultSprite,"Jhat"),
                        new Dialogue(counterOffer +  " embers was it? Done!", defaultSprite,"Jhat"),
                    },
                    after:ev1, endAfter:false));
                }
                
            }else{
                //COUNTER OFFER ON HALF
                makeOffer(3, lastPlayerOffer + (int)((lastNPCOffer-lastPlayerOffer)/2f));
            }
        }
        return 0;
    }

    private void Deal(int value, bool playerAccepted=false)
    {
        UnityEvent ev1 = new UnityEvent();
        ev1.AddListener(()=>CloseDeal(value));

        switch(DealType(value)){
            case 0:
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Uff... finally reached an agreement eh?", defaultSprite,"Jhat"),
                new Dialogue("I'm pretty sure I'm losing money here by selling this at " + value + " embers", defaultSprite,"Jhat"),
                new Dialogue("So you better compensate for that in your next purchases! Hmpf!", defaultSprite,"Jhat"),
                new Dialogue("Anywaaays... if you have interest in anything else, just let me know!", defaultSprite,"Jhat"),
                },
                after:ev1, endAfter:false));
            break;
            case 1:
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Glad to negotiate with you! " + value + " embers seems like a fair value", defaultSprite,"Jhat"),
                    new Dialogue("Are you interested in anything else perhaps?", defaultSprite,"Jhat"),
                },
                after:ev1, endAfter:false));
            break;
            case 2:
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=115%>Sweet!<size=100%> You surely got a nice deal!", defaultSprite,"Jhat"),
                    new Dialogue(value + " embers right? Here you go!", defaultSprite,"Jhat"),
                    new Dialogue("If you need anything else, you are always welcome to our store!", defaultSprite,"Jhat"),
                },
                after:ev1, endAfter:false));
            break;
        }
        
        
        
    }
    private void CloseDeal(int value){

        QuitStore();
    }
    public void CounterOffer(){
        Bargain(false);
        int value = int.Parse(counterOfferInput.text);
        getOfferPrice(value);
    }


    private void makeOffer(int type, int value, int subtype=0){
        
        value = Norm(value, lastNPCOffer);
        switch(type){
            case 0:
                
                if(Random.Range(0f,1f) >= (Mood/100f)){

                    UnityEvent ev1 = new UnityEvent();
                    ev1.AddListener(()=>QuitStore());

                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("<size=115%>WHA-<size=100%>I'm sorry, but... don't you think that's a bit too low of an offer?", defaultSprite,"Jhat"),
                        new Dialogue("I thought you would be a bit less cheap! I don't think I need that behaviour in my shop!", defaultSprite,"Jhat"),
                        new Dialogue("I'm gonna give you some time for you to think about the kind of offer you just made me...", defaultSprite,"Jhat"),
                    },
                    after:ev1, endAfter:true));

                }else{

                    UnityEvent ev2 = new UnityEvent();
                    ev2.AddListener(()=>ShowOffer("I'm gonna repeat myself, what do you think of " + lastNPCOffer + " embers for this?"));

                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("<size=115%>UHHH... <size=100%>I'm sorry? Did you just say " + value + "?", defaultSprite,"Jhat"),
                        new Dialogue("I must've misheard it right? Because " + value + " would be an incredibly low of an offer don't you think?", defaultSprite,"Jhat")
                    },
                    after:ev2, endAfter:false));
                }
                
             break;
            case 1:
                UnityEvent ev3 = new UnityEvent();
                ev3.AddListener(()=>ShowOffer("But look, what about " + value + "?"));

                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Oh-That's quite the low offer don't you think?", defaultSprite,"Jhat"),
                    new Dialogue("I'm sorry but I can't possibly go that low...", defaultSprite,"Jhat")
                },
                after:ev3, endAfter:false));
             break;
            case 2:
                UnityEvent ev4 = new UnityEvent();
                ev4.AddListener(()=>ShowOffer("What do you think of " + value + "?"));

                switch (Random.Range(0,3))
                {
                    case 0:
                        StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                            new Dialogue("No can do! " + lastPlayerOffer + " to " + lastNPCOffer + " is quite the jump! Hmpf!", defaultSprite,"Jhat"),
                            new Dialogue("Going that low is impossible for me but I can try to lower it just a bit... let me seeee...", defaultSprite,"Jhat")
                        },
                        after:ev4, endAfter:false));
                        break;
                    case 1:
                        StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                            new Dialogue("Hmpf! That's quite low still! Master wouldn't allow me to sell this for such a low value", defaultSprite,"Jhat"),
                            new Dialogue("But look... I can try and lower the price just a little bit... can't do much more than that ok?", defaultSprite,"Jhat")
                        },
                        after:ev4, endAfter:false));
                        break;
                    case 2:
                        StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                            new Dialogue("I see... your offer is still not up to this store's standards!", defaultSprite,"Jhat"),
                            new Dialogue("I'll give in a little bit but you have to come forward with a better offer aswell understand?", defaultSprite,"Jhat")
                        },
                        after:ev4, endAfter:false));
                        break;
                    
                }
                
             break;
            case 3:
                UnityEvent ev5 = new UnityEvent();
                ev5.AddListener(()=>ShowOffer("Look! Let's not waste our time ok? Shall we meet halfway? What about " + value + "?"));

                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Hmmm... It's not really a bad deal...", defaultSprite,"Jhat"),
                },
                after:ev5, endAfter:false));
             break;
            case 4:
             break;
        }

        lastNPCOffer = value;        
    }
    private void ShowOffer(string costumMsg){
        Debug.Log("Round: " + bargainingRounds + " Patience: " + patience);
        bargainingRounds++;
        if (bargainingRounds > patience){
            Debug.Log("Last Offer");
            Chat.Instance.ChatSingular("With that said, " + lastNPCOffer + " is my absolute last offer! Take it or leave it!", defaultSprite, "Jhat", new string[]{"<size=80%>Accept!","<size=80%>Decline"}, new UnityAction[]{
                new UnityAction(()=>Deal(lastNPCOffer)), 
                new UnityAction(()=>Leave()),
            });
            return;
        }
        Debug.Log("Not Last Offer");
        Chat.Instance.ChatSingular(costumMsg, defaultSprite, "Jhat", new string[]{"<size=80%>Deal!","<size=80%>Bargain","<size=80%>Persuade","<size=80%>Leave"}, new UnityAction[]{
            new UnityAction(()=>Deal(lastNPCOffer)), 
            new UnityAction(()=>Bargain(true)),
            new UnityAction(()=>Persuade(lastNPCOffer)),
            new UnityAction(()=>Leave())
        });
    }

    private void Leave(){  
        declineAmt++;
        if(declineAmt>1){
            UnityEvent ev1 = new UnityEvent();
            ev1.AddListener(()=>QuitStore());
            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Oh I see... Well if you want to leave so bad I can't stop you...", defaultSprite,"Jhat"),
            },
            after:ev1, endAfter:false));
            return;
        }
        if(bargainingRounds<=1){
            UnityEvent ev1 = new UnityEvent();
            ev1.AddListener(()=>QuitStore());
            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Oh! Not interested? Got it...", defaultSprite,"Jhat"),
            },
            after:ev1, endAfter:false));
            return;
        }
        float behaviour = Random.Range(0f,1f);
        int diff = initialOffer - ItemAtHand.MinimumPrice;
        int priceDelta = (int) (diff / (2f + 3f*(Mood/100f)));
        if(behaviour < Mood/50f-1f){


            lastNPCOffer = lastPlayerOffer + (int)((lastNPCOffer-lastPlayerOffer)/2f);
            UnityEvent ev5 = new UnityEvent();
            ev5.AddListener(()=>ShowOffer(lastNPCOffer + " what do you say? Sweet deal right?"));

            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Wait! Don't leave yet! Let's talk!", defaultSprite,"Jhat"),
                new Dialogue("Ok ok... I'll lower the price a bit more...", defaultSprite,"Jhat")
            },
            after:ev5, endAfter:false));
            return;
        }

        if(behaviour < Mood/20f - 4.5f && lastPlayerOffer > ItemAtHand.MinimumPrice + priceDelta){
            
            UnityEvent ev5 = new UnityEvent();
            ev5.AddListener(()=>CloseDeal(lastPlayerOffer));
            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Uh! Wait are you really leaving?", defaultSprite,"Jhat"),
                new Dialogue("C'mooonnn! I thought we were getting along no?", defaultSprite,"Jhat"),
                new Dialogue("Look! Since I'm in a good mood, I will let you keep this item for the price you requested ok?", defaultSprite,"Jhat"),
                new Dialogue("It's a once in a lifetime promotion, just for you!", defaultSprite,"Jhat"),
                new Dialogue(lastPlayerOffer + " embers was it? Deal!", defaultSprite,"Jhat"),
            },
            after:ev5, endAfter:false));
            return;
        }

        if(Mood<=25){
            WriteMood(Mood-5);
            UnityEvent ev1 = new UnityEvent();
            ev1.AddListener(()=>QuitStore());
            StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                new Dialogue("Look... I've been in the market for far too long! Hmpf!", defaultSprite,"Jhat"),
                new Dialogue("I know all about these techniques! Pretend lack of interest so I lower the price, yada, yada!", defaultSprite,"Jhat"),
                new Dialogue("I'm sorry but that won't be happening here! If you want to leave then go for it!", defaultSprite,"Jhat"),
            },
            after:ev1, endAfter:false));
        }else{
            if(patience < bargainingRounds && Mood >60){

                lastNPCOffer = lastPlayerOffer + (int)(3*(lastNPCOffer-lastPlayerOffer)/4f);
                UnityEvent ev1 = new UnityEvent();
                ev1.AddListener(()=>ShowOffer(lastNPCOffer + " embers what do you say?"));

                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Woah! We just started bargaining! Are you really going leaving it already?", defaultSprite,"Jhat"),
                    new Dialogue("That's no fun, Hmpf! Look, I can lower the price a bit more ok?", defaultSprite,"Jhat"),
                },
                after:ev1, endAfter:false));
            }else{
                UnityEvent ev1 = new UnityEvent();
                ev1.AddListener(()=>QuitStore());
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("Well... if you aren't interested there's nothing I can do about it...", defaultSprite,"Jhat"),
                    new Dialogue("If there's anything else I can help you with... just say something...", defaultSprite,"Jhat"),
                },
                after:ev1, endAfter:false));
            }
            
        }
        
    }


    string[] Actions = new string[]{"*You compliment the Store*", "*You compliment Jhat's outfit*", "*You ask Jhat about himself*", "*You ask Jhat about its day*", "*You give Jhat puppy eyes*"};
    string[][] Responses = new string[][]{
        new string[]{"Oh here we go, typical haggling conversation...", "Complimenting the store? Hmpf! Could've at least complimented me...", "Thanks, thanks, but let's not waste our time here ok?", "Oh! You like what I did here? I've been trying a couple things you know how it is...", "OH YOU LIKE IT? I knew it was cool! Master kept doubting me, Hmpf!"},
        new string[]{"Not this again...", "Complimenting the robe? Hmpf! Could've at least complimented me...", "Right, I know it fits me well but let's not waste our time here", "I also think it's a pretty nice robe! It used to belong to master when he was younger", "RIGHT? This robe is awesome! Master keeps telling me not to touch it so don't snitch on me!"},
        new string[]{"Why are you going off-track? Hmpf!", "I don't have a lot to say right now! Hmpf!", "Could be better, so let's not waste our time here ok?", "Oh! I don't really have much to tell but I appreciate you asking that!", "All my life has been business and bargaining so I can't really tell you much... But hey, I appreciate the consideration!"},
        new string[]{"Are you actually asking me that right now?", "Let's not do this please", "Might get better if you accept the deal once and for all", "Its better now that I have you here bargaining with me! Isn't this fun?", "I just woke up and stayed here waiting for you to come... The answer is quite basic but I appreciate the consideration!"},
        new string[]{"What th- I feel SO uncomfortable right now...", "Ok can you stop with that? Puppy eyes? Really?", "I wasn't built for this type of interaction... literally...", "I don't... I don't know what this is supposed to be... but it looks funny! Hmpf Hmpf!", "That's a funny look you got there! Hmpf Hmpf!"},};
    private void Persuade(int last)
    {
        charismaTries++;
        int charismaType = (int)Math.Round(Random.Range(-1.25f, -0.25f) + 1.5f * Mood/100f); //-1 0 1

        if(charismaTries <= charismaLimit/2 + (Mood < 33 ? -1 : Mood < 66 ? 0 : 1)){
            charismaType = Math.Min(2, charismaType+1);
        }
        else if(charismaTries >= charismaLimit + (Mood < 33 ? -1 : Mood < 66 ? 0 : 1)){
            charismaType = Math.Max(-2, charismaType-1);
        }

        
        int ActionTaken = Random.Range(0, Actions.Length - 1);

        UnityEvent ev2 = new UnityEvent();
        switch(charismaType){
            case -2:
                WriteMood(Mood-15);
                ev2.AddListener(()=>ShowOffer("Again, " + lastNPCOffer + " embers, what do you think?"));

                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                    new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                    new Dialogue("Don't you think I'm already aware of these lower effort tricks?", defaultSprite,"Jhat"),
                    new Dialogue("Persuading me isn't gonna lead you no where", defaultSprite,"Jhat"),  
                    new Dialogue("Focus on the business not on the business man", defaultSprite,"Jhat"),  
                },
                after:ev2, endAfter:false));

            break;
            case -1:
                WriteMood(Mood-10);
                ev2.AddListener(()=>ShowOffer(ItemAtHand.Name + " for " + lastNPCOffer + " embers, what do you think?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                    new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                    new Dialogue("I know I look just like a kid, but I can see trough your tactics", defaultSprite,"Jhat"),
                    new Dialogue("Persuading you opponent is a good move but not when he is aware of it! Hmpf!", defaultSprite,"Jhat"),  
                    new Dialogue("Now let's get back to business!", defaultSprite,"Jhat"),  
                },
                after:ev2, endAfter:false));
            break;
            case 0:
                WriteMood(Mood-5);
                ev2.AddListener(()=>ShowOffer("Again, " + lastNPCOffer + " embers for it, deal?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                    new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                    new Dialogue("Meaningless chit-chat will not take us nowhere! Hmpf! We got business to take care of!", defaultSprite,"Jhat"),
                },
                after:ev2, endAfter:false));
            break;
            case 1:
                if(SkillTreeManager.Instance.PlayerData.embers * 4f/3f >= lastNPCOffer && SkillTreeManager.Instance.PlayerData.embers < lastNPCOffer){

                    lastNPCOffer = (int)SkillTreeManager.Instance.PlayerData.embers;
                    Debug.Log("lastNPCOffer:" + lastNPCOffer);

                    ev2.AddListener(()=>ShowOffer(lastNPCOffer + " embers for this item, what about that?"));
                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                        new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                        new Dialogue("I saw that you don't really have a lot of embers right? Since you were so nice to me I will lower the deal a bit", defaultSprite,"Jhat"),  
                    },
                    after:ev2, endAfter:false));
                    return;
                }
                WriteMood(Mood+5);
                ev2.AddListener(()=>ShowOffer("So, " + lastNPCOffer + " embers for it, deal?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                    new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                    new Dialogue("Anyways that was nice of you! But look, the price won't budge for this ok?", defaultSprite,"Jhat"),  
                },
                after:ev2, endAfter:false));
            break;
            case 2:
                if(SkillTreeManager.Instance.PlayerData.embers * 5f/3f >= lastNPCOffer && SkillTreeManager.Instance.PlayerData.embers < lastNPCOffer){

                    lastNPCOffer = (int)SkillTreeManager.Instance.PlayerData.embers;
                    Debug.Log("lastNPCOffer:" + lastNPCOffer);
                    ev2.AddListener(()=>ShowOffer(lastNPCOffer + " embers for this item, what about that?"));
                    StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                        new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                        new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                        new Dialogue("Look, I saw you are quite tight on embers. Since we are friends, just give me what you have and we call it a deal!", defaultSprite,"Jhat"),  
                    },
                    after:ev2, endAfter:false));
                    return;
                }
                WriteMood(Mood+10);
                lastNPCOffer = lastPlayerOffer + (int)(3*(lastNPCOffer-lastPlayerOffer)/4f);
                ev2.AddListener(()=>ShowOffer("I'll lower the price to " + lastNPCOffer + " deal?"));
                StartCoroutine(Chat.Instance.StartDialogue(new Dialogue[]{
                    new Dialogue("<size=80%><i><color=#CCCCCC>" + Actions[ActionTaken], defaultSprite,"Jhat"),
                    new Dialogue(Responses[ActionTaken][2+charismaType], defaultSprite,"Jhat"),
                    new Dialogue("You just made my day with that! I'm going to show you how kindness, no matter how small, always pays off!", defaultSprite,"Jhat"),  
                },
                after:ev2, endAfter:false));
                
            break;

        }
        //BAD CHARISMA TRY
    }

    public void Bargain(bool on)
    {
        counterOfferPanel.SetActive(on);
    }

    private void QuitStore(){
        
        Chat.Instance.EndChat();
       
    }
    
    private int Norm(int value, int cur){
        int digits = value == 0  ? 1 : (int)Math.Floor(Math.Log10(Math.Abs(value))) + 1;
        int insignificant = digits - (digits/2 + digits%2); 
        int rest = (int) (value % Math.Pow(10,insignificant));
        int result = value - rest;

        if(result == cur){
            return (int)Math.Max(result-Math.Pow(10, insignificant), lastPlayerOffer);
        }else{
            return result;
        }
    }

   
}
