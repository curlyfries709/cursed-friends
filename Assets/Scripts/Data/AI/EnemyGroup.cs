using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class EnemyGroup : MonoBehaviour
{
    [Header("Formation")]
    public int formationWidth = 5;
    public int formationLength = 3;
    [Header("Conditions")]
    public bool allowSceneDataToAlterHostility = true;
    [SerializeField] bool setGroupAsFriendly = false;
    public bool disableMembersOnStart = false;
    [Tooltip("This Applies to story Battles & Monster chests")]
    public bool isFixedBattleGroup = false;

    public List<EnemyStateMachine> linkedEnemies { get; private set; } = new List<EnemyStateMachine>();
    Dictionary<EnemyStateMachine, FixedBattleStartPos> fixedBattlePosData = new Dictionary<EnemyStateMachine, FixedBattleStartPos>();

    public class FixedBattleStartPos
    {
        public Vector3 startingPos;
        public Quaternion startingRot;

        public FixedBattleStartPos(Vector3 startPos, Quaternion startRot)
        {
            startingPos = startPos;
            startingRot = startRot;
        }
    }

    private void Awake()
    {
        linkedEnemies = GetComponentsInChildren<EnemyStateMachine>(true).ToList();

        foreach(EnemyStateMachine enemy in linkedEnemies)
        {
            enemy.enemyGroup = this;

            if (setGroupAsFriendly)
                enemy.isHostile = false;

            if (isFixedBattleGroup)
                fixedBattlePosData[enemy] = new FixedBattleStartPos(enemy.transform.position, enemy.transform.rotation);
        }
    }

    private void Start()
    {
        foreach (EnemyStateMachine enemy in linkedEnemies)
        {
            if (disableMembersOnStart)
                enemy.GetComponent<CharacterGridUnit>().ActivateUnit(false);
        }
    }

    public void WarpFixedBattleParticipant(EnemyStateMachine enemy)
    {
        if (!fixedBattlePosData.ContainsKey(enemy)) { return; }

        FixedBattleStartPos data = fixedBattlePosData[enemy];

        enemy.transform.position = data.startingPos;
        enemy.transform.rotation = data.startingRot;
    }

}
