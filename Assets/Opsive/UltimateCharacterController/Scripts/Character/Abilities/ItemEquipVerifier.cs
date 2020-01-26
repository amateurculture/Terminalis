/// ---------------------------------------------
/// Ultimate Character Controller
/// Copyright (c) Opsive. All Rights Reserved.
/// https://www.opsive.com
/// ---------------------------------------------

using UnityEngine;
using Opsive.UltimateCharacterController.Character.Abilities.Items;
using Opsive.UltimateCharacterController.Events;
using Opsive.UltimateCharacterController.Inventory;
using Opsive.UltimateCharacterController.Items;
using Opsive.UltimateCharacterController.Utility;
using System.Collections.Generic;

namespace Opsive.UltimateCharacterController.Character.Abilities
{
    /// <summary>
    /// Verifies that the items are equipped or unequipped according to the AllowEquippedSlotsMask.
    /// </summary>
    [DefaultStartType(AbilityStartType.Manual)]
    public class ItemEquipVerifier : Ability
    {
        public override bool IsConcurrent { get { return true; } }

        private EquipUnequip[] m_EquipUnequipAbilities;
        private bool m_Equip;
        private Ability m_StartingAbility;
        private bool m_UnequippedItems;
        private ItemSetManager m_ItemSetManager;
        private bool m_CanToggleItem = true;
        private bool m_Active;
        private bool m_CanStopAbility;
        private HashSet<ItemAbility> m_ActiveEquipUnequipAbilities = new HashSet<ItemAbility>();

        private int m_StartEquippedSlotMask;
        private bool m_StartAllowPositionalInput;
        private bool m_StartAllowRotationalInput;
        private int[] m_StartingItemSetIndex;
        private int[] m_TargetItemSetIndex;

        /// <summary>
        /// Initialize the default values.
        /// </summary>
        public override void Awake()
        {
            base.Awake();

            m_EquipUnequipAbilities = m_CharacterLocomotion.GetAbilities<EquipUnequip>();
            if (m_EquipUnequipAbilities == null || m_EquipUnequipAbilities.Length == 0) {
                Debug.LogError("Error: At least one EquipUnequip ability must be added to the character in order for the ItemEquipVerifier ability to work.");
                Enabled = false;
                return;
            }
            m_ItemSetManager = m_GameObject.GetCachedComponent<ItemSetManager>();
            m_StartingItemSetIndex = new int[m_EquipUnequipAbilities.Length];
            m_TargetItemSetIndex = new int[m_EquipUnequipAbilities.Length];

            EventHandler.RegisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
        }

        /// <summary>
        /// Tries to toggle an equip or unequip based on the AllowedEquippedSlotsMask. True will be returned if the ability starts.
        /// </summary>
        /// <param name="ability">The ability that is trying to be started/stopped.</param>
        /// <param name="activate">True if the ability is being activated, false if it is being deactivated.</param>
        /// <returns>True if the ability started.</returns>
        public bool TryToggleItem(Ability ability, bool activate)
        {
            if (!Enabled || !m_CanToggleItem || ability is PickupItem) {
                return false;
            }

            // The starting ability may stop before the unequip has completed. Stop unequipping and restart the equip.
            if (ability == m_StartingAbility && IsActive && !activate) {
                StopAbility();

                for (int i = 0; i < m_EquipUnequipAbilities.Length; ++i) {
                    m_EquipUnequipAbilities[i].StartEquipUnequip(m_StartingItemSetIndex[i]);
                }
                m_StartingAbility = null;
                return true;
            } else if ((m_StartingAbility != null && m_StartingAbility != ability) || (m_Active && activate)) {
                // No need to run again if the ability is already working on toggling the item equip.
                return ability == m_StartingAbility;
            }

            var start = false;
            var equip = false;
            var allowEquippedSlotMask = 0;
            // If the ability is activated then the current set of items may need to be unequipped.
            if (activate && (ability.AllowEquippedSlotsMask != -1 || (ability.AllowItemTypes != null && ability.AllowItemTypes.Length > 0))) {
                // A mask can specify if the item should be equipped.
                if (ability.AllowEquippedSlotsMask != -1) {
                    // The ability may not need to activate if the not allowed equipped items are already not equipped.
                    var currentEquippedSlots = 0;
                    Item item;
                    for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                        if ((item = m_Inventory.GetItem(i)) != null) {
                            // If the current ItemSet is the defualt ItemSet then the current item can be considered unequipped. This for example will allow the body item
                            // (for puncing/kicking) to be active even if the ability says no item should be active.
                            if (IsDefaultItemType(item.ItemType)) {
                                continue;
                            }
                            currentEquippedSlots |= 1 << i;
                        }
                    }

                    if (currentEquippedSlots != 0 && !MathUtility.InLayerMask(currentEquippedSlots, 1 << ability.AllowEquippedSlotsMask)) {
                        start = true;
                        m_UnequippedItems = true;
                        allowEquippedSlotMask = ability.AllowEquippedSlotsMask;
                    } else {
                        m_UnequippedItems = false;
                    }
                }

                // An array can specify if the item can be equipped.
                if (!m_UnequippedItems && ability.AllowItemTypes != null && ability.AllowItemTypes.Length > 0) {
                    Item item;
                    for (int i = 0; i < m_Inventory.SlotCount; ++i) {
                        if ((item = m_Inventory.GetItem(i)) != null) {
                            // The equipped item is a defualt item - it can be considered unequipped.
                            if (IsDefaultItemType(item.ItemType)) {
                                continue;
                            }

                            // The item should be unequipped if it doesn't match any of the allow item types.
                            var unequip = true;
                            for (int j = 0; j < ability.AllowItemTypes.Length; ++j) {
                                if (ability.AllowItemTypes[j] == null) {
                                    continue;
                                }
                                if (item.ItemType == ability.AllowItemTypes[j]) {
                                    allowEquippedSlotMask |= 1 << i;
                                    unequip = false;
                                    break;
                                }
                            }
                            if (!m_UnequippedItems && unequip) {
                                start = true;
                                m_UnequippedItems = true;
                            }
                        }
                    }
                }
            // If slots were unequipped when the ability started then they should be equipped when the ability stops.
            } else if (!activate && ability.ReequipSlots && m_UnequippedItems) {
                start = true;
                equip = true;
            }

            if (start) {
                m_Equip = equip;
                if (ability is MoveTowards) {
                    ability = (ability as MoveTowards).OnArriveAbility;
                }

                // When the ability is unequipping the items it should inherit the allow field values from the starting ability.
                // When the ability is complete (equpping the items) it should use the starting field values.
                if (!m_Equip) {
                    m_StartEquippedSlotMask = m_AllowEquippedSlotsMask;
                    m_StartAllowPositionalInput = m_AllowPositionalInput;
                    m_StartAllowRotationalInput = m_AllowRotationalInput;

                    m_AllowEquippedSlotsMask = allowEquippedSlotMask;
                    m_AllowPositionalInput = ability.AllowPositionalInput;
                    m_AllowRotationalInput = ability.AllowRotationalInput;
                } else {
                    m_AllowEquippedSlotsMask = m_StartEquippedSlotMask;
                    m_AllowPositionalInput = m_StartAllowPositionalInput;
                    m_AllowRotationalInput = m_StartAllowRotationalInput;
                }

                // Active should only be true if the ability is equipping and the original ability is reequipping the slots. If the ability is not
                // reequipping slots then the Item Equip Verifier will not be run again after it is complete.
                m_Active = !m_Equip && ability.ReequipSlots;
                m_StartingAbility = ability;
                StartAbility();
            }
            return start;
        }

        /// <summary>
        /// Is the specified ItemType the default ItemType within the ItemSetManager?
        /// </summary>
        /// <param name="itemType">The ItemType to determine if it is the default ItemType.</param>
        /// <returns>True if the specified ItemType is the default ItemType.</returns>
        private bool IsDefaultItemType(ItemType itemType)
        {
            if (m_ItemSetManager != null) {
                for (int j = 0; j < itemType.CategoryIndices.Length; ++j) {
                    var categoryIndex = itemType.CategoryIndices[j];
                    var activeItemSetIndex = m_ItemSetManager.ActiveItemSetIndex[categoryIndex];
                    if (m_ItemSetManager.CategoryItemSets[categoryIndex].DefaultItemSetIndex != activeItemSetIndex) {
                        return false;
                    }
                }
            } else {
                return false;
            }
            return true;
        }

        /// <summary>
        /// The ability has started.
        /// </summary>
        protected override void AbilityStarted()
        {
            base.AbilityStarted();

            m_ActiveEquipUnequipAbilities.Clear();
            EventHandler.RegisterEvent<ItemAbility, bool>(m_GameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);
            // The original ability may be null on the network.
            if (m_StartingAbility != null) {
                m_CanStopAbility = !m_Equip && m_StartingAbility.ImmediateUnequip;
                for (int i = 0; i < m_EquipUnequipAbilities.Length; ++i) {
                    if (m_Equip) {
                        // Don't equip if the character has since changed which item set is equipped.
                        if (m_ItemSetManager.ActiveItemSetIndex[m_EquipUnequipAbilities[i].ItemSetCategoryIndex] != m_TargetItemSetIndex[i]) {
                            continue;
                        }
                        m_EquipUnequipAbilities[i].StartEquipUnequip(m_StartingItemSetIndex[i]);
                    } else {
                        var targetItemSetIndex = m_ItemSetManager.GetTargetItemSetIndex(m_EquipUnequipAbilities[i].ItemSetCategoryIndex, m_AllowEquippedSlotsMask);
                        var activeItemSetIndex = m_ItemSetManager.ActiveItemSetIndex[m_EquipUnequipAbilities[i].ItemSetCategoryIndex];
                        m_StartingItemSetIndex[i] = activeItemSetIndex;
                        if (targetItemSetIndex != activeItemSetIndex) {
                            m_TargetItemSetIndex[i] = targetItemSetIndex;
                            m_EquipUnequipAbilities[i].StartEquipUnequip(targetItemSetIndex, false, m_StartingAbility.ImmediateUnequip);
                        } else {
                            m_TargetItemSetIndex[i] = -1;
                        }
                    }
                }
            }
            m_CanStopAbility = true;

            // If the count is still zero then all of the items were unequipped in a single frame.
            if (m_ActiveEquipUnequipAbilities.Count == 0) {
                ItemToggled();
            }
        }

        /// <summary>
        /// An ItemAbility has been activated or deactivated.
        /// </summary>
        /// <param name="itemAbility">The ItemAbility activated or deactivated.</param>
        /// <param name="active">Was the ItemAbility activated?</param>
        private void OnItemAbilityActive(ItemAbility itemAbility, bool active)
        {
            if (itemAbility is EquipUnequip) {
                // Keep a count of the number of EquipUnequip abilities started. This will allow the ability to call ItemToggled when no more 
                // EquipUnequip abilities are active.
                if (active) {
                    m_ActiveEquipUnequipAbilities.Add(itemAbility);
                } else {
                    m_ActiveEquipUnequipAbilities.Remove(itemAbility);
                }
                if (m_ActiveEquipUnequipAbilities.Count == 0 && m_CanStopAbility) {
                    ItemToggled();
                }
            }
        }

        /// <summary>
        /// The EquipUnequip ability has toggled an item slot.
        /// </summary>
        private void ItemToggled()
        {
            if (m_StartingAbility == null) {
                return;
            }

            // Stop the ability before starting the OriginalAbility ability so ItemEquipVerifier doesn't prevent the ability from starting.
            StopAbility();

            // The ability should only be started if the items were unequipped and the MoveTowards ability isn't active. If the MoveTowards ability is 
            // active then the MoveTowards ability will start the ability.
            if (!m_Equip && (m_CharacterLocomotion.MoveTowardsAbility == null || m_CharacterLocomotion.MoveTowardsAbility.OnArriveAbility == null)) {
                m_CanToggleItem = false;
                if (!m_StartingAbility.IsActive) {
                    m_CharacterLocomotion.TryStartAbility(m_StartingAbility, true, true);
                } else if (m_StartingAbility is IItemToggledReceiver) {
                    // If the ability is already active then the ability is the one that toggled the item and it should receive the callback.
                    (m_StartingAbility as IItemToggledReceiver).ItemToggled();
                }
                m_CanToggleItem = true;
            }

            if (m_Equip) {
                m_StartingAbility = null;
            }
        }

        /// <summary>
        /// The ability has stopped running.
        /// </summary>
        /// <param name="force">Was the ability force stopped?</param>
        protected override void AbilityStopped(bool force)
        {
            base.AbilityStopped(force);

            EventHandler.UnregisterEvent<Vector3, Vector3, GameObject>(m_GameObject, "OnDeath", OnDeath);
            EventHandler.UnregisterEvent<ItemAbility, bool>(m_GameObject, "OnCharacterItemAbilityActive", OnItemAbilityActive);
        }

        /// <summary>
        /// The character has died.
        /// </summary>
        /// <param name="position">The position of the force.</param>
        /// <param name="force">The amount of force which killed the character.</param>
        /// <param name="attacker">The GameObject that killed the character.</param>
        private void OnDeath(Vector3 position, Vector3 force, GameObject attacker)
        {
            Reset();
        }

        /// <summary>
        /// Resets the ability back to the starting state.
        /// </summary>
        public void Reset()
        {
            m_StartingAbility = null;
            m_Active = false;
        }
    }
}