using Opsive.UltimateCharacterController.Character;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Demo.AI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeleeAgentFix : MonoBehaviour
{
    [Tooltip("A reference to the Ultimate Character Controller character.")]
    [SerializeField] protected GameObject m_Character;

    UltimateCharacterLocomotion characterLocomotion;
    AgentMovement moveAbility;
    MeleeAgent meleeAgent;

    void Awake()
    {
        var characterLocomotion = m_Character.GetComponent<UltimateCharacterLocomotion>();
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
        meleeAgent = GetComponent<MeleeAgent>();
        meleeAgent.enabled = true;

        characterLocomotion = GetComponent<UltimateCharacterLocomotion>();
        moveAbility = characterLocomotion.GetAbility<AgentMovement>();

        characterLocomotion.TryStartAbility(moveAbility);
        moveAbility.Enabled = true;

        GetComponent<LocalLookSource>().Target = GameObject.FindGameObjectWithTag("Player").transform;
    }

    // Update is called once per frame
    void Update()
    {
        if (meleeAgent.enabled == false)
            meleeAgent.enabled = true;

        if (moveAbility.Enabled == false)
            moveAbility.Enabled = true;
    }
}
