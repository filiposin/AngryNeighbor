using UnityEngine;

public class ItemFan : ItemBase
{
    [Header("Fan Settings")]
    [SerializeField] private float meltSpeed = 20f;
    [SerializeField] private LayerMask meltableMasks;
    
    [Header("Audio")]
    [SerializeField] private AudioSource fanAudio; // Ссылка на AudioSource

    private bool isTurnedOn = false;

    protected override void Awake()
    {
        base.Awake();
        // Если забыли привязать в инспекторе, пробуем найти на этом же объекте
        if (fanAudio == null) fanAudio = GetComponent<AudioSource>();
    }

    public override void OnUse()
    {
        isTurnedOn = !isTurnedOn;

        var animController = PlayerItemHandler.inst?.animationController;
        if (animController != null)
        {
            if (isTurnedOn) 
            {
                animController.PlayUseAnimation(); // Тут можно запустить анимацию "вкл"
                PlaySound(true);
            }
            else
            {
                // Если есть анимация выключения, вызвать тут
                PlaySound(false);
            }
        }
    }

    private void Update()
    {
        if (isTurnedOn && holder != null)
        {
            ProcessFanRaycast();
        }
    }

    private void ProcessFanRaycast()
    {
        Camera cam = Camera.main;
        if (cam == null) return;

        RaycastHit hit;
        if (Physics.Raycast(cam.transform.position, cam.transform.forward, out hit, PlayerItemHandler.inst.interactDistance, meltableMasks))
        {
            if (hit.collider.TryGetComponent<IceBlock>(out var iceBlock))
            {
                iceBlock.Melt(meltSpeed * Time.deltaTime);
            }
        }
    }
    
    private void PlaySound(bool play)
    {
        if (fanAudio == null) return;

        if (play)
        {
            if (!fanAudio.isPlaying) fanAudio.Play();
        }
        else
        {
            fanAudio.Stop();
        }
    }

    public override void OnDrop()
    {
        base.OnDrop();
        TurnOff();
    }

    public override void OnReturnToPool()
    {
        base.OnReturnToPool();
        TurnOff();
    }
    public void OnDisable()
    {
        TurnOff();
    }
    private void TurnOff()
    {
        isTurnedOn = false;
        PlaySound(false);
    }
}