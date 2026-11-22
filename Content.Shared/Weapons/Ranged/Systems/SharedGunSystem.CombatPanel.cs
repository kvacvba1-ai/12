// SPDX-License-Identifier: AGPL-3.0-or-later

using Content.Shared.Weapons.Ranged.Components;
using Content.Shared.Weapons.Ranged.Events;

namespace Content.Shared.Weapons.Ranged.Systems;

public abstract partial class SharedGunSystem
{
    /// <summary>
    /// Consumes one round from the held gun's real ammo provider without firing
    /// or spawning a projectile. Used by systems that resolve their own hit.
    /// </summary>
    public bool TryConsumeAmmoWithoutFiring(EntityUid user)
    {
        if (!TryGetGun(user, out var gun))
            return false;

        var takeAmmo = new TakeAmmoEvent(
            1,
            new List<(EntityUid? Entity, IShootable Shootable)>(),
            Transform(gun).Coordinates,
            user);

        RaiseLocalEvent(gun, takeAmmo);

        if (takeAmmo.Ammo.Count == 0)
        {
            var emptyShot = new OnEmptyGunShotEvent(user);
            RaiseLocalEvent(gun, ref emptyShot);
            UpdateAmmoCount(gun);
            return false;
        }

        // Provider extraction and chamber cycling have already happened.
        // Delete the extracted round instead of passing it to Shoot(), which
        // would create a projectile and duplicate the panel's role-play damage.
        foreach (var (ammoEntity, _) in takeAmmo.Ammo)
        {
            if (ammoEntity is not { } ammo || TerminatingOrDeleted(ammo))
                continue;

            // Cartridge weapons keep their normal spent-shell behavior.
            if (TryComp<CartridgeAmmoComponent>(ammo, out var cartridge))
            {
                // Bolt-action / manually cycled weapons can return the same
                // spent shell while it is still in the chamber. Never treat
                // that casing as another live round.
                if (cartridge.Spent)
                {
                    if (!Containers.IsEntityInContainer(ammo))
                        EjectCartridge(ammo);

                    var emptyShot = new OnEmptyGunShotEvent(user);
                    RaiseLocalEvent(gun, ref emptyShot);
                    UpdateAmmoCount(gun);
                    return false;
                }

                SetCartridgeSpent(ammo, cartridge, true);

                if (cartridge.DeleteOnSpawn)
                    Del(ammo);
                else if (!Containers.IsEntityInContainer(ammo))
                    EjectCartridge(ammo);

                Dirty(ammo, cartridge);
                continue;
            }

            // Entity and hitscan providers return disposable ammunition entities.
            Del(ammo);
        }

        UpdateAmmoCount(gun);
        return true;
    }
}
