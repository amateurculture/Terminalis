using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Demo.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AgentFix : MonoBehaviour
{
    UltimateCharacterLocomotion characterLocomotion;
    AgentMovement moveAbility;
    MeleeAgent meleeAgent;
    LocalLookSource lookSource;

    public bool canMove = true;
    public bool canAttack = true;

    void Awake()
    {
        var lookSource = GetComponent<LocalLookSource>();
        var characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        meleeAgent = GetComponent<MeleeAgent>();
        characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        moveAbility = characterLocomotion.GetAbility<AgentMovement>();
        lookSource.Target = GameObject.FindGameObjectWithTag("Player").transform;

        if (characterLocomotion != null)
        {
            // Get the EquipNext ability and start it. The next item within the ItemSetManager will be equipped.    
            var equipNext = characterLocomotion.GetAbility<EquipNext>();
            if (equipNext != null)
            {
                characterLocomotion.TryStartAbility(equipNext);
            }

            // Equip a specific index within the ItemSetManager with the EquipUnequip ability.
            var equipUnequip = characterLocomotion.GetAbility<EquipUnequip>();
            if (equipUnequip != null)
            {
                // Equip the ItemSet at index 2 within the ItemSetManager.
                //equipUnequip.StartEquipUnequip(2);
            }
        }

        if (meleeAgent != null)
        {
            if (canAttack)
            {
                meleeAgent.enabled = true;
            }
            else
            {
                meleeAgent.enabled = false; 
            }
        }

        if (moveAbility != null && characterLocomotion != null)
        {
            if (canMove)
            {
                characterLocomotion.TryStartAbility(moveAbility);
                moveAbility.Enabled = true;
            }
            else
            {
                characterLocomotion.TryStopAbility(moveAbility);
                moveAbility.Enabled = false;
            }
            
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (canAttack && meleeAgent != null && meleeAgent.enabled == false)
            meleeAgent.enabled = true;

        if (canMove && moveAbility != null && moveAbility.Enabled == false)
            moveAbility.Enabled = true;
    }
}
