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

    private StoryCharacter m_storyCharacter = StoryCharacter.NONE;

    public void SetStoryCharacter(StoryCharacter aCharacter)
    {
        m_storyCharacter = aCharacter;
    }

    public override bool IsInteractable(GfcCursorRayhit aHit, ref string aNonInteractableReason) { return CornManagerPhone.GetCurrentlyShownAvatar() == null; }

    public override void Interact(GfcCursorRayhit aHit)
    {
        CornManagerPhone.PressedAvatar(PhoneEventIndex);
    }

    public void DestroySelf()
    {
        Interactable = false;
        GfcPooling.Destroy(gameObject);
    }
}