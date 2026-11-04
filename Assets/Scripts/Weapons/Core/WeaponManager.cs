using System;
using UnityEngine;

public class WeaponManager : MonoBehaviour
{
    [Header("Weapon Slots")]
    [SerializeField] private WeaponData[] weaponSlots;

    [Header("References")]
    [SerializeField] private Transform firingPoint;
    [SerializeField] private ProjectilePool projectilePool;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private Behaviour movementController;
    [SerializeField] private LaserBeam laserBeam;

    private Weapon[] weapons;
    private int selectedSlotIndex = -1;
    private bool hasBeenEnabled;

    public Weapon CurrentWeapon =>
        selectedSlotIndex >= 0 ? weapons[selectedSlotIndex] : null;

    public int SelectedSlotIndex => selectedSlotIndex;
    public int SlotCount => weapons.Length;

    public Weapon GetWeapon(int slotIndex)
    {
        if (weapons == null || slotIndex < 0 || slotIndex >= weapons.Length)
            return null;

        return weapons[slotIndex];
    }

    public event Action<Weapon> OnSelectedWeaponChanged;

    private void Awake()
    {
        InitializeWeapons();
    }

    private void Update()
    {
        foreach (Weapon weapon in weapons)
        {
            weapon?.Tick(Time.deltaTime);
        }
    }

    private void OnEnable()
    {
        if (hasBeenEnabled)
            CurrentWeapon?.Equip();

        hasBeenEnabled = true;
    }

    private void OnDisable()
    {
        CurrentWeapon?.Unequip();
    }

    private void InitializeWeapons()
    {
        weapons = new Weapon[weaponSlots.Length];

        WeaponContext context = new WeaponContext(
            firingPoint,
            this,
            projectilePool,
            audioSource,
            laserBeam,
            SetMovementLocked
        );

        for (int i = 0; i < weaponSlots.Length; i++)
        {
            WeaponData data = weaponSlots[i];

            if (data == null)
                continue;

            if (data.WeaponBehaviour == null)
            {
                Debug.LogWarning(
                    $"A arma no slot {i + 1} não tem um comportamento associado.",
                    this
                );

                continue;
            }

            weapons[i] = new Weapon(data, context);
        }

        for (int i = 0; i < weapons.Length; i++)
        {
            if (weapons[i] != null)
            {
                SelectSlot(i);
                break;
            }
        }
    }

    public void SelectNextWeapon()
    {
        SelectRelativeWeapon(1);
    }

    public void SelectPreviousWeapon()
    {
        SelectRelativeWeapon(-1);
    }

    public void SelectSlot(int slotIndex)
    {
        if (slotIndex < 0 || slotIndex >= weapons.Length)
            return;

        Weapon newWeapon = weapons[slotIndex];

        if (newWeapon == null || slotIndex == selectedSlotIndex)
            return;

        CurrentWeapon?.Unequip();

        selectedSlotIndex = slotIndex;

        CurrentWeapon.Equip();

        OnSelectedWeaponChanged?.Invoke(CurrentWeapon);
    }

    private void SelectRelativeWeapon(int direction)
    {
        if (weapons.Length <= 1)
            return;

        for (int i = 1; i <= weapons.Length; i++)
        {
            int nextIndex =
                (selectedSlotIndex + direction * i + weapons.Length)
                % weapons.Length;

            if (weapons[nextIndex] != null)
            {
                SelectSlot(nextIndex);
                return;
            }
        }
    }

    private void SetMovementLocked(bool locked)
    {
        if (movementController != null)
            movementController.enabled = !locked;
    }
}