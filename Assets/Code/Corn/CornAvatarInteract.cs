using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CornAvatarInteract : GfcInteractable
{
    [SerializeField] private ParticleSystem m_spawnParticles;
    [SerializeField] private GfcSound m_spawnSound;

    private void OnEnable()
    {
        if (m_spawnParticles) m_spawnParticles.Play();
        m_spawnSound.Play(transform.position); //m_spawnSound?.Play(); doesn't work
    }

    public int PhoneEventIndex = -1;

    private GfcStoryCharacter m_storyCharacter = GfcStoryCharacter.NONE;

    public void SetStoryCharacter(GfcStoryCharacter aCharacter)
    {
        m_storyCharacter = aCharacter;
    }

    public override bool Interactable(GfcCursorRayhit aHit, out string aNonInteractableReason)
    {
        bool baseInteractable = base.Interactable(aHit, out aNonInteractableReason);
        bool canShowAvatar = CornManagerPhone.GetCurrentlyShownAvatar() == null;
        if (canShowAvatar)
            aNonInteractableReason = null;
        return baseInteractable && canShowAvatar;
    }

    public override void Interact(GfcCursorRayhit aHit) { CornManagerPhone.PressedAvatar(PhoneEventIndex); }

    public void DestroySelf()
    {
        SetInteractable(false);
        GfcPooling.Destroy(gameObject);
    }
}