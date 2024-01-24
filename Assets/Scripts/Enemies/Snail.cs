using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Snail : Enemy
{
    
    [SerializeField]  private bool withShell;
    [SerializeField] private float maxSpeed;
    private void Start() {
        base.flame = Flamey.Instance;
        maxSpeed =  Distribuitons.RandomTruncatedGaussian(0.02f,Speed,0.075f);
        Speed = maxSpeed;
        
        MaxHealth = Health;
        StartAnimations(withShell? 0 : 2);
    }
   

    public void SlideOn(){
        Speed = maxSpeed;
    }
    public void SlideOff(){
        Speed = 0.00001f;
    }
    override public void setSpeed(float s){
        maxSpeed = s;
    }
    override public float getSpeed(){
        return maxSpeed;
    }

    
    
}

