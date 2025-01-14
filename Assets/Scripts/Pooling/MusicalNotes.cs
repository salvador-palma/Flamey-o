using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicalNotes : IPoolable
{
    public float Timer = 2f;
    private void Update() {
        Timer-=Time.deltaTime;
        if(Timer <= 0){
            UnPool();
        }
    }

    public override void Pool()
    {
        Timer = 2f;
        GetComponent<ParticleSystem>().Clear();
        GetComponent<ParticleSystem>().Play();
    }

    public override string getReference()
    {
        return "Music";
    }
    public override void Define(float[] args)
    {
        transform.position = new Vector2(args[0], args[1]);
    }
}