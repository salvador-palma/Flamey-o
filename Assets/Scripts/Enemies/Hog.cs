using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using System;
public class Hog : Enemy
{
    public int chargeAmount;

   

    private void Start() {

        
        base.flame = Flamey.Instance;
        
        Speed =  Distribuitons.RandomTruncatedGaussian(0.02f,Speed,0.075f);
        if(EnemySpawner.Instance.current_round >= 60){
            int x = EnemySpawner.Instance.current_round;
            Health = (int)(Health * (float) (Math.Pow(x-30, 2)/350) + 1f);
            Armor = (int)(Armor * (x-45f)/15f); 
            Speed *= (float) (Math.Pow(x-30, 2)/4000f) + 1f;
            Damage = (int)(Damage * (float) (Math.Pow(x-30, 2)/5000f) + 1f);
        }
        MaxHealth = Health;

        //StartAnimations(6);
    }


    override public void UpdateEnemy() {
           
           
        if(chargeAmount < 0 ){
            base.UpdateEnemy();
        }
    }
    

    public void decreaseCharge(){
        chargeAmount--;
        if(chargeAmount <0){
            GetComponent<Animator>().Play("Run");
        }
    }

    bool AttackedAlready = false;
    public override void Attack(){
        if(AttackedAlready){
            flame.Hitted(Damage/2, ArmorPen, this);
        }else{
            AttackedAlready = true;
            flame.Hitted(Damage, ArmorPen, this);
        }
       
    }

    public override void Hitted(int Dmg, int TextID, bool ignoreArmor, bool onHit, string except = null, string source = null){
        if(source!= null && source.Equals("Lava Pool")){
            base.Hitted(SkillTreeManager.Instance.getLevel("Lava Pool") >= 1 ? Dmg/2 : Dmg/10, TextID, ignoreArmor, onHit, except);
        }else{
            base.Hitted(Dmg, TextID, ignoreArmor, onHit, except);
        }
    }


   
    override protected IEnumerator PlayAttackAnimation(float delay){
        while(Health>0){
            GetComponent<Animator>().Play("Attack");
            yield return new WaitForSeconds(delay);
            yield return new WaitForSeconds(extraAtkSpeedDelay);
        }
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
