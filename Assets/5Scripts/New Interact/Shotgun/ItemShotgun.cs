using UnityEngine;
using System.Collections;

public class ItemShotgun : ItemBase
{
    [Header("Shotgun Settings")]
    public int ammoCount = 0;
    public int maxAmmo = 100; 
    public float shootingRange = 20f;
    public float fireRate = 1.0f;
    public LayerMask hitMask;     
    [Header("Effects")]
    public ParticleSystem muzzleFlash;
    public AudioSource audioSource;
    public AudioClip shootSound;
    public AudioClip emptyClickSound;
    public AudioClip reloadSound;

    private float nextFireTime;

    public void AI_Fire(Vector3 origin, Vector3 direction)
    {
        PerformShootEffects();
        
        RaycastHit hit;
        if (Physics.Raycast(origin, direction, out hit, shootingRange, hitMask))
        {
            // 1. Пытаемся нанести урон живому существу (Сосед)
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target == null) target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage();

            // 2. --- НОВОЕ: Пытаемся разбить окно (SimpleReplacer) ---
            SimpleReplacer replacer = hit.collider.GetComponent<SimpleReplacer>();
            if (replacer == null) replacer = hit.collider.GetComponentInParent<SimpleReplacer>();
            
            if (replacer != null)
            {
                // Отправляем ID (например "4"), чтобы окно разбилось
                replacer.Replace();
            }
        }
    }

    public override void OnUse()
    {
        bool isPlayer = (holder != null && holder.CompareTag("Player"));
        
        if (ammoCount <= 0)
        {
            if (isPlayer && audioSource && emptyClickSound) 
            {
                audioSource.PlayOneShot(emptyClickSound);
            }
            return;
        }

        if (Time.time < nextFireTime) return;
        nextFireTime = Time.time + fireRate;

        ammoCount--; 

        if (isPlayer)
        {
            var animController = PlayerItemHandler.inst?.animationController;
            if (animController != null) animController.PlayUseAnimation();
        }

        PlayerShoot();
    }

    private void PerformShootEffects()
    {
        if (audioSource && shootSound) audioSource.PlayOneShot(shootSound);
        if (muzzleFlash) 
        {
            muzzleFlash.Stop(); 
            muzzleFlash.Play();
        }
    }

    private void PlayerShoot()
    {
        PerformShootEffects();

        RaycastHit hit;
        Camera cam = Camera.main;
        if (cam == null) return;

        // Стреляем из камеры
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, shootingRange, hitMask))
        {
            // 1. Логика для IDamageable (Убийство соседа)
            IDamageable target = hit.collider.GetComponent<IDamageable>();
            if (target == null) target = hit.collider.GetComponentInParent<IDamageable>();
            if (target != null) target.TakeDamage();

            // 2. --- НОВОЕ: Логика для SimpleReplacer (Разбитие окна) ---
            // Ищем скрипт SimpleReplacer на объекте или его родителе
            SimpleReplacer replacer = hit.collider.GetComponent<SimpleReplacer>();
            if (replacer == null) replacer = hit.collider.GetComponentInParent<SimpleReplacer>();
            
            if (replacer != null)
            {
                // Вызываем метод замены, передавая ID дробовика
                replacer.Replace();
            }
        }
    }

    public bool TryReload(int amount)
    {
        if (ammoCount >= maxAmmo) return false;
        ammoCount += amount;
        if(audioSource && reloadSound) audioSource.PlayOneShot(reloadSound);
        return true;
    }
}