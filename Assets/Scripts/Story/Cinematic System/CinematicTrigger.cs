
using UnityEngine;
using Sirenix.OdinInspector;
using UnityEngine.Events;
using Sirenix.Serialization;

public class CinematicTrigger : MonoBehaviour, ISaveable
{
    [Title("Start Cutscene")]
    public Cutscene startCutscene;
    [Space(10)]
    public bool fadeIn = false;
    [ShowIf("fadeIn")]
    public CanvasGroup fader;
    [Title("Conditions")]
    public bool playOnLevelLoaded;
    [Space(5)]
    [SerializeField] bool isAreaTrigger;
    [SerializeField] bool canTriggerInCombat;
    [Space(5)]
    [Tooltip("This only applies to cinematics trigger by interacting with a shop or object. Leave false to trigger when player cancels interaction like exiting shop")]
    public bool playOnInteraction;
    [Title("Spawn Point")]
    [Tooltip("For area triggers that can't trigger during combat, if player enters trigger during combat, they will be respawned at this point on Combat End")]
    [SerializeField] Transform combatEndSpawnPoint;
    [Title("Events")]
    [Tooltip("Events to trigger when this is set as the next cinematic to play. E.G setting itself as the merchant's cinematic trigger.")]
    public UnityEvent onReadyEvents;

    //Saving Data
    [SerializeField, HideInInspector]
    private CinematicTriggerState cinematicState = new CinematicTriggerState();
    public bool AutoRestoreOnNewTerritoryEntry { get; set; } = true;

    bool cinematicTriggered = false;
    bool isReady;

    private void OnEnable()
    {
        if (playOnLevelLoaded)
            PlayCinematic();
    }

    private void Start()
    {
        //Round Warp Pos to grid pos for Loot Spawn.
        if(combatEndSpawnPoint)
            combatEndSpawnPoint.position = LevelGrid.Instance.gridSystem.RoundWorldPositionToGridPosition(combatEndSpawnPoint.position);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (isAreaTrigger && other.CompareTag("Player"))
        {
            PlayCinematic();
        }
    }

    public bool PlayCinematic()
    {
        if (cinematicTriggered)
        {
            return false;
        }
        else if (isAreaTrigger && FantasyCombatManager.Instance.InCombat() && !canTriggerInCombat)
        {
            //Set Warp Pos so player can run into cinematic.
            FantasyCombatManager.Instance.SetPostCombatWarpPoint(combatEndSpawnPoint);
            return false;
        }

        cinematicTriggered = CinematicManager.Instance.PlayCinematic(this);

        if (cinematicTriggered)
            isReady = false;

        return cinematicTriggered;
    }

    public void ReadyCinematic()
    {
        if (cinematicTriggered) 
        {
            gameObject.SetActive(false);
            return; //no Point Readying if played.
        } 

        Debug.Log(transform.name + " Ready: " + isReady + " Triggered: " + cinematicTriggered);

        isReady = true;
        gameObject.SetActive(true);

        Cutscene[] cutscenes = GetComponentsInChildren<Cutscene>(true);

        foreach (Cutscene cutscene in cutscenes)
        {
            cutscene.ResetEvents(); //In Case a cinematic played and player reloads an old save that occurs in the same scene. 
        }

        onReadyEvents.Invoke();
    }

    public void TriggerCinematicViaEvent() //Called By Unity Events
    {
        PlayCinematic();
    }

    //Saving
    [System.Serializable]
    public class CinematicTriggerState
    {
        //Story
        public bool isReady;
        public bool cinematicTriggered;
    }
    public object CaptureState()
    {
        cinematicState.isReady = isReady;
        cinematicState.cinematicTriggered = cinematicTriggered;

        return SerializationUtility.SerializeValue(cinematicState, DataFormat.Binary);
    }

    public void RestoreState(object state)
    {
        if (state == null) { return; }

        byte[] bytes = state as byte[];
        cinematicState = SerializationUtility.DeserializeValue<CinematicTriggerState>(bytes, DataFormat.Binary);

        isReady = cinematicState.isReady;
        cinematicTriggered = cinematicState.cinematicTriggered;


        if (isReady && !cinematicTriggered)
        {
            ReadyCinematic();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }
}
