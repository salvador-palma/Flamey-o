using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class Bison : Enemy
{
    public int maxCharge;
    public int chargeAmount;
    public int dmgPerCharge;
    
    public bool running = false;
   

    private void Start() {
        if(!EnemySpawner.Instance.PresentEnemies.Contains(this)){
            EnemySpawner.Instance.PresentEnemies.Add(this);
        }
        base.flame = Flamey.Instance;
        Speed =  Distribuitons.RandomTruncatedGaussian(0.02f,Speed,0.075f);
        MaxHealth = Health;

    }


    override public void UpdateEnemy() {
           
        
        if(Health < MaxHealth/2 || chargeAmount >= maxCharge){
            GetComponent<Animator>().Play("Run");
            running = true;
            
        }
        if(running){
            base.UpdateEnemy();
        }
    }
    

    public void charge(){
        chargeAmount++;
    }

    bool AttackedAlready = false;
    public override void Attack(){
        if(AttackedAlready){
            flame.Hitted(Damage/2, ArmorPen, this);
        }else{
            AttackedAlready = true;
            flame.Hitted(Damage + (chargeAmount*dmgPerCharge), 1, this);
        }
       
    }


    override protected void PlayAttackAnimation(){
        GetComponent<Animator>().Play("Attack");
    }
    public override void CheckFlip()
    {
        base.CheckFlip();
        if(transform.position.x < 0){
            
            transform.Find("SmokeFront").GetComponent<SpriteRenderer>().flipX = true;
            transform.Find("SmokeBack").GetComponent<SpriteRenderer>().flipX = true;
        }
        

    }
}