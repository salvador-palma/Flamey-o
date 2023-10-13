using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
public class CameraShake : MonoBehaviour
{
    [SerializeField] private Transform _camera;
    
    static CameraShake INSTANCE;
    private void Start() {
        INSTANCE = this;
    }
    public static void Shake(float duration, float strenght){
        INSTANCE.ShakeInstance(duration, strenght);
    }
    public void ShakeInstance(float duration, float strenght){
        _camera.DOComplete();
        _camera.DOShakePosition(duration, strenght);
        _camera.DOShakeRotation(duration,strenght);
    }

}   
