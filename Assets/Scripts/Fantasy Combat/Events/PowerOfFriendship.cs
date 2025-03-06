using Cinemachine;
using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Linq;

public class PowerOfFriendship : CombatAction
{
    [Header("UI Components")]
    [SerializeField] Transform companionHeader;
    [SerializeField] Transform missingCompanionsHeader;
    [Header("Faders")]
    [SerializeField] CanvasGroup blackFader;
    [SerializeField] CanvasGroup whiteFader;
    [Header("Timers")]
    [SerializeField] float fadeInTime = 0.75f;
    [SerializeField] float fadeOutTime = 0.25f;
    [SerializeField] float canvasAnimTime = 1.6f;
    [SerializeField] float whiteFadeInTime = 0.5f;
    [SerializeField] float whiteFadeOutTime = 0.25f;
    [Header("Value")]
    [SerializeField] PowerGrade pofPowerGrade = PowerGrade.A;
    [Space(10)]
    [SerializeField] float targetGroupWeight = 1;
    [SerializeField] float targetGroupRadius = 1.5f;
    [Header("Cameras")]
    [SerializeField] GameObject pofCamera;
    [SerializeField] GameObject pofDamageCam;
    [SerializeField] CinemachineTargetGroup targetGroup;
    [Header("Scene Indices")]
    [SerializeField] int pofBuildIndex;

    List<PlayerGridUnit> playersParticipating = new List<PlayerGridUnit>();
    List<CharacterGridUnit> beatenUpEnemies = new List<CharacterGridUnit>();

    int intiatorIndex = 0;
    string intiatorName;

    int currentSceneIndex;

    Action OnFadeCompleteCallback;
    public static Action PowerOfFriendshipTriggered;

    CanvasGroup pofCanvasGroup;
    CinemachineImpulseSource impulseSource;

    private void Awake()
    {
        pofCanvasGroup = pofCamera.GetComponentInChildren<CanvasGroup>();
        impulseSource = GetComponent<CinemachineImpulseSource>();
    }

    public void TriggerPOF(PlayerGridUnit intiator)
    {
        BeginAction();

        ControlsManager.Instance.DisableControls();

        currentSceneIndex = SceneManager.GetActiveScene().buildIndex;

        PowerOfFriendshipTriggered();

        FantasyCombatManager.Instance.TeamAttackInitiator = intiator;
        FantasyCombatManager.Instance.CombatCinematicPlaying = true;

        //Begin Loading Scene.
        StartCoroutine(LoadPOFSceneRoutine());

        SetupData(intiator);
        SetupCanvas(FantasyCombatManager.Instance.GetPlayerCombatParticipants(true, true), playersParticipating);

        FadeIn();

    }

    IEnumerator LoadPOFSceneRoutine()
    {
        AsyncOperation asyncOperation = SceneManager.LoadSceneAsync(pofBuildIndex, LoadSceneMode.Additive);

        //Don't let the Scene activate until you allow it to
        asyncOperation.allowSceneActivation = false;
        yield return new WaitForSeconds(fadeInTime + canvasAnimTime);
        asyncOperation.allowSceneActivation = true;
        //Activate new scene.
        yield return asyncOperation;
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(pofBuildIndex));
        POFCinematicSetup();
        pofCamera.SetActive(false);
        FantasyCombatManager.Instance.ShowHUD(false, true);
        POFDirector.Instance.BeginCinematic();
    }



    public void UnloadPOF()
    {
        StartCoroutine(UnloadPOFRoutine());
    }

    IEnumerator UnloadPOFRoutine()
    {
        whiteFader.alpha = 0;
        whiteFader.gameObject.SetActive(true);
        whiteFader.DOFade(1, whiteFadeInTime);
        yield return new WaitForSeconds(whiteFadeInTime);
        SceneManager.SetActiveScene(SceneManager.GetSceneByBuildIndex(currentSceneIndex));
        SceneManager.UnloadSceneAsync(pofBuildIndex);
        whiteFader.DOFade(0, whiteFadeOutTime);
        ActivateTargetGroupCam();
        yield return new WaitForSeconds(whiteFadeOutTime);
        whiteFader.gameObject.SetActive(false);

        FantasyCombatManager.Instance.CombatCinematicPlaying = false;
        Health.RaiseHealthChangeEvent(true);

        impulseSource.GenerateImpulse();       
    }

    private void POFCinematicSetup()
    {
        DamageEnemies();
        POFDirector.Instance.Setup(intiatorIndex, intiatorName, playersParticipating, beatenUpEnemies, false, this);
    }

    private void SetupCanvas(List<PlayerGridUnit> party, List<PlayerGridUnit> unitsParticipating)
    {
        int missingCompanionCount = 4 - unitsParticipating.Count;

        foreach(Transform child in missingCompanionsHeader)
        {
            //int index = child.GetSiblingIndex();
            child.gameObject.SetActive(false);
        }

        List<PlayerGridUnit> newParty = new List<PlayerGridUnit>(party);

        if(party.Count != unitsParticipating.Count)
        {
            List<PlayerGridUnit> missingMembers = party.Where((unit) => !unitsParticipating.Contains(unit)).ToList();

            //Insert Missing Member at Index 1.
            PlayerGridUnit missingMember = missingMembers[0];

            newParty.Remove(missingMember);
            newParty.Insert(1, missingMember);
            
            if(missingMembers.Count > 1)
            {
                missingMember = missingMembers[1];

                newParty.Remove(missingMember);
                newParty.Insert(3, missingMember);
            }
        }

        foreach (Transform child in companionHeader)
        {
            int index = child.GetSiblingIndex();
            Image image = child.GetComponent<Image>();

            if(index < party.Count)
            {
                image.enabled = true;
                image.sprite = newParty[index].transparentBackgroundPotrait;
            }
            else
            {
                image.enabled = false;
            }
        }

        for (int i = 0; i < missingCompanionCount; i++)
        {
            missingCompanionsHeader.GetChild(i).gameObject.SetActive(true);
        }
    }

    private void ActivateTargetGroupCam()
    {
        //Set TargetGroup
        foreach (var target in targetGroup.m_Targets)
        {
            targetGroup.RemoveMember(target.target);
        }

        foreach (CharacterGridUnit unit in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true))
        {
            targetGroup.AddMember(unit.camFollowTarget, targetGroupWeight, targetGroupRadius);
        }

        pofDamageCam.SetActive(true);
    }

    private void FadeIn()
    {
        blackFader.alpha = 0;
        pofCanvasGroup.alpha = 1;

        blackFader.gameObject.SetActive(true);
        blackFader.DOFade(1, fadeInTime).OnComplete(() => ActivatePOFCam());
    }

    private void ActivatePOFCam()
    {
        //OnFadeCompleteCallback();
        blackFader.gameObject.SetActive(false);
        pofCamera.SetActive(true);
    }

    public override void EndAction()
    {
        pofDamageCam.SetActive(false);
        base.EndAction();
    }

    private void SetupData(PlayerGridUnit intiator)
    {
        playersParticipating = FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false);
        beatenUpEnemies = FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true);

        foreach (PlayerGridUnit player in playersParticipating)
        {
            if(player == intiator)
            {
                intiatorIndex = playersParticipating.IndexOf(player);
                intiatorName = player.unitName;
                break;
            }
        }
    }

    private void DamageEnemies()
    {
        foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true))
        {
            int totalDamage = 0;

            foreach (PlayerGridUnit player in FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false))
            {
                int rawDamage = TheCalculator.Instance.CalculateRawDamage(player, false, pofPowerGrade, out bool isCritical, false);
                totalDamage = totalDamage + rawDamage;
            }

            enemy.CharacterHealth().TakeDamage(GetAttackData(totalDamage), DamageType.Ultimate);
        }

        SetActionTargets(FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true).ToList<GridUnit>());
    }

    private AttackData GetAttackData(int damage)
    {
        AttackData attackData = new AttackData(FantasyCombatManager.Instance.TeamAttackInitiator, FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false).Cast<GridUnit>().ToList()
            , Element.None, damage, FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true).Count);

        attackData.canEvade = false;

        attackData.isPhysical = true;

        attackData.powerGrade = pofPowerGrade;
        attackData.canCrit = false;

        return attackData;
    }

    /* private bool WillEndInVictory()
     {
         bool willKO = true;

         foreach (CharacterGridUnit enemy in FantasyCombatManager.Instance.GetEnemyCombatParticipants(false, true))
         {
             int damage = TheCalculator.Instance.CalculatePOFDamage(FantasyCombatManager.Instance.GetPlayerCombatParticipants(false, false), enemy, pofPowerGrade);

             if (!enemy.Health().WillDamageKO(damage))
             {
                 willKO = false;
             }

             enemy.Health().TakeDamageBasic(null, damage, true);
         }

         return willKO;
     }*/


}
