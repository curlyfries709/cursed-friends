
using UnityEngine;
using Sirenix.OdinInspector;

public abstract class EnchantmentEffect : MonoBehaviour
{
    [Title("Combat Events")]
    [SerializeField] bool listenToCombatManagerActionComplete;
    [Title("Unit Events")]
    [SerializeField] bool listenToNewTurn;
    [Space(10)]
    [SerializeField] bool listenToUnitHit;
    [SerializeField] bool listenToMyUnitTurnStart;
    [SerializeField] bool listenToMyUnitTurnEnd;
    [Title("Unit Damage Events")]
    [SerializeField] bool alterUnitDamageReduction;

    protected CharacterGridUnit owner;

    protected int percentageValue;
    protected int numberValue;

    bool setupCalled = false;

    private void Awake()
    {
        enabled = false;
    }

    public void Setup(CharacterGridUnit unit, int percentageValue, int numberValue)
    {
        owner = unit;

        this.percentageValue = percentageValue;
        this.numberValue = numberValue;

        enabled = true;
        setupCalled = true;
    }

    protected virtual void OnEnable()
    {
        OnCombatBegin();

        if (listenToUnitHit)
            IDamageable.unitHit += OnUnitHit;

        if (alterUnitDamageReduction)
            owner.AlterDamageReductionAttack += OnAlterDamageReductionAttack;

        if (listenToNewTurn)
            FantasyCombatManager.Instance.OnNewTurn += OnNewTurn;

        if (listenToMyUnitTurnStart)
            owner.BeginTurn += OnUnitTurnStart;

        if (listenToMyUnitTurnEnd)
            owner.EndTurn += OnUnitTurnEnd;

        if (listenToCombatManagerActionComplete)
            FantasyCombatManager.Instance.ActionComplete += OnActionComplete;
    }
    protected virtual void OnCombatBegin()
    {

    }

    protected virtual void OnUnitHit(DamageData damageData)
    {

    }

    protected virtual void OnNewTurn(CharacterGridUnit actingUnit, int turnNumber)
    {

    }

    protected virtual DamageReceivedModifier OnAlterDamageReductionAttack(bool isBackstab)
    {
        return new DamageReceivedModifier(1);
    }

    protected virtual void OnUnitTurnStart()
    {

    }

    protected virtual void OnUnitTurnEnd()
    {

    }
    protected virtual void OnCombatEnd()
    {
    }

    protected virtual void OnActionComplete()
    {
    }

   

    protected virtual void OnDisable()
    {
        if (!setupCalled) { return; }

        OnCombatEnd();

        if (listenToUnitHit)
            IDamageable.unitHit -= OnUnitHit;
        if (alterUnitDamageReduction)
            owner.AlterDamageReductionAttack -= OnAlterDamageReductionAttack;
        if (listenToNewTurn)
            FantasyCombatManager.Instance.OnNewTurn -= OnNewTurn;
        if (listenToMyUnitTurnStart)
            owner.BeginTurn -= OnUnitTurnStart;
        if (listenToMyUnitTurnEnd)
            owner.EndTurn -= OnUnitTurnEnd;
        if (listenToCombatManagerActionComplete)
            FantasyCombatManager.Instance.ActionComplete -= OnActionComplete;
    }


    protected int GetPercentageAsValue(int valuePercentageBasedOn)
    {
        return Mathf.RoundToInt(((float)percentageValue / 100) * valuePercentageBasedOn);
    }
}
