using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class SquirrelFlying : Squirrel
{
    [Range(0f,1f)] public float arenaLand;
    public float startingHeight;
    public bool flying = true;
    public float flyingSpeedRatio;

    public Vector2 LandDest;
    public float cos;
    override protected void Start() {

        base.Start();

        Vector3 vectorAB = transform.position - Flamey.Instance.transform.position;
        cos = Math.Abs(Vector3.Dot(Vector3.right, vectorAB.normalized));


        LandDest =  Flamey.Instance.transform.position;
        LandDest.x += (transform.position.x - Flamey.Instance.transform.position.x) * arenaLand;
        LandDest.y += (transform.position.y - Flamey.Instance.transform.position.y) * arenaLand;
        transform.position=  new Vector2(transform.position.x, transform.position.y + (float)(startingHeight * Math.Pow(cos,3)));

    }
    public override void UpdateEnemy()  {
        base.UpdateEnemy();
        if( flying && Vector2.Distance(LandDest, HitCenter.position) < 0.3f ){
            Land();
        }
    }

    public override void Move(){
        if(!flying){
            transform.position = Vector2.MoveTowards(transform.position, flame.transform.position, Speed * Time.deltaTime * (placedBomb? -1f : 1));
        }else{
            transform.position = Vector2.MoveTowards(transform.position, LandDest, Speed * flyingSpeedRatio * Time.deltaTime);
        }
        
    }

    private void Land(){
        flying = false;
        GetComponent<Animator>().SetTrigger("InGround");
    }

    public static new int DEATH_AMOUNT = 0;
    public override int getDeathAmount(){return DEATH_AMOUNT;}
    public override void incDeathAmount(){DEATH_AMOUNT++;}
    public override void ResetStatic(){DEATH_AMOUNT = 0;}
}
