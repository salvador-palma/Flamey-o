using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CatterPillar : Enemy
{
    
    private bool check = false;
    
    private void Start() {
        base.flame = Flamey.Instance;
        Speed =  Distribuitons.RandomGaussian(0.02f,Speed);
        // AttackDelay = 2f;
        // AttackRange = 0.8f;
        // Damage = 20;
        // Health = 50;
        // ArmorPen = 0.2f;
        // Armor = 0;
        MaxHealth = Health;
        StartAnimations(1);
    }
    private void Update() {
        
        base.Move();
        if(Vector2.Distance(flame.transform.position, transform.position) < AttackRange && !check){
            check = true;
            Speed = 0.00001f;
            InvokeRepeating("Attack",0f, AttackDelay);

        }
    }

    
    
}
